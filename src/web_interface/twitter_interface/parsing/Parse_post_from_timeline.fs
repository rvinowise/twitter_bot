﻿namespace rvinowise.twitter

open System
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.parallell.functions
open rvinowise.html_parsing
open FSharp.Data



type post = {
    id: string
    auhtor: User_handle
    created_at: DateTime
    message: string
    replies_amount: int
    likes_amount: int
    reposts_amount: int
    views_amount: int  
}

type Additional_post_load =
    |Quotation of Html_node
    |External_url of Html_node
    |Image_and_quotation of Html_node
    |Images of Html_node
    
type Html_segments_of_post = {
    header: Html_node
    message: Html_node
    load: Html_node option
    footing: Html_node
}

module Parse_post_from_timeline =
    
    
    let valuable_part_of_article article_html =
        article_html
        |>Html_node.descend 2
        |>Html_node.direct_children |> Seq.item 1
        |>Html_node.direct_children |> Seq.item 1
    
    let compound_name_span post_header =
        post_header
        |>Html_node.direct_children
        |>Seq.head
        |>Html_node.descendant "a"
        |>Html_node.descend 2
    
    
    let post_header_node node =
        node
        |>Html_node.descendants "data-testid='User-Name'"
        
    let additional_load_from_its_node additional_load_node =
        let link_node =
            additional_load_node
            |>Html_node.try_descendant "div[role='link']"
        
        let image_preview_nodes =
            additional_load_node
            |>Html_node.descendants "div[data-testid='tweetPhoto']"
        
        let video_node =
            additional_load_node
            |>Html_node.descendants "aria-label='Embedded video' ing[alt='Embedded video']"
        
        let external_url_node =
            additional_load_node
            |>Html_node.try_descendant "data-testid='card.wrapper'"
        
        match link_node, external_url_node with 
        |Some link_node,_ ->
            let quoted_source_node =
                link_node
                |>Html_node.try_descendant "data-testid='tweetText'"
            let image_nodes =
                link_node
                //|>Html_node.descendant "img[alt='Image']"
                |>Html_node.descendants "data-testid='tweetPhoto'"
            
            match quoted_source_node, image_nodes with
            |Some quoted_source_node, [] ->
                additional_load_node
                |>Additional_post_load.Quotation
                |>Some
            |None, image_nodes when (not image_nodes.IsEmpty) ->
                additional_load_node
                |>Additional_post_load.Images
                |>Some
            |None,[]->
                Log.error "a link_node doesn't have quoted_source_node or image_node"
                |>Exception|>raise
            |Some quoted_source_node, image_node::rest_image_nodes ->
                Log.error "a link_node has both quoted_source_node and image_node"
                |>Exception|>raise
        | _,Some external_url_node ->
            external_url_node
            |>Additional_post_load.External_url
            |>Some
        |None,None ->
            None
        
        
    
    
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
        |>Html_node.attributeValue "href"
    
        
    let is_post_quoting_another_source_with_show_more_button body_elements =

        let messages_of_article =
            body_elements
            |>Seq.map (
                Html_parsing.descendants "data-testid='tweetText'"
            )
        if Seq.length messages_of_article > 2 then
            Log.error $"article has {Seq.length messages_of_article} messages in it, expected 2 maximum (the post and its quoted source)"
        
        //a quoted_post, image, image & quoted_post
        let additional_load_node =
            messages_of_article
            |>Seq.tryItem 1
        
        match additional_load_node with
        |Some quoted_source_node ->
            url_of_quoted_source quoted_source_node
        |None ->
            
        
        let quoted_source_url =
            messages_of_article
            |>Seq.tryItem 1
            |>Option.map (Html_parsing.descendant "data-testid='tweet-text-show-more-link'")
            |>Option.map (Html_node.attributeValue "href")  
        
        
    let is_post_replying_to_another_post post_body =
        ()
    
    let is_post_shown_fully body_elements =
        long_post_has_show_more_button body_elements
        ||post_quotes_another_source body_elements
        ||is_post_replying_to_another_post body_elements
        |>not
    
    
    let html_segments_of_post article_html =
        
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
            load = additional_load
            footing = footing_with_stats
        }
    
    let additional_load_of_post article_html = ()
        
    
    let repost_mark article_html =
        article_html
        |>Html_node.try_descendant "span[data-testid='socialContext']"
    
    
    let parse_twitter_post article_html =
        
        let post_html_segments =
            html_segments_of_post article_html
        
        let author_name =
            post_html_segments.header
            |>compound_name_span
            |>Html_parsing.readable_text_from_html_segments
        
        let created_at_node =
            post_html_segments.header
            |>Html_node.descendant "time"
        
        let post_url =
            post_html_segments.header
            |>Html_node.descendants "a"
            |>Html_node.which_have_direct_child created_at_node
            |>Html_node.should_be_single
            |>Html_node.attribute_value "href"
        
        let post_id =
            Html_parsing.last_url_segment post_url
        
        let created_at =
            created_at_node
            |>Html_node.attributeValue "datetime"
            |>Html_parsing.parse_datetime "yyyy-MM-dd'T'HH:mm:ss.fff'Z'"
            
        
        let author_handle =
            post_html_segments.header
            |>Html_node.direct_children |>Seq.item 1
            |>Html_node.descendant "a[role='link']"
            |>Html_node.attribute_value "href"
            |>fun url_with_slash->url_with_slash[1..]
            |>User_handle
        
        
        let number_inside_footer_element node =
            node
            |>Html_node.descendant "span[data-testid='app-text-transition-container'] > span > span"
            |>Html_node.inner_text
            |>Parsing_abbreviated_number.parse_abbreviated_number
        
        let replies_amount =
            post_html_segments.footing
            |>Html_node.direct_children
            |>Seq.head
            |>number_inside_footer_element
            
        let likes_amount =
            post_html_segments.footing
            |>Html_node.direct_children
            |>Seq.item 1
            |>number_inside_footer_element
            
        let reposts_amount =
            post_html_segments.footing
            |>Html_node.direct_children
            |>Seq.item 2
            |>number_inside_footer_element
        
        let views_amount =
            post_html_segments.footing
            |>Html_node.direct_children
            |>Seq.item 3
            |>number_inside_footer_element
        
        let message = 
            post_html_segments.message
            |>Html_parsing.readable_text_from_html_segments
            
        {
            id = post_id
            auhtor = author_handle
            created_at = created_at
            message = message
            replies_amount = replies_amount
            likes_amount = likes_amount
            reposts_amount = reposts_amount
            views_amount = views_amount
        }
        
        
    [<Fact>]
    let ``try from html``()=
        let test = 
            """<article aria-labelledby="id__nctub98pzw id__zsh96bwzskr id__k4x5213cygh id__2m8zf3duum4 id__zzivmgx42r id__6ju7w764q0u id__3k6v9p30n1u id__64sb18hog6 id__2ysmemzb8f9 id__aevhdniyhao id__qvb3hgypdo id__i51h7gycqgo id__3nul9j8x5fo id__8yzoqdylrqq id__nsih0cl4mq8 id__hscm9p08o8k id__q334pveebj id__3dsuyaltmxp id__p8ojm1g8pn" role="article" tabindex="0" class="css-1dbjc4n r-1loqt21 r-18u37iz r-1ny4l3l r-1udh08x r-1qhn6m8 r-i023vh r-o7ynqc r-6416eg" data-testid="tweet"><div class="css-1dbjc4n r-eqz5dr r-16y2uox r-1wbh5a2"><div class="css-1dbjc4n r-16y2uox r-1wbh5a2 r-1ny4l3l"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-1iusvr4 r-16y2uox r-ttdzmv"></div></div></div><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-1awozwy r-onrtq4 r-18kxxzh r-1b7u577"><div class="css-1dbjc4n" data-testid="Tweet-User-Avatar"><div class="css-1dbjc4n r-18kxxzh r-1wbh5a2 r-13qz1uu"><div class="css-1dbjc4n r-1adg3ll r-bztko3" data-testid="UserAvatar-Container-DanilaImmortal" style="height: 40px; width: 40px;"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-1adg3ll r-1pi2tsx r-1wyvozj r-bztko3 r-u8s1d r-1v2oles r-desppf r-13qz1uu"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-ggadg3 r-1udh08x r-u8s1d r-8jfcpp" style="height: calc(100% - -4px); width: calc(100% - -4px);"><a href="/DanilaImmortal" aria-hidden="true" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1niwhzg r-1loqt21 r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1niwhzg r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-14lw9ot r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-14lw9ot r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1adg3ll r-1udh08x" style=""><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div aria-label="" class="css-1dbjc4n r-1p0dtai r-1mlwlqe r-1d2f490 r-1udh08x r-u8s1d r-zchlnj r-ipm5af r-417010"><div class="css-1dbjc4n r-1niwhzg r-vvn4in r-u6sd8q r-4gszlv r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw" style="background-image: url(&quot;https://pbs.twimg.com/profile_images/1695579644282441728/w_YNIGnR_bigger.jpg&quot;);"></div><img alt="" draggable="true" src="https://pbs.twimg.com/profile_images/1695579644282441728/w_YNIGnR_bigger.jpg" class="css-9pa8cd"></div></div></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-12181gd r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"></div></div></a></div></div></div></div></div></div></div></div><div class="css-1dbjc4n r-1iusvr4 r-16y2uox r-1777fci r-kzbkwu"><div class="css-1dbjc4n r-zl2h9q"><div class="css-1dbjc4n r-k4xj1c r-18u37iz r-1wtj0ep"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs r-1ny4l3l" id="id__zzivmgx42r" data-testid="User-Name"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/DanilaImmortal" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div dir="ltr" class="css-901oao r-1awozwy r-18jsvk2 r-6koalj r-37j5jr r-a023e6 r-b88u0q r-rjixqe r-bcqeeo r-1udh08x r-3s2u2q r-qvutc0"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Danila Immortalist</span></span></div><div dir="ltr" class="css-901oao r-18jsvk2 r-xoduu5 r-18u37iz r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-1awozwy r-xoduu5 r-poiln3 r-bcqeeo r-qvutc0"><svg viewBox="0 0 22 22" aria-label="Verified account" role="img" class="r-1cvl2hr r-4qtqp9 r-yyyyoo r-1xvli5t r-9cviqr r-f9ja8p r-og9te1 r-bnwqim r-1plcrui r-lrvibr" data-testid="icon-verified"><g><path d="M20.396 11c-.018-.646-.215-1.275-.57-1.816-.354-.54-.852-.972-1.438-1.246.223-.607.27-1.264.14-1.897-.131-.634-.437-1.218-.882-1.687-.47-.445-1.053-.75-1.687-.882-.633-.13-1.29-.083-1.897.14-.273-.587-.704-1.086-1.245-1.44S11.647 1.62 11 1.604c-.646.017-1.273.213-1.813.568s-.969.854-1.24 1.44c-.608-.223-1.267-.272-1.902-.14-.635.13-1.22.436-1.69.882-.445.47-.749 1.055-.878 1.688-.13.633-.08 1.29.144 1.896-.587.274-1.087.705-1.443 1.245-.356.54-.555 1.17-.574 1.817.02.647.218 1.276.574 1.817.356.54.856.972 1.443 1.245-.224.606-.274 1.263-.144 1.896.13.634.433 1.218.877 1.688.47.443 1.054.747 1.687.878.633.132 1.29.084 1.897-.136.274.586.705 1.084 1.246 1.439.54.354 1.17.551 1.816.569.647-.016 1.276-.213 1.817-.567s.972-.854 1.245-1.44c.604.239 1.266.296 1.903.164.636-.132 1.22-.447 1.68-.907.46-.46.776-1.044.908-1.681s.075-1.299-.165-1.903c.586-.274 1.084-.705 1.439-1.246.354-.54.551-1.17.569-1.816zM9.662 14.85l-3.429-3.428 1.293-1.302 2.072 2.072 4.4-4.794 1.347 1.246z"></path></g></svg></span></div></div></a></div></div><div class="css-1dbjc4n r-18u37iz r-1wbh5a2 r-13hce6t"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/DanilaImmortal" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div dir="ltr" class="css-901oao css-1hf3ou5 r-14j79pv r-18u37iz r-37j5jr r-1wvb978 r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">@DanilaImmortal</span></div></a></div><div dir="ltr" aria-hidden="true" class="css-901oao r-14j79pv r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-s1qlax r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">·</span></div><div class="css-1dbjc4n r-18u37iz r-1q142lx"><a href="/DanilaImmortal/status/1700556920451383555" dir="ltr" aria-label="Sep 9" role="link" class="css-4rbku5 css-18t94o4 css-901oao r-14j79pv r-1loqt21 r-xoduu5 r-1q142lx r-1w6e6rj r-37j5jr r-a023e6 r-16dba41 r-9aw3ui r-rjixqe r-bcqeeo r-3s2u2q r-qvutc0"><time datetime="2023-09-09T17:08:59.000Z">Sep 9</time></a></div></div></div></div></div></div><div class="css-1dbjc4n r-1jkjb"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1cmwbt1 r-1wtj0ep"><div class="css-1dbjc4n r-1awozwy r-6koalj r-18u37iz"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="More" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="caret"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M3 12c0-1.1.9-2 2-2s2 .9 2 2-.9 2-2 2-2-.9-2-2zm9 2c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm7 0c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div><div class="css-1dbjc4n"><div dir="auto" lang="en" class="css-901oao r-18jsvk2 r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-bnwqim r-qvutc0" id="id__qvb3hgypdo" data-testid="tweetText"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">We claim that human life holds the highest value, yet when it comes to preserving human life, putting things on ice somehow isn't the default setting. It's not just about freezing people; it's a litmus test for how much we truly value human life.

At the very brink, the pivotal…</span><span tabindex="0" class="css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0" data-testid="tweet-text-show-more-link" role="link"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">&nbsp;</span><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Show more</span></span></div></div><div aria-labelledby="id__vfbxucrpmpb id__la9nyr5z25" class="css-1dbjc4n r-1ssbvtb r-1s2bzr4" id="id__3nul9j8x5fo"><div class="css-1dbjc4n r-9aw3ui"><div class="css-1dbjc4n"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-1ets6dv r-1867qdf r-1phboty r-rs99b7 r-1ny4l3l r-1udh08x r-o7ynqc r-6416eg"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-16y2uox r-1pi2tsx r-13qz1uu"><a href="/DanilaImmortal/status/1700556920451383555/photo/1" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1pi2tsx r-1ny4l3l"><div class="css-1dbjc4n r-1adg3ll r-1udh08x"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 56.044%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div aria-label="Image" class="css-1dbjc4n r-1p0dtai r-1mlwlqe r-1d2f490 r-p1pxzi r-11wrixw r-61z16t r-1mnahxq r-1udh08x r-u8s1d r-zchlnj r-ipm5af r-417010" data-testid="tweetPhoto"><div class="css-1dbjc4n r-1niwhzg r-vvn4in r-u6sd8q r-4gszlv r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw" style="background-image: url(&quot;https://pbs.twimg.com/media/F5mXEZNXsAAXOQ6?format=jpg&amp;name=900x900&quot;);"></div><img alt="Image" draggable="true" src="https://pbs.twimg.com/media/F5mXEZNXsAAXOQ6?format=jpg&amp;name=900x900" class="css-9pa8cd"></div></div></div></a></div></div></div></div></div></div></div><div class="css-1dbjc4n"><div aria-label="2 replies, 12 reposts, 47 likes, 2 bookmarks, 3053 views" role="group" class="css-1dbjc4n r-1kbdv8c r-18u37iz r-1wtj0ep r-1s2bzr4 r-hzcoqn" id="id__p8ojm1g8pn"><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-label="2 Replies. Reply" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="reply"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M1.751 10c0-4.42 3.584-8 8.005-8h4.366c4.49 0 8.129 3.64 8.129 8.13 0 2.96-1.607 5.68-4.196 7.11l-8.054 4.46v-3.69h-.067c-4.49.1-8.183-3.51-8.183-8.01zm8.005-6c-3.317 0-6.005 2.69-6.005 6 0 3.37 2.77 6.08 6.138 6.01l.351-.01h1.761v2.3l5.087-2.81c1.951-1.08 3.163-3.13 3.163-5.36 0-3.39-2.744-6.13-6.129-6.13H9.756z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1e081e0 r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">2</span></span></span></div></div></div></div><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="12 reposts. Repost" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="retweet"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M4.5 3.88l4.432 4.14-1.364 1.46L5.5 7.55V16c0 1.1.896 2 2 2H13v2H7.5c-2.209 0-4-1.79-4-4V7.55L1.432 9.48.068 8.02 4.5 3.88zM16.5 6H11V4h5.5c2.209 0 4 1.79 4 4v8.45l2.068-1.93 1.364 1.46-4.432 4.14-4.432-4.14 1.364-1.46 2.068 1.93V8c0-1.1-.896-2-2-2z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1e081e0 r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">12</span></span></span></div></div></div></div><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-label="47 Likes. Like" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="like"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M16.697 5.5c-1.222-.06-2.679.51-3.89 2.16l-.805 1.09-.806-1.09C9.984 6.01 8.526 5.44 7.304 5.5c-1.243.07-2.349.78-2.91 1.91-.552 1.12-.633 2.78.479 4.82 1.074 1.97 3.257 4.27 7.129 6.61 3.87-2.34 6.052-4.64 7.126-6.61 1.111-2.04 1.03-3.7.477-4.82-.561-1.13-1.666-1.84-2.908-1.91zm4.187 7.69c-1.351 2.48-4.001 5.12-8.379 7.67l-.503.3-.504-.3c-4.379-2.55-7.029-5.19-8.382-7.67-1.36-2.5-1.41-4.86-.514-6.67.887-1.79 2.647-2.91 4.601-3.01 1.651-.09 3.368.56 4.798 2.01 1.429-1.45 3.146-2.1 4.796-2.01 1.954.1 3.714 1.22 4.601 3.01.896 1.81.846 4.17-.514 6.67z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1e081e0 r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">47</span></span></span></div></div></div></div><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><a href="/DanilaImmortal/status/1700556920451383555/analytics" aria-label="3053 views. View post analytics" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M8.75 21V3h2v18h-2zM18 21V8.5h2V21h-2zM4 21l.004-10h2L6 21H4zm9.248 0v-7h2v7h-2z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1e081e0 r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">3,053</span></span></span></div></div></a></div><div class="css-1dbjc4n" style="display: inline-grid; justify-content: inherit; transform: rotate(0deg) scale(1) translate3d(0px, 0px, 0px); -webkit-box-pack: inherit;"><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="Share post" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M12 2.59l5.7 5.7-1.41 1.42L13 6.41V16h-2V6.41l-3.3 3.3-1.41-1.42L12 2.59zM21 15l-.02 3.51c0 1.38-1.12 2.49-2.5 2.49H5.5C4.11 21 3 19.88 3 18.5V15h2v3.5c0 .28.22.5.5.5h12.98c.28 0 .5-.22.5-.5L19 15h2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div></div></article>"""
            |>Html_node.Parse |>Seq.head
            |>parse_twitter_post
        ()