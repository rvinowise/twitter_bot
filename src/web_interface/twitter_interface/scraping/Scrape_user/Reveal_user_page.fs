namespace rvinowise.twitter

open canopy.parallell.functions
open rvinowise.html_parsing
open rvinowise.web_scraping

type Page_invisibility =
|Failed_loading
|Protected

type Page_revealing =
|Revealed
|Failed of Page_invisibility

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
        )|>function
        |true->
            Some Failed_loading
        |false->
            None
    
    let is_timeline_protected_from_strangers
        browser
        html_context
        =
        browser
        |>Browser.elements "span"
        |>List.map (Html_node.from_scraped_node_and_context html_context)
        |>List.exists(fun span_node ->
            span_node
            |>Html_node.direct_text = "These posts are protected"
        )|>function
        |true->
            Some Protected
        |false->
            None
    
    
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
        

    let timeline_visibility_testers = [
        is_timeline_failing_loading
        is_timeline_protected_from_strangers
    ]
    
    let reveal_timeline
        browser
        html_context
        tab
        user
        =
        Browser.open_url $"{Twitter_settings.base_url}/{User_handle.value user}/{tab}" browser
        surpass_content_warning browser
        
        timeline_visibility_testers
        |>List.tryPick(fun parser -> 
            parser browser html_context
        )
        |>Option.map Page_revealing.Failed
        |>Option.defaultValue Page_revealing.Revealed
     

  


    
    



    


