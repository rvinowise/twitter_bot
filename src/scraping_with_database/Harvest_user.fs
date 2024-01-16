﻿namespace rvinowise.twitter

open System
open OpenQA.Selenium
open canopy.types
open rvinowise.web_scraping

module Harvest_user =
    
    
    let harvest_top_of_user_page
        browser
        parsing_context
        db_connection
        user
        =
        Log.info $"harvesting bio of user {User_handle.value user}"
        Reveal_user_page.reveal_user_page browser user
        
        try 
            user
            |>Scrape_user_briefing.scrape_user_briefing parsing_context browser 
            |>Twitter_user_database.write_user_briefing
                db_connection
        with
        | :? CanopyElementNotFoundException as exc ->
            $"can't scrape bio of user {User_handle.value user}: {exc.Message}"
            |>Log.error|>ignore
            
        user
        |>Scrape_user_social_activity.try_scrape_user_social_activity
            browser
        |>Social_activity_database.write_optional_social_activity_of_user
              db_connection
              DateTime.Now
              user
  
    
    
    
    let harvest_members_of_matrix()=
        let browser = Browser.open_browser()
        let html_context = AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default
        let central_database = Central_database.open_connection()
        let local_database = Local_database.open_connection()
        
        let matrix_members =
            Adjacency_matrix.read_sorted_members_of_matrix
                central_database
                "Longevity members"
                
        matrix_members
        |>List.iter (
            harvest_top_of_user_page
                browser
                html_context
                local_database
        )