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
                all_sorted_users
                matrices_data
        |None ->
            $"a sheet with name 'Everything' isn't found in the google-sheet document {sheet_document},
            skipping writing this type of user interactions"
            |>Log.important