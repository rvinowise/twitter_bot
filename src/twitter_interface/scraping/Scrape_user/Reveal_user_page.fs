namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.classic
open canopy.types
open rvinowise.twitter
open FParsec



module Reveal_user_page =

            
    let reveal_user_page user
        =
        url (User_handle.url_from_handle user)    
        "div[data-testid='empty_state_button_text']"
        |>Scraping.try_element
        |>function
        |Some revealing_button-> click revealing_button
        |None ->()
        

    
    

  


    
    



    


