

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
type User_attention = {
    attentive_user: User_handle
    target: User_handle
    amount: int
}

module Stitching_user_attention =
    
    
    let attention_rows_from_posts
        read_interactions
        attention_type
        (local_attentive_users: User_handle seq)
        =
        local_attentive_users
        |>Seq.collect(fun attentive_user ->
            read_interactions attentive_user
            |>Seq.map(fun (target, amount) ->
                {|
                    attention_type=attention_type
                    attentive_user=attentive_user
                    target=target
                    amount=amount
                |}
            )
        )
    
    let upload_all_users_attention
        read_interactions
        (target_db:NpgsqlConnection)
        attention_type
        local_attentive_users
        =
            
        let attention_rows =
            attention_rows_from_posts
                read_interactions
                attention_type
                local_attentive_users
        
        target_db.BulkInsert(
            $"""insert into {user_attention} (
                {user_attention.attention_type},
                {user_attention.attentive_user},
                {user_attention.target},
                {user_attention.amount}
            )
            values (
                @attention_type,
                @attentive_user,
                @target,
                @amount
            )
            ...
            
            """,
            attention_rows
        )|> ignore

    
    let update_attention_row_in_database //slow
        (database:NpgsqlConnection)
        attention
        =
        database.Query<unit>(
            $"""insert into {user_attention} (
                {user_attention.attention_type},
                {user_attention.attentive_user},
                {user_attention.target},
                {user_attention.amount}
            )
            values (
                @attention_type,
                @attentive_user,
                @target,
                @amount
            )
            on conflict (attention_type, attentive_user, target)
            do update set (amount) = row(@amount)
            """,
            attention
        )|> ignore
    
    let update_all_users_attention_in_database //slow
        read_interactions
        (database:NpgsqlConnection)
        (attention_type)
        (local_attentive_users: User_handle seq)
        =
        let attention_rows =
            attention_rows_from_posts
                read_interactions
                attention_type
                local_attentive_users
                
        attention_rows
        |>Seq.iter (
            update_attention_row_in_database
                database
        )
            
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
            upload_all_users_attention
            //update_all_users_attention_in_database
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
        database.Query<User_attention>(
            $"select
                {user_attention.attentive_user},
                {user_attention.target},
                {user_attention.amount}
            from
                {user_attention}
            where
                {user_attention.attention_type} = @attention_type",
            {|
                attention_type=attention_type
            |}
        )
        |>rows_of_user_attention_to_maps
        
    let read_attentions_within_matrix
        (database:NpgsqlConnection)
        attention_type
        datetime
        =
        database.Query<User_attention>(
            $"""
            select * 
            from {user_attention} as main_attention
            where 
                exists ( --the target of attention should be part of the last matrix
                    select ''
                    from {user_to_scrape}
                    where 
                        {user_to_scrape.created_at} = ( --the matrix will contain the last scraped batch of users
                            SELECT MAX({user_to_scrape.created_at}) FROM {user_to_scrape}
                        )
                        and {user_to_scrape.status} = 'Success'
                        and ( --the target of attention should be part of the matrix
                            {user_to_scrape.handle} = main_attention.{user_attention.target}
                        ) 
                )
                --take only attentions with the closest scraping datetime 
                and main_attention.{user_attention.when_scraped} = (
                    select max({user_attention.when_scraped}) 
                    from {user_attention} as closest_attention
                    where
                        main_attention.{user_attention.attentive_user} = closest_attention.{user_attention.attentive_user}
                        and main_attention.{user_attention.target} = closest_attention.{user_attention.target}
                        and main_attention.{user_attention.attention_type} = closest_attention.{user_attention.attention_type}
                        and closest_attention.{user_attention.when_scraped} < @datetime
                )
                and main_attention.{user_attention.attention_type} = @attention_type
            order by main_attention.{user_attention.attentive_user}, main_attention.{user_attention.target}
            """,
            {|
                attention_type=attention_type
                datetime=datetime
            |}
        )
        |>rows_of_user_attention_to_maps
        
        