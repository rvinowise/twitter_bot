namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit



module Adjacency_matrix_single =
        

    
    
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
        all_sorted_users
        googlesheet
        (interactions: Interaction_type)
        =
        let interaction_to_intensity_color =
            Adjacency_matrix.cell_color_for_value
                Color.white
                interactions.color
                interactions.key_values_with_others 
        
        let self_interaction_to_intensity_color =
            Adjacency_matrix.cell_color_for_value
                Color.white
                {r=0.5;g=0.5;b=0.5}
                interactions.key_values_with_oneself
        
        let all_sorted_handles =
            all_sorted_users
            |>List.map Twitter_user.handle
        
        let all_handles =
            Set.ofList all_sorted_handles
        
        let colored_interactions =    
            interactions.values
            |>Adjacency_matrix.add_zero_interactions all_handles 
            |>user_interactions_to_colored_values
                interaction_to_intensity_color
                self_interaction_to_intensity_color
        
        
        let rows_of_interactions =
            all_sorted_handles
            |>List.map (fun user ->
                colored_interactions
                |>Map.find user
                |>Adjacency_matrix.row_of_interactions_for_user
                      all_sorted_handles
            )
        
        Adjacency_matrix.compose_adjacency_matrix
            all_sorted_users
            rows_of_interactions
        |>Googlesheet_writing.write_table
            (Googlesheet.create_googlesheet_service())
            googlesheet
    
    
