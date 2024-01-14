namespace rvinowise.twitter

open Google.Apis.Sheets.v4
open Npgsql
open rvinowise.twitter
open Xunit



module Write_matrix_to_sheet =
        
    let write_relative_interactions_to_googlesheet
        (sheet_service: SheetsService)
        sheet
        relative_interactions
        handle_to_hame
        all_sorted_users
        =
        Single_adjacency_matrix.update_googlesheet
            sheet_service
            sheet
            handle_to_hame
            all_sorted_users
            relative_interactions
    
    
    let try_write_separate_interactions_to_sheet
        sheet_service
        sheet_document
        titles_and_interactions
        handle_to_hame
        all_sorted_users
        =
        titles_and_interactions
        |>List.iter ( fun (title, interaction_type) ->
                
            match
                Googlesheet_id.try_sheet_id_from_title
                    sheet_service
                    sheet_document
                    title
            with
            |Some sheet_id ->
                write_relative_interactions_to_googlesheet
                    sheet_service
                    {
                        Google_spreadsheet.doc_id = sheet_document
                        page_name=title
                    }
                    interaction_type
                    handle_to_hame
                    all_sorted_users
            |None -> 
                $"a sheet with name {title} isn't found in the google-sheet document {sheet_document},
                skipping writing this type of user interactions"
                |>Log.important
        ) 
            
    let try_write_combined_interactions_to_sheet
        sheet_service
        sheet_document
        titles_and_interactions
        handle_to_hame
        all_sorted_users
        =
        match
            Googlesheet_id.try_sheet_id_from_title
                    sheet_service
                    sheet_document
                    "Everything"
        with
        |Some page_id ->
            titles_and_interactions
            |>List.map snd
            |>Combined_adjacency_matrix.write_combined_interactions_to_googlesheet
                sheet_service
                {
                    Google_spreadsheet.doc_id = sheet_document
                    page_name="Everything"
                }
                3
                0.4
                handle_to_hame
                all_sorted_users
        |None ->
            $"a sheet with name 'Everything' isn't found in the google-sheet document {sheet_document},
            skipping writing this type of user interactions"
            |>Log.important