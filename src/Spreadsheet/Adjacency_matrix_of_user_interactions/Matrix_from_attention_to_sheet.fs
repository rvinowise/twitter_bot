namespace rvinowise.twitter

open System
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
        matrix_title
        matrix_datetime
        handle_to_name
        =
        let titles_and_interactions =
            [
                Adjacency_matrix_helpers.likes_design;
                Adjacency_matrix_helpers.reposts_design;
                Adjacency_matrix_helpers.replies_design
            ]
            |>List.map(fun design ->
                design.attention_type,
                
                let users_attention =
                    User_attention_database.read_attentions_within_matrix
                        database
                        matrix_title
                        design.attention_type
                        matrix_datetime        
                
                let users_total_attention =
                    User_attention_database.read_total_attention_from_users
                        database 
                        design.attention_type
                        matrix_datetime
                
                let users_relative_attention =
                    Adjacency_matrix_helpers.absolute_attention_to_percents
                        users_attention
                        users_total_attention
                
                Adjacency_matrix_helpers.attention_matrix_for_colored_interactions
                    design.color
                    users_relative_attention
            )
        
        let sorted_members_of_matrix =
            Adjacency_matrix.read_sorted_members_of_matrix
                database
                matrix_title
        
        Write_matrix_to_sheet.try_write_separate_interactions_to_sheet
            sheet_service
            doc_id
            titles_and_interactions
            handle_to_name
            sorted_members_of_matrix
            
        Write_matrix_to_sheet.try_write_combined_interactions_to_sheet
            sheet_service
            doc_id
            titles_and_interactions
            handle_to_name
            sorted_members_of_matrix
         
    
    let ``interactions_to_sheet``()=
        
        let doc_id = "1Rb9cGqTb-3OknU_DWuPMBlMpRAV9PHhOvfc1LlN3h6U"
        let matrix_title = "Longevity members"
        
        let central_db = Central_database.open_connection()
        let local_db = Local_database.open_connection()
        
        let handle_to_name =
            Twitter_user_database.handle_to_username
                local_db
        
        write_matrices_to_sheet
            (Googlesheet.create_googlesheet_service())
            central_db
            doc_id
            matrix_title
            DateTime.Now
            handle_to_name
        ()
   
