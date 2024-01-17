

namespace rvinowise.twitter

open System
open Dapper
open Faithlife.Utility.Dapper
open Npgsql
open Xunit
open rvinowise.html_parsing
open rvinowise.twitter
open rvinowise.twitter.database_schema
open rvinowise.twitter.database_schema.tables



[<CLIMutable>]
type User_attention = {
    attentive_user: User_handle
    target: User_handle
    amount: int
    attention_type: string
}

module User_attention_database =
    
    
    let attention_rows_from_posts
        read_interactions
        attention_type
        (scraping_jobs: (User_handle*DateTime) seq)
        =
        scraping_jobs
        |>Seq.collect(fun (scraped_user,when_scraped) ->
            read_interactions scraped_user
            |>Seq.map(fun (target, amount) ->
                {|
                    attention_type=attention_type
                    attentive_user=scraped_user
                    target=target
                    amount=amount
                    when_scraped=when_scraped
                |}
            )
        )
    
    let delete_user_attention
        (database:NpgsqlConnection)
        accounts
        =
        accounts
        |>Seq.iter(fun account ->
            database.Query<unit>(
                $"""
                delete from {user_attention}
                where
                    {user_attention.attentive_user} = @account
                """,
                {|
                    account=account 
                |}
            )|>ignore
        )
    
    let upload_all_users_attention
        read_interactions
        (target_db:NpgsqlConnection)
        attention_type
        scraping_jobs
        =
            
        let attention_rows =
            attention_rows_from_posts
                read_interactions
                attention_type
                scraping_jobs
        
        target_db.BulkInsert(
            $"""insert into {user_attention} (
                {user_attention.attention_type},
                {user_attention.attentive_user},
                {user_attention.target},
                {user_attention.amount},
                {user_attention.when_scraped}
            )
            values (
                @attention_type,
                @attentive_user,
                @target,
                @amount,
                @when_scraped
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
            on conflict (
                {user_attention.attention_type},
                {user_attention.attentive_user}, 
                {user_attention.target}
            )
            do update set ({user_attention.amount}) = row(@amount)
            """,
            attention
        )|> ignore
    
    let update_all_users_attention_in_database //slow
        read_interactions
        (database:NpgsqlConnection)
        (attention_type)
        (scraping_jobs)
        =
        let attention_rows =
            attention_rows_from_posts
                read_interactions
                attention_type
                scraping_jobs
                
        attention_rows
        |>Seq.iter (
            update_attention_row_in_database
                database
        )
    

    let upload_attention_of_scraped_users
        local_db
        central_db
        accounts_when_scraped
        =
        [
            User_attention_from_posts.read_likes_by_user, "Likes"
            User_attention_from_posts.read_reposts_by_user, "Reposts"
            User_attention_from_posts.read_replies_by_user, "Replies"
        ]
        |>List.iter (fun (read_attention_type,matrix_name) -> 
            upload_all_users_attention
            //update_all_users_attention_in_database
                (read_attention_type local_db)
                central_db
                matrix_name
                accounts_when_scraped
        )
    
    let upload_last_scraped_attentions () =
        let central_db = Central_database.open_connection()
        let local_db = Local_database.open_connection()
        
        Distributing_jobs_database.read_last_completed_jobs
            central_db
        |>upload_attention_of_scraped_users
            local_db
            central_db
            
    let upload_all_local_attentions () =
        let central_db = Central_database.open_connection()
        let local_db = Local_database.open_connection()
        
        This_worker.this_worker_id local_db
        |>Distributing_jobs_database.read_jobs_completed_by_worker central_db
        |>upload_attention_of_scraped_users
            local_db
            central_db
        
            
        
    
    let rows_of_user_attention_to_maps
        rows
        =
        rows
        |>Seq.groupBy (fun attention -> attention.attentive_user)//_.attentive_user
        |>Seq.map (fun (attentive_user, attentions) ->
            attentive_user,
            attentions
            |>Seq.map (fun attention -> attention.target, attention.amount)
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
    
    
    let inner_sql_reading_closest_scraped_date datetime =
        $"""(
        select max({user_attention.when_scraped}) 
        from {user_attention} as closest_attention
        where
            main_attention.{user_attention.attentive_user} = closest_attention.{user_attention.attentive_user}
            and main_attention.{user_attention.target} = closest_attention.{user_attention.target}
            and main_attention.{user_attention.attention_type} = closest_attention.{user_attention.attention_type}
            and closest_attention.{user_attention.when_scraped} < TO_TIMESTAMP('{datetime}', 'YYYY-MM-DD HH24:MI:ss')
        )"""
    
    let ``try inner_sql_reading_closest_scraped_date``()=
        let test = inner_sql_reading_closest_scraped_date DateTime.Now
        ()
    
    let read_attention_within_matrix
        (database:NpgsqlConnection)
        matrix_title
        attention_type
        datetime
        =
        database.Query<User_attention>(
            $"""
            select * 
            from {user_attention} as main_attention
            where 
                --the target of attention should be part of the desired matrix
                exists ( 
                    select ''
                    from {account_of_matrix}
                    where 
                        --find the matrix by title
                        {account_of_matrix.title} = @matrix_title
                        
                        --the target of attention should be part of the matrix
                        and {account_of_matrix.account} = main_attention.{user_attention.target}
                )
                --take only attentions with the closest scraping datetime 
                and main_attention.{user_attention.when_scraped} = {inner_sql_reading_closest_scraped_date datetime}
                
                and main_attention.{user_attention.attention_type} = @attention_type
            order by main_attention.{user_attention.attentive_user}, main_attention.{user_attention.target}
            """,
            {|
                attention_type = attention_type
                matrix_title = matrix_title
            |}
        )
        |>rows_of_user_attention_to_maps
        
    
    let read_combined_attention_within_matrix
        (database:NpgsqlConnection)
        matrix_title
        datetime
        =
        database.Query<User_attention>(
            $"""
            select * 
            from {user_attention} as main_attention
            where 
                --the target of attention should be part of the desired matrix
                exists ( 
                    select ''
                    from {account_of_matrix}
                    where 
                        --find the matrix by title
                        {account_of_matrix.title} = @matrix_title
                        
                        --the target of attention should be part of the matrix
                        and {account_of_matrix.account} = main_attention.{user_attention.target}
                )
                --take only attentions with the closest scraping datetime 
                and main_attention.{user_attention.when_scraped} = {inner_sql_reading_closest_scraped_date datetime}
                
            order by main_attention.{user_attention.attentive_user}, main_attention.{user_attention.target}
            """,
            {|
                matrix_title = matrix_title
            |}
        )
        |>rows_of_user_attention_to_maps
    
    [<CLIMutable>]
    type User_total_attention = {
        attentive_user: User_handle
        total_amount: int
    }    
    let read_total_attention_from_users
        (database:NpgsqlConnection)
        attention_type
        datetime
        =
        
        database.Query<User_total_attention>(
            (* select total attention values for all users of the matrix,
            to calculate the percentage of their attention to the targets from the matrix
            *)
            $"""
            select 
                {user_attention.attentive_user}, 
                sum({user_attention.amount}) as total_amount
            from 
                {user_attention} as main_attention
            where 
                --take only attentions with the closest scraping datetime 
                main_attention.{user_attention.when_scraped} = {inner_sql_reading_closest_scraped_date datetime}
               
                and main_attention.{user_attention.attention_type} = @attention_type

            group by {user_attention.attentive_user}

            order by main_attention.{user_attention.attentive_user}
            """,
            {|
                attention_type=attention_type
                datetime=datetime
            |}
        )
        |>Seq.map (fun attention -> attention.attentive_user, attention.total_amount)
        |>Map.ofSeq
    
    let read_total_combined_attention_from_users
        (database:NpgsqlConnection)
        datetime
        =
        
        database.Query<User_total_attention>(
            (* select total attention values for all users of the matrix,
            to calculate the percentage of their attention to the targets from the matrix
            *)
            $"""
            select 
                {user_attention.attentive_user}, 
                sum({user_attention.amount}) as total_amount
            from 
                {user_attention} as main_attention
            where 
                --take only attentions with the closest scraping datetime 
                main_attention.{user_attention.when_scraped} = {inner_sql_reading_closest_scraped_date datetime}

            group by {user_attention.attentive_user}

            order by main_attention.{user_attention.attentive_user}
            """,
            {|
                datetime=datetime
            |}
        )
        |>Seq.map (fun attention -> attention.attentive_user, attention.total_amount)
        |>Map.ofSeq
   
    let ``try read_attentions_within_matrix``()=
        let result =
            read_attention_within_matrix
                (Central_database.open_connection())
                "Longevity members"
                "Likes"
                (Html_parsing.parse_datetime "yyyy-MM-dd HH:mm:ss" "2024-01-06 00:35:00")
        ()
    
    let ``try read_total_attention_from_users``()=
        let result =
            read_total_attention_from_users
                (Central_database.open_connection())
                "Likes"
                (Html_parsing.parse_datetime "yyyy-MM-dd HH:mm:ss" "2024-01-06 00:35:00")
        ()
            