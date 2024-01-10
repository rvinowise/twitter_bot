namespace rvinowise.twitter

open Google.Apis.Sheets.v4
open Npgsql
open rvinowise.twitter
open rvinowise.web_scraping
open Xunit



module Matrix_from_posts_to_sheet =
        
    
    
    
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
        read_interactions
        color
        all_user_handles
        =
        all_user_handles
        |>Adjacency_matrix_helpers.maps_of_user_interactions
            read_interactions    
        |>Adjacency_matrix_helpers.interaction_type_for_colored_interactions
            color
    
    let write_interaction_type_to_googlesheet
        (sheet_service: SheetsService)
        sheet
        relative_interaction
        all_sorted_users
        =
        let update_googlesheet_with_interaction_type =
            Single_adjacency_matrix.update_googlesheet
                sheet_service
                all_sorted_users
        
        update_googlesheet_with_interaction_type
            sheet
            3
            0.4
            relative_interaction
    
    
    let try_write_separate_interactions_to_sheet
        sheet_service
        sheet_document
        titles_and_interactions
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
                write_interaction_type_to_googlesheet
                    sheet_service
                    {
                        Google_spreadsheet.doc_id = sheet_document
                        page_name=title
                    }
                    interaction_type
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
            |>Combined_adjacency_matrix.update_googlesheet_with_total_interactions
                sheet_service
                {
                    Google_spreadsheet.doc_id = sheet_document
                    page_name="Everything"
                }
                3
                0.4
                all_sorted_users
        |None ->
            $"a sheet with name 'Everything' isn't found in the google-sheet document {sheet_document},
            skipping writing this type of user interactions"
            |>Log.important
    
    let write_all_interactions_to_googlesheet
        (sheet_service: SheetsService)
        (database: NpgsqlConnection)
        sheet_document
        all_sorted_handles
        =
        let all_sorted_users =
            sorted_users_from_handles
                database
                all_sorted_handles
        let titles_and_interactions =
            [
                "Likes", Adjacency_matrix_helpers.likes_color, User_interactions_from_posts.read_likes_by_user
                "Reposts", Adjacency_matrix_helpers.reposts_color, User_interactions_from_posts.read_reposts_by_user
                "Replies", Adjacency_matrix_helpers.replies_color, User_interactions_from_posts.read_replies_by_user
            ]
            |>List.map(fun (title, color, read_user_interactions) ->
                title,
                interaction_type_from_db
                    (read_user_interactions database)
                    color
                    (Set.ofSeq all_sorted_handles)
            )
            
        try_write_separate_interactions_to_sheet
            sheet_service
            sheet_document
            titles_and_interactions
            all_sorted_users
        
        try_write_combined_interactions_to_sheet
            sheet_service
            sheet_document
            titles_and_interactions
            all_sorted_users
        
    
    let users_from_sheet
        (sheet_service: SheetsService)
        (database: NpgsqlConnection)
        (sheet: Google_spreadsheet)
        =
        Googlesheet_reading.read_table
           
    [<Fact>]        
    let ``try write_all_interactions_to_googlesheet``()=
        let service = Googlesheet.create_googlesheet_service()
        
        let all_handles =
            Googlesheet_reading.read_range
                Parse_google_cell.visible_text_from_cell
                service
                {
                    doc_id = "1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU"
                    page_name = "Posts amount" 
                }
                ((2,3),(2,100))
            |>Table.trim_table
                (fun text -> text = "")
            |>List.collect id
            |>List.map User_handle
            
        write_all_interactions_to_googlesheet
            service
            (Twitter_database.open_connection())
            "1Rb9cGqTb-3OknU_DWuPMBlMpRAV9PHhOvfc1LlN3h6U"
            all_handles
            