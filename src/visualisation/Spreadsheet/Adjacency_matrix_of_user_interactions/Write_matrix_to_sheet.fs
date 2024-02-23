namespace rvinowise.twitter

open Google.Apis.Sheets.v4
open Npgsql
open rvinowise.twitter
open Xunit



module Write_matrix_to_sheet =
        

    
    
    let try_write_separate_interactions_to_sheet
        handle_to_hame
        sheet_service
        sheet_document
        first_cell
        (matrices_data: Attention_in_matrix list)
        all_sorted_users
        =
        matrices_data
        |>List.iter ( fun matrix_data ->
            let page_title = string matrix_data.design.attention_type
            match
                Googlesheet_id.try_sheet_id_from_title
                    sheet_service
                    sheet_document
                    page_title
            with
            |Some sheet_id ->
                Single_adjacency_matrix.update_googlesheet
                    handle_to_hame
                    sheet_service
                    {
                        Google_spreadsheet.doc_id = sheet_document
                        page_name=page_title
                    }
                    first_cell
                    all_sorted_users
                    matrix_data
            |None -> 
                $"a sheet with name {page_title} isn't found in the google-sheet document {sheet_document},
                skipping writing this type of user interactions"
                |>Log.important
        ) 
            
    let try_write_combined_interactions_to_sheet
        handle_to_hame
        sheet_service
        sheet_document
        info_cell
        (matrices_data: Attention_in_matrix list)
        all_sorted_users
        =
        match
            Googlesheet_id.try_sheet_id_from_title
                    sheet_service
                    sheet_document
                    "Everything"
        with
        |Some page_id ->
            Combined_adjacency_matrix.write_combined_interactions_to_googlesheet
                handle_to_hame
                sheet_service
                {
                    Google_spreadsheet.doc_id = sheet_document
                    page_name="Everything"
                }
                info_cell
                all_sorted_users
                matrices_data
        |None ->
            $"a sheet with name 'Everything' isn't found in the google-sheet document {sheet_document},
            skipping writing this type of user interactions"
            |>Log.important
            
    
    let write_matrices_to_sheet
        handle_to_name
        sheet_service
        (database: NpgsqlConnection)
        doc_id
        matrix_title
        matrix_datetime
        =
        let computed_attention =
            Prepare_attention_matrix.attention_in_matrices
                database
                matrix_title
                matrix_datetime
        
        let combined_inout_relative_attention =
            Prepare_attention_matrix.combined_inout_relative_attention
                computed_attention
        
        let sorted_matrix_members =
            Adjacency_matrix_database.read_members_of_matrix
                database
                matrix_title
            |>Prepare_attention_matrix.sort_accounts_by_their_values
                combined_inout_relative_attention
            |>List.map fst
        
        let info_cell =
            Prepare_attention_matrix.info_cell
                matrix_title
                matrix_datetime
              
        try_write_separate_interactions_to_sheet
            handle_to_name
            sheet_service
            doc_id
            info_cell
            computed_attention
            sorted_matrix_members
            
        try_write_combined_interactions_to_sheet
            handle_to_name
            sheet_service
            doc_id
            info_cell
            computed_attention
            sorted_matrix_members
            
    let attention_matrix_to_sheet()=
        
        let doc_id = "1IghY1FjqODJq5QpaDcCDl2GyerqEtRR79-IcP55aOxI" //philosophy members
        let doc_id = "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0" //longevity members
        let doc_id = "1JLFoJEQiDzpn-FZ3_jsCIGfcN5IyzTW3QC7uu9uLYVw" //Transhumanist members
        
        let central_db = Central_database.resiliently_open_connection()
        let local_db = Local_database.open_connection()
        
        let handle_to_name =
            Twitter_user_database.handle_to_username
                local_db
        
        Adjacency_matrix_database.read_timeframes_of_matrix
            central_db
            local_db
            Adjacency_matrix.Transhumanist_members
        |>List.last
        |> _.last_completion
        |>write_matrices_to_sheet
            handle_to_name
            (Googlesheet.create_googlesheet_service())
            local_db
            doc_id
            Adjacency_matrix.Transhumanist_members
        ()