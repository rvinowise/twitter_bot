namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter


type Parsed_post_header = {
    author:Twitter_user
    written_at: DateTime
    post_url: string option //quotations don't have their URL, only main posts do
}

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
            |>Html_node.descendants "span"
            |>List.head
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
            Parsed_post_header.author = {name=author_name;handle=author_handle}
            written_at = datetime
            post_url=url
        }
        
        
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
        (*node with all
            img[]
            and
            video[]
        of the post, excluding a potential quoted post and images outside of the load (e.g. user picture)*)
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
                |>List.map (Html_node.descendant "img")
                |>List.map (fun image_node ->
                    image_node
                    |>Posted_image.from_html_image_node
                    |>Media_item.Image
                )
            with
            | :? ArgumentException as e->
                raise
                <| Bad_post_structure_exception(
                    "can't parse posted images from large layout",
                    load_node
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
    
    let text_from_external_source_detail_segment segment =
        segment
        |>Html_node.descendant ":scope> span"
        |>Html_parsing.readable_text_from_html_segments
    let parse_external_website_from_its_node
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
            |>Bad_post_structure_exception
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
 
            Some {
                External_url.base_url = base_url
                page=page
                message = message
                obfuscated_url=obfuscated_url
            }
    let parse_potential_external_website_from_additional_load
        ``node of potential card.wrapper``
        =
        if
            ``node of potential card.wrapper``
            |>Html_node.matches "div[data-testid='card.wrapper']"
        then
            parse_external_website_from_its_node
                ``node of potential card.wrapper``
        else None
    
     
    let parse_reply_header reply_header =
        reply_header
        |>Html_node.first_descendants_with_css "span"
        |>List.head
        |>Html_node.inner_text
        |>User_handle.trim_potential_atsign
        |>User_handle
    
    
    let try_reply_target_from_reply_header tweet_text_node =
        tweet_text_node
        |>Html_node.parent
        |>Html_node.direct_children
        |>List.head
        |>fun potential_header ->
            if
                Find_segments_of_post.is_reply_header potential_header
            then
                let reply_author =
                    potential_header
                    |>parse_reply_header
                Some reply_author
            else
                None
    
    let try_reply_status_from_thread_mark user node =
        if (
            node
            |>Html_node.parent
            |>Html_node.direct_children
            |>List.last
            |>Find_segments_of_post.is_mark_of_thread)
        then    
            {Reply.to_user = user; to_post=None; is_direct=true}
            |>Some
        else
            None
    
    let parse_reply_of_quotable_post author ``tweetText node`` =
        let reply_target_from_header =
            try_reply_target_from_reply_header
                ``tweetText node``
            
        match reply_target_from_header with
        |Some reply_target->
            {Reply.to_user=reply_target; to_post=None; is_direct=true}
            |>Some
        |None->
            try_reply_status_from_thread_mark author ``tweetText node``
    
    
    type Previous_cell =
        |Adjacent_post of Post_id * User_handle
        |Distant_connected_message of Post_id * User_handle
        |No_cell
    
    
    let post_has_linked_post_after article_html =
        article_html
        |>Html_node.descendants "div[data-testid='Tweet-User-Avatar']"
        |>List.head
        |>Html_node.parent
        |>Html_node.direct_children
        |>List.length
        |>function
        |1 -> false
        |2 -> true
        |unexpected_number ->
            (
                $"an unexpected number ({unexpected_number}) of children at the vertical thread-line",
                article_html
            )
            |>Bad_post_structure_exception
            |>raise 
    
    let post_has_linked_post_before article_html =
        article_html
        |>Html_node.descend 2
        |>Html_node.direct_children|>List.head
        |>Html_node.descend 1
        |>Html_node.direct_children|>List.head
        |>Html_node.direct_children
        |>List.isEmpty|>not
    
    let reply_from_local_thread
        (previous_post: Previous_cell)
        (article_html)
        =
        if
            post_has_linked_post_before article_html
        then
            match previous_post with
            |Adjacent_post (post,user) ->
                Some {Reply.to_user = user; to_post=Some post; is_direct=true}
            |Distant_connected_message (post,user) ->
                Some {Reply.to_user = user; to_post=Some post; is_direct=false}
            |No_cell -> None
        else
            None
    
    
    
    let parse_reply_of_main_post
        author
        has_social_context_header
        article_node
        (previous_cell: Previous_cell)
        =
        let reply_from_quotable_core =
            article_node
            |>Html_node.descendants "div[data-testid='tweetText']"
            |>List.tryHead
            |>function
            |Some message_node ->
                message_node
                |>parse_reply_of_quotable_post
                    author
            |None -> None
            
        match reply_from_quotable_core with
        |Some reply_status -> Some reply_status
        |None->
            if
                has_social_context_header
            then 
                None
            else
                reply_from_local_thread previous_cell article_node
    
    let quotation_is_a_poll
        ``node with role=link of the quotation``
        =
        let quoted_message_node =
            ``node with role=link of the quotation``
            |>Html_node.descendant "div[data-testid='tweetText']"
        
        let node_with_poll_mark =
            quoted_message_node
            |>Html_node.parent
            |>Html_node.direct_children
            |>List.last
        
        if
            node_with_poll_mark
            |>Html_node.ancestors "div[data-testid='tweetText']"
            |>List.isEmpty
        then    
            node_with_poll_mark
            |>Html_node.inner_text = "Show this poll"
        else
            false
            
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
            
            let reply =
                parse_reply_of_quotable_post quoted_header.author.handle quoted_message_node
            
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
            
            let header={
                author = quoted_header.author
                created_at = quoted_header.written_at
                reply=reply
            }
            
            let message =
                Post_message.from_html_node
                    quoted_message_node
            
            if
                quotation_is_a_poll
                    ``node with potential role=link of the quotation``
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
                })
        
        |None->None            
    
    
    let parse_quoted_source_from_its_node
        ``node with either role=link of quotation, or card.wrapper`` 
        //either a quoted-post, or an external-url
        =
        match
            parse_quoted_post_from_its_node
                ``node with either role=link of quotation, or card.wrapper``
        with
        |Some quoted_post -> 
            Some quoted_post
        |None ->
            match
                parse_potential_external_website_from_additional_load
                    ``node with either role=link of quotation, or card.wrapper``
            with
            |Some external_url_load ->
                external_url_load
                |>External_source.External_url 
                |>Some
            |None -> None
            
        
    let reposting_user social_context_node =
        social_context_node
        |>Html_node.ancestors "a[role='link']"
        |>List.tryHead
        |>function
        |Some link->
            link
            |>Html_node.attribute_value "href"
            |>User_handle.trim_potential_slash
            |>User_handle|>Some
        |None->None
    
    let is_pinned social_context_node =
        social_context_node
        |>Html_node.first_descendants_with_css "span"
        |>List.tryHead
        |>Option.map Html_node.inner_text
        |>Option.map ((=)"Pinned")
        |>Option.defaultValue false
    
    let parse_poll_choice li_node =
        let choice_nodes =
            li_node
            |>Html_node.descend 1
            |>Html_node.direct_children
            |>List.tail
        
        let text =
            choice_nodes
            |>List.head
            |>Html_node.descendants "span"
            |>List.head
            |>Html_node.inner_text
            
        let votes_percent =
            choice_nodes
            |>List.last
            |>Html_node.descendant "span"
            |>Html_node.inner_text
            |>fun text -> //68.5%
                Double.Parse(
                    text.TrimEnd('%'),
                    CultureInfo.InvariantCulture    
                )
       
        {
            Poll_choice.text=text
            votes_percent=votes_percent
        }            
        
    let votes_amount_text_to_int (text:String) =
        //1 vote // 10 votes
        text.Split(" ")
        |>Array.head
        |>Parsing_twitter_datatypes.parse_abbreviated_number
    
    let parse_poll_details cardPoll_node =
        let choices =
            cardPoll_node
            |>Html_node.descendant "ul[role='list']"
            |>Html_node.descendants "li[role='listitem']"
            |>List.map parse_poll_choice
        
        let votes_amount =
            cardPoll_node
            |>Html_node.direct_children
            |>List.item 1
            |>Html_node.descendants "span>span"
            |>List.head
            |>Html_node.inner_text
            |>votes_amount_text_to_int
        
        choices,votes_amount
        
        
    let parse_main_twitter_post
        (thread: Previous_cell )
        (article_html:Html_node)
        =
        
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
        
            
        let reposting_user,is_pinned =
            match post_html_segments.social_context_header with
            |Some social_context ->
                reposting_user social_context
                ,
                is_pinned social_context
            |None -> None,false
        
        let reply =
            parse_reply_of_main_post
                parsed_header.author.handle
                (reposting_user.IsSome || is_pinned)
                article_html
                thread
        
        let header = {
            author =parsed_header.author
            created_at=parsed_header.written_at
            reply=reply
        }
            
        let message =
            post_html_segments.message
            |>function
            |Some message_node ->
                Post_message.from_html_node message_node
            |None -> Post_message.Full ""
        
        let body=
            match post_html_segments.poll_choices_and_summary with
            |Some poll_detail ->
                let choices,votes_amount =
                    parse_poll_details poll_detail
                Main_post_body.Poll {
                    Poll.quotable_part = {
                        header=header
                        question=message|>Post_message.text
                    }
                    choices= choices
                    votes_amount=votes_amount
                }
            |None->
                let media_items = 
                    match post_html_segments.media_load with
                    |Some media -> parse_media_from_large_layout media
                    |None->[]
                
                let external_source =
                    match post_html_segments.quotation_load with
                    |Some quotation -> parse_quoted_source_from_its_node quotation
                    |None -> None
                
                Main_post_body.Message (
                    {
                        Quotable_message.header = header
                        message = message
                        media_load = media_items
                    }
                    ,
                    external_source
                )
        
        let post_stats =
            Parse_footer_with_stats.parse_post_footer post_html_segments.footer
        
        let post_for_context =
            Adjacent_post (post_id, header.author.handle)
        
        {
            Main_post.id=post_id
            body=body
            stats=post_stats
            reposter=reposting_user
            is_pinned = is_pinned
        },post_for_context
        