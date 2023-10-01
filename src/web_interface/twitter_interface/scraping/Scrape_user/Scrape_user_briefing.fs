namespace rvinowise.twitter

open System
open canopy.parallell.functions
open canopy.types
open rvinowise.html_parsing
open rvinowise.web_scraping



type User_briefing = {
    handle: User_handle
    name: string
    bio: string
    location: string
    date_joined: DateTime
    web_site: string
    profession: string
}
module Scrape_user_briefing =


    module Scrape_user_elements=
        let name browser =
            try
                browser
                |>Browser.element """div[data-testid='UserName'] span > span:nth-child(1)""" 
                |>Html_node.from_scraped_node
                |>Html_parsing.readable_text_from_html_segments
            with
            | :? CanopyElementNotFoundException as exc ->
                Log.error $"name_field isn't found on the web-page: {exc.Message}" |>ignore
                ""
            
                
        let bio parsing_context browser =
            """div[data-testid='UserDescription']"""
            |>Browser.try_element browser
            |>function
            |Some element ->
                element
                |>Html_node.from_scraped_node_and_context parsing_context
                |>Html_parsing.readable_text_from_html_segments
            |None->""
            
        let location browser =
            "span[data-testid='UserLocation'] > span > span"
            |>Browser.try_text browser
            |>Option.defaultValue ""
            
        let web_site browser =
            "a[data-testid='UserUrl'] > span"
            |>Browser.try_text browser
            |>Option.defaultValue ""
        
        let profession browser =
            "span[data-testid='UserProfessionalCategory'] > span[role='button']"
            |>Browser.try_text browser
            |>Option.defaultValue ""
        
        let joined_date browser =
            "span[data-testid='UserJoinDate'] > span"
            |>Browser.try_text browser
            |>function
            |Some date_text -> Parsing_twitter_datatypes.parse_joined_date date_text
            |None->DateTime.MinValue
            
            
    let scrape_user_briefing parsing_context browser user_handle =
        {
            User_briefing.handle = user_handle
            name = Scrape_user_elements.name browser
            bio = Scrape_user_elements.bio parsing_context browser
            date_joined = Scrape_user_elements.joined_date browser
            location = Scrape_user_elements.location browser
            profession = Scrape_user_elements.profession browser
            web_site = Scrape_user_elements.web_site browser
        }
            

    
    

  


    
    



    


