﻿namespace rvinowise.twitter

open System
open OpenQA.Selenium
open Xunit
open rvinowise.html_parsing
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
            let mutable free_user = Central_task_database.resiliently_take_next_free_job worker_id
            while free_user.IsSome do
                yield free_user.Value
                free_user <- Central_task_database.resiliently_take_next_free_job worker_id
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
        
        let browser,posts_amount =
            Harvest_posts_from_timeline.resiliently_harvest_user_timeline
                (when_to_stop needed_posts_amount)
                browser
                html_context
                work_db
                tab
                user
        let is_sufficient = 
            if posts_amount < needed_posts_amount then
                Harvest_posts_from_timeline.is_scraping_sufficient
                    browser
                    tab
                    user
                    posts_amount
            else true
        browser,posts_amount,is_sufficient
        
    let harvest_timelines_from_jobs
        local_db
        announce_result
        needed_posts_amount
        jobs
        =
        let browser =
            Assigning_browser_profiles.open_browser_with_free_profile
                (Central_task_database.resiliently_open_connection())
                (This_worker.this_worker_id local_db)
                
        let html_context = AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default
        
        jobs
        |>Seq.fold(fun browser user ->
            
            let browser,posts_amount,is_enough_posts =
                harvest_timeline_with_error_check
                    browser
                    html_context
                    local_db
                    Timeline_tab.Posts_and_replies
                    user
                    needed_posts_amount
            
            let browser,likes_amount,is_enough_likes =    
                harvest_timeline_with_error_check
                    browser
                    html_context
                    local_db
                    Timeline_tab.Likes
                    user
                    needed_posts_amount
            
            match is_enough_likes,is_enough_posts with
            |true,true ->
                Scraping_user_status.Completed    
            | _ ->
                Scraping_user_status.Insufficient
            |>announce_result
                (This_worker.this_worker_id local_db)
                user
                posts_amount
                likes_amount
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
            (Central_task_database.resiliently_write_final_result)
            article_amount
            (jobs_from_central_database worker_id)
    
    [<Fact>]//(Skip="manual")
    let ``try harvest_timelines``()=
        [
            User_handle "AD74593974"
        ]
        |>harvest_timelines_from_jobs
              (Twitter_database.open_connection())
              (fun _ _ _ _ _-> ())
              100
        

    [<Fact(Skip="manual")>]
    let ``prepare tasks for scraping``() =
        let central_db =
            Central_task_database.open_connection()
            
        {
            Google_spreadsheet.doc_id = "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0"
            page_id=0
            page_name="Members"
        }
        |>users_from_spreadsheet
            (Googlesheet.create_googlesheet_service())
        |>Central_task_database.write_users_for_scraping central_db
        