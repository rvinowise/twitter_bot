namespace rvinowise.twitter

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
    
  
    let jobs_from_central_database =
        seq {
            let mutable free_user = Central_task_database.resiliently_take_next_free_job ()
            while free_user.IsSome do
                yield free_user.Value
                free_user <- Central_task_database.resiliently_take_next_free_job ()
        }    
    
    
    let harvest_timeline_with_error_check
        browser
        work_db
        tab
        user
        needed_posts_amount
        =
        let when_to_stop needed_amount =
            Finish_harvesting_timeline.finish_after_amount_of_invocations needed_amount
        
        let posts_amount =
            Harvest_posts_from_timeline.resiliently_harvest_user_timeline
                (when_to_stop needed_posts_amount)
                browser
                work_db
                tab
                user
        if needed_posts_amount < posts_amount then
            Harvest_posts_from_timeline.check_insufficient_scraping
                browser
                Timeline_tab.Posts_and_replies
                user
                posts_amount
        posts_amount
    let harvest_timelines_from_jobs
        announce_completeness
        needed_posts_amount
        jobs
        =
        let work_db = Twitter_database.open_connection()
        
        let browser = Browser.open_browser()
        
        jobs
        |>Seq.iter(fun user ->
            
            let posts_amount =
                harvest_timeline_with_error_check
                    browser
                    work_db
                    Timeline_tab.Posts_and_replies
                    user
                    needed_posts_amount
            
            let likes_amount =    
                harvest_timeline_with_error_check
                    browser
                    work_db
                    Timeline_tab.Likes
                    user
                    needed_posts_amount
            
            announce_completeness
                user
                posts_amount
                likes_amount
        ) 
        
    
    let harvest_timelines_from_central_database article_amount =
        harvest_timelines_from_jobs
            (Central_task_database.resiliently_set_task_as_complete Central_task_database.this_working_session_id)
            article_amount
            jobs_from_central_database
    
    [<Fact>]//(Skip="manual")
    let ``try harvest_timelines``()=
        [
            User_handle "GordianBio"
        ]
        |>harvest_timelines_from_jobs
              (Central_task_database.resiliently_set_task_as_complete Central_task_database.this_working_session_id)
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
        