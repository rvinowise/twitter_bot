namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.parallell.functions
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
        let name browser =
            try
                browser
                |>element """div[data-testid='UserName'] span > span:nth-child(1)""" 
                |>Parsing.html_node_from_web_element
                |>Parsing.readable_text_from_html_segments
            with
            | :? CanopyElementNotFoundException as exc ->
                Log.error $"name_field isn't found on the web-page: {exc.Message}" |>ignore
                ""
            
                
        let bio browser =
            """div[data-testid='UserDescription']"""
            |>Scraping.try_element browser
            |>function
            |Some element ->
                element
                |>Parsing.html_node_from_web_element
                |>Parsing.readable_text_from_html_segments
            |None->""
            
        let location browser =
            "span[data-testid='UserLocation'] > span > span"
            |>Scraping.try_text browser
            |>Option.defaultValue ""
            
        let web_site browser =
            "a[data-testid='UserUrl'] > span"
            |>Scraping.try_text browser
            |>Option.defaultValue ""
        
        let profession browser =
            "span[data-testid='UserProfessionalCategory'] > span[role='button']"
            |>Scraping.try_text browser
            |>Option.defaultValue ""
        
        let joined_date browser =
            "span[data-testid='UserJoinDate'] > span"
            |>Scraping.try_text browser
            |>function
            |Some date_text -> Parsing.parse_joined_date date_text
            |None->DateTime.MinValue
            
            
    let scrape_user_briefing browser user_handle =
        {
            User_briefing.handle = user_handle
            name = Scrape_user_elements.name browser
            bio = Scrape_user_elements.bio browser
            date_joined = Scrape_user_elements.joined_date browser
            location = Scrape_user_elements.location browser
            profession = Scrape_user_elements.profession browser
            web_site = Scrape_user_elements.web_site browser
        }
            

    
    

  


    
    



    


