

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

module Stitching_user_attention =
    
    
    let upload_user_attention
        read_interactions
        (target_db:NpgsqlConnection)
        (matrix_title)
        (local_attentive_users: User_handle seq)
        =
            
        let interactions =
            local_attentive_users
            |>Seq.collect(fun attentive_user ->
                read_interactions attentive_user
                |>Seq.map(fun (target, amount) ->
                    {|
                        matrix_title=matrix_title
                        attentive_user=attentive_user
                        target=target
                        amount=amount
                    |}
                )
            )
        
        target_db.BulkInsert(
            $"""insert into {user_interaction} (
                {user_interaction.attention_type},
                {user_interaction.attentive_user},
                {user_interaction.target},
                {user_interaction.amount}
            )
            values (
                @matrix_title,
                @attentive_user,
                @target,
                @amount
            )
            ...
            
            """,
            interactions
        )|> ignore

    
    let upload_all_local_attentions () =
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
            upload_user_attention
                (read local_db)
                central_db
                matrix_name
                local_attentive_users
        )
            
    
    let rows_of_user_attention_to_maps
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
        
    let read_all_attentions
        (database:NpgsqlConnection)
        attention_type
        =
        database.Query<User_interaction>(
            $"select
                {user_interaction.attentive_user},
                {user_interaction.target},
                {user_interaction.amount}
            from
                {user_interaction}
            where
                {user_interaction.attention_type} = @attention_type",
            {|
                attention_type=attention_type
            |}
        )
        |>rows_of_user_attention_to_maps
        
        