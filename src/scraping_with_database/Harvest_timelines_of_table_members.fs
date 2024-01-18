namespace rvinowise.twitter

open System
open OpenQA.Selenium
open Xunit
open rvinowise.html_parsing
open rvinowise.twitter
open rvinowise.web_scraping





module Harvest_timelines_of_table_members =
    
    
    let users_from_spreadsheet
        service
        sheet
        =
        Googlesheet_reading.read_table
            Parse_google_cell.urls
            service
            sheet
        |>Table.transpose Cell.empty
        |>List.head
        |>List.skip 1
        |>List.map (fun user_cell ->
            match user_cell.value with
            |Cell_value.Text url ->
                match User_handle.try_handle_from_url url with
                |Some handle -> handle
                |None -> raise (TypeAccessException $"failed to find a user handle in the url {url}")
            |_ ->
                $"this cell should have text in it, but there's {user_cell.value}"
                |>TypeAccessException
                |>raise 
        )
    
  
    let jobs_from_central_database worker_id =
        seq {
            let mutable free_user = Distributing_jobs_database.resiliently_take_next_free_job worker_id
            while free_user.IsSome do
                yield free_user.Value
                free_user <- Distributing_jobs_database.resiliently_take_next_free_job worker_id
        }    
    
    
    let harvest_timeline_with_error_check
        browser
        html_context
        work_db
        tab
        user
        needed_posts_amount
        =
        let when_to_stop needed_amount =
            Finish_harvesting_timeline.finish_after_amount_of_invocations needed_amount
        
        let browser,result =
            Harvest_posts_from_timeline.resiliently_harvest_user_timeline
                (when_to_stop needed_posts_amount)
                browser
                html_context
                work_db
                tab
                user
        
        browser
        ,
        match result with
        |Harvested_some_amount amount ->
            if
                amount < needed_posts_amount
                &&
                Harvest_posts_from_timeline.is_scraping_sufficient
                    browser
                    tab
                    user
                    amount
                |>not
            then
                Insufficient amount
            else
                Success amount
        |Hidden_timeline Protected ->
            result
        |Exception _ ->
            result
        |Success _
        |Insufficient _
        |Hidden_timeline Loading_denied ->
            $"unexpected harvesting result at this point: {result}"
            |>Log.error
            |>ignore
            result
        
    
    
    let unify_results
        (results: Harvesting_timeline_result seq)
        =
        let importance (result: Harvesting_timeline_result) =
            match result with
            |Harvested_some_amount _-> 0
            |Success _-> 1
            |Insufficient _-> 3
            |Hidden_timeline reason ->
                match reason with
                |Protected -> 2
                |Loading_denied -> 4
            |Exception _-> 5
        
        results
        |>Seq.fold(fun combined_result result ->
            if
                importance result > importance combined_result
            then
                result
            else
                combined_result
        )
            (Success 0)
        
    let harvest_timelines_from_jobs
        local_db
        announce_result
        needed_posts_amount
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
                harvest_timeline_with_error_check
                    browser
                    html_context
                    local_db
                    Timeline_tab.Posts_and_replies
                    user
                    needed_posts_amount
            
            let browser,likes_result =    
                harvest_timeline_with_error_check
                    browser
                    html_context
                    local_db
                    Timeline_tab.Likes
                    user
                    needed_posts_amount
            
            unify_results
                [posts_result;likes_result]
            |>announce_result
                (This_worker.this_worker_id local_db)
                user
                (Harvesting_timeline_result.posts_amount posts_result)
                (Harvesting_timeline_result.posts_amount likes_result)
            browser    
        )
            browser
        |>ignore
    
    let harvest_timelines_from_central_database
        local_db
        article_amount 
        =
        let worker_id = This_worker.this_worker_id local_db
        harvest_timelines_from_jobs
            local_db
            (Distributing_jobs_database.resiliently_write_final_result)
            article_amount
            (jobs_from_central_database worker_id)
    

    let ``try harvest_timelines``()=
        [
            User_handle "AD74593974"
        ]
        |>harvest_timelines_from_jobs
              (Local_database.open_connection())
              (fun _ _ _ _ _-> ())
              100
        


    let ``prepare tasks for scraping``() =
        let central_db =
            Central_database.open_connection()
            
        {
            Google_spreadsheet.doc_id = "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0"
            page_name="Members"
        }
        |>users_from_spreadsheet
            (Googlesheet.create_googlesheet_service())
        |>Distributing_jobs_database.write_users_for_scraping central_db
    
    let write_tasks_to_scrape_next_matrix_timeframe ()=
        let central_db =
            Central_database.open_connection()
        
        Adjacency_matrix_database.read_members_of_matrix
            central_db
            Adjacency_matrix.Longevity_members
        |>Distributing_jobs_database.write_users_for_scraping
            central_db
            