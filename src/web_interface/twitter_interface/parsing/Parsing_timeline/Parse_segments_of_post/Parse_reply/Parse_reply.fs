namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter


module Parse_reply =
    
    

    
    let has_reply_header node =
        node
        |>Html_node.descendants_with_this "div"
        |>List.exists (fun node ->
            Html_node.direct_text node = "Replying to "
        )
    
    let try_find_reply_header
        node
        =
        node
        |>Html_node.descendants "div"
        |>List.filter (fun node ->
            node
            |>Html_node.direct_text = "Replying to "
        )|>List.tryHead
        
    
    let user_from_reply_header reply_header =
        reply_header
        |>Html_node.descendants_from_highest_level "span"
        |>List.head
        |>Html_node.inner_text
        |>User_handle.trim_potential_atsign
        |>User_handle 