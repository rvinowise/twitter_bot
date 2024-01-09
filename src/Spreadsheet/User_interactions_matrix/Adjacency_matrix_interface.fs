namespace rvinowise.twitter

open Npgsql
open rvinowise.twitter
open rvinowise.web_scraping
open Xunit



module Adjacency_matrix_interface =
        
    let likes_color = {r=1;g=0;b=0}
    let reposts_color = {r=0;g=1;b=0}
    let replies_color = {r=0.2;g=0.2;b=1}
    
    
    let sorted_users_from_handles
        database
        all_sorted_handles
        =
        let user_names =
            Social_user_database.read_user_names_from_handles
                database
        
        all_sorted_handles
        |>List.map (fun handle ->
            {
                handle= handle
                name =
                    (user_names
                    |>Map.tryFind handle
                    |>Option.defaultValue (User_handle.value handle))
            }    
        )
    
    let interaction_type_from_db
        all_user_handles
        read_interactions
        color
        =
        all_user_handles
        |>Adjacency_matrix.maps_of_user_interactions
            read_interactions    
        |>Adjacency_matrix.interaction_type_for_colored_interactions
            color
    
   
    let update_googlesheet
        (database: NpgsqlConnection)
        sheet_document
        all_sorted_handles
        =
        //https://docs.google.com/spreadsheets/d/1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY/edit#gid=0
        //"1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
       
        let competition_document = sheet_document
        let likes_googlesheet = {
            Google_spreadsheet.doc_id = competition_document
            page_id=0
            page_name="Likes"
        }
        
        let reposts_googlesheet = {
            Google_spreadsheet.doc_id = competition_document
            page_id=2108706810
            page_name="Reposts"
        }
   
        let replies_googlesheet = {
            Google_spreadsheet.doc_id = competition_document
            page_id=2007335692
            page_name="Replies"
        }
        
        let everything_googlesheet = {
            Google_spreadsheet.doc_id = competition_document
            page_id=1019851571
            page_name="Everything"
        }
            
        let all_sorted_users =
            sorted_users_from_handles
                database
                all_sorted_handles
        
        let all_user_handles =
            Set.ofList all_sorted_handles
        
        let likes_interactions =
            interaction_type_from_db
                all_user_handles
                (User_interactions_from_posts.read_likes_by_user database)
                likes_color
        
        let reposts_interactions =
            interaction_type_from_db
                all_user_handles
                (User_interactions_from_posts.read_reposts_by_user database)
                reposts_color
        
        let replies_interactions =
            interaction_type_from_db
                all_user_handles
                (User_interactions_from_posts.read_replies_by_user database)
                replies_color
        
        
        let update_googlesheet_with_interaction_type =
            Adjacency_matrix_single.update_googlesheet
                all_sorted_users
        
    
        update_googlesheet_with_interaction_type
            likes_googlesheet
            3
            0.4
            likes_interactions

        update_googlesheet_with_interaction_type
            reposts_googlesheet
            3
            0.4
            reposts_interactions
        
        update_googlesheet_with_interaction_type
            replies_googlesheet
            3
            0.4
            replies_interactions
            
            
        Adjacency_matrix_compound.update_googlesheet_with_total_interactions
            everything_googlesheet
            3
            0.4
            all_sorted_users
            [
                likes_interactions;
                reposts_interactions;
                replies_interactions
            ]
            
    
    [<Fact>]
    let ``update googlesheet with all matrices``() =
        let database = Central_task_database.open_connection()
        
        let all_users =
            Central_task_database.read_last_user_jobs_with_status
                database
                (Scraping_user_status.Completed (Success 0))
            |>List.ofSeq //how to sort?
            
        update_googlesheet
            database
            "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0"
            all_users
            
        
            
    let combine_pieces_of_matrix () =
        let central_db = Central_task_database.open_connection()
        let local_db = Twitter_database.open_connection()
        let this_worker = This_worker.this_worker_id local_db
        
        let users =
            Central_task_database.read_jobs_completed_by_worker
                central_db
                this_worker 
    
        ()
        