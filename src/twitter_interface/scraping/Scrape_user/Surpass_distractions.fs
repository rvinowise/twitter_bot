namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.parallell.functions
open canopy.types
open rvinowise.twitter
open FParsec



module Surpass_distractions =

            
    let surpass_cookies_agreement browser
        =
        "div[data-testid='BottomBar'] div[role='button']"
        |>Scraping.try_element browser
        |>function
        |Some cookies_button-> click cookies_button browser
        |None ->()
        

    
    

  


    
    



    


