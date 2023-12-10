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
    
    
    
    
    let user_from_reply_header reply_header =
        reply_header
        |>Html_node.first_descendants_with_css "span"
        |>List.head
        |>Html_node.inner_text
        |>User_handle.trim_potential_atsign
        |>User_handle    
    
    let try_find_reply_header
        quotation_node //role=link
        =
        quotation_node
        |>Html_node.descendants"div"
        |>List.filter (fun node ->
            node
            |>Html_node.direct_text = "Replying to "
        )|>List.tryHead
    
    let reply_of_quoted_post
        quote_node
        =
        match 
            try_find_reply_header
                quote_node
        with
        |Some reply_header ->
            let reply_target_from_header =
                reply_header
                |>user_from_reply_header
                    
            Some {
                Reply.to_user = reply_target_from_header
                to_post=None
                is_direct=true
            }
        |None -> None
    
    
   