namespace rvinowise.twitter

open System
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.parallell.functions
open rvinowise.html_parsing
open FSharp.Data
open rvinowise.twitter



type Html_segments_of_quoted_post = {
    header: Html_node
    message: Html_node
    media: Html_node option
}

type Html_segments_of_main_post = {
    header: Html_node
    message: Html_node
    additional_load: Html_node option
    footer: Html_node
}
//type Parsed_post_footer = {
//    replies_amount: int
//    reposts_amount: int
//    likes_amount: int
//    views_amount: int
//}

type Post_header = {
    author:Twitter_user
    written_at: DateTime
    post_url: string option
}
type Post_stats = {
    replies_amount: int
    likes_amount: int
    reposts_amount: int
    views_amount: int
}

//module Posts_stats =
//    let from_parsed_post (footer:Parsed_post_footer) =
//        {
//            Post_stats.replies_amount = footer.replies_amount
//            likes_amount = footer.likes_amount
//            reposts_amount = footer.reposts_amount
//            views_amount = footer.views_amount
//        }

    


type Abbreviated_message = {
    message: string
    show_more_url: string
}
type Post_message =
    | Abbreviated of Abbreviated_message
    | Full of string

module Post_message =
    let from_html_node (node:Html_node) = //"tweetText"
        let show_mode_node =
            node
            |>Html_node.try_descendant "a[data-testid='tweet-text-show-more-link']"
        
        let message =
            node
            |>Html_node.direct_children
            |>List.map Html_parsing.segment_of_composed_text_as_text
            |>String.concat ""
            
        match show_mode_node with
        |Some show_mode_node ->
            Post_message.Abbreviated {
                message = message
                show_more_url =
                    show_mode_node
                    |>Html_node.attribute_value "href"
            }
        |None ->
            Post_message.Full message
            
type Posted_image = {
    url: string
    description: string
}

module Posted_image =
    let from_html_node (node: Html_node) = //img[]
        {
            Posted_image.url=
                node
                |>Html_node.attribute_value "src"
            description=
                node
                |>Html_node.attribute_value "alt"
        }
        
type Posted_video = {
    url: string
    poster: string
}

module Posted_video =
    let from_html_node (node: Html_node) = //video[]
        {
            Posted_video.url=
                node
                |>Html_node.attribute_value "src"
            poster=
                node
                |>Html_node.attribute_value "poster"
        }
    let from_poster_node (node: Html_node) = //aria-label="Embedded video" data-testid="previewInterstitial"
        {
            Posted_video.url=""
            poster=
                node
                |>Html_node.descendant "img"
                |>Html_node.attribute_value "src"
        }
type Quotable_media_item =
    |Image of Posted_image
    |Video of Posted_video
    
type Quotable_post = {
    author: Twitter_user
    created_at: DateTime
    message: Post_message
    additional_load: Quotable_media_item list
}

type External_url = {
    base_url: string
    gist: string
    obfuscated_url: string
}

type Additional_post_load =
    |Quotation of Quotable_post
    |External_url of string
    |Quotable_additional_load of Quotable_media_item list
    
type Post =
    |Main_post of Quotable_post * Additional_post_load * Post_stats
    |Quoted_post of Quotable_post * Quotable_media_item

type Main_post_from_timeline = {
    post: Quotable_post
    load:Additional_post_load
    stats: Post_stats
}
type Quoted_post_from_timeline = {
    post: Quotable_post
    load: Quotable_media_item
}



module Parse_post_from_timeline =
    
    
    let valuable_part_of_article article_html =
        article_html
        |>Html_node.descend 2
        |>Html_node.direct_children |> Seq.item 1
        |>Html_node.direct_children |> Seq.item 1
   
    
    
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
    
    
    
    
    let parse_post_header header_node =
        let author_name =
            header_node
            |>Html_node.direct_children
            |>List.head
            |>Html_node.descendants "span"
            |>List.head
            |>Html_parsing.readable_text_from_html_segments
            
        let author_handle =
            header_node
            |>Html_node.direct_children
            |>List.item 1
            |>Html_node.descendant "span"
            |>Html_node.inner_text
            |>fun url_with_atsign->url_with_atsign[1..]
            |>User_handle
        
        let datetime_node =
            header_node
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
    
    [<Fact>]
    let ``try parse_post_header``()=
        let parsing_context = Html_parsing.parsing_context()
        let main_header =
            """
            <div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs r-1ny4l3l" id="id__2lloufemgrh" data-testid="User-Name"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/tuftbear86" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div dir="ltr" class="css-901oao r-1awozwy r-18jsvk2 r-6koalj r-37j5jr r-a023e6 r-b88u0q r-rjixqe r-bcqeeo r-1udh08x r-3s2u2q r-qvutc0"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Super Grumpy Bear</span></span></div><div dir="ltr" class="css-901oao r-18jsvk2 r-xoduu5 r-18u37iz r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-1awozwy r-xoduu5 r-poiln3 r-bcqeeo r-qvutc0"></span></div></div></a></div></div><div class="css-1dbjc4n r-18u37iz r-1wbh5a2 r-13hce6t"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/tuftbear86" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div dir="ltr" class="css-901oao css-1hf3ou5 r-14j79pv r-18u37iz r-37j5jr r-1wvb978 r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">@tuftbear86</span></div></a></div><div dir="ltr" aria-hidden="true" class="css-901oao r-14j79pv r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-s1qlax r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">·</span></div><div class="css-1dbjc4n r-18u37iz r-1q142lx"><a href="/tuftbear86/status/1703597157826093349" dir="ltr" aria-label="23 hours ago" role="link" class="css-4rbku5 css-18t94o4 css-901oao r-14j79pv r-1loqt21 r-xoduu5 r-1q142lx r-1w6e6rj r-37j5jr r-a023e6 r-16dba41 r-9aw3ui r-rjixqe r-bcqeeo r-3s2u2q r-qvutc0"><time datetime="2023-09-18T02:29:48.000Z">23h</time></a></div></div></div></div>
            """
            |>Html_node.from_text_and_context parsing_context
            |>parse_post_header
        let quoted_header =
            """
            <div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs r-1ny4l3l" data-testid="User-Name"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div dir="ltr" class="css-901oao r-1awozwy r-18jsvk2 r-6koalj r-37j5jr r-a023e6 r-b88u0q r-rjixqe r-bcqeeo r-1udh08x r-3s2u2q r-qvutc0"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Star Trek Minus Context</span></span></div><div dir="ltr" class="css-901oao r-18jsvk2 r-xoduu5 r-18u37iz r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-1awozwy r-xoduu5 r-poiln3 r-bcqeeo r-qvutc0"></span></div></div></div></div></div><div class="css-1dbjc4n r-18u37iz r-1wbh5a2 r-13hce6t"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><div tabindex="-1" class="css-1dbjc4n r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div dir="ltr" class="css-901oao css-1hf3ou5 r-14j79pv r-18u37iz r-37j5jr r-1wvb978 r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">@NoContextTrek</span></div></div></div><div dir="ltr" aria-hidden="true" class="css-901oao r-14j79pv r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-s1qlax r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">·</span></div><div class="css-1dbjc4n r-18u37iz r-1q142lx"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div dir="ltr" aria-label="Sep 18" class="css-901oao r-14j79pv r-xoduu5 r-1q142lx r-1w6e6rj r-37j5jr r-a023e6 r-16dba41 r-9aw3ui r-rjixqe r-bcqeeo r-3s2u2q r-qvutc0"><time datetime="2023-09-18T02:12:52.000Z">Sep 18</time></div></div></div></div></div></div>
            """
            |>Html_node.from_text_and_context parsing_context
            |>parse_post_header
        let main_header_blue_mark =
            """
            <div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs r-1ny4l3l" id="id__gbii77jt59k" data-testid="User-Name"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/balajis" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div dir="ltr" class="css-901oao r-1awozwy r-18jsvk2 r-6koalj r-37j5jr r-a023e6 r-b88u0q r-rjixqe r-bcqeeo r-1udh08x r-3s2u2q r-qvutc0"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Balaji</span></span></div><div dir="ltr" class="css-901oao r-18jsvk2 r-xoduu5 r-18u37iz r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-1awozwy r-xoduu5 r-poiln3 r-bcqeeo r-qvutc0"><svg viewBox="0 0 22 22" aria-label="Verified account" role="img" class="r-1cvl2hr r-4qtqp9 r-yyyyoo r-1xvli5t r-9cviqr r-f9ja8p r-og9te1 r-bnwqim r-1plcrui r-lrvibr" data-testid="icon-verified"><g><path d="M20.396 11c-.018-.646-.215-1.275-.57-1.816-.354-.54-.852-.972-1.438-1.246.223-.607.27-1.264.14-1.897-.131-.634-.437-1.218-.882-1.687-.47-.445-1.053-.75-1.687-.882-.633-.13-1.29-.083-1.897.14-.273-.587-.704-1.086-1.245-1.44S11.647 1.62 11 1.604c-.646.017-1.273.213-1.813.568s-.969.854-1.24 1.44c-.608-.223-1.267-.272-1.902-.14-.635.13-1.22.436-1.69.882-.445.47-.749 1.055-.878 1.688-.13.633-.08 1.29.144 1.896-.587.274-1.087.705-1.443 1.245-.356.54-.555 1.17-.574 1.817.02.647.218 1.276.574 1.817.356.54.856.972 1.443 1.245-.224.606-.274 1.263-.144 1.896.13.634.433 1.218.877 1.688.47.443 1.054.747 1.687.878.633.132 1.29.084 1.897-.136.274.586.705 1.084 1.246 1.439.54.354 1.17.551 1.816.569.647-.016 1.276-.213 1.817-.567s.972-.854 1.245-1.44c.604.239 1.266.296 1.903.164.636-.132 1.22-.447 1.68-.907.46-.46.776-1.044.908-1.681s.075-1.299-.165-1.903c.586-.274 1.084-.705 1.439-1.246.354-.54.551-1.17.569-1.816zM9.662 14.85l-3.429-3.428 1.293-1.302 2.072 2.072 4.4-4.794 1.347 1.246z"></path></g></svg></span></div></div></a></div></div><div class="css-1dbjc4n r-18u37iz r-1wbh5a2 r-13hce6t"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/balajis" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div dir="ltr" class="css-901oao css-1hf3ou5 r-14j79pv r-18u37iz r-37j5jr r-1wvb978 r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">@balajis</span></div></a></div><div dir="ltr" aria-hidden="true" class="css-901oao r-14j79pv r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-s1qlax r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">·</span></div><div class="css-1dbjc4n r-18u37iz r-1q142lx"><a href="/balajis/status/1703853282639450382" dir="ltr" aria-label="7 hours ago" role="link" class="css-4rbku5 css-18t94o4 css-901oao r-14j79pv r-1loqt21 r-xoduu5 r-1q142lx r-1w6e6rj r-37j5jr r-a023e6 r-16dba41 r-9aw3ui r-rjixqe r-bcqeeo r-3s2u2q r-qvutc0"><time datetime="2023-09-18T19:27:33.000Z">7h</time></a></div></div></div></div>
            """
            |>Html_node.from_text_and_context parsing_context
            |>parse_post_header
        ()
        
    
    let parse_post_main_message message_node =
        message_node
        |>Html_node.descendant "div[data-testid='tweetText']"
        |>Html_parsing.readable_text_from_html_segments
    
    
    let parse_media_from_additional_load load_node =
        
        let posted_images =
            load_node
            |>Html_node.descendants "div[data-testid='tweetPhoto'] > img"
            |>List.map (fun image_node ->
                image_node
                |>Posted_image.from_html_node
                |>Quotable_media_item.Image
            )
        
        let posted_videos =
            load_node
            |>Html_node.descendants "div[data-testid='videoComponent'] video"
            |>List.map (fun video_node ->
                video_node
                |>Posted_video.from_html_node
                |>Quotable_media_item.Video
            )
        
        posted_videos
        |>List.append posted_images
    
    
    let html_segments_of_quoted_post quotation_html = //role=link
        let header =
            quotation_html
            |>Html_node.descend 1
            |>Html_node.descendant "div[data-testid='User-Name']"
        
        let media =
            quotation_html
            |>Html_node.try_descendant "div[data-testid='testCondensedMedia']"
            
            
        let message =
            quotation_html
            |>Html_node.descendant "div[data-testid='tweetText']"
            
        {
            Html_segments_of_quoted_post.header=header
            media=media
            message=message
        }
    
    let html_segments_of_main_post article_html =
        
        let post_elements = 
            valuable_part_of_article article_html
            |>Html_node.direct_children
        
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
            |>Seq.skip 1
            |>Seq.take (Seq.length post_elements - 2)
        
        let message =
            body_elements
            |>Seq.head
            |>Html_node.descendant "div[data-testid='tweetText']"
            
        let additional_load =
            body_elements
            |>Seq.tryItem 1
            
        {
            header = header
            message = message
            additional_load = additional_load
            footer = footing_with_stats
        }
    
    let external_source_details source_node =
        
    
    let parse_external_source_from_additional_load load_node =
        let external_source_node =
            load_node
            |>Html_node.try_descendant "div[data-testid='card.wrapper']"
            
        match external_source_node with
        |Some external_source_node ->
            let small_poster_css = "> div[data-testid='card.layoutSmall.media']"
            let large_poster_css = "> div[data-testid='card.layoutLarge.media']"
            let poster_node =
                external_source_node
                |>Html_node.try_descendant small_poster_css
                |>Option.defaultWith (fun()->
                    external_source_node
                    |>Html_node.try_descendant large_poster_css
                )
            let source_link_node =
                external_source_node
                |>Html_node.direct_children_except small_poster_css
                |>Html_node.should_be_single
                |>Html_node.descendant "a[role='link']"
            
            let details_segments =
                source_link_node
                |>Html_node.descendant "div[data-testid='card.layoutSmall.detail']"
                |>Html_node.direct_children
            
            details_segments
            |>Seq.head
            |>Html_node.inner_text
            
            {
                External_url.base_url =
                    source_link_node
                    
                obfuscated_url=
                    source_link_node
                    |>Html_node.attribute_value "href"
                    
            }
        |None -> None
    
    let parse_quoted_post_from_additional_load load_node =
        let quoted_message_node =
            load_node
            |>Html_node.try_descendant "div[data-testid='tweetText']"
            
        match quoted_message_node with
        |Some quoted_message_node ->
            let quoted_post_node =
                quoted_message_node
                |>Html_node.ancestors "div[role='link']"
                |>List.head
           
            let quoted_post_segments =
                html_segments_of_quoted_post
                    quoted_message_node
            
            let quoted_header =
                parse_post_header quoted_post_segments.header
            
            let quoted_message =
                Post_message.from_html_node
                    quoted_message_node
            
            let quoted_media_items =
                match quoted_post_segments.media with
                |Some quoted_media ->
                    let quoted_images =
                        quoted_media
                        |>Html_node.descendants "img"
                        |>List.map (Posted_image.from_html_node>>Quotable_media_item.Image)
                    let quoted_videos =
                        quoted_media
                        |>Html_node.descendants "div[aria-label='Embedded video']"
                        |>List.map (Posted_video.from_poster_node>>Quotable_media_item.Video)
                    quoted_images@quoted_videos
                |None ->[]
                
            
            Some {
                author = quoted_header.author
                created_at = quoted_header.written_at
                message = quoted_message
                additional_load = quoted_media_items
            }
                    
        |None -> None
    
    
    let parse_additional_load_from_its_node additional_load_node =
        
        let media_load =
            parse_media_from_additional_load
                additional_load_node
                
        let external_url_load =
            parse_external_source_from_additional_load
                additional_load_node
        
        let quoted_source =
            parse_quoted_post_from_additional_load
                additional_load_node
            
        
        let quoted_audio_call =
            additional_load_node
            |>Html_node.descendants "div[data-testid='placementTracking']"
        
        match
            media_load,
            external_url_load,
            quoted_source
        with 
        | _,Some external_url_node,_ ->
            let cover_image_node =
                external_url_node
                |>Html_node.descendants "img[alt='Content cover image']"
            
            let urls_to_external_source =
                external_url_node
                |>Html_node.descendants "div[role='link']"
                
            if (has_different_items urls_to_external_source) then
                (report_external_source_having_different_links
                    additional_load_node
                    urls_to_external_source)
            
            match cover_image_node,urls_to_external_source with
            |_,external_url::rest_urls ->
                external_url
                |>Additional_post_load.External_url
                |>Some
            |image_node,_->
                image_node
                |>Additional_post_load.Images
                |>Some
                
        |image_node::rest_image_nodes,_,_->
            image_node::rest_image_nodes
            |>Additional_post_load.Images
            |>Some
        | _,_,Some quoted_source_node ->
            
            let show_more_button =
                quoted_source_node
                |>Html_node.descendant "a[data-testid='tweet-text-show-more-link']"
            
            let quoted_header =
                quoted_source_node
                |>Html_node.descendant "div[data-testid='User-Name']"
            
            let photos_in_quotation_node =
                quoted_source_node
                |>Html_node.descendants "div[data-testid='tweetPhoto']"
            
            let quoted_photos_explanation =
                photos_in_quotation_node
                |>List.map (Html_node.attribute_value "aria-label")
            
            quoted_source_node
            |>Additional_post_load.Quotation
            |>Some
        
            
        |[],None,None ->
            None
        
    
    
    
    let parse_post_footer footer_node =
        let number_inside_footer_element node =
            node
            |>Html_node.descendant "span[data-testid='app-text-transition-container'] > span > span"
            |>Html_node.inner_text
            |>Parsing_twitter_datatypes.parse_abbreviated_number
        
        {
            Post_stats.replies_amount=
                footer_node
                |>Html_node.direct_children
                |>Seq.head
                |>number_inside_footer_element
            reposts_amount=
                footer_node
                |>Html_node.direct_children
                |>Seq.item 1
                |>number_inside_footer_element
            likes_amount=
                footer_node
                |>Html_node.direct_children
                |>Seq.item 2
                |>number_inside_footer_element
            views_amount=
                footer_node
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
    let is_post_quoting_another_source body_elements =
        body_elements
        |>Seq.map (Html_node.descendants "data-testid='tweetText'")
    
    
    let url_from_show_more_button button_show_more =
        button_show_more
        |>Html_node.attribute_value "href"
    
       
    
    
    let additional_load_of_post article_html = ()
        
    
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
            |>Option.map Html_parsing.last_url_segment 
            |>Option.map int64
        
        
        let post_stats =
            parse_post_footer post_html_segments.footer

        let message = 
            post_html_segments.message
            |>Html_parsing.readable_text_from_html_segments
        
        let additional_load =
            post_html_segments.additional_load
            |>Option.bind parse_additional_load_from_its_node
        
        Main_post
        {
            Quotable_post.author =parsed_header.author
            created_at=parsed_header.written_at
            message = ""
            additional_load = additional_load
        }
            id = post_id
            auhtor = parsed_header.author
            created_at = parsed_header.written_at
            message = message
            additional_load = additional_load
            stats=post_stats
        
        
        
    [<Fact>]
    let ``try from html``()=
        let test = 
            """<article aria-labelledby="id__nctub98pzw id__zsh96bwzskr id__k4x5213cygh id__2m8zf3duum4 id__zzivmgx42r id__6ju7w764q0u id__3k6v9p30n1u id__64sb18hog6 id__2ysmemzb8f9 id__aevhdniyhao id__qvb3hgypdo id__i51h7gycqgo id__3nul9j8x5fo id__8yzoqdylrqq id__nsih0cl4mq8 id__hscm9p08o8k id__q334pveebj id__3dsuyaltmxp id__p8ojm1g8pn" role="article" tabindex="0" class="css-1dbjc4n r-1loqt21 r-18u37iz r-1ny4l3l r-1udh08x r-1qhn6m8 r-i023vh r-o7ynqc r-6416eg" data-testid="tweet"><div class="css-1dbjc4n r-eqz5dr r-16y2uox r-1wbh5a2"><div class="css-1dbjc4n r-16y2uox r-1wbh5a2 r-1ny4l3l"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-1iusvr4 r-16y2uox r-ttdzmv"></div></div></div><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-1awozwy r-onrtq4 r-18kxxzh r-1b7u577"><div class="css-1dbjc4n" data-testid="Tweet-User-Avatar"><div class="css-1dbjc4n r-18kxxzh r-1wbh5a2 r-13qz1uu"><div class="css-1dbjc4n r-1adg3ll r-bztko3" data-testid="UserAvatar-Container-DanilaImmortal" style="height: 40px; width: 40px;"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-1adg3ll r-1pi2tsx r-1wyvozj r-bztko3 r-u8s1d r-1v2oles r-desppf r-13qz1uu"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-ggadg3 r-1udh08x r-u8s1d r-8jfcpp" style="height: calc(100% - -4px); width: calc(100% - -4px);"><a href="/DanilaImmortal" aria-hidden="true" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1niwhzg r-1loqt21 r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1niwhzg r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-14lw9ot r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-14lw9ot r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1adg3ll r-1udh08x" style=""><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div aria-label="" class="css-1dbjc4n r-1p0dtai r-1mlwlqe r-1d2f490 r-1udh08x r-u8s1d r-zchlnj r-ipm5af r-417010"><div class="css-1dbjc4n r-1niwhzg r-vvn4in r-u6sd8q r-4gszlv r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw" style="background-image: url(&quot;https://pbs.twimg.com/profile_images/1695579644282441728/w_YNIGnR_bigger.jpg&quot;);"></div><img alt="" draggable="true" src="https://pbs.twimg.com/profile_images/1695579644282441728/w_YNIGnR_bigger.jpg" class="css-9pa8cd"></div></div></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-12181gd r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"></div></div></a></div></div></div></div></div></div></div></div><div class="css-1dbjc4n r-1iusvr4 r-16y2uox r-1777fci r-kzbkwu"><div class="css-1dbjc4n r-zl2h9q"><div class="css-1dbjc4n r-k4xj1c r-18u37iz r-1wtj0ep"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs r-1ny4l3l" id="id__zzivmgx42r" data-testid="User-Name"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/DanilaImmortal" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div dir="ltr" class="css-901oao r-1awozwy r-18jsvk2 r-6koalj r-37j5jr r-a023e6 r-b88u0q r-rjixqe r-bcqeeo r-1udh08x r-3s2u2q r-qvutc0"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Danila Immortalist</span></span></div><div dir="ltr" class="css-901oao r-18jsvk2 r-xoduu5 r-18u37iz r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-1awozwy r-xoduu5 r-poiln3 r-bcqeeo r-qvutc0"><svg viewBox="0 0 22 22" aria-label="Verified account" role="img" class="r-1cvl2hr r-4qtqp9 r-yyyyoo r-1xvli5t r-9cviqr r-f9ja8p r-og9te1 r-bnwqim r-1plcrui r-lrvibr" data-testid="icon-verified"><g><path d="M20.396 11c-.018-.646-.215-1.275-.57-1.816-.354-.54-.852-.972-1.438-1.246.223-.607.27-1.264.14-1.897-.131-.634-.437-1.218-.882-1.687-.47-.445-1.053-.75-1.687-.882-.633-.13-1.29-.083-1.897.14-.273-.587-.704-1.086-1.245-1.44S11.647 1.62 11 1.604c-.646.017-1.273.213-1.813.568s-.969.854-1.24 1.44c-.608-.223-1.267-.272-1.902-.14-.635.13-1.22.436-1.69.882-.445.47-.749 1.055-.878 1.688-.13.633-.08 1.29.144 1.896-.587.274-1.087.705-1.443 1.245-.356.54-.555 1.17-.574 1.817.02.647.218 1.276.574 1.817.356.54.856.972 1.443 1.245-.224.606-.274 1.263-.144 1.896.13.634.433 1.218.877 1.688.47.443 1.054.747 1.687.878.633.132 1.29.084 1.897-.136.274.586.705 1.084 1.246 1.439.54.354 1.17.551 1.816.569.647-.016 1.276-.213 1.817-.567s.972-.854 1.245-1.44c.604.239 1.266.296 1.903.164.636-.132 1.22-.447 1.68-.907.46-.46.776-1.044.908-1.681s.075-1.299-.165-1.903c.586-.274 1.084-.705 1.439-1.246.354-.54.551-1.17.569-1.816zM9.662 14.85l-3.429-3.428 1.293-1.302 2.072 2.072 4.4-4.794 1.347 1.246z"></path></g></svg></span></div></div></a></div></div><div class="css-1dbjc4n r-18u37iz r-1wbh5a2 r-13hce6t"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/DanilaImmortal" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div dir="ltr" class="css-901oao css-1hf3ou5 r-14j79pv r-18u37iz r-37j5jr r-1wvb978 r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">@DanilaImmortal</span></div></a></div><div dir="ltr" aria-hidden="true" class="css-901oao r-14j79pv r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-s1qlax r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">·</span></div><div class="css-1dbjc4n r-18u37iz r-1q142lx"><a href="/DanilaImmortal/status/1700556920451383555" dir="ltr" aria-label="Sep 9" role="link" class="css-4rbku5 css-18t94o4 css-901oao r-14j79pv r-1loqt21 r-xoduu5 r-1q142lx r-1w6e6rj r-37j5jr r-a023e6 r-16dba41 r-9aw3ui r-rjixqe r-bcqeeo r-3s2u2q r-qvutc0"><time datetime="2023-09-09T17:08:59.000Z">Sep 9</time></a></div></div></div></div></div></div><div class="css-1dbjc4n r-1jkjb"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1cmwbt1 r-1wtj0ep"><div class="css-1dbjc4n r-1awozwy r-6koalj r-18u37iz"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="More" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="caret"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M3 12c0-1.1.9-2 2-2s2 .9 2 2-.9 2-2 2-2-.9-2-2zm9 2c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm7 0c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div><div class="css-1dbjc4n"><div dir="auto" lang="en" class="css-901oao r-18jsvk2 r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-bnwqim r-qvutc0" id="id__qvb3hgypdo" data-testid="tweetText"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">We claim that human life holds the highest value, yet when it comes to preserving human life, putting things on ice somehow isn't the default setting. It's not just about freezing people; it's a litmus test for how much we truly value human life.

At the very brink, the pivotal…</span><span tabindex="0" class="css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0" data-testid="tweet-text-show-more-link" role="link"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">&nbsp;</span><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Show more</span></span></div></div><div aria-labelledby="id__vfbxucrpmpb id__la9nyr5z25" class="css-1dbjc4n r-1ssbvtb r-1s2bzr4" id="id__3nul9j8x5fo"><div class="css-1dbjc4n r-9aw3ui"><div class="css-1dbjc4n"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-1ets6dv r-1867qdf r-1phboty r-rs99b7 r-1ny4l3l r-1udh08x r-o7ynqc r-6416eg"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-16y2uox r-1pi2tsx r-13qz1uu"><a href="/DanilaImmortal/status/1700556920451383555/photo/1" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1pi2tsx r-1ny4l3l"><div class="css-1dbjc4n r-1adg3ll r-1udh08x"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 56.044%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div aria-label="Image" class="css-1dbjc4n r-1p0dtai r-1mlwlqe r-1d2f490 r-p1pxzi r-11wrixw r-61z16t r-1mnahxq r-1udh08x r-u8s1d r-zchlnj r-ipm5af r-417010" data-testid="tweetPhoto"><div class="css-1dbjc4n r-1niwhzg r-vvn4in r-u6sd8q r-4gszlv r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw" style="background-image: url(&quot;https://pbs.twimg.com/media/F5mXEZNXsAAXOQ6?format=jpg&amp;name=900x900&quot;);"></div><img alt="Image" draggable="true" src="https://pbs.twimg.com/media/F5mXEZNXsAAXOQ6?format=jpg&amp;name=900x900" class="css-9pa8cd"></div></div></div></a></div></div></div></div></div></div></div><div class="css-1dbjc4n"><div aria-label="2 replies, 12 reposts, 47 likes, 2 bookmarks, 3053 views" role="group" class="css-1dbjc4n r-1kbdv8c r-18u37iz r-1wtj0ep r-1s2bzr4 r-hzcoqn" id="id__p8ojm1g8pn"><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-label="2 Replies. Reply" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="reply"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M1.751 10c0-4.42 3.584-8 8.005-8h4.366c4.49 0 8.129 3.64 8.129 8.13 0 2.96-1.607 5.68-4.196 7.11l-8.054 4.46v-3.69h-.067c-4.49.1-8.183-3.51-8.183-8.01zm8.005-6c-3.317 0-6.005 2.69-6.005 6 0 3.37 2.77 6.08 6.138 6.01l.351-.01h1.761v2.3l5.087-2.81c1.951-1.08 3.163-3.13 3.163-5.36 0-3.39-2.744-6.13-6.129-6.13H9.756z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1e081e0 r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">2</span></span></span></div></div></div></div><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="12 reposts. Repost" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="retweet"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M4.5 3.88l4.432 4.14-1.364 1.46L5.5 7.55V16c0 1.1.896 2 2 2H13v2H7.5c-2.209 0-4-1.79-4-4V7.55L1.432 9.48.068 8.02 4.5 3.88zM16.5 6H11V4h5.5c2.209 0 4 1.79 4 4v8.45l2.068-1.93 1.364 1.46-4.432 4.14-4.432-4.14 1.364-1.46 2.068 1.93V8c0-1.1-.896-2-2-2z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1e081e0 r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">12</span></span></span></div></div></div></div><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-label="47 Likes. Like" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="like"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M16.697 5.5c-1.222-.06-2.679.51-3.89 2.16l-.805 1.09-.806-1.09C9.984 6.01 8.526 5.44 7.304 5.5c-1.243.07-2.349.78-2.91 1.91-.552 1.12-.633 2.78.479 4.82 1.074 1.97 3.257 4.27 7.129 6.61 3.87-2.34 6.052-4.64 7.126-6.61 1.111-2.04 1.03-3.7.477-4.82-.561-1.13-1.666-1.84-2.908-1.91zm4.187 7.69c-1.351 2.48-4.001 5.12-8.379 7.67l-.503.3-.504-.3c-4.379-2.55-7.029-5.19-8.382-7.67-1.36-2.5-1.41-4.86-.514-6.67.887-1.79 2.647-2.91 4.601-3.01 1.651-.09 3.368.56 4.798 2.01 1.429-1.45 3.146-2.1 4.796-2.01 1.954.1 3.714 1.22 4.601 3.01.896 1.81.846 4.17-.514 6.67z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1e081e0 r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">47</span></span></span></div></div></div></div><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><a href="/DanilaImmortal/status/1700556920451383555/analytics" aria-label="3053 views. View post analytics" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M8.75 21V3h2v18h-2zM18 21V8.5h2V21h-2zM4 21l.004-10h2L6 21H4zm9.248 0v-7h2v7h-2z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1e081e0 r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">3,053</span></span></span></div></div></a></div><div class="css-1dbjc4n" style="display: inline-grid; justify-content: inherit; transform: rotate(0deg) scale(1) translate3d(0px, 0px, 0px); -webkit-box-pack: inherit;"><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="Share post" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M12 2.59l5.7 5.7-1.41 1.42L13 6.41V16h-2V6.41l-3.3 3.3-1.41-1.42L12 2.59zM21 15l-.02 3.51c0 1.38-1.12 2.49-2.5 2.49H5.5C4.11 21 3 19.88 3 18.5V15h2v3.5c0 .28.22.5.5.5h12.98c.28 0 .5-.22.5-.5L19 15h2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div></div></article>"""
            |>Html_node.from_text
            |>parse_main_twitter_post
        ()
        
   
    let is_post_replying_to_another_post post_body =
        ()
    
    