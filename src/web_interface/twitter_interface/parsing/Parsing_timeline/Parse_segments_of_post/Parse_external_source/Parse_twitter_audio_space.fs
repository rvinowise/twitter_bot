namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter


module Parse_twitter_audio_space =
    
    
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
    
    let try_find_twitter_audio_space_node
        post_node
        =
        let placement_tracking_node =
            post_node
            |>Html_node.try_descendant "div[data-testid='placementTracking']"
        
        match placement_tracking_node with
        |Some placement_tracking_node ->
            placement_tracking_node
            |>Html_node.descendants "div[role='button'] span > span"
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
        
    let try_parse_twitter_audio_space
        post_node
        =
        match
            try_find_twitter_audio_space_node post_node
        with
        |Some audio_space_node ->
            parse_twitter_audio_space audio_space_node
            |>Some
        |None -> None
    
    
    let detach_and_parse_twitter_audio_space
        post_node
        =
        let audio_space_node =
            try_find_twitter_audio_space_node
                post_node
        
        audio_space_node
        |>Option.map Html_node.detach_from_parent
        |>ignore
        
        audio_space_node
        |>Option.map parse_twitter_audio_space
