namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter


module Parse_quoted_post =
    
    
    let try_quotation_node article_node =
        article_node
        |>Html_node.descendants "div[role='link']"
        |>List.tryFind(fun link_node->
            link_node
            |>Html_node.parent
            |>Html_node.direct_children
            |>List.head
            |>Html_node.direct_children
            |>function
            |[possible_quote_mark]->
                possible_quote_mark
                |>Html_node.direct_text
                |> (=) "Quote"
            |_ ->false
        )
        |>Option.map External_source_node.Quoted_message
        
    
    let top_level_segments_of_quotation
        quotation_node //role=link
        =
        quotation_node
        |>Html_node.descend 1
        |>Html_node.direct_children
    
    let detach_message_node
        article_node
        =
        let message_node =
            article_node
            |>Html_node.try_descendant "div[data-testid='tweetText']"
        
        message_node //can have images which are emojis
        |>Option.map Html_node.detach_from_parent
            
    let parse_quoted_post
        quotation_node //div[role='link'] //will be modified
        = 
        let parsed_header =
            quotation_node
            |>Parse_header.detach_and_parse_header
        
        //at this point, the header is removed from the quotation node
        
        let reply =
            Parse_reply_in_quoted_post.reply_of_quoted_post quotation_node
        
        let header={
            author = parsed_header.author
            created_at = parsed_header.written_at
            reply=reply
        }
        
        let twitter_space =
            Parse_twitter_audio_space.detach_and_parse_twitter_audio_space
                quotation_node
            
        //at this point, twitter audio space is removed from the quotation node
        
        let message =
            quotation_node
            |>detach_message_node
            |>Option.map Post_message.from_html_node
            |>Option.defaultValue Post_message.empty
        
        //at this point, the message is removed from the quotation node
        
        let media_items =
            Parse_media.parse_media_from_stripped_post quotation_node
            
       
        if
            Parse_poll.quotation_is_a_poll
                quotation_node
        then
            External_source.Quoted_poll{
                header=header
                question =
                    message
                    |>Post_message.text
            }
        else
            External_source.Quoted_message{
                header=header
                message = message
                media_load = media_items
                audio_space = twitter_space 
            }
        

    
 
    
    