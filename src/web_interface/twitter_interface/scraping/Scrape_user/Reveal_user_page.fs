namespace rvinowise.twitter

open canopy.parallell.functions
open rvinowise.web_scraping



module Reveal_user_page =

    
    let surpass_content_warning browser =
        "div[data-testid='empty_state_button_text']"
        |>Browser.try_element browser
        |>function
        |Some revealing_button-> Browser.click_element revealing_button browser
        |None ->()
            
    let reveal_user_page browser user
        =
        Browser.open_url (User_handle.url_from_handle user) browser   
        surpass_content_warning browser
        

    
    

  


    
    



    


