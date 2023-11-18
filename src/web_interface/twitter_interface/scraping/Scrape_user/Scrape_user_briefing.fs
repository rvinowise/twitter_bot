namespace rvinowise.twitter

open System
open canopy.types
open rvinowise.html_parsing
open rvinowise.web_scraping




module Scrape_user_briefing =


    let scrape_user_briefing parsing_context browser user_handle =
        browser
        |>Browser.element "div:has(>div[data-testid='UserName'])"
        |>Html_node.from_scraped_node_and_context parsing_context
        |>Parse_twitter_user_briefing.parse_briefing_node user_handle
        
            

    
    

  


    
    



    


