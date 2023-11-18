namespace rvinowise.twitter

open System
open Dapper
open Npgsql
open rvinowise.twitter
open rvinowise.twitter.database



module Twitter_post_database =

    
    
    let write_stats
        (db_connection:NpgsqlConnection)
        (post_id: Post_id)
        (stats: Post_stats)
        =
        db_connection.Query( 
            $"insert into {tables.post.stats} (
                post_id,
                datetime,
                replies,
                likes,
                reposts,
                views,
                bookmarks
            )
            values (
                @post_id,
                @datetime,
                @replies,
                @likes,
                @reposts,
                @views,
                @bookmarks
            )
            on conflict (post_id, datetime)
            do update set (replies, likes, reposts, views, bookmarks)
            = (@replies, @likes, @reposts, @views, @bookmarks)",
            {|
                post_id=post_id
                datetime=DateTime.Now
                replies=stats.replies
                likes=stats.likes
                reposts=stats.reposts
                views=stats.views
                bookmarks=stats.bookmarks
            |}
        ) |> ignore 
    
    
    let write_image
        (db_connection:NpgsqlConnection)
        (image: Posted_image)
        (main_post: Post_id)
        (sorting_index:int)
        (is_quotation:bool)
        =
        db_connection.Query( 
            $"insert into {tables.post.image} (
                post_id,
                url,
                description,
                sorting_index,
                is_quotation
            )
            values (
                @post_id,
                @url,
                @description,
                @sorting_index,
                @is_quotation
            )
            on conflict (post_id, sorting_index, is_quotation)
            do update set (url, description) = (@url, @description)",
            {|
                post_id=main_post
                url=image.url
                description=image.description
                sorting_index=sorting_index
                is_quotation=is_quotation
            |}
        ) |> ignore 
    
    let write_video
        (db_connection:NpgsqlConnection)
        (video_url: string)
        (main_post: Post_id)
        (sorting_index:int)
        (is_quotation:bool)
        =
        db_connection.Query( 
            $"insert into {tables.post.video} (
                post_id,
                url,
                sorting_index,
                is_quotation
            )
            values (
                @post_id,
                @url,
                @sorting_index,
                @is_quotation
            )
            on conflict (post_id, sorting_index, is_quotation)
            do update set (url)
            = (@url)",
            {|
                post_id=main_post
                url=video_url
                sorting_index=sorting_index
                is_quotation=is_quotation
            |}
        ) |> ignore 
    
    
    let write_media_item
        (db_connection:NpgsqlConnection)
        (media_item: Media_item)
        (main_post: Post_id)
        (sorting_index:int)
        (is_quotation:bool)
        =
        match media_item with
        |Image image ->
            write_image
                db_connection
                image
                main_post
                sorting_index
                is_quotation
        |Video_poster video ->
            write_video
                db_connection
                video
                main_post
                sorting_index
                is_quotation
                
    let write_media_items
        (db_connection:NpgsqlConnection)
        (media_items: Media_item list)
        (owner_post: Post_id)
        (is_quotation: bool)
        =
        media_items
        |>List.iteri (fun index media_item ->
            write_media_item
                db_connection
                media_item
                owner_post
                index
                is_quotation
        )
    
    let write_textual_part_of_quotable_core
        (db_connection:NpgsqlConnection)
        (quotable_core: Quotable_post)
        (main_post_id:Post_id)
        (is_quotation:bool)
        =
        let message,show_more_url,is_abbreviated =
            match quotable_core.message with
            |Full text -> text,"",false
            |Abbreviated message ->
                message.message,
                (
                    message.show_more_url
                    |>Option.defaultValue ""
                ),
                true
        
        let reply_to_user,reply_to_post =
            match quotable_core.reply_status with
            |Some reply_status ->
                match reply_status with
                |External_message (other_user, other_post) ->
                    Some other_user,other_post
                |External_thread user -> Some user,None
                |Starting_local_thread -> None,None
                |Continuing_local_thread other_post ->
                    (Some quotable_core.author.handle), Some other_post
                |Ending_local_thread other_post ->
                    (Some quotable_core.author.handle), Some other_post
            |None-> None,None
        
        db_connection.Query(
            $"insert into {tables.post.quotable_core} (
                main_post_id,
                author,
                created_at,
                message,
                show_more_url,
                is_abbreviated,
                is_quotation,
                reply_to_user,
                reply_to_post
            )
            values (
                @main_post_id,
                @author,
                @created_at,
                @message,
                @show_more_url,
                @is_abbreviated,
                @is_quotation,
                @reply_to_user,
                @reply_to_post
            )
            on conflict (main_post_id, is_quotation)
            do update set (author, created_at, message, show_more_url, is_abbreviated,reply_to_user,reply_to_post)
            = (@author, @created_at, @message, @show_more_url, @is_abbreviated, @reply_to_user, @reply_to_post)
            ",
            {|
                main_post_id=main_post_id
                author=quotable_core.author.handle
                created_at=quotable_core.created_at
                message=message
                show_more_url=show_more_url
                is_abbreviated=is_abbreviated
                is_quotation=is_quotation
                reply_to_user = reply_to_user
                reply_to_post = reply_to_post
            |}
        ) |> ignore
            
    let write_quotable_core
        (db_connection:NpgsqlConnection)
        (quotable_core: Quotable_post)
        (main_post_id:Post_id)
        (is_quotation:bool)
        =
            
        write_media_items
            db_connection
            quotable_core.media_load
            main_post_id
            is_quotation
        
        write_textual_part_of_quotable_core
            db_connection
            quotable_core
            main_post_id
            is_quotation
        
        
   
    
    let write_external_url
        (db_connection:NpgsqlConnection)
        (external_url: External_url)
        (post_id:Post_id)
        =
        db_connection.Query(
            $"insert into {tables.post.external_url} (
                post_id,
                base_url,
                page,
                message,
                obfuscated_url
            )
            values (
                @post_id,
                @base_url,
                @page,
                @message,
                @obfuscated_url
            )
            on conflict (post_id)
            do update set (base_url, page, message, obfuscated_url)
            = (@base_url, @page, @message, @obfuscated_url)",
            {|
                post_id=post_id
                base_url=external_url.base_url|>Option.defaultValue ""
                page=external_url.page|>Option.defaultValue ""
                message=external_url.message|>Option.defaultValue ""
                obfuscated_url=external_url.obfuscated_url|>Option.defaultValue ""
            |}
        ) |> ignore
     
    let write_main_post
        (db_connection:NpgsqlConnection)
        (main_post: Main_post)
        =
        write_quotable_core
            db_connection
            main_post.quotable_core
            main_post.id
            false
        
        match main_post.external_source with
        |None -> ()
        |Some (Quotation quotation) ->
            write_quotable_core
                db_connection
                quotation
                main_post.id
                true
                
        |Some (External_url external_url) -> 
            write_external_url
                db_connection
                external_url
                main_post.id
                
        write_stats
            db_connection
            main_post.id
            main_post.stats