namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter


module Parse_quotation =
    
    
    
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
                External_source.External_url{
                    base_url = base_url
                    page=page
                    message = message
                    obfuscated_url=obfuscated_url
                }
            )
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
    
    let try_parse_twitter_event html_node =
        html_node
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
                        id
                        |>int64|>Event_id
                        |>parse_twitter_event card_node
                        |>Some
                    |None ->None
                else
                    None
            | _ -> None
        |None -> None
        
    
    let parse_twitter_audio_space placement_tracking_node =
        let segments =
            placement_tracking_node
            |>Html_node.descend 2
            |>Html_node.direct_children
        
        match segments with
        |header_html::title_html::stats_html::_ ->
            let host =
                header_html
                |>Html_node.descendants "span"
                |>List.head
                |>Html_parsing.readable_text_from_html_segments
            let title =
                title_html
                |>Html_node.inner_text
            let audience_amount =
                stats_html
                |>Html_node.descend 1
                |>Html_node.direct_text
            let audience_amount =
                audience_amount.Split(" ")
                |>Array.head
                |>Parsing_twitter_datatypes.parse_abbreviated_number
                
            {
                Twitter_audio_space.host = host
                title=title;
                audience_amount=audience_amount
            }
        | _ ->raise (Bad_post_exception("wrong segments of a twitter audio space"))
        
    let try_parse_twitter_audio_space html_node =
        let placement_tracking_node =
            html_node
            |>Html_node.try_descendant "div[data-testid='placementTracking']"
        
        match placement_tracking_node with
        |Some placement_tracking_node ->
            placement_tracking_node
            |>Html_node.try_descendant "div[role='button'] span > span"
            |>function
            |Some potential_follow_button ->
                if
                    (potential_follow_button|>Html_node.inner_text = "Follow host")
                then
                    parse_twitter_audio_space placement_tracking_node
                    |>Some
                else None
            |None -> None
            
        |None -> None
    
            
    let parse_quoted_post
        html_node
        quoted_message_node
        = 
        let quotation_root_node =
            quoted_message_node
            |>Html_node.ancestors "div[role='link']"
            |>List.head    
            
        let quoted_header =
            quotation_root_node
            |>Html_node.descendant "div[data-testid='User-Name']"
            |>Parse_header.parse_post_header
        
        let reply =
            Parse_reply.parse_reply_of_quotable_post quoted_header.author.handle quoted_message_node
        
        let quoted_media_items =
            Parse_media.parse_media_items_from_quotation html_node
            
        let twitter_space =
            try_parse_twitter_audio_space html_node
            
        
        let header={
            author = quoted_header.author
            created_at = quoted_header.written_at
            reply=reply
        }
        
        let message =
            Post_message.from_html_node
                quoted_message_node
        
        if
            Parse_poll.quotation_is_a_poll
                html_node
        then
            Some (External_source.Quoted_poll{
                header=header
                question =
                    message
                    |>Post_message.text
            })
        else
            Some (External_source.Quoted_message{
                header=header
                message = message
                media_load = quoted_media_items
                twitter_space = twitter_space 
            })
        
    let try_parse_quoted_post html_node =
        let quoted_message_node =
            html_node
            |>Html_node.try_descendant "div[data-testid='tweetText']"
            
        match quoted_message_node with
        |Some quoted_message_node ->
            parse_quoted_post html_node quoted_message_node
        |None->None
    
 
    
    let parse_external_source_from_its_node
        //either a quoted-post, or an external-url; node with either role=link of quotation, or card.wrapper
        html_node 
        =
        [
            try_parse_quoted_post;
            try_parse_external_website;
            try_parse_twitter_event;
        ]
        |>List.pick (fun parser ->
            parser html_node
        )
            
    