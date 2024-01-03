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
    
  
    let needed_posts_amount_of_user
        (users: Map<User_handle, (int*int)>)
        user
        =
        ()
    
    
    [<Fact(Skip="manual")>]//
    let ``try harvest_timelines``()=
  
        let central_db =
            Central_task_database.open_connection()
        
        let work_db = Twitter_database.open_connection()
        
        let browser = Browser.open_browser()
        
        seq {
            let mutable free_user = Central_task_database.take_next_free_job central_db
            while free_user.IsSome do
                yield free_user.Value
                free_user <- Central_task_database.take_next_free_job central_db
        }
        |>Seq.iter(fun user ->
            let posts_amount =
                Harvest_posts_from_timeline.resiliently_harvest_user_timeline
                    (Finish_harvesting_timeline.finish_after_amount_of_invocations 100)
                    browser
                    work_db
                    Timeline_tab.Posts_and_replies
                    user
            
            let likes_amount =    
                Harvest_posts_from_timeline.resiliently_harvest_user_timeline
                    (Finish_harvesting_timeline.finish_after_amount_of_invocations 100)
                    browser
                    work_db
                    Timeline_tab.Likes
                    user
                    
            Central_task_database.set_task_as_complete
                central_db
                Central_task_database.this_working_session_id
                user
                posts_amount
                likes_amount
        )        
        
        
       

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
        