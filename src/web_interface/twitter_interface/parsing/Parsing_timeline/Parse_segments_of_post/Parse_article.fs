namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter



module Parse_article =
    
   
        
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
    
    
    
    let parse_quotable_part_of_post
        html_node
        =()
        
    
    let parse_very_top_header_of_social_context article_node =
        let social_context_header =
            article_node
            |>Html_node.try_descendant "span[data-testid='socialContext']"
            |>function
            |Some context-> Some context
            |None ->
                article_node
                |>Html_node.try_descendant "div[data-testid='socialContext']"
            
        match social_context_header with
        |Some social_context ->
            let reposter = reposting_user social_context
            let is_pinned = is_pinned social_context
            if (reposter.IsSome && is_pinned) then
                raise (Bad_post_exception("a repost can't be pinned"))
            
            reposter,is_pinned
            
        |None -> None,false
    
    
    let parse_messages_of_article article_node =
        article_node
        |>Html_node.descendants "div[data-testid='tweetText']"
        |>function
        |main_message_node::[quotation_message_node] ->
            Post_message.from_html_node main_message_node,
            Post_message.from_html_node quotation_message_node
        |[message_node] ->
            message_node
            |>Html_node.ancestors "div[role='link']"
            |>function
            |[]->
                Post_message.from_html_node message_node,
                Post_message.empty
            |quotation_mark ->
                Post_message.empty,
                Post_message.from_html_node message_node
        |[] -> Post_message.empty,Post_message.empty
        |wrong_messages ->
            raise (Bad_post_exception($"an article can have 2 message nodes maximum, but it had {wrong_messages}"))
    
    
    let parse_message node =
        node
        |>Html_node.try_descendant "div[data-testid='tweetText']"
        |>function
        |Some message_node ->
            Post_message.from_html_node message_node
        |None -> Post_message.empty
    
    (* poll details can only exist in the Main post, not in the quoted post *)
    let try_parse_poll_details article_node =
        let poll_choices_and_summary =
            article_node
            |>Html_node.try_descendant "div[data-testid='cardPoll']"
        
        match poll_choices_and_summary with
        |Some poll_detail ->
            let choices,votes_amount =
                Parse_poll.parse_poll_detail poll_detail
            Some (choices,votes_amount)
        |None->None
    
    
    let try_parse_article_with_poll
        header
        article_node
        =
        let poll_choices_and_summary =
            try_parse_poll_details
                article_node
        
        let message =
            parse_message article_node
            |>Post_message.text
            
        
        match poll_choices_and_summary with
        |Some (choices,votes_amount) ->
            Main_post_body.Poll {
                Poll.quotable_part = {
                    header=header
                    question=message
                }
                choices= choices
                votes_amount=votes_amount
            }|>Some
        |None->None
    
    let try_parse_article_without_poll
        header
        article_node
        =
        
        let external_source_node =
            Parse_external_source.external_source_node_of_main_post
                article_node
        
        match external_source_node with
        |Some external_source_node ->
            external_source_node
            |>External_source_node.html_node
            |>Html_node.detach_from_parent
            |>ignore
        |None->()
        
        //at this point the article node doesn't have its external source        
        
        let main_message =
            article_node
            |>Parse_quoted_post.detach_nodes_with_images_outside_of_media_load
            |>Option.map Post_message.from_html_node
            |>Option.defaultValue Post_message.empty
        
        //at this point the article node doesn't have its message and user's avatar
        
        let media_items = 
            Parse_media.parse_media_from_stripped_post article_node
        
        let external_source =
            external_source_node
            |>Option.map Parse_external_source.parse_external_source_of_main_post 
            
        Main_post_body.Message (
            {
                Quotable_message.header = header
                message = main_message
                media_load = media_items
                twitter_space = None
            }
            ,
            external_source
        )|>Some
        
    let parse_article_as_either_poll_or_not_poll
        header
        article_node
        =
        [
            (try_parse_article_with_poll header);
            (try_parse_article_without_poll header)
        ]|>List.pick (fun parser -> parser article_node)
    
    let parse_main_post_header
        article_node
        thread
        (reposting_user: User_handle option)
        is_pinned
        (header_node: Html_node)
        =
        let parsed_header =
            Parse_header.parse_post_header header_node
        let post_id =
            parsed_header.post_url
            |>function
            |Some url ->
                url
                |>Html_parsing.last_url_segment
                |>int64|>Post_id
            |None ->
                ("a post header doesn't have its url",header_node)
                |>Bad_post_exception
                |>raise    
        
        let reply =
            Parse_reply_in_main_post.parse_reply_of_main_post
                (reposting_user.IsSome || is_pinned)
                article_node
                thread
        
        {
            Post_header.author =parsed_header.author
            created_at=parsed_header.written_at
            reply=reply
        },post_id
        
    let parse_twitter_article
        (thread: Previous_cell )
        (article_node:Html_node)
        =
        
        let reposting_user, is_pinned =
            parse_very_top_header_of_social_context article_node
        
        let segment_nodes =
            Find_segments_of_post.top_level_segments_of_main_post article_node
        
        let header,post_id =
            segment_nodes
            |>List.head
            |>parse_main_post_header
                article_node
                thread
                reposting_user
                is_pinned
        
        let post_stats =
            segment_nodes
            |>List.last
            |>Parse_footer_with_stats.parse_post_footer
        
        let post_body =
            parse_article_as_either_poll_or_not_poll
                header
                article_node
        
        let post_for_context =
            Adjacent_post (post_id, header.author.handle)
        
        {
            Main_post.id=post_id
            body=post_body
            stats=post_stats
            reposter=reposting_user
            is_pinned = is_pinned
        },post_for_context
        