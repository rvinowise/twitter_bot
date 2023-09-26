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
        let article_node = Html_node.from_html_string article_node

        ()
            
            
    let scrape_likes_given_by_user browser user =
        url $"{Twitter_settings.base_url}/{User_handle.value user}/likes" browser
        Reveal_user_page.surpass_content_warning browser    
        "article[data-testid='tweet']"
        |>Scrape_dynamic_list.consume_items_of_dynamic_list
            browser
            (parse_post_from_timeline browser)
            Int32.MaxValue
        |>Map.keys
        |>Seq.map (Html_node.from_html_string>>Parse_segments_of_post.parse_main_twitter_post)
        

  


    
    



    


