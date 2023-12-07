namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter



module Parse_segments_of_post =
    
   
        
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
    
    
    
        
        
    let parse_main_twitter_post
        (thread: Previous_cell )
        (article_html:Html_node)
        =
        
        let post_html_segments =
            Find_segments_of_post.html_segments_of_main_post article_html
        
        let parsed_header =
            Parse_header.parse_post_header post_html_segments.header

        let post_id =
            parsed_header.post_url
            |>function
            |Some url ->
                url
                |>Html_parsing.last_url_segment
                |>int64|>Post_id
            |None ->
                ("main post doesn't have its url",article_html)
                |>Bad_post_exception
                |>raise
            
        let reposting_user,is_pinned =
            match post_html_segments.social_context_header with
            |Some social_context ->
                reposting_user social_context
                ,
                is_pinned social_context
            |None -> None,false
        
        let reply =
            Parse_reply.parse_reply_of_main_post
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
                    Parse_poll.parse_poll_detail poll_detail
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
                    |Some media -> Parse_media.parse_media_from_large_layout media
                    |None->[]
                
                let external_source =
                    match post_html_segments.quotation_load with
                    |Some quotation ->
                         Parse_quotation.parse_external_source_from_its_node quotation
                         |>Some
                    |None -> None
                
                Main_post_body.Message (
                    {
                        Quotable_message.header = header
                        message = message
                        media_load = media_items
                        twitter_space = None
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
        