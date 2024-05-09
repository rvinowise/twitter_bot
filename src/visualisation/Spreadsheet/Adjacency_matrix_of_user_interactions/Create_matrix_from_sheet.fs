namespace rvinowise.twitter

open System
open Google.Apis.Sheets.v4
open Npgsql
open rvinowise.twitter
open Xunit



module Create_matrix_from_sheet =
    
    
    let users_from_spreadsheet
        service
        sheet
        =
        Googlesheet_reading.read_table
            Parse_google_cell.try_twitter_user_handle_from_google_cell
            service
            sheet
        |>Table.transpose None
        |>List.head
        |>List.filter Option.isSome
    
       
    let ``add_matrix_from_sheet``()=
        {
            Google_spreadsheet.doc_id = "1C9Lafu5PxMoPj5GpxRwFs1Senc5H-us5AvltSLFpszg"
            page_name="Members"
        }
        |>users_from_spreadsheet
            (Googlesheet.create_googlesheet_service())
        |>Adjacency_matrix_database.write_members_of_matrix
            (Local_database.open_connection())
            Adjacency_matrix.Antiislam_members

    
    
    let rows_with_null_users
        service
        sheet
        =
        Googlesheet_reading.read_table
            Parse_google_cell.try_twitter_user_handle_from_google_cell
            service
            sheet
        |>Table.transpose None
        |>List.head
        |>List.indexed
        |>List.filter(fun (index,user) -> user.IsNone)
        
    
    let ``check rows without users``()=
        {
            Google_spreadsheet.doc_id = "18iGlQOaSihzVhAhpbmilBTTUWk55hrVzdSM0FlbQhtQ"
            page_name="Members"
        }
        |>rows_with_null_users
            (Googlesheet.create_googlesheet_service())
        |>ignore
        ()
    

    
    let ``add_matrix_from_sheet (old)``()=
        Googlesheet_reading.read_range
            Parse_google_cell.visible_text_from_cell
            (Googlesheet.create_googlesheet_service())
            {
                Google_spreadsheet.doc_id="1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU"
                page_name = "Followers amount"
            }
            ((2,3),(2,1000))
        |>Table.trim_table String.IsNullOrEmpty
        |>List.collect id
        |>List.map (fun handle ->
            handle
            |>User_handle.trim_potential_atsign
            |>User_handle
        )
        |>Adjacency_matrix_database.write_members_of_matrix
            (Local_database.open_connection())
            "Twitter network"
                
        ()