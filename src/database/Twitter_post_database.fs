namespace rvinowise.twitter

open System
open Dapper
open Npgsql
open Xunit
open rvinowise.twitter
open rvinowise.twitter.database_schema
open rvinowise.web_scraping



module Twitter_post_database =

    
    
    let write_stats
        (db_connection:NpgsqlConnection)
        (post_id: Post_id)
        (stats: Post_stats)
        =
        db_connection.Query( 
            $"insert into {tables.post.stats} (
                {tables.post.stats.post_id},
                {tables.post.stats.datetime},
                {tables.post.stats.replies},
                {tables.post.stats.likes},
                {tables.post.stats.reposts},
                {tables.post.stats.views},
                {tables.post.stats.bookmarks}
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
            on conflict (
                {tables.post.stats.post_id},
                {tables.post.stats.datetime}
            )
            do update set (
                {tables.post.stats.replies},
                {tables.post.stats.likes},
                {tables.post.stats.reposts},
                {tables.post.stats.views},
                {tables.post.stats.bookmarks}
            )
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
                {tables.post.image.post_id},
                {tables.post.image.url},
                {tables.post.image.description},
                {tables.post.image.sorting_index},
                {tables.post.image.is_quotation}
            )
            values (
                @post_id,
                @url,
                @description,
                @sorting_index,
                @is_quotation
            )
            on conflict (
                {tables.post.image.post_id},
                {tables.post.image.sorting_index},
                {tables.post.image.is_quotation}
            )
            do update set (
                {tables.post.image.url},
                {tables.post.image.description}
            ) = (@url, @description)",
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
                {tables.post.video.post_id},
                {tables.post.video.url},
                {tables.post.video.sorting_index},
                {tables.post.video.is_quotation}
            )
            values (
                @post_id,
                @url,
                @sorting_index,
                @is_quotation
            )
            on conflict (
                {tables.post.video.post_id},
                {tables.post.video.sorting_index},
                {tables.post.video.is_quotation}
            )
            do update set (
                {tables.post.video.url}
            )
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
            $"insert into {tables.post.reply} (
                {tables.post.reply.previous_post},
                {tables.post.reply.previous_user},
                {tables.post.reply.next_post},
                {tables.post.reply.is_direct}
            )
            values (
                @previous_post,
                @previous_user,
                @next_post,
                @is_direct
            )
            on conflict (
                {tables.post.reply.next_post}, 
                {tables.post.reply.is_direct}
            )
            do update set (
                {tables.post.reply.previous_post},
                {tables.post.reply.previous_user}
            )
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
            $"insert into {tables.post.header} (
                {tables.post.header.main_post_id},
                {tables.post.header.author},
                {tables.post.header.created_at},
                {tables.post.header.is_quotation}
            )
            values (
                @main_post_id,
                @author,
                @created_at,
                @is_quotation
            )
            on conflict (
                {tables.post.header.main_post_id},
                {tables.post.header.is_quotation}
            )
            do update set (
                {tables.post.header.author},
                {tables.post.header.created_at}
            )
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
            $"insert into {tables.post.twitter_space} (
                {tables.post.twitter_space.main_post_id},
                {tables.post.twitter_space.is_quotation},
                {tables.post.twitter_space.host},
                {tables.post.twitter_space.title},
                {tables.post.twitter_space.audience_amount}
            )
            values (
                @main_post_id,
                @is_quotation,
                @host,
                @title,
                @audience_amount
            )
            on conflict (
                {tables.post.twitter_space.main_post_id},
                {tables.post.twitter_space.is_quotation}
            )
            do update set (
                {tables.post.twitter_space.host},
                {tables.post.twitter_space.title},
                {tables.post.twitter_space.audience_amount}
            )
            = (@host, @title, @audience_amount)
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
            $"insert into {tables.post.twitter_event} (
                {tables.post.twitter_event.id},
                {tables.post.twitter_event.presenter_handle},
                {tables.post.twitter_event.presenter_name},
                {tables.post.twitter_event.title}
            )
            values (
                @id,
                @presenter_handle,
                @presenter_name,
                @title
            )
            on conflict ({tables.post.twitter_event.id})
            do update set (
                {tables.post.twitter_event.presenter_handle},
                {tables.post.twitter_event.presenter_name},
                {tables.post.twitter_event.title}
            )
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
            $"insert into {tables.post.twitter_event_in_post} (
                {tables.post.twitter_event_in_post.main_post_id},
                {tables.post.twitter_event_in_post.event_id}
            )
            values (
                @main_post_id,
                @event_id
            )
            on conflict (
                {tables.post.twitter_event_in_post.main_post_id},
                {tables.post.twitter_event_in_post.event_id}
            )
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
            $"insert into {tables.post.quotable_message_body} (
                {tables.post.quotable_message_body.main_post_id},
                {tables.post.quotable_message_body.message},
                {tables.post.quotable_message_body.show_more_url},
                {tables.post.quotable_message_body.is_abbreviated},
                {tables.post.quotable_message_body.is_quotation}
            )
            values (
                @main_post_id,
                @message,
                @show_more_url,
                @is_abbreviated,
                @is_quotation
            )
            on conflict (
                {tables.post.quotable_message_body.main_post_id},
                {tables.post.quotable_message_body.is_quotation}
            )
            do update set (
                {tables.post.quotable_message_body.message},
                {tables.post.quotable_message_body.show_more_url},
                {tables.post.quotable_message_body.is_abbreviated}
            )
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
            $"insert into {tables.post.external_url} (
                {tables.post.external_url.post_id},
                {tables.post.external_url.base_url},
                {tables.post.external_url.page},
                {tables.post.external_url.message},
                {tables.post.external_url.obfuscated_url}
            )
            values (
                @post_id,
                @base_url,
                @page,
                @message,
                @obfuscated_url
            )
            on conflict (
                {tables.post.external_url.post_id}
            )
            do update set (
                {tables.post.external_url.base_url},
                {tables.post.external_url.page},
                {tables.post.external_url.message},
                {tables.post.external_url.obfuscated_url}
            )
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
        index
        (post_id: Post_id)
        =
        db_connection.Query(
            $"insert into {tables.post.poll_choice} (
                {tables.post.poll_choice.post_id},
                {tables.post.poll_choice.index},
                {tables.post.poll_choice.text},
                {tables.post.poll_choice.votes_percent}
            )
            values (
                @post_id,
                @index,
                @text,
                @votes_percent
            )
            on conflict (
                {tables.post.poll_choice.post_id},
                {tables.post.poll_choice.index}
            )
            do update set (
                {tables.post.poll_choice.text},
                {tables.post.poll_choice.votes_percent}
            )
            = (@text, @votes_percent)",
            {|
                post_id=post_id
                index=index
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
            $"insert into {tables.post.quotable_part_of_poll} (
                {tables.post.quotable_part_of_poll.post_id},
                {tables.post.quotable_part_of_poll.is_quotation},
                {tables.post.quotable_part_of_poll.question}
            )
            values (
                @post_id,
                @is_quotation,
                @question
            )
            on conflict (
                {tables.post.quotable_part_of_poll.post_id},
                {tables.post.quotable_part_of_poll.is_quotation}
            )
            do update set (
                {tables.post.quotable_part_of_poll.question}
            )
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
            $"insert into {tables.post.poll_summary} (
                {tables.post.poll_summary.post_id},
                {tables.post.poll_summary.votes_amount}
            )
            values (
                @post_id,
                @votes_amount
            )
            on conflict (
                {tables.post.poll_summary.post_id}
            )
            do update set (
                {tables.post.poll_summary.votes_amount}
            )
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
        |>List.iteri (fun index choice ->
            write_poll_choice
                db_connection
                choice
                index
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
            $"insert into {tables.post.repost} (
                {tables.post.repost.post},
                {tables.post.repost.reposter}
            )
            values (
                @post,
                @reposter
            )
            on conflict (
                {tables.post.repost.post},
                {tables.post.repost.reposter}
            )
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
            $"insert into {tables.post.like} (
                {tables.post.like.post},
                {tables.post.like.liker}
            )
            values (
                @post,
                @liker
            )
            on conflict (
                {tables.post.like.post},
                {tables.post.like.liker}
            )
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
            
    
    let write_newest_last_visited_post
        (db_connection:NpgsqlConnection)
        (timeline_tab: Timeline_tab)
        account
        (post: Post_id)
        =
        db_connection.Query(
            $"insert into {tables.last_visited_post_in_timeline} (
                {tables.last_visited_post_in_timeline.account},
                {tables.last_visited_post_in_timeline.timeline},
                {tables.last_visited_post_in_timeline.post},
                {tables.last_visited_post_in_timeline.visited_at}
            )
            values (
                @account,
                @timeline,
                @post,
                @visited_at
            )
            on conflict (
                {tables.last_visited_post_in_timeline.account},
                {tables.last_visited_post_in_timeline.timeline}
            )
            do update set (
                {tables.last_visited_post_in_timeline.post}, 
                {tables.last_visited_post_in_timeline.visited_at}
            )
            = (@post, @visited_at)",
            {|
                account=account
                timeline=Timeline_tab.human_name timeline_tab
                post=post
                visited_at=DateTime.Now
            |}
        ) |> ignore
        
    let read_newest_last_visited_post
        (db_connection:NpgsqlConnection)
        (timeline_tab: Timeline_tab)
        account
        =
        db_connection.Query<Post_id>(
            $"
            select {tables.last_visited_post_in_timeline.post} 
            from {tables.last_visited_post_in_timeline}
            where 
                {tables.last_visited_post_in_timeline.account}=@account
                and {tables.last_visited_post_in_timeline.timeline}=@timeline
            ",
            {|
                account = account
                timeline = Timeline_tab.human_name timeline_tab
            |}
        )|>Seq.tryHead
        
    let ``try read_last_visited_post``() =
        let result =
            read_newest_last_visited_post
                (Local_database.open_connection())
                Timeline_tab.Posts
                (User_handle "MikhailBatin")
        ()
        
        
    let ``delete all posts from a user's timeline``()=
        let user = User_handle "TheHarrisSultan"
        let result =
            Local_database.open_connection().Query<Post_id>(
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