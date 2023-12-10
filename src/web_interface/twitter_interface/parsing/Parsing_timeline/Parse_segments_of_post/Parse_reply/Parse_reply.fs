namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter


module Parse_reply =
    
    
    
    
    
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
    
    
    
    
    
    
    let has_reply_header node =
        node
        |>Html_node.descendants_with_this "div"
        |>List.exists (fun node ->
            Html_node.direct_text node = "Replying to "
        )
    
