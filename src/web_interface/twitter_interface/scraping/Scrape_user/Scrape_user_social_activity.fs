namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.parallell.functions
open rvinowise.twitter


type User_social_activity = {
    posts_amount: int option
    followers_amount: int option
    followees_amount: int option
}

module User_social_activity =
    let followers_amount state =
        state.followers_amount

    let followees_amount state =
        state.followees_amount
    let posts_amount state =
        state.posts_amount




module Scrape_user_social_activity =

    
    

    
    [<Fact>]
    let ``try parse twitter-style numbers with final letters``()=
        [
            "454 posts",454
            "87.2M",87200000
            "4,383 posts",4383
            "10.7K posts",10700
        ]|>List.map (fun (input,expected_number)->
            input
            |>Parsing_twitter_datatypes.try_parse_abbreviated_number
            |>function
            | None -> raise (Exception())
            | Some result_number -> should equal expected_number result_number
        )
        
    let scrape_posts_amount browser =
        let posts_qty_field =
            "div:has(>h2[role='heading']) > div[dir='ltr']"
            |>Scraping.try_element browser
        
        posts_qty_field
        |>function
        |None->
            "posts_qty_field isn't on the page"
            |>Log.error|>ignore
            None
        |Some posts_qty_field->
            browser
            |>read posts_qty_field
            |>Parsing_twitter_datatypes.try_parse_abbreviated_number 
    
    
    
    let scrape_acquaintances_amount_of_user browser user_handle link =
        let followers_qty_field = $"a[href='/{User_handle.value user_handle}/{link}'] span span"
        followers_qty_field
        |>Scraping.try_element browser
        |>function
        |None->
            $"url '{User_handle.url_from_handle user_handle}' doesn't show the {link} field"
            |>Log.error|>ignore
            None
        |Some followers_qty_field->
            browser
            |>read followers_qty_field
            |>Parsing_twitter_datatypes.try_parse_abbreviated_number
    
    let scrape_followers_amount_of_user browser user_handle =
        scrape_acquaintances_amount_of_user browser user_handle "verified_followers"
    
    let scrape_followees_amount_of_user browser user_handle =
        scrape_acquaintances_amount_of_user browser user_handle "following"
    
    let scrape_user_social_activity browser user_handle =
        {
            User_social_activity.posts_amount = scrape_posts_amount browser
            followers_amount = scrape_followers_amount_of_user browser user_handle
            followees_amount = scrape_followees_amount_of_user browser user_handle
        }
            
   

    
    

  


    
    



    


