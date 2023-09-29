namespace rvinowise.twitter

open canopy.parallell.functions
open rvinowise.twitter



module Reveal_user_page =

    
    let surpass_content_warning browser =
        "div[data-testid='empty_state_button_text']"
        |>Scraping.try_element browser
        |>function
        |Some revealing_button-> click revealing_button browser
        |None ->()
            
    let reveal_user_page browser user
        =
        url (User_handle.url_from_handle user) browser   
        surpass_content_warning browser
        

    
    

  


    
    



    


