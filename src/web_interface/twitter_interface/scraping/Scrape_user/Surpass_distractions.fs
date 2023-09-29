namespace rvinowise.twitter

open canopy.parallell.functions
open rvinowise.twitter



module Surpass_distractions =

            
    let surpass_cookies_agreement browser
        =
        "div[data-testid='BottomBar'] div[role='button']"
        |>Scraping.try_element browser
        |>function
        |Some cookies_button-> click cookies_button browser
        |None ->()
        

    
    

  


    
    



    


