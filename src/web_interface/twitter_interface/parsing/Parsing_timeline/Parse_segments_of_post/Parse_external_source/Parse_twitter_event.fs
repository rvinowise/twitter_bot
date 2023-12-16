namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter



    
module Parse_twitter_event =

    
    let try_user_handle_from_node node =
        Html_node.inner_text node
        |>User_handle.try_handle_from_text
    
    let parse_event_presenter
        presenter_node
        =
        let text_spans =
            presenter_node
            |>Html_node.descendents_without_deepening "span"
        
        let name =
            text_spans
            |>List.head
            |>Html_parsing.readable_text_from_html_segments
        
        let handle =
            text_spans
            |>List.tail
            |>List.tryPick(fun span_node ->
                try_user_handle_from_node span_node
            )
            
        
        match handle with
        |Some handle ->
            Twitter_event_presenter.User{
                handle=handle
                name=name
            }
        |None ->
            Twitter_event_presenter.Company name
    
    let parse_twitter_event card_node event_id =
        card_node
        |>Html_node.descendant "div[data-testid='card.layoutSmall.detail']"
        |>Html_node.direct_children
        |>function
        |presenter_node::[ title_node ] ->
            
            let presenter =
                parse_event_presenter
                    presenter_node
            
            let title =
                title_node
                |>Html_node.descendants "span"
                |>List.head
                |>Html_parsing.readable_text_from_html_segments
            
            External_source.Twitter_event {
                id=event_id
                presenter=presenter
                title=title
            }
        |wrong_nodes->
            raise (Bad_post_exception($"the Card node of a twitter event should have User and Title, but there was {wrong_nodes}"))
    
    let try_twitter_event_node article_node =
        let card_wrapper_node =
            article_node
            |>Html_node.try_descendant "div[data-testid='card.wrapper']"
        match card_wrapper_node with
        |Some card_wrapper_node ->
            card_wrapper_node
            |>Html_node.descendants_from_highest_level "a[role='link']"
            |>function
            | link_node::_ ->
                link_node
                |>Html_node.attribute_value "href"
                |>fun url->url.Split("/")
                |>List.ofArray
                |>List.rev
                |>function
                |id::keyword::rest ->
                    if keyword = "events" then
                        
                        External_source_node.Twitter_event(
                            card_wrapper_node
                            ,
                            id
                            |>int64|>Event_id
                        )
                        |>Some
                    else
                        None
                | _ -> None
            |_ -> None
        |None->None