namespace rvinowise.twitter

open System
open OpenQA.Selenium
open canopy.types
open rvinowise.web_scraping

module Harvest_followers_network =
    
    
    let harvest_user
        browser
        parsing_context
        db_connection
        user
        =
        Log.important $"harvesting user {user}"
        Twitter_notifications.surpass_cookies_agreement browser
        Harvest_user.harvest_top_of_user_page
            browser
            parsing_context
            db_connection
            user
        |>ignore

        let followees,followers =
            user
            |>Scrape_followers_network.scrape_acquaintances_of_user
                  browser
                  parsing_context
        
        Social_following_database.write_social_connections_of_user
            db_connection   
            user
            followees
            followers
        
        Social_following_database.mark_user_as_visited_now
            db_connection
            user
            
        followees,followers
    
    exception Browser_failed_when_harvesting_following of User_handle list
    
    
    let harvest_user_adding_his_acquaintances
        browser
        parsing_context
        db_connection
        repeat_if_older_than
        unknown_users_around
        =
        match unknown_users_around with
        |[] ->
            Log.info "harvesting acquaintances network has finished because there's no unknown users around"
            []
        |observed_user::rest_unknown_users->
            let followees, followers =
                harvest_user
                    browser
                    parsing_context
                    db_connection
                    observed_user
        
            let new_unknown_users_around =
                followees
                |>Seq.append followers
                |>Set.ofSeq 
                |>Set.filter(fun user->
                    rest_unknown_users
                    |>List.contains user
                    |>not
                )
                |>Seq.append rest_unknown_users
                |>List.ofSeq
            
            
            new_unknown_users_around
            |>List.filter (
                Social_following_database.was_user_harvested_recently
                    db_connection
                    (DateTime.UtcNow-repeat_if_older_than)
                >>not
            )
            
    let rec resilient_step_of_harvesting_following
        (browser:Browser)
        parsing_context
        db_connection
        repeat_if_older_than
        unknown_users_around
        =
        let browser,new_unknown_users_around =
            try
                browser,
                harvest_user_adding_his_acquaintances
                    browser
                    parsing_context
                    db_connection
                    repeat_if_older_than
                    unknown_users_around
            with
            | :? WebDriverException as exc ->
                Log.error $"""can't harvest user: {exc.Message}. Restarting scraping browser"""|>ignore
                
                browser|>Browser.restart,
                unknown_users_around
            | :? ArgumentNullException as exc ->
                Log.error $"""was the browser closed? {exc.Message}. Restarting scraping browser"""|>ignore
                browser|>Browser.restart,
                unknown_users_around
            | :? CanopyElementNotFoundException as exc ->
                Log.error $"""scraping user {List.head unknown_users_around} failed: {exc.Message}. skipping this user"""|>ignore
                browser,List.tail unknown_users_around
                
        if new_unknown_users_around <> [] then
            resilient_step_of_harvesting_following
                browser
                parsing_context
                db_connection
                repeat_if_older_than
                new_unknown_users_around        
            
    let harvest_following_network_around_user
        browser
        parsing_context
        db_connection
        repeat_if_older_than
        root_user
        =
        resilient_step_of_harvesting_following
            browser
            parsing_context
            db_connection
            repeat_if_older_than
            [root_user]
            
   