namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter



module Parse_media =
    
   
        
    let parse_media_from_small_quoted_post
        ``node with all img of the quotation`` //shouldn't include the img of its main post
        =
        ``node with all img of the quotation``
        |>Html_node.descendants "div[data-testid='tweetPhoto']"
        |>List.map (fun media_item_node->
            media_item_node
            |>Html_node.try_descendant "div[aria-label='Embedded video'][data-testid='previewInterstitial']"
            |>function
            |Some video_node ->
                video_node
                |>Posted_video.from_poster_node
                |>Media_item.Video_poster
            |None ->
                media_item_node
                |>Html_node.descendant "img"
                |>Posted_image.from_html_image_node
                |>Media_item.Image
        )
    
    let parse_media_from_large_layout //the layout of either a main-post, or a big-quotation
        load_node
        = 
        
        let posted_images =
            try
                load_node
                |>Html_node.descendants "div[data-testid='tweetPhoto']"
                |>List.filter(fun footage_node ->
                    footage_node
                    |>Html_node.try_descendant "div[data-testid='videoComponent']"
                    |>function
                    |Some _ ->false
                    |None->true
                )
                |>List.choose (Html_node.try_descendant "img")
                |>List.map (fun image_node ->
                    image_node
                    |>Posted_image.from_html_image_node
                    |>Media_item.Image
                )
            with
            | :? ArgumentException as e->
                raise <| Bad_post_exception("can't parse posted images from large layout", load_node)
        
        // videos are also part of data-testid="tweetPhoto" node, like images
        let posted_videos =
            load_node
            |>Html_node.descendants "div[data-testid='videoComponent'] video"
            |>List.map (fun video_node ->
                video_node
                |>Posted_video.from_html_video_node
                |>Media_item.Video_poster
            )
        let posted_videos_as_images =
            load_node
            |>Html_node.descendants "div[aria-label='Embedded video'][data-testid='previewInterstitial']"
            |>List.map (fun video_node ->
                video_node
                |>Posted_video.from_div_node_with_image
                |>Media_item.Video_poster
            )
            
        posted_videos
        |>List.append posted_videos_as_images
        |>List.append posted_images
    
    let parse_media_items_from_quotation html_node =
        html_node
        |>Html_node.try_descendant "div[data-testid='testCondensedMedia']"
        |>function
        |Some _->
            parse_media_from_small_quoted_post
                html_node
        |None ->
            parse_media_from_large_layout
                html_node