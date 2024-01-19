namespace rvinowise.twitter

open System
open System.Globalization
open AngleSharp.Dom
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
        |>Html_node.descendants_from_highest_level "span"
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
        
        match poll_choices_and_summary with
        |Some (choices,votes_amount) ->
            let message =
                parse_message article_node
                |>Post_message.text
            
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
        
        let external_source =
            Parse_external_source.detach_and_parse_external_source
                article_node
        
        //at this point the article node doesn't have its external source
        
        let twitter_audio_space =
            Parse_twitter_audio_space.detach_and_parse_twitter_audio_space
                article_node
        
        //at this point the article node doesn't have its twitter audio space
        
        let main_message =
            article_node
            |>Parse_quoted_post.detach_message_node
            |>Option.map Post_message.from_html_node
            |>Option.defaultValue Post_message.empty
        
        //at this point the article node doesn't have its message and user's avatar
        
        let media_items = 
            Parse_media.parse_media_from_stripped_node article_node

            
        Main_post_body.Message (
            {
                Quotable_message.header = header
                message = main_message
                media_load = media_items
                audio_space = twitter_audio_space
            }
            ,
            external_source
        )|>Some
        
    let parse_article_as_either_poll_or_not_poll
        header
        article_node
        =
        [
            (try_parse_article_with_poll header)
            (try_parse_article_without_poll header)
        ]|>List.pick (fun parser -> parser article_node)
    
    
        
    
    let parse_main_post_header
        article_node
        thread
        (reposting_user: User_handle option)
        is_pinned
        =
        let parsed_header =
            Parse_header.detach_and_parse_header article_node
        
        //at this point the article doesn't have its header
        
        let post_id =
            parsed_header.post_url
            |>function
            |Some url ->
                Parse_header.post_id_from_post_url url
            |None ->
                "a post header doesn't have its url"
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
        (previous_cell: Thread_context )
        (original_article_node:Html_node)
        =
        
        let article_node = original_article_node.Clone() :?> Html_node
        
        let reposting_user, is_pinned =
            parse_very_top_header_of_social_context article_node
        
        let segment_nodes =
            Find_segments_of_post.top_level_segments_of_main_post article_node
        
        let header,post_id =
            parse_main_post_header
                article_node
                previous_cell
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
        
        let parsed_post =
            {
                Main_post.id=post_id
                body=post_body
                stats=post_stats
                reposter=reposting_user
                is_pinned = is_pinned
            }
        
        parsed_post
        