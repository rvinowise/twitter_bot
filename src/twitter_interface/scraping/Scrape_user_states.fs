namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.classic
open rvinowise.twitter
open FParsec


type Scraped_user_state = {
    posts_amount: Result<int,string>
    followers_amount: Result<int,string>
}

module Scraped_user_state =
    let followers_amount state =
        state.followers_amount

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


module Scrape_user_states =

    let remove_commas (number:string) =
        number.Replace(",", "")
        
    
    let parse_number_with_multiplier_letter text =
        text
        |>remove_commas
        |>run Lettered_number_parser.number_with_letter
        |>function
        |Success (number,_,_) -> Result.Ok number
        |Failure (error,_,_) -> 
            $"""error while parsing number of followers:
            string: {text}
            error: {error}"""
            |>Log.error
            |>Result.Error
    
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
            | Result.Error error -> raise (Exception(error))
            | Result.Ok result_number -> should equal expected_number result_number
        )
        
    let scrape_posts_amount ()=
        let posts_qty_field =
            "div:has(>h2[role='heading']) > div[dir='ltr']"
            |>Scraping.try_element
        
        posts_qty_field
        |>function
        |None->
            "posts_qty_field isn't on the page"
            |>Log.error
            |>Result.Error
        |Some posts_qty_field->
            posts_qty_field
            |>read
            |>parse_number_with_multiplier_letter
    
    
    
    let scrape_followers_amount_of_user user_handle =
        let followers_qty_field = $"a[href='/{User_handle.value user_handle}/followers'] span span"
        followers_qty_field
        |>Scraping.try_element
        |>function
        |None->
            $"url '{Twitter_user.url_from_handle user_handle}' doesn't show the Followers field"
            |>Log.error
            |>Result.Error
        |Some followers_qty_field->
            followers_qty_field
            |>read
            |>parse_number_with_multiplier_letter
    
    
    
    let scrape_user_page user_handle =
        url (Twitter_user.url_from_handle user_handle)
        {
            Scraped_user_state.posts_amount = scrape_posts_amount ()
            followers_amount = scrape_followers_amount_of_user user_handle
        }
            
    let scrape_stats_of_users 
        (users: User_handle seq) 
        =
        Log.info "scraping stats of members... "
        users
        |>Seq.map (fun twitter_user ->
            twitter_user
            ,
            scrape_user_page twitter_user
        )

    
    

  


    
    



    


