namespace rvinowise.twitter

open canopy.parallell.functions
open Xunit

module Scrape_followers_network =
    
    let followers_url user =
        $"{Twitter_settings.base_url}/{User_handle.value user}/followers"
        
    let followees_url user =
        $"{Twitter_settings.base_url}/{User_handle.value user}/following"
        
    
    let scrape_following_of_user
        browser
        (url: User_handle -> string)
        starting_user
        =
        starting_user
        |>url
        |>Scrape_dynamic_list.scrape_catalog browser
        |>Set.map Parse_twitter_user.parse_twitter_user_cell
    
    
    
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