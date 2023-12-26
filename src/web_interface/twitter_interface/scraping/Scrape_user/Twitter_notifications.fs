namespace rvinowise.twitter

open canopy.parallell.functions
open rvinowise.web_scraping



module Twitter_notifications =

            
    let surpass_cookies_agreement browser =
        "div[data-testid='BottomBar'] div[role='button']"
        |>Browser.try_element browser
        |>function
        |Some cookies_button-> Browser.click_element cookies_button browser
        |None ->()
        

    
    let is_timeline_refusing_to_load browser =()
        

  


    
    



    


