namespace rvinowise.twitter

open Npgsql
open rvinowise.twitter
open rvinowise.web_scraping
open Xunit



module Color_blending_example =
        
    let likes_color = {r=1;g=0;b=0}
    let reposts_color = {r=0;g=1;b=0}
    let replies_color = {r=0.2;g=0.2;b=1}
    
    
    
    let ``fill table with the test of color blending ``() =
        let googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_name="Color blending example"
        }
        let googlesheet2 = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_name="Color blending example2"
        }
        
        
        let all_sorted_users =
            List.init 30 (fun index ->
                index|>string|>User_handle
            )
        
        let total_attention =
            all_sorted_users
            |>List.map (fun user ->
                user,
                10
            )|>Map.ofList
        
        let likes_interactions =
            all_sorted_users
            |>List.mapi (fun row user ->
                user,
                all_sorted_users
                |>List.mapi(fun column other_user ->
                    other_user,
                    row%10
                )|>Map.ofList
            )|>Map.ofList
            |>Adjacency_matrix_helpers.attention_matrix_for_colored_interactions
                likes_color
                total_attention
                
        let reposts_interactions =
            all_sorted_users
            |>List.mapi (fun row user ->
                user,
                all_sorted_users
                |>List.mapi(fun column other_user ->
                    other_user,
                    column%10
                )|>Map.ofList
            )|>Map.ofList
            |>Adjacency_matrix_helpers.attention_matrix_for_colored_interactions
                reposts_color
                total_attention
        
        let square_side = 10
        let replies_interactions =
            all_sorted_users
            |>List.mapi (fun row user ->
                user,
                all_sorted_users
                |>List.mapi(fun column other_user ->
                    other_user,
                    (column/square_side) +
                    (row/square_side)*
                    (all_sorted_users.Length/square_side)
                )|>Map.ofList
            )|>Map.ofList
            |>Adjacency_matrix_helpers.attention_matrix_for_colored_interactions
                replies_color
                total_attention
       
        let sheet_service = Googlesheet.create_googlesheet_service()       
        Combined_adjacency_matrix.write_combined_interactions_to_googlesheet
            sheet_service
            googlesheet
            3
            0.4
            User_handle.value
            all_sorted_users
            [
                likes_interactions;
                reposts_interactions;
                replies_interactions;
            ]
        // Adjacency_matrix_compound.update_googlesheet_with_total_interactions
        //     googlesheet
        //     3
        //     0.4
        //     all_sorted_users
        //     [
        //         reposts_interactions;
        //         replies_interactions;
        //         likes_interactions;
        //     ]
    
    