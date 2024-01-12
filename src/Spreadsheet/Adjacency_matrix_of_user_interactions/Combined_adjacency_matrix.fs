namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit




module Combined_adjacency_matrix =
        
    
    let compound_interactions_to_intensity_colors_functions
        amplifying_accuracy
        amplifier_of_average
        (interaction_types: Relative_interaction list)
        =
        
        let interactions_to_color_coefficient =
            interaction_types
            |>List.map(fun {border_values_with_others = key_values} ->
                Adjacency_matrix_helpers.coefficient_between_values
                    amplifying_accuracy
                    amplifier_of_average
                    key_values
            )
        
        
        let interaction_to_intensity_color
            (values: int list)
            =             
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
            |>Color.mix_colors
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
        sheet_service
        googlesheet
        handle_to_hame
        all_sorted_users
        values_to_text
        values_to_color
        (interactions_types: Relative_interaction list)
        =
        
        let rows_of_interactions =
            all_sorted_users
            |>List.map (fun user ->
                all_sorted_users
                |>List.map (fun other_user ->
                    let interactions =
                        interactions_types
                        |>List.map (fun interaction_type -> interaction_type.values)
                        |>interactions_between_users
                            user
                            other_user
                    if interactions = [0;0;8] then
                        ()//test
                    if interactions = [0;1;5] then
                        ()//test
                    Cell.from_colored_text
                        (values_to_text interactions)
                        (values_to_color interactions)
                )
            )
        
        Adjacency_matrix_helpers.add_headers_to_adjacency_matrix
            handle_to_hame
            all_sorted_users
            rows_of_interactions
        |>Googlesheet_writing.write_table
            sheet_service
            googlesheet
            
    let write_combined_interactions_to_googlesheet
        sheet_service
        googlesheet
        amplifying_accuracy
        amplifier_of_average
        handle_to_hame
        all_sorted_users
        all_interaction_types
        =
        let values_to_text
            (interactions: int list)
            =
            $"{interactions[0]}\n{interactions[1]},{interactions[2]}"
        
        let values_to_color =
            compound_interactions_to_intensity_colors_functions
                amplifying_accuracy
                amplifier_of_average
                all_interaction_types
        
        update_googlesheet_with_compound_interactions
            sheet_service
            googlesheet
            handle_to_hame
            all_sorted_users
            values_to_text
            values_to_color
            all_interaction_types