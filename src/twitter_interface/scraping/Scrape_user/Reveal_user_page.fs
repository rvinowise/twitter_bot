namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.parallell.functions
open rvinowise.twitter
open FParsec



module Reveal_user_page =

            
    let reveal_user_page browser user
        =
        url (User_handle.url_from_handle user) browser   
        "div[data-testid='empty_state_button_text']"
        |>Scraping.try_element browser
        |>function
        |Some revealing_button-> click revealing_button browser
        |None ->()
        

    
    

  


    
    



    


