namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter


module Parse_reply_in_main_post=
    
    
    let try_reply_target_from_reply_header tweet_text_node =
        tweet_text_node
        |>Html_node.parent
        |>Html_node.direct_children
        |>List.head
        |>fun potential_header ->
            if
                is_reply_header potential_header
            then
                let reply_author =
                    potential_header
                    |>parse_reply_header
                Some reply_author
            else
                None
   

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
        (previous_post: Previous_cell)
        (article_html)
        =
        if
            post_has_linked_post_before article_html
        then
            match previous_post with
            |Adjacent_post (post,user) ->
                Some {Reply.to_user = user; to_post=Some post; is_direct=true}
            |Distant_connected_message (post,user) ->
                Some {Reply.to_user = user; to_post=Some post; is_direct=false}
            |No_cell -> None
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
        author
        has_social_context_header
        article_node
        second_segment
        (previous_cell: Previous_cell)
        =
        let reply_from_reply_header =
            try_parse_reply_header_of_main_post
                second_segment
            
        match reply_from_reply_header with
        |Some reply_status -> Some reply_status
        |None->
            if
                has_social_context_header //reposts and pinned posts can't be part of a local thread in the timeline
            then 
                None
            else
                reply_from_local_thread previous_cell article_node
    
    
   