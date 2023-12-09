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
        
            
    let parse_quoted_post
        html_node
        quoted_message_node
        = 
        let quotation_root_node =
            quoted_message_node
            |>Html_node.ancestors "div[role='link']"
            |>List.head    
            
        let quoted_header =
            quotation_root_node
            |>Html_node.descendant "div[data-testid='User-Name']"
            |>Parse_header.parse_post_header
        
        let reply =
            Parse_reply.parse_reply_of_quotable_post quoted_header.author.handle quoted_message_node
        
        let quoted_media_items =
            Parse_media.parse_media_items_from_quotation html_node
            
        let twitter_space =
            Parse_external_source.try_parse_twitter_audio_space html_node
            
        
        let header={
            author = quoted_header.author
            created_at = quoted_header.written_at
            reply=reply
        }
        
        let message =
            Post_message.from_html_node
                quoted_message_node
        
        if
            Parse_poll.quotation_is_a_poll
                html_node
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
                twitter_space = twitter_space 
            })
        
    let try_parse_quoted_post html_node =
        let quoted_message_node =
            html_node
            |>Html_node.try_descendant "div[data-testid='tweetText']"
            
        match quoted_message_node with
        |Some quoted_message_node ->
            parse_quoted_post html_node quoted_message_node
        |None->None
    
 
    
    