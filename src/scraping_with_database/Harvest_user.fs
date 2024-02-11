namespace rvinowise.twitter

open System
open OpenQA.Selenium
open canopy.types
open rvinowise.twitter.database_schema
open rvinowise.web_scraping

module Harvest_user =
    
    
    let harvest_top_of_user_page
        browser
        parsing_context
        database
        user
        =
        Log.info $"harvesting bio and social activity of user {User_handle.value user}"
        
        let revealing_page_successful = 
            try
                Reveal_user_page.reveal_user_page browser user
                true
            with
            | :? WebDriverException as exc ->
                $"can't reveal the page of user {User_handle.value user}: {exc.Message}"
                |>Log.error|>ignore
                false
        
        let scraping_briefing_successful = 
            try 
                user
                |>Scrape_user_briefing.scrape_user_briefing parsing_context browser 
                |>Twitter_user_database.write_user_briefing
                    database
                true
            with
            | :? CanopyElementNotFoundException
            | :? ArgumentException as exc ->
                $"can't scrape bio of user {User_handle.value user}: {exc.Message}"
                |>Log.error|>ignore
                false
        
        let social_activity =
            user
            |>Scrape_user_social_activity.try_scrape_user_social_activity
                browser
           
        Social_activity_database.write_optional_social_activity_of_user
              database
              DateTime.UtcNow
              user
              social_activity
        
        revealing_page_successful
        && scraping_briefing_successful
        && (User_social_activity.all_fields_available social_activity)
    
    let ``try harvest_top_of_user_page``()=
        let result =
            harvest_top_of_user_page
                (Browser.open_browser())
                (AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default)
                (Local_database.open_connection())
                (User_handle "societyinexcess")
        ()
    
    let rec resiliently_harvest_top_pages_of_users
        browser
        html_context
        database
        (users: User_handle list)
        =
        match users with
        |[]->()
        |user_to_scrape::rest_users ->
            let is_successful =
                harvest_top_of_user_page
                    browser
                    html_context
                    database
                    user_to_scrape
            
            let new_browser = 
                if is_successful then
                    browser
                else
                    Assigning_browser_profiles.switch_profile
                        (Central_database.resiliently_open_connection())
                        (This_worker.this_worker_id database)
                        browser
            
            resiliently_harvest_top_pages_of_users
                new_browser
                html_context
                database
                rest_users
            
    let harvest_members_of_matrix_briefing()=
        let browser = Browser.open_browser()
        let html_context = AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default
        let local_database = Local_database.open_connection()
        
        let matrix_members =
            Adjacency_matrix_database.read_members_of_matrix
                local_database
                Adjacency_matrix.Longevity_members
        
        let known_names =
            Twitter_user_database.read_usernames_map_from_briefing local_database
        
        matrix_members
        |>List.filter (fun account -> Map.containsKey account known_names |> not)
        |>resiliently_harvest_top_pages_of_users
            browser
            html_context
            local_database

    let last_date_of_known_social_activity
        database
        user
        =
        let amounts_with_dates =
            [
                Social_activity_amounts_type.Followers
                Social_activity_amounts_type.Followees
                Social_activity_amounts_type.Posts
            ]
            |>List.map(fun activity_type ->
                activity_type,
                Social_activity_database.read_last_amount_for_user
                    database
                    activity_type
                    user
            )
            |>List.sortBy(fun (activity,scraped_amount) -> scraped_amount.datetime)
        
        let olders_scraping_date =
            amounts_with_dates
            |>List.head
            |>snd
            |> _.datetime
        
        olders_scraping_date,
        amounts_with_dates
        |>List.map(fun (activity_type,scraped_amount) ->
            activity_type, scraped_amount.amount    
        )
        |>Map.ofList
            
                
        