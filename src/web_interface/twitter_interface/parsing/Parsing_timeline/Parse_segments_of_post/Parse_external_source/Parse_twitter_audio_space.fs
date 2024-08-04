namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter


module Parse_twitter_audio_space =
    
    
    let parse_twitter_audio_space placement_tracking_node =
        let segments =
            placement_tracking_node
            |>Html_node.travel_down [1;1]
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
                |>Html_parsing.readable_text_from_html_segments
            let audience_amount =
                stats_html
                |>Html_node.descend 1
                |>Html_node.direct_text
            let audience_amount =
                audience_amount.Split(" ")
                |>Array.head
                |>Parsing_twitter_datatypes.parse_abbreviated_number
                
            {
                Valid_audio_space.host = host
                title=title;
                audience_amount=audience_amount
            }
        | _ ->raise (Bad_post_exception("wrong segments of a twitter audio space"))
    
    let try_find_valid_twitter_audio_space_node
        article_node
        =
        let placement_tracking_node =
            article_node
            |>Html_node.try_descendant "div[data-testid='placementTracking']"
        
        match placement_tracking_node with
        |Some placement_tracking_node ->
            placement_tracking_node
            |>Html_node.descendants "button[role='button'] span > span"
            |>List.exists(fun button_node ->
                Html_node.inner_text button_node = "Follow host"
                ||
                Html_node.inner_text button_node = "Play recording"
            )
            |>function
            |false -> None
            |true ->
                Some placement_tracking_node
        |None -> None
    
    let try_find_broken_audio_space_node
        article_node
        =
        let placement_tracking_node =
            article_node
            |>Html_node.try_descendant "div[data-testid='placementTracking']"
        
        match placement_tracking_node with
        |Some placement_tracking_node ->
            placement_tracking_node
            |>Html_node.descendants "span"
            |>List.exists(fun span_node ->
                Html_node.inner_text span_node = "Details not available"
            )
            |>function
            |false -> None
            |true ->
                Some placement_tracking_node
        |None -> None
        
    let try_parse_twitter_audio_space
        article_node
        =
        match
            try_find_valid_twitter_audio_space_node article_node
        with
        |Some audio_space_node ->
            parse_twitter_audio_space audio_space_node
            |>Some
        |None -> None
    
    
    let detach_and_parse_twitter_audio_space
        article_node
        =
        let valid_audio_space =
            try_find_valid_twitter_audio_space_node
                article_node
        
        match valid_audio_space with
        |Some valid_audio_space ->
            valid_audio_space
            |>Html_node.detach_from_parent
            |>ignore
            
            valid_audio_space
            |>parse_twitter_audio_space
            |>Audio_space.Valid_audio_space
            |>Some
        |None ->
            let broken_audio_space_node =
                try_find_broken_audio_space_node
                    article_node
            match broken_audio_space_node with
            |Some _ ->
                Some Broken_audio_space
            |None -> None