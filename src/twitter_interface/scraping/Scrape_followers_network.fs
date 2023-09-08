namespace rvinowise.twitter

open canopy.classic
open Xunit

module Scrape_followers_network =
    
    let followers_url user =
        $"{Twitter_settings.base_url}/{User_handle.value user}/followers"
        
    let followees_url user =
        $"{Twitter_settings.base_url}/{User_handle.value user}/following"
        
    
    let scrape_following_of_user
        (url: User_handle -> string)
        starting_user
        =
        starting_user
        |>url
        |>Scrape_catalog.scrape_catalog
        |>Set.map Parse_twitter_user.parse_twitter_user_cell
    
    
    
    let scrape_acquaintances_of_user root_user =
        let followees =
            root_user
            |>scrape_following_of_user
                  followees_url
        let followers =
            root_user
            |>scrape_following_of_user
                  followers_url
        
        followees
        |>Set.map Twitter_profile_from_catalog.handle
        ,
        followers
        |>Set.map Twitter_profile_from_catalog.handle