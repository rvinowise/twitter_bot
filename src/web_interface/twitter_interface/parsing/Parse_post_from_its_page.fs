namespace rvinowise.twitter

open rvinowise.html_parsing



module Parse_post_from_its_page =
    
    
    let message article_html =
        article_html
        |>Html_node.descendant "div[data-testid='tweetText']"
        |>Html_parsing.readable_text_from_html_segments
       
    let quoted_post article_html =
        article_html
        |>Html_node.descendant "div[data-testid='tweetText']"
        |>Html_parsing.readable_text_from_html_segments   
    

    
//    let parse_twitter_post article_html =
//        {
//            message = message article_html
//            related_post = quoted_post article_html
//        }
        
        
    