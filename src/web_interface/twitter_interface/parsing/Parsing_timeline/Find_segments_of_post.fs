﻿namespace rvinowise.twitter

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
    reply_header: Html_node option
    media_load: Html_node option
    quotation_load: Html_node option
    footer: Html_node
}


module Find_segments_of_post =
    
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
            
    
    let is_reply_header node =
        node
        |>Html_node.matches "div"
        &&
        node.FirstChild.NodeType = NodeType.Text
        &&
        node.FirstChild.TextContent = "Replying to "
    
//    let has_reply_header segment =
//        segment
//        |>Html_node.descend 1
        
    
    let is_quotation_of_external_source segment =
        segment
        |>Html_node.matches "div[data-testid='card.wrapper']"
        |>function
        |true -> true
        |false->
            segment
            |>Html_node.try_descendant "div[data-testid='tweetText']"
            |>function
            |Some quoted_message -> true
            |None -> false
    
    let html_segments_of_main_post
        ``node with article[test-id='tweet']``
        = 
        let post_segments = 
            valuable_segments_of_post ``node with article[test-id='tweet']``
        
        let header =
            post_segments
            |>List.head
            |>Html_node.descendant "div[data-testid='User-Name']"
        
        let footing_with_stats =
            post_segments
            |>List.last
            |>Html_node.descend 1
            
        let body_segments =
            post_segments
            |>List.skip 1|>List.take (List.length post_segments - 2)
            
        let reply_header,rest_segments =
            match body_segments with
            |maybe_reply_header::rest_segments ->
                if is_reply_header maybe_reply_header then
                    Some maybe_reply_header,rest_segments
                else
                    None,body_segments
        
        let message =
            rest_segments
            |>Seq.head
            |>Html_node.descendant "div[data-testid='tweetText']"
            
        let additional_load =
            rest_segments
            |>Seq.tryItem 1
        
        let media_load,quotation_load = //direct children of the additional_load_node
            match additional_load with
            |Some additional_load ->
                match
                    additional_load
                    |>Html_node.direct_children    
                with
                |[media;quotation] ->
                    Some media,Some quotation
                |[media_or_quotation] ->
                    if is_quotation_of_external_source media_or_quotation then
                        None,Some media_or_quotation
                    else
                        Some media_or_quotation,None
                        
                | _-> raise (Bad_post_structure_exception (
                    "additional post load has >2 children",
                    ``node with article[test-id='tweet']``
                    ))
            |None -> None,None     
        
        {
            Html_segments_of_main_post.header = header
            message = message
            reply_header = reply_header
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
    
    