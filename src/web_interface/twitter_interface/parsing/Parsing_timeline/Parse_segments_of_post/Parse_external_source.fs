namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter



    
module Parse_external_source =

    
    
    
    let text_from_external_source_detail_segment segment =
        segment
        |>Html_node.descendant ":scope> span"
        |>Html_parsing.readable_text_from_html_segments
    
    let parse_external_website
        card_wrapper_node
        =
        let details_segments =
            card_wrapper_node
            |>Find_segments_of_post.details_of_external_source
        
        let obfuscated_url=
            card_wrapper_node
            |>Html_node.descendants "a[role='link']"
            |>List.tryHead
            |>Option.map (Html_node.attribute_value "href")
        
        match details_segments,obfuscated_url with
        |[],None->
            ("the card.wrapper node of the external url has neither details nor the url",
            card_wrapper_node)
            |>Bad_post_exception
            |>raise 
        |details_segments,obfuscated_url->
                
            let base_url =
                details_segments
                |>List.tryHead
                |>Option.map text_from_external_source_detail_segment
                
            let page =
                details_segments
                |>List.tryItem 1
                |>Option.map text_from_external_source_detail_segment
                
            let message =
                details_segments
                |>List.tryItem 2
                |>Option.map text_from_external_source_detail_segment
 
            Some (
                External_source.External_website{
                    base_url = base_url
                    page=page
                    message = message
                    obfuscated_url=obfuscated_url
                }
            )
    
    let try_external_website_node
        article_node
        =
        article_node
        |>Html_node.try_descendant "div[data-testid='card.wrapper']"
        |>Option.map External_source_node.External_website
    
    let try_parse_external_website
        ``node of potential card.wrapper``
        =
        if
            ``node of potential card.wrapper``
            |>Html_node.matches "div[data-testid='card.wrapper']"
        then
            parse_external_website
                ``node of potential card.wrapper``
        else None
    
    
    let parse_twitter_event card_node event_id =
        card_node
        |>Html_node.direct_children
        |>function
        |user_node::[ title_node ] ->
            let name =
                user_node
                |>Html_node.descendants "span"
                |>List.head
                |>Html_parsing.readable_text_from_html_segments
            let handle =
                user_node
                |>Html_node.descend 1
                |>Html_node.direct_children
                |>List.item 1
                |>Html_node.descendant "span"
                |>Html_node.inner_text
                |>User_handle.trim_potential_atsign
                |>User_handle
            let title =
                title_node
                |>Html_node.descendants "span"
                |>List.head
                |>Html_parsing.readable_text_from_html_segments
            External_source.Twitter_event {
                id=event_id
                user={
                    handle=handle
                    name=name
                }
                title=title
            }
        |wrong_nodes->
            raise (Bad_post_exception($"the Card node of a twitter event should have User and Title, but there was {wrong_nodes}"))
    
    let try_twitter_event_node article_node =
        article_node
        |>Html_node.descendants "a[role='link']"
        |>List.tryItem 1
        |>function
        |Some link_node ->
            link_node
            |>Html_node.attribute_value "href"
            |>fun url->url.Split("/")
            |>List.ofArray
            |>List.rev
            |>function
            |id::keyword::rest ->
                if keyword = "events" then
                    link_node
                    |>Html_node.try_descendant "div[data-testid='card.layoutSmall.detail']"
                    |>function
                    |Some card_node ->
                        External_source_node.Twitter_event(
                            card_node
                            ,
                            id
                            |>int64|>Event_id
                        )
                        |>Some
                    |None ->None
                else
                    None
            | _ -> None
        |None -> None
    

    let external_source_node_of_main_post
        article_node 
        =
        [
            Parse_quoted_post.try_quotation_node;
            try_external_website_node;
            try_twitter_event_node;
        ]
        |>List.tryPick (fun parser ->
            parser article_node
        )
