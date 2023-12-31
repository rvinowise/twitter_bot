namespace rvinowise.twitter

open System
open Dapper
open Npgsql
open Xunit
open rvinowise.twitter
open rvinowise.twitter.database
open rvinowise.web_scraping



module Twitter_post_database =

    
    
    let write_stats
        (db_connection:NpgsqlConnection)
        (post_id: Post_id)
        (stats: Post_stats)
        =
        db_connection.Query( 
            $"insert into {tables.post_tables.stats} (
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
            $"insert into {tables.post_tables.image} (
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
            $"insert into {tables.post_tables.video} (
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
            = row(@url)",
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
    
    
    let write_reply
        (db_connection:NpgsqlConnection)
        (reply:Reply)
        (post:Post_id)
        =
        db_connection.Query(
            $"insert into {tables.post_tables.reply} (
                previous_post,
                previous_user,
                next_post,
                is_direct
            )
            values (
                @previous_post,
                @previous_user,
                @next_post,
                @is_direct
            )
            on conflict (next_post, is_direct)
            do update set (previous_post, previous_user)
            = row(@previous_post, @previous_user)",
            {|
                previous_post=reply.to_post
                previous_user=reply.to_user
                next_post=post
                is_direct=reply.is_direct
            |}
        ) |> ignore
        
    
    let write_post_header
        (db_connection:NpgsqlConnection)
        (header: Post_header)
        (main_post_id:Post_id)
        (is_quotation:bool)
        =
        db_connection.Query(
            $"insert into {tables.post_tables.header} (
                main_post_id,
                author,
                created_at,
                is_quotation
            )
            values (
                @main_post_id,
                @author,
                @created_at,
                @is_quotation
            )
            on conflict (main_post_id, is_quotation)
            do update set (author, created_at)
            = (@author, @created_at)
            ",
            {|
                main_post_id=main_post_id
                author=header.author.handle
                created_at=header.created_at
                is_quotation=is_quotation
            |}
        ) |> ignore
        
        match header.reply with
        |Some reply ->
            write_reply
                db_connection
                reply
                main_post_id
        |None->()
    
    let write_twitter_space
        (db_connection:NpgsqlConnection)
        (twitter_space: Twitter_audio_space)
        (main_post_id:Post_id)
        (is_quotation:bool)
        =
        db_connection.Query(
            $"insert into {tables.post_tables.twitter_space} (
                main_post_id,
                is_quotation,
                host,
                title,
                audience_amount
            )
            values (
                @main_post_id,
                @is_quotation,
                @host,
                @title,
                @audience_amount
            )
            on conflict (main_post_id, is_quotation)
            do update set (message, show_more_url, is_abbreviated)
            = (@message, @show_more_url, @is_abbreviated)
            ",
            {|
                main_post_id=main_post_id
                is_quotation=is_quotation
                host=twitter_space.host
                title=twitter_space.title
                audience_amount=twitter_space.audience_amount
            |}
        ) |> ignore
    
    let write_twitter_event
        (db_connection:NpgsqlConnection)
        (twitter_event: Twitter_event)
        =
        let presenter_handle,presenter_name =
            match twitter_event.presenter with
            |User user ->
                User_handle.value user.handle,
                user.name
            |Company name ->
                "", name
            
        db_connection.Query(
            $"insert into {tables.post_tables.twitter_event} (
                id,
                presenter_handle,
                presenter_name,
                title
            )
            values (
                @id,
                @presenter_handle,
                @presenter_name,
                @title
            )
            on conflict (id)
            do update set (presenter_handle, presenter_name, title)
            = (@presenter_handle, @presenter_name, @title)
            ",
            {|
                id=twitter_event.id
                presenter_handle=presenter_handle
                presenter_name=presenter_name
                title=twitter_event.title
            |}
        ) |> ignore
    
    let write_twitter_event_in_post
        (db_connection:NpgsqlConnection)
        main_post_id
        event_id
        =
        db_connection.Query(
            $"insert into {tables.post_tables.twitter_event_in_post} (
                main_post_id,
                event_id
            )
            values (
                @main_post_id,
                @event_id
            )
            on conflict (main_post_id, event_id)
            do nothing
            ",
            {|
                main_post_id=main_post_id
                event_id=event_id
            |}
        ) |> ignore
    
    let write_quotable_message_body
        (db_connection:NpgsqlConnection)
        (quotable_message: Quotable_message)
        (main_post_id:Post_id)
        (is_quotation:bool)
        =
        let message,show_more_url,is_abbreviated =
            match quotable_message.message with
            |Full text -> text,"",false
            |Abbreviated message ->
                message.message,
                (
                    message.show_more_url
                    |>Option.defaultValue ""
                ),
                true

        db_connection.Query(
            $"insert into {tables.post_tables.quotable_message_body} (
                main_post_id,
                message,
                show_more_url,
                is_abbreviated,
                is_quotation
            )
            values (
                @main_post_id,
                @message,
                @show_more_url,
                @is_abbreviated,
                @is_quotation
            )
            on conflict (main_post_id, is_quotation)
            do update set (message, show_more_url, is_abbreviated)
            = (@message, @show_more_url, @is_abbreviated)
            ",
            {|
                main_post_id=main_post_id
                message=message
                show_more_url=show_more_url
                is_abbreviated=is_abbreviated
                is_quotation=is_quotation
            |}
        ) |> ignore    
    
            
    let write_quotable_message
        (db_connection:NpgsqlConnection)
        (quotable_core: Quotable_message)
        (main_post_id:Post_id)
        (is_quotation:bool)
        =
        write_post_header
            db_connection
            quotable_core.header
            main_post_id
            is_quotation
        
        write_media_items
            db_connection
            quotable_core.media_load
            main_post_id
            is_quotation
        
        write_quotable_message_body
            db_connection
            quotable_core
            main_post_id
            is_quotation
   
        if quotable_core.audio_space.IsSome then
            write_twitter_space
                (db_connection:NpgsqlConnection)
                quotable_core.audio_space.Value
                (main_post_id:Post_id)
                (is_quotation:bool)
    
    let write_external_url
        (db_connection:NpgsqlConnection)
        (external_url: External_website)
        (post_id:Post_id)
        =
        db_connection.Query(
            $"insert into {tables.post_tables.external_url} (
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
     
    
    let write_poll_choice
        (db_connection: NpgsqlConnection)
        (choice: Poll_choice)
        (post_id: Post_id)
        =
        db_connection.Query(
            $"insert into {tables.post_tables.poll_choice} (
                post_id,
                text,
                votes_percent
            )
            values (
                @post_id,
                @text,
                @votes_percent
            )
            on conflict (post_id)
            do update set (text, votes_percent)
            = (@text, @votes_percent)",
            {|
                post_id=post_id
                text=choice.text
                votes_percent=choice.votes_percent
            |}
        ) |> ignore
    
    
    let write_quotable_part_of_poll
        (db_connection: NpgsqlConnection)
        (quotable_poll: Quotable_poll)
        (post_id: Post_id)
        (is_quotation: bool)
        =
        db_connection.Query(
            $"insert into {tables.post_tables.quotable_part_of_poll} (
                post_id,
                question,
                is_quotation
            )
            values (
                @post_id,
                @question,
                @is_quotation
            )
            on conflict (post_id, is_quotation)
            do update set (question)
            = row(@question)",
            {|
                post_id=post_id
                is_quotation=is_quotation
                question=quotable_poll.question
            |}
        ) |> ignore
    
    let write_poll_summary
        (db_connection: NpgsqlConnection)
        (poll: Poll)
        (post_id: Post_id)
        =
        db_connection.Query(
            $"insert into {tables.post_tables.poll_summary} (
                post_id,
                votes_amount
            )
            values (
                @post_id,
                @votes_amount
            )
            on conflict (post_id)
            do update set (votes_amount)
            = row(@votes_amount)",
            {|
                post_id=post_id
                votes_amount=poll.votes_amount
            |}
        ) |> ignore
    
            
    let write_post_body_without_poll
        (db_connection:NpgsqlConnection)
        post_id
        quotable_message
        external_source
        =
        write_quotable_message
            db_connection
            quotable_message
            post_id
            false
        
        match external_source with
        |None -> ()
        |Some (Quoted_message quotation) ->
            write_quotable_message
                db_connection
                quotation
                post_id
                true
        |Some (External_website external_url) -> 
            write_external_url
                db_connection
                external_url
                post_id
        |Some (External_source.Quoted_poll quotable_poll) -> 
            write_quotable_part_of_poll
                db_connection
                quotable_poll
                post_id
                true
        |Some (Twitter_event event) ->    
            write_twitter_event
                db_connection
                event
            write_twitter_event_in_post
                db_connection
                post_id
                event.id
            
    let write_post_body_with_poll
        (db_connection: NpgsqlConnection)
        (post_id: Post_id)
        (poll: Poll)
        =
        poll.choices
        |>List.iter (fun choice ->
            write_poll_choice
                db_connection
                choice
                post_id
        )
        write_poll_summary    
            db_connection
            poll
            post_id
            
        write_quotable_part_of_poll
            db_connection
            poll.quotable_part
            post_id
            false
            
            
    let write_repost
        (db_connection:NpgsqlConnection)
        (reposter: User_handle)
        (post_id: Post_id)
        =
        db_connection.Query(
            $"insert into {tables.post_tables.repost} (
                post,
                reposter
            )
            values (
                @post,
                @reposter
            )
            on conflict (post,reposter)
            do nothing",
            {|
                post=post_id
                reposter=reposter
            |}
        ) |> ignore
        
        
    let write_like
        (db_connection:NpgsqlConnection)
        (liker: User_handle)
        (post_id: Post_id)
        =
        db_connection.Query(
            $"insert into {tables.post_tables.like} (
                post,
                liker
            )
            values (
                @post,
                @liker
            )
            on conflict (post,liker)
            do nothing",
            {|
                post=post_id
                liker=liker
            |}
        ) |> ignore
        
            
            
    let write_main_post
        (db_connection:NpgsqlConnection)
        (main_post: Main_post)
        =
        match main_post.body with
        |Message(quotable_message,external_source) ->
            write_post_body_without_poll
                db_connection
                main_post.id
                quotable_message
                external_source
        |Poll poll ->
            write_post_body_with_poll
                db_connection
                main_post.id
                poll
        
        match main_post.reposter with
        |Some reposter ->
            write_repost
                db_connection
                reposter
                main_post.id
        |None -> ()
                    
        write_stats
            db_connection
            main_post.id
            main_post.stats
            
    
    let db_table_for_last_visited_post
        (timeline_tab: Timeline_tab) =
        match timeline_tab with
        | Posts -> "last_visited_post_in_timeline"
        | Posts_and_replies -> "last_visited_reply_in_timeline"
        | Media -> raise (NotSupportedException "this timeline tab can not be scraped")
        | Likes -> "last_visited_like_in_timeline"
    
    let write_newest_last_visited_post
        (db_connection:NpgsqlConnection)
        (timeline_tab: Timeline_tab)
        user
        (post: Post_id)
        =
        let table=db_table_for_last_visited_post timeline_tab
        db_connection.Query(
            $"insert into {table} (
                twitter_user,
                post,
                visited_at
            )
            values (
                @twitter_user,
                @post,
                @visited_at
            )
            on conflict (twitter_user)
            do update set (post, visited_at)
            = (@post, @visited_at)",
            {|
                twitter_user=user
                post=post
                visited_at=DateTime.Now
            |}
        ) |> ignore
        
    let read_newest_last_visited_post
        (db_connection:NpgsqlConnection)
        (timeline_tab: Timeline_tab)
        user
        =
        let table = db_table_for_last_visited_post timeline_tab
        db_connection.Query<Post_id>(
            $"select post from {table}
            where twitter_user=@user",
            {|
                user =user
            |}
        )|>Seq.tryHead
        
    [<Fact(Skip="manual")>]
    let ``try read_last_visited_post``() =
        let result =
            read_newest_last_visited_post
                (Twitter_database.open_connection())
                Timeline_tab.Posts
                (User_handle "MikhailBatin")
        ()
        
        
    [<Fact(Skip="manual")>]
    let ``delete all posts from a user's timeline``()=
        let user = User_handle "TheHarrisSultan"
        let result =
            Twitter_database.open_connection().Query<Post_id>(
                $"""
delete from post_quotable_message_body
using post_header
where post_quotable_message_body.main_post_id = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from poll_choice
using post_header
where poll_choice.post_id = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from poll_summary
using post_header
where poll_summary.post_id = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from post_audio_space
using post_header
where post_audio_space.main_post_id = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from post_external_url
using post_header
where post_external_url.post_id = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from post_image
using post_header
where post_image.post_id = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from post_like
using post_header
where post_like.post = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from post_poll_choice
using post_header
where post_poll_choice.post_id = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from post_quotable_poll
using post_header
where post_quotable_poll.post_id = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from post_reply
using post_header
where post_reply.next_post = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from post_repost
where post_repost.reposter = @user;

delete from post_stats
using post_header
where post_stats.post_id = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from post_twitter_event
using post_header, post_twitter_event_in_post
where post_twitter_event.id = post_twitter_event_in_post.event_id
and post_twitter_event_in_post.main_post_id = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from post_twitter_event_in_post
using post_header
where post_twitter_event_in_post.main_post_id = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from post_video
using post_header
where post_video.post_id = post_header.main_post_id
and post_header.is_quotation = false
and post_header.author = @user;

delete from post_header
where post_header.author = @user;
""",
                {|
                    user =user
                |}
            )
            
        ()