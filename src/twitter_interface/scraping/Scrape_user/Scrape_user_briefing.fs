namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.classic
open canopy.types
open rvinowise.twitter
open FParsec



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
        let name () =
            try
                """div[data-testid='UserName'] span > span:nth-child(1)"""
                |>element
                |>Parsing.html_node_from_web_element
                |>Parse_twitter_user.readable_text_from_html_segments
            with
            | :? CanopyElementNotFoundException as exc ->
                Log.error $"name_field isn't found on the web-page: {exc.Message}" |>ignore
                ""
            
                
        let bio () =
            """div[data-testid='UserDescription']"""
            |>Scraping.try_element
            |>function
            |Some element ->
                element
                |>Parsing.html_node_from_web_element
                |>Parse_twitter_user.readable_text_from_html_segments
            |None->""
            
        let location () =
            "span[data-testid='UserLocation'] > span > span"
            |>Scraping.try_text
            |>Option.defaultValue ""
            
        let web_site () =
            "a[data-testid='UserUrl'] > span"
            |>Scraping.try_text
            |>Option.defaultValue ""
        
        let profession () =
            "span[data-testid='UserProfessionalCategory'] > span[role='button']"
            |>Scraping.try_text
            |>Option.defaultValue ""
        
        let joined_date () =
            "span[data-testid='UserJoinDate'] > span"
            |>Scraping.try_text
            |>function
            |Some date_text -> Parsing.parse_joined_date date_text
            |None->DateTime.MinValue
            
            
    let scrape_user_briefing user_handle =
        {
            User_briefing.handle = user_handle
            name = Scrape_user_elements.name()
            bio = Scrape_user_elements.bio()
            date_joined = Scrape_user_elements.joined_date()
            location = Scrape_user_elements.location()
            profession = Scrape_user_elements.profession()
            web_site = Scrape_user_elements.web_site()
        }
            

    
    

  


    
    



    


