﻿namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter


module Parse_reply_in_main_post=
    
    

    let post_has_linked_post_after article_html =
        article_html
        |>Html_node.descendants "div[data-testid='Tweet-User-Avatar']"
        |>List.head
        |>Html_node.parent
        |>Html_node.direct_children
        |>List.length
        |>function
        |1 -> false
        |2 -> true
        |unexpected_number ->
            (
                $"an unexpected number ({unexpected_number}) of children at the vertical thread-line",
                article_html
            )
            |>Bad_post_exception
            |>raise 
    
    let post_has_linked_post_before article_html =
        article_html
        |>Html_node.descend 2
        |>Html_node.direct_children|>List.head
        |>Html_node.descend 1
        |>Html_node.direct_children|>List.head
        |>Html_node.direct_children
        |>List.isEmpty|>not
    
    let reply_from_local_thread
        (previous_cell: Thread_context)
        (article_html)
        =
        if
            post_has_linked_post_before article_html
        then
            match previous_cell with
            |Post post ->
                Some {
                    Reply.to_user = Main_post.author_handle post
                    to_post=Some post.id
                    is_direct=true
                }
            |Hidden_thread_replies last_visible_post ->
                Some {
                    Reply.to_user = Main_post.author_handle last_visible_post
                    to_post=Some last_visible_post.id
                    is_direct=false
                }
            |Thread_context.Empty_context ->
                "a post has a linked thread-post before, but the previous cell context is Empty"
                |>Harvesting_exception
                |>raise
        else
            None
    
    
    
    let has_reply_header node =
        node
        |>Html_node.descendants_with_this "div"
        |>List.exists (fun node ->
            Html_node.direct_text node = "Replying to "
        )
    
    let parse_reply_header_of_main_post
        reply_header //second segment of top-level post segments
        =
        let target_user = 
            reply_header
            |>Html_node.descendant "a[role='link']"
            |>Html_node.attribute_value "href"
            |>User_handle.try_handle_from_url
        match target_user with
        |Some target_user ->
            {Reply.to_user = target_user; to_post=None; is_direct=true}
        |None->
            raise (Bad_post_exception("can't read target user from the reply header"))
            
    let try_parse_reply_header_of_main_post
        reply_header 
        =
        if has_reply_header reply_header then
            parse_reply_header_of_main_post reply_header
            |>Some
        else
            None
    
    
    
    
    let parse_reply_of_main_post
        has_social_context_header
        article_node
        previous_cell
        =
        let reply_header = 
            Parse_reply.try_find_reply_header
                article_node
            
        match reply_header with
        |Some reply_header ->
            let reply_target_from_header =
                reply_header
                |>Parse_reply.user_from_reply_header
                    
            Some {
                Reply.to_user = reply_target_from_header
                to_post=None
                is_direct=true
            }
        |None->
            if
                has_social_context_header //reposts and pinned posts can't be part of a local thread in the timeline
            then 
                None
            else
                reply_from_local_thread previous_cell article_node
    
    
   