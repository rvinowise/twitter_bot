

namespace rvinowise.twitter

open System
open Dapper
open Faithlife.Utility.Dapper
open Npgsql
open Xunit
open rvinowise.twitter
open rvinowise.twitter.database
open rvinowise.twitter.database.tables



[<CLIMutable>]
type User_interaction = {
    attentive_user: User_handle
    target: User_handle
    amount: int
}

module Combining_user_interactions =
    
    
    let upload_interactions
        read_interactions
        (target_db:NpgsqlConnection)
        (matrix_name)
        (local_attentive_users: User_handle seq)
        (all_targets: User_handle Set)
        =
            
        let interactions =
            local_attentive_users
            |>Seq.collect(fun attentive_user ->
                read_interactions attentive_user
                |>Seq.filter (fun (target,_) -> Set.contains target all_targets)
                |>Seq.map(fun (target, amount) ->
                    {|
                        matrix=matrix_name
                        attentive_user=attentive_user
                        target=target
                        amount=amount
                    |}
                )
            )
        
        target_db.BulkInsert(
            $"""insert into {user_interaction} (
                {user_interaction.matrix},
                {user_interaction.attentive_user},
                {user_interaction.target},
                {user_interaction.amount}
            )
            values (
                @matrix,
                @attentive_user,
                @target,
                @amount
            )
            ...
            
            """,
            interactions
        )|> ignore
    (*on conflict (
                {tables.user_interaction.matrix},
                {tables.user_interaction.attentive_user},
                {tables.user_interaction.target}
            )
            do update set ({tables.user_interaction.amount}) = row(@amount)*)
    
    let upload_all_local_interactions () =
        let central_db = Central_task_database.open_connection()
        let local_db = Twitter_database.open_connection()
        
        let local_attentive_users = 
            This_worker.this_worker_id local_db
            |>Central_task_database.read_jobs_completed_by_worker central_db
        
        let all_targets =
            Central_task_database.read_last_user_jobs_with_status
                central_db
                (Scraping_user_status.Completed (Success 0))
       
        [
            User_interactions_from_posts.read_likes_by_user, "Likes"
            User_interactions_from_posts.read_reposts_by_user, "Reposts"
            User_interactions_from_posts.read_replies_by_user, "Replies"
        ]
        |>List.iter (fun (read,matrix_name) -> 
            upload_interactions
                (read local_db)
                central_db
                matrix_name
                local_attentive_users
                (Set.ofSeq all_targets)
        )
        
    [<Fact>]
    let ``manually upload_all_local_interactions``() =
        upload_all_local_interactions ()
    
    
    
    let rows_of_user_interactions_to_maps
        rows
        =
        rows
        |>Seq.groupBy (fun interaction -> interaction.attentive_user)//_.attentive_user
        |>Seq.map (fun (attentive_user, interaction) ->
            attentive_user,
            interaction
            |>Seq.map (fun interaction -> interaction.target, interaction.amount)
            |>Map.ofSeq
        )
        |>Map.ofSeq
        
    let read_all_interactions
        (database:NpgsqlConnection)
        matrix
        =
        database.Query<User_interaction>(
            $"select (
                {user_interaction.attentive_user},
                {user_interaction.target},
                {user_interaction.amount}
            )
            from
                {user_interaction}
            where
                {user_interaction.matrix} = @matrix",
            {|
                matrix=matrix
            |}
        )
        |>rows_of_user_interactions_to_maps
        
        
        
    [<Fact>]    
    let ``try export interactions of one user ``()=
        let central_db = Central_task_database.open_connection()
        let local_db= Twitter_database.open_connection()
        let this_worker = This_worker.this_worker_id local_db
        
        let local_attentive_users =
            Central_task_database.read_jobs_completed_by_worker
                central_db
                this_worker 
        
        let result =
            upload_interactions
                (User_interactions_from_posts.read_likes_by_user local_db)
                central_db
                "User_interactions"
                local_attentive_users
                (Set.ofSeq local_attentive_users)
        ()