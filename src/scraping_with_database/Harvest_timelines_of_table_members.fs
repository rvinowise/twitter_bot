namespace rvinowise.twitter

open System
open Npgsql
open OpenQA.Selenium
open Xunit
open rvinowise.html_parsing
open rvinowise.twitter
open rvinowise.web_scraping





module Harvest_timelines_of_table_members =
    
    
    let rec resiliently_parse_user_timeline
        (browser: Browser)
        html_context
        database
        needed_posts_amount
        timeline_tab
        user
        parse_post
        should_finish_at_post
        
        =
        $"start harvesting timeline {timeline_tab} or user {user}"
        |>Log.info
        
        let result =
            try
                Scrape_timeline.reveal_and_parse_timeline
                    browser
                    html_context
                    timeline_tab
                    user
                    parse_post
                    should_finish_at_post
            with
            | :? ArgumentException
            | :? WebDriverException
            | :? PostgresException
            | :? Harvesting_exception as exc ->
                Parsing_timeline_result.Exception exc 
        
        match result with
        |Success _
        |Hidden_timeline Protected ->
            browser, result
        |Insufficient amount ->
            $"""
            restarting browser and skipping the timeline because of an insufficient amount of scraped posts.
            """
            |>Log.error|>ignore
            
            Assigning_browser_profiles.switch_profile
                (Central_database.resiliently_open_connection())
                (This_worker.this_worker_id database)
                browser,
            result
            
        |Hidden_timeline failing_of_our_browser ->
            
            match failing_of_our_browser with
            |Loading_denied ->
                Log.info $"""
                Timeline {Timeline_tab.human_name timeline_tab} of user {User_handle.value user} didn't load.
                Switching browser profiles"""
            |No_login |_ ->
                Log.info $"""
                browser {browser.profile} is not logged in to twitter.
                Switching browser profiles"""
            
            let new_browser = 
                Assigning_browser_profiles.switch_profile
                    (Central_database.resiliently_open_connection())
                    (This_worker.this_worker_id database)
                    browser
            resiliently_parse_user_timeline
                new_browser
                html_context
                database
                needed_posts_amount
                timeline_tab
                user
                parse_post
                should_finish_at_post
        |Exception exc ->
            Log.error $"""
                can't harvest timeline {Timeline_tab.human_name timeline_tab} of user {User_handle.value user}:
                {exc.GetType()}
                {exc.Message}.
                Restarting scraping browser and skipping the timeline"""|>ignore
            Assigning_browser_profiles.switch_profile
                    (Central_database.resiliently_open_connection())
                    (This_worker.this_worker_id database)
                    browser,
            result
    
  
    let jobs_from_central_database worker_id =
        seq {
            let mutable free_user = Distributing_jobs_database.resiliently_take_next_free_job worker_id
            while free_user.IsSome do
                yield free_user.Value
                free_user <- Distributing_jobs_database.resiliently_take_next_free_job worker_id
        }    
    
   
    let resiliently_harvest_user_timeline
        browser
        html_context
        local_db
        maximum_posts_amount
        timeline_tab
        user
        =
        
        let familiar_posts_streak = 5;
        
        let is_invoked_many_times =
            Finish_harvesting_timeline.finish_after_amount_of_invocations maximum_posts_amount
        
        let has_encountered_a_streak_of_familiar_posts =
            match timeline_tab with
            |Timeline_tab.Likes ->
                Finish_harvesting_timeline.finish_when_encountered_familiar_liked_posts
                    local_db
                    user
                    familiar_posts_streak
            |Timeline_tab.Posts_and_replies ->
                Finish_harvesting_timeline.finish_when_encountered_familiar_published_posts
                    local_db
                    familiar_posts_streak
            |_-> raise (Harvesting_exception("unknown timeline tab"))
        
        let finish_at_familiar_posts_or_too_many_posts
            post
            =
            match is_invoked_many_times() with
            |Some rason_to_stop ->
                $"{maximum_posts_amount} posts were scraped forom timeline {timeline_tab} of user {user}, it's enough"
                |>Log.info
                Some rason_to_stop
            |None ->
                match
                    has_encountered_a_streak_of_familiar_posts post
                with
                |Some reason ->
                    $"{familiar_posts_streak} familiar posts in a row were scraped forom timeline {timeline_tab} of user {user}, it's enough"
                    |>Log.info
                    Some reason
                |None ->
                    None
        
        resiliently_parse_user_timeline
            browser
            html_context
            local_db
            maximum_posts_amount
            timeline_tab
            user
            (Harvest_posts_from_timeline.write_post_to_db local_db Timeline_tab.Posts_and_replies user)
            finish_at_familiar_posts_or_too_many_posts
            
            
    let harvest_timelines_from_jobs
        local_db
        announce_result
        maximum_posts_amount
        jobs
        =

        
        let browser =
            Assigning_browser_profiles.open_browser_with_free_profile
                (Central_database.resiliently_open_connection())
                (This_worker.this_worker_id local_db)
                
        let html_context = AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default
        
        jobs
        |>Seq.fold(fun browser user ->
            
            let browser,posts_result =
                resiliently_harvest_user_timeline
                    browser
                    html_context
                    local_db
                    maximum_posts_amount
                    Timeline_tab.Posts_and_replies
                    user
            
            let browser,likes_result =    
                match posts_result with
                |Success _ ->
                    let browser,likes_result = 
                        resiliently_harvest_user_timeline
                            browser
                            html_context
                            local_db
                            maximum_posts_amount
                            Timeline_tab.Likes
                            user
                            
                    browser, Some likes_result
                | _ ->
                    browser, None
            
            likes_result
            |>Option.defaultValue posts_result
            |>announce_result
                (This_worker.this_worker_id local_db)
                user
                (Parsing_timeline_result.articles_amount posts_result)
                (
                    likes_result
                    |>Option.map Parsing_timeline_result.articles_amount
                    |>Option.defaultValue 0
                )
            browser    
        )
            browser
    
    let harvest_timelines_from_central_database
        local_db
        article_amount 
        =
        let worker_id = This_worker.this_worker_id local_db
        
        jobs_from_central_database worker_id
        |>harvest_timelines_from_jobs
            local_db
            (Distributing_jobs_database.resiliently_write_final_result)
            article_amount
        |>ignore
        
        Assigning_browser_profiles.release_browser_profile
            (Central_database.resiliently_open_connection())
            worker_id
            
    let ``try harvest_timelines``()=
        [
            User_handle "0xtantin"
        ]
        |>harvest_timelines_from_jobs
              (Local_database.open_connection())
              (fun _ _ _ _ _-> ())
              200
        |>ignore


    let ``prepare tasks for scraping``() =
        let central_db =
            Central_database.open_connection()
            
        {
            Google_spreadsheet.doc_id = "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0"
            page_name="Members"
        }
        |>Create_matrix_from_sheet.users_from_spreadsheet
            (Googlesheet.create_googlesheet_service())
        |>Distributing_jobs_database.write_users_for_scraping central_db

        
    let write_tasks_to_scrape_next_matrix_timeframe matrix_title =

        let central_db =
            Central_database.resiliently_open_connection()
        
        let local_db =
            Local_database.open_connection()
        
        Adjacency_matrix_database.read_members_of_matrix
            local_db
            matrix_title
        |>Distributing_jobs_database.write_users_for_scraping
            central_db
            
    let write_tasks_to_scrape_next_timeframe_of_matrices matrices =

        let central_db =
            Central_database.resiliently_open_connection()
        
        let local_db =
            Local_database.open_connection()
        
        matrices
        |>Seq.collect (
            Adjacency_matrix_database.read_members_of_matrix local_db
        )
        |>Set.ofSeq
        |>Distributing_jobs_database.write_users_for_scraping
            central_db
            
            
            
    let ``try harvest_all_last_actions_of_users (specific tabs)``()=
        
        resiliently_parse_user_timeline
            (Browser.open_browser())
            (AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default)
            (Local_database.open_connection())
            Int32.MaxValue
            Timeline_tab.Posts_and_replies
            (User_handle "KeithComito")            
        |>ignore