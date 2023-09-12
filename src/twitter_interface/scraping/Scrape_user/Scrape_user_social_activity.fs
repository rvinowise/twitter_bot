namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.parallell.functions
open rvinowise.twitter
open FParsec


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

module Lettered_number_parser =
    let final_multiplier =
        (pstring "K"|>>fun _-> 1000) <|>
        (pstring "M"|>>fun _->1000000) <|>
        (spaces|>>fun _->1)
        
    let number_with_letter =
        pfloat .>>. final_multiplier
        |>> (fun (first_number,last_multiplier) ->
            (first_number * float last_multiplier)
            |> int
        )


module Scrape_user_social_activity =

    let remove_commas (number:string) =
        number.Replace(",", "")
        
    
    let parse_number_with_multiplier_letter text =
        text
        |>remove_commas
        |>run Lettered_number_parser.number_with_letter
        |>function
        |Success (number,_,_) -> Some number
        |Failure (error,_,_) -> 
            $"""error while parsing number of followers:
            string: {text}
            error: {error}"""
            |>Log.error|>ignore
            None
    
//    let parse_posts_qty_from_string text =
//        text
//        |>run (Lettered_number_parser.number_with_letter .>> spaces)
//        |>function
//        |Success (number,_,_) -> Result.Ok number
//        |Failure (error,_,_) -> 
//            $"""error while parsing amount of posts:
//            string: {text}
//            error: {error}"""
//            |>Log.error
//            |>Result.Error
    
    [<Fact>]
    let ``try parse twitter-style numbers with final letters``()=
        [
            "454 posts",454
            "87.2M",87200000
            "4,383 posts",4383
            "10.7K posts",10700
        ]|>List.map (fun (input,expected_number)->
            input
            |>parse_number_with_multiplier_letter
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
            |>parse_number_with_multiplier_letter 
    
    
    
    let scrape_acquaintances_amount_of_user browser user_handle link =
        let followers_qty_field = $"a[href='/{User_handle.value user_handle}/{link}'] span span"
        followers_qty_field
        |>Scraping.try_element browser
        |>function
        |None->
            $"url '{User_handle.url_from_handle user_handle}' doesn't show the Followers field"
            |>Log.error|>ignore
            None
        |Some followers_qty_field->
            browser
            |>read followers_qty_field
            |>parse_number_with_multiplier_letter
    
    let scrape_followers_amount_of_user browser user_handle =
        scrape_acquaintances_amount_of_user browser user_handle "followers"
    
    let scrape_followees_amount_of_user browser user_handle =
        scrape_acquaintances_amount_of_user browser user_handle "following"
    
    let scrape_user_social_activity browser user_handle =
        {
            User_social_activity.posts_amount = scrape_posts_amount browser
            followers_amount = scrape_followers_amount_of_user browser user_handle
            followees_amount = scrape_followees_amount_of_user browser user_handle
        }
            
   

    
    

  


    
    



    


