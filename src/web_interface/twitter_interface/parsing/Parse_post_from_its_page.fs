namespace rvinowise.twitter

open System
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.parallell.functions
open rvinowise.twitter
open FSharp.Data
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
        
        
    