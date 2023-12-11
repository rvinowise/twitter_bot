namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter


module Parse_reply_in_quoted_post =
    
    
    
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
   
    
    
    let reply_of_quoted_post
        quote_node
        =
        match 
            Parse_reply.try_find_reply_header
                quote_node
        with
        |Some reply_header ->
            let reply_target_from_header =
                reply_header
                |>Parse_reply.user_from_reply_header
                    
            Some {
                Reply.to_user = reply_target_from_header
                to_post=None
                is_direct=true
            }
        |None -> None
    
    
   