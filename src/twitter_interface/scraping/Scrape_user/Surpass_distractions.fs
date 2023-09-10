namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.classic
open canopy.types
open rvinowise.twitter
open FParsec



module Surpass_distractions =

            
    let surpass_cookies_agreement ()
        =
        "div[data-testid='BottomBar'] div[role='button']"
        |>Scraping.try_element
        |>function
        |Some cookies_button-> click cookies_button
        |None ->()
        

    
    

  


    
    



    


