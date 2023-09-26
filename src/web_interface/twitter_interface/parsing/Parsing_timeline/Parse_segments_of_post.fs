namespace rvinowise.twitter

open System
open AngleSharp.Dom
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.parallell.functions
open rvinowise.html_parsing
open FSharp.Data
open rvinowise.twitter


module Parse_segments_of_post =
    
    let parse_post_header
        ``node of data-testid="User-Name"``
        =
        let author_name =
            ``node of data-testid="User-Name"``
            |>Html_node.direct_children
            |>List.head
            |>Html_node.descendants "span"
            |>List.head
            |>Html_parsing.readable_text_from_html_segments
            
        let author_handle =
            ``node of data-testid="User-Name"``
            |>Html_node.direct_children
            |>List.item 1
            |>Html_node.descendant "span"
            |>Html_node.inner_text
            |>fun url_with_atsign->url_with_atsign[1..]
            |>User_handle
        
        let datetime_node =
            ``node of data-testid="User-Name"``
            |>Html_node.direct_children
            |>List.item 1
            |>Html_node.descendant "time"
        
        let datetime =
            datetime_node
            |>Html_node.attribute_value "datetime"
            |>Parsing_twitter_datatypes.parse_twitter_datetime
        
        let url =
            datetime_node
            |>Html_node.parent
            |>Html_node.try_attribute_value "href"
        
        {
            Post_header.author = {name=author_name;handle=author_handle}
            written_at = datetime
            post_url=url
        }
        
        
    let parse_media_from_small_quoted_post
        ``node with all img of the quotation`` //shouldn't include the img of its main post
        =
        ``node with all img of the quotation``
        |>Html_node.descendants "div[aria-label='Embedded video'][data-testid='previewInterstitial']"
        |>List.map (Posted_video.from_poster_node>>Media_item.Video_poster)
    
    let parse_media_from_large_layout //the layout of either a main-post, or a big-quotation
        (*node with all
            img[]
            and
            video[]
        of the post, excluding a potential quoted post and images outside of the load (e.g. user picture)*)
        load_node
        = 
        
        let posted_images =
            load_node
            |>Html_node.descendants "div[data-testid='tweetPhoto']"
            |>List.filter(fun footage_node ->
                footage_node
                |>Html_node.try_descendant "div[data-testid='videoComponent']"
                |>function
                |Some _ ->false
                |None->true
            )
            |>List.map (Html_node.descendant "img")
            |>List.map (fun image_node ->
                image_node
                |>Posted_image.from_html_image_node
                |>Media_item.Image
            )
        
        // videos are also part of data-testid="tweetPhoto" node, like images
        let posted_videos =
            load_node
            |>Html_node.descendants "div[data-testid='videoComponent'] video"
            |>List.map (fun video_node ->
                video_node
                |>Posted_video.from_html_video_node
                |>Media_item.Video_poster
            )
        
        posted_videos
        |>List.append posted_images
    
    let parse_external_source_from_additional_load
        ``node of potential card.wrapper``
        =
        if
            ``node of potential card.wrapper``
            |>Html_node.matches "div[data-testid='card.wrapper']"
        then
            
            let details_segments =
                ``node of potential card.wrapper``
                |>Find_segments_of_post.details_of_external_source
            
            Some {
                External_url.base_url =
                    details_segments
                    |>Seq.head
                    |>Html_node.descendant ":scope> span"
                    |>Html_parsing.readable_text_from_html_segments
                page=
                    details_segments
                    |>Seq.item 1
                    |>Html_node.descendant ":scope> span"
                    |>Html_parsing.readable_text_from_html_segments
                message=
                    details_segments
                    |>Seq.item 2
                    |>Html_node.descendant ":scope> span"
                    |>Html_parsing.readable_text_from_html_segments
                obfuscated_url=
                    ``node of potential card.wrapper``
                    |>Html_node.descendant "a[role='link']"
                    |>Html_node.attribute_value "href"
                    
            }
        else None
    
     
    let parse_reply_header reply_header =
        reply_header
        |>Html_node.descendant "span"
        |>Html_node.inner_text
        |>User_handle.trim_potential_atsign
        |>User_handle
    
    let parse_reply_target author ``tweetText node`` =
        let reply_target_from_header =
            ``tweetText node``
            |>Html_node.parent
            |>Html_node.direct_children
            |>List.head
            |>fun potential_header ->
                if
                    Find_segments_of_post.is_reply_header potential_header
                then
                    potential_header
                    |>parse_reply_header
                    |>Some
                else
                    None
        
        match reply_target_from_header with
        |Some reply->Some reply
        |None->
            if (
                ``tweetText node``
                |>Html_node.parent
                |>Html_node.direct_children
                |>List.last
                |>Find_segments_of_post.is_mark_of_thread)
            then    
                Some author
            else
                None
        
    let parse_quoted_post_from_its_node
        ``node with potential role=link of the quotation``
        = 
        
        let quoted_message_node =
            ``node with potential role=link of the quotation``
            |>Html_node.try_descendant "div[data-testid='tweetText']"
            
        match quoted_message_node with
        |Some quoted_message_node ->
            let quotation_root_node =
                quoted_message_node
                |>Html_node.ancestors "div[role='link']"
                |>List.head    
                
            let quoted_header =
                quotation_root_node
                |>Html_node.descendant "div[data-testid='User-Name']"
                |>parse_post_header
            
            let reply_target =
                parse_reply_target quoted_header.author.handle quoted_message_node
                |>function
                |Some reply_target_user ->
                    Some (reply_target_user,None)
                |None->None
            
            let quoted_media_items =
                ``node with potential role=link of the quotation``
                |>Html_node.try_descendant "div[data-testid='testCondensedMedia']"
                |>function
                |Some _->
                    parse_media_from_small_quoted_post
                        ``node with potential role=link of the quotation``
                |None ->
                    parse_media_from_large_layout
                        ``node with potential role=link of the quotation``
            
            Some {
                author = quoted_header.author
                created_at = quoted_header.written_at
                reply_target=reply_target
                message =
                    Post_message.from_html_node
                        quoted_message_node
                media_load = quoted_media_items
            }
        
        |None->None            
    
    
    let parse_quoted_source_from_its_node
        ``node with either role=link of quotation, or card.wrapper`` 
        //either a quoted-post, or an external-url
        =
        
        let quoted_post =
            parse_quoted_post_from_its_node
                ``node with either role=link of quotation, or card.wrapper``
                
        let external_url_load =
            parse_external_source_from_additional_load
                ``node with either role=link of quotation, or card.wrapper``
            
        match
            quoted_post,
            external_url_load
        with
        |None, Some external_url ->
            external_url
            |>External_source.External_url 
            |>Some
        |Some quoted_post, None ->
            quoted_post
            |>External_source.Quotation
            |>Some
        | _,_ ->
            Log.error $"""
                a strange composition of the additional_load of a post.
                additional_load_node: {``node with either role=link of quotation, or card.wrapper``}
                quoted_post: {quoted_post}
                external_url_load: {external_url_load}
                """|>ignore
            None
        
    
    
    
    let parse_post_footer
        ``node with all app-text-transition-container of a post``
        =
        let number_inside_footer_element node =
            node
            |>Html_node.try_descendant "span[data-testid='app-text-transition-container'] > span > span"
            |>function
            |None -> 0
            |Some node ->
                node
                |>Html_node.inner_text
                |>Parsing_twitter_datatypes.parse_abbreviated_number
        
        {
            Post_stats.replies_amount=
                ``node with all app-text-transition-container of a post``
                |>Html_node.direct_children
                |>Seq.head
                |>number_inside_footer_element
            reposts_amount=
                ``node with all app-text-transition-container of a post``
                |>Html_node.direct_children
                |>Seq.item 1
                |>number_inside_footer_element
            likes_amount=
                ``node with all app-text-transition-container of a post``
                |>Html_node.direct_children
                |>Seq.item 2
                |>number_inside_footer_element
            views_amount=
                ``node with all app-text-transition-container of a post``
                |>Html_node.direct_children
                |>Seq.item 3
                |>number_inside_footer_element
        }
    
    
        
    let parse_main_twitter_post article_html =
        
        let post_html_segments =
            Find_segments_of_post.html_segments_of_main_post article_html
        
        let parsed_header =
            parse_post_header post_html_segments.header

        let post_id =
            parsed_header.post_url
            |>function
            |Some url ->
                url
                |>Html_parsing.last_url_segment
                |>int64|>Post_id
            |None ->
                ("main post doesn't have its url",article_html)
                |>Bad_post_structure_exception
                |>raise
        
        let reply_target =
            match post_html_segments.reply_header with
            |Some reply -> Some (parse_reply_header reply,None)
            |None->None
            
        
        
        let media_items = 
            match post_html_segments.media_load with
            |Some media -> parse_media_from_large_layout media
            |None->[]
        
        let external_source =
            match post_html_segments.quotation_load with
            |Some quotation -> parse_quoted_source_from_its_node quotation
            |None -> None
        
        let post_stats =
            parse_post_footer post_html_segments.footer
        
        {
            Main_post.id=post_id
            quotable_core = {
                Quotable_post.author =parsed_header.author
                created_at=parsed_header.written_at
                reply_target=reply_target
                message =
                    Post_message.from_html_node
                        post_html_segments.message
                media_load = media_items
            }
            external_source=external_source
            stats=post_stats
        }