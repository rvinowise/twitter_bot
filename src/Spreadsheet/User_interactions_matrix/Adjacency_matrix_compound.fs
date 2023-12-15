namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit


type Interaction_type = {
    interactions: Map<User_handle, Map<User_handle, int>>
    color: Color
}

type Interaction_cell = {
    likes: int
    reposts: int
    replies: int
}

module Adjacency_matrix_compound =
        
    
    let mix_colors
        (base_color: Color)
        (colors: (Color*float) list)
        =
        let colors_amount =
            colors
            |>List.length
            |>float
        let total_amount =
            colors
            |>List.map snd
            |>List.reduce (+)
        
        colors
        |>List.fold(fun base_color (added_color, added_amount) ->
            if total_amount > 1 then
                base_color
                |>Color.mix_two_colors
                    added_color
                    (added_amount/(total_amount))
            else
                base_color
                |>Color.mix_two_colors
                    added_color
                    added_amount
        )
            base_color
    
    let compound_interactions_to_intensity_colors_functions
        (interaction_types: Interaction_type list)
        =
        let interactions_with_border_values =
            interaction_types
            |>List.map(fun interaction_type ->
                interaction_type,
                Adjacency_matrix.border_and_average_interactions
                    interaction_type.interactions
            )
        
        let interactions_to_color_coefficient =
            interactions_with_border_values
            |>List.map(fun (interaction_type, (min,max,average)) ->
                Adjacency_matrix.coefficient_between_values min max average
            )
        
        
        let interaction_to_intensity_color
            (values: int list)
            =
            if values=[77;39;70] then
                ()
                
            let color_multipliers =
                values
                |>List.mapi(fun index value ->
                    (
                        interactions_to_color_coefficient
                        |>List.item index
                    ) value                
                )
            
            let colors =
                interaction_types
                |>List.map (fun interaction_type->interaction_type.color)
            
            color_multipliers
            |>List.zip colors
            |>mix_colors
                Color.white
                
        interaction_to_intensity_color
        
    let interactions_between_users
        user
        other_user
        interactions
        =
        interactions
        |>List.map (fun interaction_type ->
            interaction_type
            |>Map.find user
            |>Map.tryFind other_user
            |>Option.defaultValue 0
        )
    
    let update_googlesheet_with_compound_interactions
        googlesheet
        all_sorted_users
        values_to_text
        (interactions_types: Interaction_type list)
        =
        let all_sorted_handles =
            all_sorted_users
            |>List.map Twitter_user.handle    
        
        let values_to_color =
            compound_interactions_to_intensity_colors_functions
                interactions_types
                
        let rows_of_interactions =
            all_sorted_handles
            |>List.map (fun user ->
                all_sorted_handles
                |>List.map (fun other_user ->
                    let interactions =
                        interactions_types
                        |>List.map (fun interaction_type -> interaction_type.interactions)
                        |>interactions_between_users
                            user
                            other_user
                        
                    Cell.from_colored_text
                        (values_to_text interactions)
                        (values_to_color interactions)
                )
            )
        
        Adjacency_matrix.compose_adjacency_matrix
            all_sorted_users
            rows_of_interactions
        |>Googlesheet_writing.write_table
            (Googlesheet.create_googlesheet_service())
            googlesheet
            
    let update_googlesheet_with_total_interactions
        googlesheet
        all_sorted_users
        likes_interactions
        reposts_interactions
        replies_interactions
        =
        let values_to_text
            (interactions: int list)
            =
            $"{interactions[0]}\n{interactions[1]},{interactions[2]}"
        
        [
            {
                interactions = likes_interactions;
                color = Adjacency_matrix.likes_color
            };{
                interactions = reposts_interactions;
                color = Adjacency_matrix.reposts_color
            };{
                interactions = replies_interactions;
                color = Adjacency_matrix.replies_color
            }
        ]
        |>update_googlesheet_with_compound_interactions
            googlesheet
            all_sorted_users
            values_to_text