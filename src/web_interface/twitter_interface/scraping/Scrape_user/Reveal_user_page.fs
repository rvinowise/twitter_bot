namespace rvinowise.twitter

open canopy.parallell.functions
open rvinowise.html_parsing
open rvinowise.web_scraping


type Timeline_not_opened_reason =
|Loading_denied
|Protected
|No_login
|Nonexisting_user

module Timeline_not_opened_reason =
    let db_value (reason:Timeline_not_opened_reason) =
        match reason with
        |Loading_denied -> "Loading_denied"
        |Protected -> "Protected"
        |No_login -> "No_login"
        |Nonexisting_user -> "Nonexisting_user"
        
type Page_revealing_result =
|Revealed
|Failed of Timeline_not_opened_reason


module Reveal_user_page =

    
    let is_browser_profile_without_login
        browser
        html_context
        =
        "a[data-testid='login']"
        |>Browser.try_element  browser
        |>function
        |Some login_button->
            Some Loading_denied
        |None->
            None
    
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
            Some Loading_denied
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
    
    let is_nonexisting_user
        browser
        html_context
        =
        browser
        |>Browser.elements "div[data-testid='empty_state_header_text']"
        |>List.map (Html_node.from_scraped_node_and_context html_context)
        |>List.exists(fun node ->
            node
            |>Html_node.inner_text = "This account doesn’t exist"
        )|>function
        |true->
            Some Nonexisting_user
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
        is_nonexisting_user
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
        |>Option.map Page_revealing_result.Failed
        |>Option.defaultValue Page_revealing_result.Revealed
     

  


    
    



    


