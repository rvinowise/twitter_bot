﻿namespace rvinowise.twitter

open Npgsql
open rvinowise.twitter
open rvinowise.web_scraping
open Xunit



module Color_blending_example =
        
    let likes_color = {r=1;g=0;b=0}
    let reposts_color = {r=0;g=1;b=0}
    let replies_color = {r=0.2;g=0.2;b=1}
    
    
    
    [<Fact(Skip="manual")>]//
    let ``fill table with the test of color blending ``() =
        let googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=398318420
            page_name="Color blending example"
        }
        let googlesheet2 = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=1806921415
            page_name="Color blending example2"
        }
        
        
        let all_sorted_users =
            List.init 30 (fun index ->
                {
                    handle= index|>string|>User_handle
                    name= index|>string
                }    
            )
            
        let all_sorted_handles =
            all_sorted_users
            |>List.map (fun user -> user.handle)
        
        let likes_interactions =
            all_sorted_handles
            |>List.mapi (fun row user ->
                user,
                all_sorted_handles
                |>List.mapi(fun column other_user ->
                    other_user,
                    row%10
                )|>Map.ofList
            )|>Map.ofList
            |>Adjacency_matrix.interaction_type_for_colored_interactions
                likes_color
                
        let reposts_interactions =
            all_sorted_handles
            |>List.mapi (fun row user ->
                user,
                all_sorted_handles
                |>List.mapi(fun column other_user ->
                    other_user,
                    column%10
                )|>Map.ofList
            )|>Map.ofList
            |>Adjacency_matrix.interaction_type_for_colored_interactions
                reposts_color
        
        let square_side = 10
        let replies_interactions =
            all_sorted_handles
            |>List.mapi (fun row user ->
                user,
                all_sorted_handles
                |>List.mapi(fun column other_user ->
                    other_user,
                    (column/square_side) +
                    (row/square_side)*
                    (all_sorted_handles.Length/square_side)
                )|>Map.ofList
            )|>Map.ofList
            |>Adjacency_matrix.interaction_type_for_colored_interactions
                replies_color
                
        Adjacency_matrix_compound.update_googlesheet_with_total_interactions
            googlesheet
            3
            0.4
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
    
    