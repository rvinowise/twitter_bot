namespace rvinowise.twitter

open Npgsql
open rvinowise.twitter
open Xunit

(* user attention from local databases are combined in the central database.
this module exports them into the google sheet as a matrix *)

module Matrix_from_attention_to_sheet =
    

    
    let write_matrices_to_sheet
        sheet_service
        (database: NpgsqlConnection)
        doc_id
        handle_to_name
        all_sorted_users
        =
        let titles_and_interactions =
            [
                Adjacency_matrix_helpers.likes_design;
                //Adjacency_matrix_helpers.reposts_design;
                //Adjacency_matrix_helpers.replies_design
            ]
            |>List.map(fun design ->
                design.title,
                Stitching_user_attention.read_all_attentions
                    database
                    design.title
                |>Adjacency_matrix_helpers.interaction_type_for_colored_interactions
                    design.color
            )
        Write_matrix_to_sheet.try_write_separate_interactions_to_sheet
            sheet_service
            doc_id
            titles_and_interactions
            handle_to_name
            all_sorted_users
            
//        Write_matrix_to_sheet.try_write_combined_interactions_to_sheet
//            sheet_service
//            doc_id
//            titles_and_interactions
//            handle_to_name
//            all_sorted_users
         
    
    let ``stitched_interactions_to_sheet``()=
        
        let doc_id = "1Rb9cGqTb-3OknU_DWuPMBlMpRAV9PHhOvfc1LlN3h6U"
        
        let central_db = Central_task_database.open_connection()
        
        let all_users =
            Central_task_database.read_last_user_jobs_with_status
                central_db
                (Scraping_user_status.Completed (Success 0))
            |>List.ofSeq //how to sort?
        
        let handle_to_name =
            Twitter_user_database.handle_to_username
                central_db
        
        write_matrices_to_sheet
            (Googlesheet.create_googlesheet_service())
            central_db
            doc_id
            handle_to_name
            all_users
        ()
   
