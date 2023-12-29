namespace rvinowise.twitter

open rvinowise.web_scraping

module Scrape_followers_network =
    
    let followers_url user =
        $"{Twitter_settings.base_url}/{User_handle.value user}/followers"
        
    let followees_url user =
        $"{Twitter_settings.base_url}/{User_handle.value user}/following"
        
        
        
    let scrape_user_catalog
        browser
        html_context
        catalog_url 
        = 
        Log.info $"reading elements of catalog {catalog_url} ... " 
        Browser.open_url catalog_url browser
        
        let user_catalog_element = "div[aria-label='Home timeline'] div[data-testid='UserCell']"
        let catalogue =
            Browser.try_element browser $"div:has(>{Twitter_settings.timeline_cell_css})"
        
        match catalogue with
        |Some _ ->
            let items =
                Scrape_dynamic_list.collect_all_html_items_of_dynamic_list
                    browser
                    html_context
                    (fun()-> Scrape_list_members.wait_for_list_loading browser)
                    user_catalog_element
            Log.important $"catalogue has {Seq.length items} items"
            items
        |None->
            Log.error $"{catalog_url} doesn't have a catalogue "|>ignore
            []    
    
    let scrape_following_of_user
        browser
        html_context
        (url_from_user: User_handle -> string)
        starting_user
        =
        starting_user
        |>url_from_user
        |>scrape_user_catalog browser html_context
        |>List.map Parse_twitter_user.parse_twitter_user_cell
    
    
    
    let scrape_acquaintances_of_user
        browser
        html_context
        root_user 
        =
        let followees =
            root_user
            |>scrape_following_of_user
                  browser
                  html_context
                  followees_url
        let followers =
            root_user
            |>scrape_following_of_user
                  browser
                  html_context
                  followers_url
        
        followees
        |>List.map Twitter_profile_from_catalog.handle
        ,
        followers
        |>List.map Twitter_profile_from_catalog.handle
        
        
    