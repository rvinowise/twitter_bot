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
        
    
    [<Fact>]//(Skip="manual")
    let ``try harvest_all_last_actions_of_users (both tabs)``()=
  
        let user_timelines =
            users_from_spreadsheet
                (Googlesheet.create_googlesheet_service())
                {
                    Google_spreadsheet.doc_id = "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0"
                    page_id=0
                    page_name="Members"
                }
            |>List.collect (fun user ->
                [
                    user,Timeline_tab.Posts_and_replies;
                    user,Timeline_tab.Likes
                ]
            )
        
        let database = Twitter_database.open_connection()
        
        Harvest_posts_from_timeline.resilient_step_of_harvesting_timelines
            (Finish_harvesting_timeline.finish_after_amount_of_invocations 500)
            (Browser.open_browser())
            database
            user_timelines
