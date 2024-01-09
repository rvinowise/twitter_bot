namespace rvinowise.twitter

open Npgsql
open rvinowise.twitter
open rvinowise.web_scraping
open Xunit
open System

(* user interactions from local databases are combined in the central database.
this module exports them into the google sheet  *)

module Matrix_from_db_to_sheet =
    
    
    let export_local_interactions
        (database: NpgsqlConnection)
        (sheet: Google_spreadsheet)
        =
        ()
        
    [<Fact>]    
    let ``try stitched_interactions_to_sheet``()=
        let combined_sheet = {
            doc_id = "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0"
            page_id = 1824018874
            page_name = "All interactions"
        }
        let central_db = Central_task_database.open_connection()
        
        let all_users =
            Central_task_database.read_last_user_jobs_with_status
                central_db
                (Scraping_user_status.Completed (Success 0))
        
        // let result =
        //     [
        //         
        //     ]
        //     |>Adjacency_matrix_compound.update_googlesheet_with_total_interactions
        //         (Central_task_database.open_connection())
        //         combined_sheet
        //         _
        //         _
        //         all_users
                
        ()
   