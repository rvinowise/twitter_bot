namespace rvinowise.twitter

open canopy.parallell.functions
open rvinowise.html_parsing

module Scrape_followers_network =
    
    let followers_url user =
        $"{Twitter_settings.base_url}/{User_handle.value user}/followers"
        
    let followees_url user =
        $"{Twitter_settings.base_url}/{User_handle.value user}/following"
        
        
        
    let scrape_user_catalog browser catalog_url = 
        Log.info $"reading elements of catalog {catalog_url} ... " 
        url catalog_url browser
        
        let user_catalog_element = "div[data-testid='UserCell']"
        let catalogue =
            Scraping.try_element browser "div:has(>div[data-testid='cellInnerDiv'])"
        
        match catalogue with
        |Some _ ->
            let items = Scrape_dynamic_list.collect_all_items_of_dynamic_list browser user_catalog_element
            Log.important $"catalogue has {Seq.length items} items"
            items
        |None->
            Log.error $"{catalog_url} doesn't have a catalogue "|>ignore
            Set.empty
    
    
    let scrape_following_of_user
        browser
        (url_from_user: User_handle -> string)
        starting_user
        =
        starting_user
        |>url_from_user
        |>scrape_user_catalog browser
        |>Set.map (Html_node.from_html_string>> Parse_twitter_user.parse_twitter_user_cell)
    
    
    
    let scrape_acquaintances_of_user browser root_user =
        let followees =
            root_user
            |>scrape_following_of_user browser
                  followees_url
        let followers =
            root_user
            |>scrape_following_of_user browser
                  followers_url
        
        followees
        |>Set.map Twitter_profile_from_catalog.handle
        ,
        followers
        |>Set.map Twitter_profile_from_catalog.handle
        
        
    