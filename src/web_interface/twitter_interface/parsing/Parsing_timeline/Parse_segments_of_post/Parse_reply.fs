namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter


module Parse_reply =
    
    let is_reply_header node =
        node
        |>Html_node.descendants_with_this "div"
        |>List.exists (fun node ->
            Html_node.direct_text node = "Replying to "
        )
    
    let is_mark_of_thread node =
        node
        |>Html_node.matches "div"
        &&
        node
        |>Html_node.direct_children
        |>List.tryHead
        |>function
        |Some span ->
            span
            |>Html_node.matches "span"
            &&
            span
            |>Html_node.inner_text = "Show this thread"
        |None -> false
     
    let parse_reply_header reply_header =
        reply_header
        |>Html_node.first_descendants_with_css "span"
        |>List.head
        |>Html_node.inner_text
        |>User_handle.trim_potential_atsign
        |>User_handle
    
    
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
    
    let try_reply_status_from_thread_mark user node =
        if (
            node
            |>Html_node.parent
            |>Html_node.direct_children
            |>List.last
            |>is_mark_of_thread)
        then    
            {Reply.to_user = user; to_post=None; is_direct=true}
            |>Some
        else
            None
    
    let parse_reply_of_quotable_post author ``tweetText node`` =
        let reply_target_from_header =
            try_reply_target_from_reply_header
                ``tweetText node``
            
        match reply_target_from_header with
        |Some reply_target->
            {Reply.to_user=reply_target; to_post=None; is_direct=true}
            |>Some
        |None->
            try_reply_status_from_thread_mark author ``tweetText node``
    
    

    
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
    
    
    
    let parse_reply_of_main_post
        author
        has_social_context_header
        article_node
        (previous_cell: Previous_cell)
        =
        let reply_from_quotable_core =
            article_node
            |>Html_node.descendants "div[data-testid='tweetText']"
            |>List.tryHead
            |>function
            |Some message_node ->
                message_node
                |>parse_reply_of_quotable_post
                    author
            |None -> None
            
        match reply_from_quotable_core with
        |Some reply_status -> Some reply_status
        |None->
            if
                has_social_context_header
            then 
                None
            else
                reply_from_local_thread previous_cell article_node
    
    
   