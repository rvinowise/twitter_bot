

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



module User_attention_database =
    
    
    let read_cached_user_attention
        attention_type
        (database:NpgsqlConnection)
        before_datetime
        attentive_user
        =
        database.Query<Cached_user_attention_row>(
            $"select *
            from
                {user_attention}
            where
                {user_attention.attention_type} = @attention_type
                and {user_attention.before_datetime} = @before_datetime
                and {user_attention.attentive_user} = @attentive_user
            ",
            {|
                attention_type=attention_type
                before_datetime=before_datetime
                attentive_user=attentive_user
            |}
        )
        |>Seq.map (fun attention -> attention.target, attention.absolute_amount)
        |>List.ofSeq
    
    let read_cached_total_user_attention
        (database:NpgsqlConnection)
        attention_type
        before_datetime
        attentive_user
        =
        database.Query<int>(
            $"select {total_user_attention.amount}
            from
                {total_user_attention}
            where
                {total_user_attention.attention_type} = @attention_type
                and {total_user_attention.before_datetime} = @before_datetime
                and {total_user_attention.attentive_user} = @attentive_user
            ",
            {|
                attention_type=attention_type
                before_datetime=before_datetime
                attentive_user=attentive_user
            |}
        )|>Seq.tryHead
        
    
    let read_user_attention_from_posts
        attention_type
        =
        match attention_type with
        |Attention_type.Likes ->
            User_attention_from_posts.read_likes_by_user
        |Attention_type.Reposts ->
            User_attention_from_posts.read_reposts_by_user
        |Attention_type.Replies ->
            User_attention_from_posts.read_replies_by_user
    
    let read_total_user_attention_from_posts
        attention_type
        =
        match attention_type with
        |Attention_type.Likes ->
            User_attention_from_posts.read_total_likes_by_user
        |Attention_type.Reposts ->
            User_attention_from_posts.read_total_reposts_by_user
        |Attention_type.Replies ->
            User_attention_from_posts.read_total_replies_by_user
    
    let calculate_and_cache_user_attention
        attention_type
        (database:NpgsqlConnection)
        before_datetime
        attentive_user
        =
            
        let attention_rows =
            read_user_attention_from_posts
                attention_type
                (database: NpgsqlConnection)
                (before_datetime: DateTime)
                attentive_user
            |>List.ofSeq
            |>function
            |[] -> [(attentive_user,0)] // an empty attention row will signify that the caching happened, and there really wasn't any attention from that user
            |existing_attention -> existing_attention
            |>List.map(fun (target, amount) ->
                {
                    Cached_user_attention_row.attentive_user = attentive_user
                    target = target
                    attention_type = attention_type 
                    before_datetime = before_datetime
                    absolute_amount = amount
                }
            )
        database.BulkInsert(
            $"""
            insert into {user_attention} (
                {user_attention.attentive_user},
                {user_attention.target},
                {user_attention.attention_type},
                {user_attention.before_datetime},
                {user_attention.absolute_amount}
            )
            values (
                @attentive_user,
                @target,
                @attention_type,
                @before_datetime,
                @absolute_amount
            )
            ...
            on conflict
            do nothing
            """,
            attention_rows
        )|> ignore
        
        attention_rows
        |>List.map(fun attention -> attention.target, attention.absolute_amount)
    
    let calculate_and_cache_total_user_attention
        attention_type
        (database:NpgsqlConnection)
        before_datetime
        attentive_user
        =
            
        let total_attention_amount =
            read_total_user_attention_from_posts
                attention_type
                (database: NpgsqlConnection)
                (before_datetime: DateTime)
                attentive_user
            
        database.Query(
            $"""
            insert into {total_user_attention} (
                {total_user_attention.attentive_user},
                {total_user_attention.attention_type},
                {total_user_attention.before_datetime},
                {total_user_attention.amount}
            )
            values (
                @attentive_user,
                @attention_type,
                @before_datetime,
                @amount
            )
            on conflict
            do nothing
            """,
            {|
                attentive_user=attentive_user
                attention_type=attention_type
                before_datetime=before_datetime
                amount=total_attention_amount  
            |}
        )|> ignore
        
        total_attention_amount
    
    let read_cached_or_calculated_attention_in_matrix
        attention_type
        (database: NpgsqlConnection)
        (before_datetime: DateTime)
        matrix_members
        =
        matrix_members
        |>Set.map(fun attentive_user ->
            attentive_user,
            read_cached_user_attention
                attention_type
                database
                before_datetime
                attentive_user
            |>function
            |[] ->
                Log.debug $"no cached attention for user {User_handle.value attentive_user} before {before_datetime} - calculating and caching it"
                calculate_and_cache_user_attention
                    attention_type
                    database
                    before_datetime
                    attentive_user
            |cached_attention ->
                cached_attention
            |>List.filter(fun (target,attention_amount) -> Set.contains target matrix_members)
            |>Map.ofList
        )|>Map.ofSeq
    
    let read_cached_or_calculated_total_attention
        attention_type
        (database: NpgsqlConnection)
        (before_datetime: DateTime)
        attentive_users
        =
        attentive_users
        |>List.map(fun attentive_user ->
            attentive_user,
            read_cached_total_user_attention
                database
                attention_type
                before_datetime
                attentive_user
            |>function
            |None ->
                calculate_and_cache_total_user_attention
                    attention_type
                    database
                    before_datetime
                    attentive_user
            |Some cached_total_attention ->
                cached_total_attention
        )|>Map.ofSeq
    
    let delete_all_cached_attention
        (database: NpgsqlConnection)
        =
        database.Query(
            $"delete from {total_user_attention}"
        )|> ignore
        
        database.Query(
            $"delete from {user_attention}"
        )|> ignore