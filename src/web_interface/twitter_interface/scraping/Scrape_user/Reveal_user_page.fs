namespace rvinowise.twitter

open canopy.parallell.functions
open rvinowise.html_parsing
open rvinowise.web_scraping

type Revealing_user_page =
|Revealed
|Timeline_failed

module Reveal_user_page =

    
    let is_timeline_failing_loading
        browser
        html_context
        =
        browser
        |>Browser.elements "span"
        |>List.map (Html_node.from_scraped_node_and_context html_context)
        |>List.exists(fun span_node ->
            span_node
            |>Html_node.direct_text = "Something went wrong. Try reloading."
        )
    
    
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
        

    let reveal_timeline
        browser
        html_context
        tab
        user
        =
        Browser.open_url $"{Twitter_settings.base_url}/{User_handle.value user}/{tab}" browser
        surpass_content_warning browser
        if
            is_timeline_failing_loading
                browser
                html_context
        then
            Revealing_user_page.Timeline_failed
        else
            Revealed

  


    
    



    


