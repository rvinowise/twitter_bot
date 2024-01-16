namespace rvinowise.twitter

open System
open Dapper
open Faithlife.Utility.Dapper
open Npgsql
open Xunit
open rvinowise.twitter

   
open rvinowise.twitter.database_schema


module Merging_different_databases =
    
      
    let merge_poll_choice
        source
        target
        =
        ()
    

    let select_from_arbitrary_table<'Row>
        (database: NpgsqlConnection)
        table_name
        =
        database.Query<'Row>(
            $"
            select *
            from {table_name}
            "
        )
        
    let prepare_sql_insert
        (table_schema: Type)
        table_name
        (row_structure: Type)
        =
        
        let insert_into_these_columns =
            table_schema.GetProperties()
            |>Array.map(fun column ->
                column.Name
            )
            |>String.concat ",\n"
        
        let insert_these_values =
            table_schema.GetProperties()
            |>Array.map(fun column ->
                "@" + column.Name
            )
            |>String.concat ",\n"

        
        $"""
        insert into {table_name} (
            {insert_into_these_columns}
        )
        values (
            {insert_these_values}
        )
        ...
        on conflict
        do nothing
        """
    
    let merge_table<'Schema, 'Row>
        (source: NpgsqlConnection)
        (target: NpgsqlConnection)
        =
        
        let table_name =
            Activator.CreateInstance(typeof<'Schema>)
        
        let sql_insert =
            prepare_sql_insert
                (typeof<'Schema>)
                table_name
                (typeof<'Row>)
        
        let rows_values =
            select_from_arbitrary_table<'Row>
                source
                table_name
            |>List.ofSeq
            
        Log.info $"merging table {table_name}, adding {List.length rows_values} rows"
        
        target.BulkInsert (
            sql_insert,
            rows_values
        )|>ignore

       
    let try_merge_tables() =
        Twitter_database_types.set_twitter_type_handlers()
        
        let source =
            Database.open_connection
                "Host=localhost:5432;Username=postgres;Password=' ';Database=twitter_black_box"
        
        let target =
            Database.open_connection
                "Host=localhost:5432;Username=postgres;Password=' ';Database=twitter"
                
        merge_table<Post_header_table, Post_header_row>
            source
            target
        
        merge_table<Post_like_table, Post_like_row>
            source
            target
            
        merge_table<Post_repost_table,Post_repost_row>
            source
            target
            
        merge_table<Post_stats_table,Post_stats_row>
            source
            target
            
        merge_table<Post_reply_table,Post_reply_row>
            source
            target
            
        merge_table<Post_external_url_table, Post_external_url_row>
            source
            target
            
        merge_table<Post_twitter_event_table, Post_twitter_event_row>
            source
            target
            
        merge_table<Post_twitter_space_table, Post_twitter_space_row>
            source
            target
            
        merge_table<Post_twitter_event_in_post_table, Post_twitter_event_in_post_row>
            source
            target
            
        merge_table<Post_image_table, Post_image_row>
            source
            target
        
        merge_table<Post_video_table, Post_video_row>
            source
            target
        
        merge_table<Poll_choice_table, Poll_choice_row>
            source
            target
        
        merge_table<Poll_summary_table, Poll_summary_row>
            source
            target
        
        merge_table<Quotable_part_of_poll_table, Quotable_part_of_poll_row>
            source
            target
            
        merge_table<Post_quotable_message_body_table, Post_quotable_message_body_row>
            source
            target
        
        
    let ``try merge``() =
        let post =
            database_schema.Post_tables()
        
        post.GetType().GetProperties()
        |>Array.iter(fun table ->
            Log.info $"merging table {table.Name}" 
            table.PropertyType.GetProperties()
            |>Array.iter(fun column ->
                printfn "%s" column.Name
            ) 
        ) 
        
           