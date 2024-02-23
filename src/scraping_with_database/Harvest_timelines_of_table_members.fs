namespace rvinowise.twitter

open System
open OpenQA.Selenium
open Xunit
open rvinowise.html_parsing
open rvinowise.twitter
open rvinowise.web_scraping





module Harvest_timelines_of_table_members =
    
    
    
    
  
    let jobs_from_central_database worker_id =
        seq {
            let mutable free_user = Distributing_jobs_database.resiliently_take_next_free_job worker_id
            while free_user.IsSome do
                yield free_user.Value
                free_user <- Distributing_jobs_database.resiliently_take_next_free_job worker_id
        }    
    
   
        
        
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
                Harvest_posts_from_timeline.resiliently_harvest_user_timeline
                    browser
                    html_context
                    local_db
                    needed_posts_amount
                    Timeline_tab.Posts_and_replies
                    user
            
            let browser,likes_result =    
                match posts_result with
                |Success _ ->
                    let browser,likes_result = 
                        Harvest_posts_from_timeline.resiliently_harvest_user_timeline
                            browser
                            html_context
                            local_db
                            needed_posts_amount
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
                (Harvesting_timeline_result.articles_amount posts_result)
                (
                    likes_result
                    |>Option.map Harvesting_timeline_result.articles_amount
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
        
        harvest_timelines_from_jobs
            local_db
            (Distributing_jobs_database.resiliently_write_final_result)
            article_amount
            (jobs_from_central_database worker_id)
        |>ignore
        
        Assigning_browser_profiles.release_browser_profile
            (Central_database.resiliently_open_connection())
            worker_id
            
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
        |>Create_matrix_from_sheet.users_from_spreadsheet
            (Googlesheet.create_googlesheet_service())
        |>Distributing_jobs_database.write_users_for_scraping central_db
    
    let write_tasks_to_scrape_next_matrix_timeframe ()=

        let central_db =
            Central_database.resiliently_open_connection()
        
        let local_db =
            Local_database.open_connection()
        
        Adjacency_matrix_database.read_members_of_matrix
            local_db
            Adjacency_matrix.AI_members
        |>Distributing_jobs_database.write_users_for_scraping
            central_db
            