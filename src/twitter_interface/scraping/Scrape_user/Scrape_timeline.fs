namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.parallell.functions
open canopy.types
open rvinowise.twitter
open FParsec



module Scrape_timeline =


   
        
     
            
    let scrape_likes_given_by_user browser user =
        url $"{Twitter_settings.base_url}/{User_handle.value user}/likes" browser
        Reveal_user_page.surpass_content_warning browser    
        "article[data-testid='tweet']"
        |>Scrape_dynamic_list.consume_all_items_of_dynamic_list
            browser
        |>Seq.map Parse_post_from_timeline.parse_twitter_post
        

  


    
    



    


