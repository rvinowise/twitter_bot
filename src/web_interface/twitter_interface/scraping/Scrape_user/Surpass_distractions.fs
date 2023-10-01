namespace rvinowise.twitter

open canopy.parallell.functions
open rvinowise.web_scraping



module Surpass_distractions =

            
    let surpass_cookies_agreement browser
        =
        "div[data-testid='BottomBar'] div[role='button']"
        |>Browser.try_element browser
        |>function
        |Some cookies_button-> Browser.click_element cookies_button browser
        |None ->()
        

    
    

  


    
    



    


