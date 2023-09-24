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


exception Bad_post_structure_exception of string*Html_node
exception Html_parsing_fail

type Html_segments_of_quoted_post = { //which quoted post? it can be big or small, with different layouts
    header: Html_node
    message: Html_node
    media: Html_node option
}

type Html_segments_of_main_post = {
    header: Html_node
    message: Html_node
    media_load: Html_node option
    quotation_load: Html_node option
    footer: Html_node
}

type Post_header = {
    author:Twitter_user
    written_at: DateTime
    post_url: string option //quotations don't have their URL, only main posts do
}
type Post_stats = {
    replies_amount: int
    likes_amount: int
    reposts_amount: int
    views_amount: int
}


type Abbreviated_message = {
    message: string
    show_more_url: string
}
type Post_message =
    | Abbreviated of Abbreviated_message
    | Full of string

module Post_message =
    let from_html_node (node:Html_node) = //"tweetText"
        let show_more_css = "a[data-testid='tweet-text-show-more-link']"
        let show_mode_node =
            node
            |>Html_node.try_descendant show_more_css
        
        let message =
            node
            |>Html_node.direct_children
            |>List.filter (Html_node.matches show_more_css >> not)
            |>List.map Html_parsing.segment_of_composed_text_as_text
            |>String.concat ""
            |>Html_parsing.standartize_linebreaks
            
        match show_mode_node with
        |Some show_mode_node ->
            Post_message.Abbreviated {
                message = message
                show_more_url =
                    show_mode_node
                    |>Html_node.attribute_value "href"
                    |>Twitter_settings.absolute_url
            }
        |None ->
            Post_message.Full message
            
type Posted_image = {
    url: string
    description: string
}

module Posted_image =
    let from_html_image_node (node: Html_node) = //img[]
        {
            Posted_image.url=
                node
                |>Html_node.attribute_value "src"
            description=
                node
                |>Html_node.attribute_value "alt"
        }
        

module Posted_video =
    let from_html_video_node
        (
            ``node of video[]``
                : Html_node
        )
        =
        ``node of video[]``
        |>Html_node.attribute_value "poster"
    let from_poster_node
        (
            ``node with single aria-label="Embedded video" data-testid="previewInterstitial"``
                : Html_node
        )
        = 
        ``node with single aria-label="Embedded video" data-testid="previewInterstitial"``
        |>Html_node.descendant "img"
        |>Html_node.attribute_value "src"
type Media_item =
    |Image of Posted_image
    |Video_poster of string


type Reply_target = User_handle * int64 option
type Quotable_post = {
    author: Twitter_user
    created_at: DateTime
    replying_to: Reply_target option
    message: Post_message
    media_load: Media_item list
}

type External_url = {
    base_url: string
    page: string
    message: string
    (* the actual referenced URL is hidden, need to click in order to see it *)
    obfuscated_url: string 
}

type External_source =
    |External_url of External_url
    |Quotation of Quotable_post

type Main_post = {
    id: int64
    quotable_core: Quotable_post
    external_source: External_source option
    stats: Post_stats
}

type Post =
    |Main_post of Main_post
    |Quoted_post of Quotable_post * Media_item list



(*
a problem: it's not clear, what html-node should be sent to a parsing function, 
which parses a certain part of the html-layout.

1: this html-node should not have other children with similar tags/attributes to those which are needed from the parsed part,
so that the needed information could be found unambiguously.

2: it makes sense to find children by unique css-identifiers, rather than in absolute terms relative to their parents, 
so that different parts of the html-hierarchy could be sent as parameters 

 *)
module Parse_post_from_timeline =
    
    let valuable_segments_of_post
        article_html //article[data-testid="tweet"] 
        = //header / body / stats
        article_html
        |>Html_node.descend 2
        |>Html_node.direct_children |> Seq.item 1
        |>Html_node.direct_children |> Seq.item 1
        |>Html_node.direct_children
   
    
    let post_header_node node =
        node
        |>Html_node.descendants "data-testid='User-Name'"
    
    let has_different_items items =
        if Seq.length items < 2 then
            false
        else
            items
            |>Seq.forall((=) (Seq.head items))
            |>not
    
    let report_external_source_having_different_links 
        additional_load_node
        urls_to_external_source
        =
        let html =
            additional_load_node
            |>Html_node.as_html_string
            
        let urls =
            urls_to_external_source
            |>Seq.map Html_node.as_html_string
            |>String.concat "; "
            
        $"additional load of tweet has different links. post:\n{html}\nlinks: {urls}"    
        |>Log.error
        |>ignore
    
    
    
    
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
    
    
    let html_segments_of_quoted_post
        ``node with role=link of quotation`` 
        =
        let header =
            ``node with role=link of quotation``
            |>Html_node.descend 1
            |>Html_node.descendant "div[data-testid='User-Name']"
        
        //only for a small-quotation, but there's also big-quotations!
        let media =
            ``node with role=link of quotation``
            |>Html_node.try_descendant "div[data-testid='testCondensedMedia']"
            
        let message =
            ``node with role=link of quotation``
            |>Html_node.descendant "div[data-testid='tweetText']"
            
        {
            Html_segments_of_quoted_post.header=header
            media=media
            message=message
        }
    
    
    
    let try_reply_header_segment segment =
        segment
        |>Html_node.descend 1
        |>fun node ->
            if
                node.FirstChild.NodeType = NodeType.Text &&
                node.FirstChild.TextContent = "Replying to "
            then
                Some segment
            else None
            
    let try_message_segment segment =
        segment
        |>Html_node.try_descendant "div[data-testid='tweetText']"
    
    let try_external_source_segment segment =
        segment
        |>Html_node.try_descendant "div[data-testid='card.wrapper']"
    
    let try_quoted_post_segment segment =
        segment
        |>Html_node.try_descendant "div[data-testid='tweetText']"
    
    let html_segments_of_main_post
        ``node with article[test-id='tweet']``
        = 
        let post_elements = 
            valuable_segments_of_post ``node with article[test-id='tweet']``
        
        let header =
            post_elements
            |>Seq.head
            |>Html_node.descendant "div[data-testid='User-Name']"
        
        let footing_with_stats =
            post_elements
            |>Seq.last
            |>Html_node.descend 1
            
        let body_elements =
            post_elements
            |>Seq.skip 1|>Seq.take (Seq.length post_elements - 2)
            
        body_elements
        |>Seq.
        
        
        let reply_header =
            post_elements
            |>Seq.tryItem 1
            |>Html_node.descend 1
            |>fun node ->
                if
                    node.FirstChild.NodeType = NodeType.Text &&
                    node.FirstChild.TextContent = "Replying to "
                then
                    post_elements|>Seq.head|>Some
                else None
            
        
        let message =
            post_elements
            |>Seq.tryItem 1
            |>Html_node.descendant "div[data-testid='tweetText']"
            
        let additional_load =
            post_elements
            |>Seq.tryItem 2
        
        let media_load,quotation_load = //direct children of the additional_load_node (2nd body child)
            match additional_load with
            |Some additional_load ->
                match
                    additional_load
                    |>Html_node.direct_children    
                with
                |[media;quotation] ->
                    Some media,Some quotation
                |[media_or_quotation] ->
                    media_or_quotation
                    |>Html_node.matches "div[data-testid='card.wrapper']"
                    |>function
                    |true ->
                        None,Some media_or_quotation
                    |false->
                        media_or_quotation
                        |>Html_node.try_descendant "div[data-testid='tweetText']"
                        |>function
                        |Some quoted_message ->
                            None,Some media_or_quotation
                        |None -> Some media_or_quotation,None
                        
                | _-> raise (Bad_post_structure_exception (
                    "additional post load has >2 children",
                    ``node with article[test-id='tweet']``
                    ))
            |None -> None,None     
        
        
        
        {
            header = header
            message = message
            media_load = media_load
            quotation_load = quotation_load
            footer = footing_with_stats
        }
    
    let details_of_external_source
        ``node with card.layoutSmall.detail or card.layoutLarge.detail``
        =
        let small_detail_css = "div[data-testid='card.layoutSmall.detail']"
        let large_detail_css = "div[data-testid='card.layoutLarge.detail']"
        ``node with card.layoutSmall.detail or card.layoutLarge.detail``
        |>Html_node.try_descendant small_detail_css
        |>function
        |Some detail_node ->
            detail_node
            |>Html_node.direct_children
        |None->
            ``node with card.layoutSmall.detail or card.layoutLarge.detail``
            |>Html_node.descendant large_detail_css
            |>Html_node.direct_children
    
    
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
                |>details_of_external_source
            
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
    
    
    
    
    
        
        
    let parse_quoted_post_from_its_node
        ``node with potential role=link of the quotation``
        = //big or small?
        
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
    
    let long_post_has_show_more_button post_body =
           post_body
           |>Html_node.try_descendant "data-testid='tweet-text-show-more-link'"
           |>function
               |Some _ ->true
               |None->false
    
    
    let url_from_show_more_button button_show_more =
        button_show_more
        |>Html_node.attribute_value "href"
    
       
    let repost_mark article_html =
        article_html
        |>Html_node.try_descendant "span[data-testid='socialContext']"
    
    
    let parse_main_twitter_post article_html =
        
        let post_html_segments =
            html_segments_of_main_post article_html
        
        let parsed_header =
            parse_post_header post_html_segments.header

        let post_id =
            parsed_header.post_url
            |>function
            |Some url ->
                url
                |>Html_parsing.last_url_segment
                |>int64
            |None ->
                ("main post doesn't have its url",article_html)
                |>Bad_post_structure_exception
                |>raise
        
        let reply_target =
            
        
        
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
                message =
                    Post_message.from_html_node
                        post_html_segments.message
                media_load = media_items
            }
            external_source=external_source
            stats=post_stats
        }
        
        

    
    