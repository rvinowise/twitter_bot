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
        detail_node
        =
        let details_segments =
            detail_node
            |>Html_node.direct_children
        
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
 
            External_source.External_website{
                base_url = base_url
                page=page
                message = message
                obfuscated_url=obfuscated_url
            }
    
    let try_external_website_node
        article_node
        =
        let card_wrapper_node = 
            article_node
            |>Html_node.try_descendant "div[data-testid='card.wrapper']"
        
        match card_wrapper_node with
        |Some card_wrapper_node ->
            card_wrapper_node
            |>Html_node.try_descendant "[data-testid='card.layoutSmall.detail']"
            |>function
            |Some card_layout_node ->
                External_source_node.External_website (card_wrapper_node,card_layout_node)
                |>Some
            |None -> None
        |None -> None

    
    

    let external_source_node_of_main_post
        article_node 
        =
        [
            Parse_quoted_post.try_quotation_node;
            Parse_twitter_event.try_twitter_event_node;
            try_external_website_node;
        ]
        |>List.tryPick (fun parser ->
            parser article_node
        )

    let parse_external_source_of_main_post
        (external_source_node: External_source_node)
        =
        match external_source_node with
        |External_source_node.Quoted_message node ->
            Parse_quoted_post.parse_quoted_post node
        |External_source_node.Quoted_poll node ->
            Parse_quoted_post.parse_quoted_post node
        |External_source_node.External_website
            (card_wrapper_node,layout_node) ->
            parse_external_website card_wrapper_node layout_node
        |External_source_node.Twitter_event (node, event_id) ->
            Parse_twitter_event.parse_twitter_event node event_id


    let detach_and_parse_external_source
        post_node
        =
        let external_source_node =
            external_source_node_of_main_post
                post_node
        
        match external_source_node with
        |Some external_source_node ->
            external_source_node
            |>External_source_node.root_html_node
            |>Html_node.detach_from_parent
            |>ignore
        |None->()
        
        external_source_node
        |>Option.map parse_external_source_of_main_post 