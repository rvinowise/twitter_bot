namespace rvinowise.twitter

open AngleSharp.Dom
open rvinowise.html_parsing


exception Bad_post_structure_exception of string*Html_node
    with
        override this.ToString() =
            this.Data0+this.Data1.OuterHtml

exception Html_parsing_fail

type Html_segments_of_quoted_post = { //which quoted post? it can be big or small, with different layouts
    header: Html_node
    message: Html_node
    media: Html_node option
}

type Html_segments_of_main_post = {
    social_context_header: Html_node option
    header: Html_node
    message: Html_node option
    reply_header: Html_node option
    media_load: Html_node option
    quotation_load: Html_node option
    poll_choices_and_summary: Html_node option
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
        |>Html_node.descendants_with_this "div"
        |>List.exists (fun node ->
            Html_node.direct_text node = "Replying to "
        )
    
    let is_mark_of_thread node =
        node
        |>Html_node.matches "div"
        &&
        node
        |>Html_node.direct_children
        |>List.tryHead
        |>function
        |Some span ->
            span
            |>Html_node.matches "span"
            &&
            span
            |>Html_node.inner_text = "Show this thread"
        |None -> false
        
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
        article_node
        =
        //siblings with Header and Stats nodes
        let post_segments = 
            valuable_segments_of_post article_node
        
        let social_context_header =
            article_node
            |>Html_node.try_descendant "span[data-testid='socialContext']"
            |>function
            |Some context-> Some context
            |None ->
                article_node
                |>Html_node.try_descendant "div[data-testid='socialContext']"
        
        let header =
            post_segments
            |>List.head
            |>Html_node.descendant "div[data-testid='User-Name']"
        
        let footing_with_stats =
            post_segments
            |>List.last
            |>Html_node.descendant "div[role='group']"
            
        let body_segments =
            post_segments
            |>List.skip 1|>List.take (List.length post_segments - 2)
            
        let reply_header,rest_segments =
            match body_segments with
            |maybe_reply_header::rest_segments ->
                try 
                    if is_reply_header maybe_reply_header then
                        Some maybe_reply_header,rest_segments
                    else
                        None,body_segments
                with
                | :? System.NullReferenceException as e ->
                    None,body_segments
                    
            |[]->raise Html_parsing_fail
        
        let message =
            rest_segments
            |>Seq.head
            |>Html_node.try_descendant "div[data-testid='tweetText']"
            
        let additional_load =
            rest_segments
            |>Seq.tryItem 1
        
        
        let
            media_load,
            quotation_load,
            poll_choices_and_summary
                =
                match additional_load with
                |None -> None,None,None
                |Some additional_load ->
                    let poll_choices_and_summary =
                        additional_load
                        |>Html_node.try_descendant "div[data-testid='cardPoll']"
                    match poll_choices_and_summary with
                    |Some poll_choices_and_summary->
                        None,None,Some poll_choices_and_summary
                    |None->
                        match
                            additional_load
                            |>Html_node.direct_children    
                        with
                        |[media;quotation] ->
                            Some media,Some quotation,None
                        |[media_or_quotation] ->
                            if is_quotation_of_external_source media_or_quotation then
                                None,Some media_or_quotation,None
                            else
                                Some media_or_quotation,None,None
                                
                        |[]->None,None,None // empty external source DIV, if there's a link in the message itself
                        | _->
                            raise
                            <| Bad_post_structure_exception 
                                ("additional post load has >2 children", article_node)
        
        {
            social_context_header = social_context_header
            Html_segments_of_main_post.header = header
            message = message
            reply_header = reply_header
            media_load = media_load
            quotation_load = quotation_load
            poll_choices_and_summary = poll_choices_and_summary
            footer = footing_with_stats
        }
        
        
    let details_of_external_source
        ``node with card.wrapper``
        =
        let small_detail_css = "div[data-testid='card.layoutSmall.detail']"
        let large_detail_css = "div[data-testid='card.layoutLarge.detail']"
        ``node with card.wrapper``
        |>Html_node.try_descendant small_detail_css
        |>function
        |Some detail_node ->
            detail_node
            |>Html_node.direct_children
        |None->
            ``node with card.wrapper``
            |>Html_node.try_descendant large_detail_css
            |>Option.map Html_node.direct_children
            |>Option.defaultValue []
    
    