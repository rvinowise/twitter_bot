namespace rvinowise.twitter

open System
open OpenQA.Selenium
open rvinowise.web_scraping

module Harvest_followers_network =
    
    
    let harvest_top_of_user_page
        parsing_context
        browser
        db_connection
        user
        =
        Log.info $"harvesting bio of user {User_handle.value user}"
        Reveal_user_page.reveal_user_page browser user
        
        let user_briefing =
            Scrape_user_briefing.scrape_user_briefing parsing_context browser user
        
        Social_user_database.write_user_briefing
            db_connection
            user_briefing

        let user_activity =
            Scrape_user_social_activity.scrape_user_social_activity
                browser
                user
                
        Social_activity_database.write_optional_social_activity_of_user
              db_connection
              DateTime.Now
              user
              user_activity
        
   
    let harvest_user
        parsing_context
        browser
        db_connection
        user
        =
        Log.important $"harvesting user {user}"
        Surpass_distractions.surpass_cookies_agreement browser
        harvest_top_of_user_page
            parsing_context
            browser
            db_connection
            user

        let followees,followers =
            user
            |>Scrape_followers_network.scrape_acquaintances_of_user browser
        
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
        parsing_context
        browser
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
                    parsing_context
                    browser
                    db_connection
                    observed_user
        
            let new_unknown_users_around =
                followers
                |>Set.union followees
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
                    (DateTime.Now-repeat_if_older_than)
                >>not
            )
            
    let rec resilient_step_of_harvesting_following
        parsing_context
        (browser:Browser)
        db_connection
        repeat_if_older_than
        unknown_users_around
        =
        let browser,new_unknown_users_around =
            try
                browser,
                harvest_user_adding_his_acquaintances
                    parsing_context
                    browser
                    db_connection
                    repeat_if_older_than
                    unknown_users_around
            with
            | :? WebDriverException as exc ->
                Log.error $"""can't harvest user: {exc.Message}. Restarting scraping browser"""|>ignore
                browser.restart()
                browser,unknown_users_around
            | :? ArgumentNullException as exc ->
                Log.error $"""was the browser closed? {exc.Message}. Restarting scraping browser"""|>ignore
                browser.restart()
                browser,unknown_users_around
                
        if new_unknown_users_around <> [] then
            resilient_step_of_harvesting_following
                parsing_context
                browser
                db_connection
                repeat_if_older_than
                new_unknown_users_around        
            
    let harvest_following_network_around_user
        parsing_context
        browser
        db_connection
        repeat_if_older_than
        root_user
        =
        resilient_step_of_harvesting_following
            parsing_context
            browser
            db_connection
            repeat_if_older_than
            [root_user]
            
   