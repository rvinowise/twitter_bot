﻿namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit



module Single_adjacency_matrix =
        

    
    
    let user_interactions_to_colored_values
        interaction_to_intensity_color
        self_interaction_to_intensity_color
        (user_interactions: Map<User_handle, Map<User_handle, int>>)
        =
        user_interactions
        |>Map.map(fun user interactions ->
            interactions
            |>Map.map (fun other_user interaction_amount ->
                if
                    other_user = user
                then
                    interaction_amount,
                    self_interaction_to_intensity_color interaction_amount
                else
                    interaction_amount,
                    interaction_to_intensity_color interaction_amount
            )
        )
    
    
        
    let update_googlesheet
        sheet_service
        googlesheet
        handle_to_hame
        all_sorted_users
        amplifying_accuracy
        amplification_of_average
        (interactions: Relative_interaction)
        =
        let interaction_to_intensity_color =
            Adjacency_matrix_helpers.cell_color_for_value
                Color.white
                interactions.color
                amplifying_accuracy
                amplification_of_average
                interactions.border_values_with_others 
        
        let self_interaction_to_intensity_color =
            Adjacency_matrix_helpers.cell_color_for_value
                Color.white
                {r=0.5;g=0.5;b=0.5}
                amplifying_accuracy
                amplification_of_average
                interactions.border_values_with_oneself
        
        let colored_interactions =    
            interactions.values
            |>Adjacency_matrix_helpers.add_zero_interactions (Set.ofList all_sorted_users) 
            |>user_interactions_to_colored_values
                interaction_to_intensity_color
                self_interaction_to_intensity_color
        
        
        let rows_of_interactions =
            all_sorted_users
            |>List.map (fun user ->
                colored_interactions
                |>Map.find user
                |>Adjacency_matrix_helpers.row_of_interactions_for_user
                      all_sorted_users
            )
        
        Adjacency_matrix_helpers.compose_adjacency_matrix
            handle_to_hame
            all_sorted_users
            rows_of_interactions
        |>Googlesheet_writing.write_table
            sheet_service
            googlesheet
    
    
