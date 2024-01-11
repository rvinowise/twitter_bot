namespace rvinowise.twitter

open Google.Apis.Sheets.v4
open Npgsql
open rvinowise.twitter
open Xunit



module Matrix_from_posts_to_sheet =
        
    
    
    
    let sorted_users_from_handles
        database
        all_sorted_handles
        =
        let user_names =
            Twitter_user_database.read_usernames_map
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
    
    
    
    let write_all_interactions_to_googlesheet
        (sheet_service: SheetsService)
        (database: NpgsqlConnection)
        sheet_document
        all_sorted_users
        =
        
        let titles_and_interactions =
            [
                Adjacency_matrix_helpers.likes_design,
                User_interactions_from_posts.read_likes_by_user;
                
                Adjacency_matrix_helpers.reposts_design,
                User_interactions_from_posts.read_reposts_by_user;
                
                Adjacency_matrix_helpers.replies_design,
                User_interactions_from_posts.read_replies_by_user
            ]
            |>List.map(fun (design, read_user_interactions) ->
                design.title,
                interaction_type_from_db
                    (read_user_interactions database)
                    design.color
                    (Set.ofSeq all_sorted_users)
            )
        
        let handle_to_name =
            Twitter_user_database.handle_to_username
                database
            
        Write_matrix_to_sheet.try_write_separate_interactions_to_sheet
            sheet_service
            sheet_document
            titles_and_interactions
            handle_to_name
            all_sorted_users
        
        Write_matrix_to_sheet.try_write_combined_interactions_to_sheet
            sheet_service
            sheet_document
            titles_and_interactions
            handle_to_name
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
        
        let all_user_handles =
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
            |>List.map (User_handle.trim_potential_atsign>>User_handle)
            
        write_all_interactions_to_googlesheet
            service
            (Twitter_database.open_connection())
            "1Rb9cGqTb-3OknU_DWuPMBlMpRAV9PHhOvfc1LlN3h6U"
            all_user_handles
            