namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.parallell.functions
open canopy.types
open rvinowise.html_parsing
open FParsec



module Scrape_timeline =


    let open_post_to_see_quoted_url
        browser
        article_node
        =
        browser
        |>click article_node
   
    let parse_post_from_timeline
        browser
        article_node
        =
        let (Html_string article_node) = article_node
        let article_node = Html_parsing.parseable_node_from_string article_node
        if Parse_post_from_timeline.is_post_shown_fully article_node then
            Parse_post_from_timeline.parse_twitter_post article_node
            ()
        else
            open_post_to_see_quoted_url browser article_node
            
            
            
    let scrape_likes_given_by_user browser user =
        url $"{Twitter_settings.base_url}/{User_handle.value user}/likes" browser
        Reveal_user_page.surpass_content_warning browser    
        "article[data-testid='tweet']"
        |>Scrape_dynamic_list.consume_items_of_dynamic_list
            browser
            (parse_post_from_timeline browser)
        |>Seq.map Parse_post_from_timeline.parse_twitter_post
        

  


    
    



    


