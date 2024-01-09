

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
        (central_db:NpgsqlConnection)
        (local_db:NpgsqlConnection)
        (matrix)
        (attentive_users: User_handle seq)
        (targets: User_handle Set)
        =
            
        let interactions =
            attentive_users
            |>Seq.collect(fun attentive_user ->
                User_interactions_from_posts.read_likes_by_user
                    local_db
                    attentive_user
                |>Seq.filter (fun (target,_) -> Set.contains target targets)
                |>Seq.map(fun (target, amount) ->
                    {|
                        matrix=matrix
                        attentive_user=attentive_user
                        target=target
                        amount=amount
                    |}
                )
            )
        
        central_db.BulkInsert(
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
    [<Fact>]
    let ``try upload_interactions``()=
        upload_interactions
            (Central_task_database.open_connection())
            (Twitter_database.open_connection())
            "User_interactions"
    
    
    let read_interactions
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
        |>Seq.groupBy (fun interaction -> interaction.attentive_user)//_.attentive_user
        |>Seq.map (fun (attentive_user, interaction) ->
            attentive_user,
            interaction
            |>Seq.map (fun interaction -> interaction.target, interaction.amount)
            |>Map.ofSeq
        )
        |>Map.ofSeq