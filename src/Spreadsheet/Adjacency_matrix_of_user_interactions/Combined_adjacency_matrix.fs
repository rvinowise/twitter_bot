namespace rvinowise.twitter

open System.Globalization
open rvinowise.twitter
open rvinowise.web_scraping
open Xunit




module Combined_adjacency_matrix =
        
    
    let compound_interactions_to_intensity_colors_functions
        (colors_within_borders: (Color*Border_values) list)
        =
        
        let interactions_to_color_coefficient =
            colors_within_borders
            |>List.map(fun (_,border_values) ->
                Adjacency_matrix_helpers.coefficient_between_values
                    border_values
            )
        
        let interaction_to_intensity_color
            values
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
                colors_within_borders
                |>List.map fst
            
            color_multipliers
            |>List.zip colors
            |>Color.mix_colors
                Color.white
                
        interaction_to_intensity_color
        
    let attention_from_user_to_user
        user
        other_user
        attention_maps
        =
        attention_maps
        |>List.map (fun attention_type ->
            attention_type
            |>Map.tryFind user
            |>Option.defaultValue Map.empty
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
        (attention_matrices: Attention_for_matrix list)
        =
        
        let rows_of_interactions =
            all_sorted_users
            |>List.map (fun user ->
                all_sorted_users
                |>List.map (fun other_user ->
                    if
                        user = User_handle "JamesRstrole"
                        && other_user = User_handle "JamesRstrole"
                    then
                        Log.debug "test"
                        
                    let interactions =
                        attention_matrices
                        |>List.map (fun matrix_data -> matrix_data.absolute_attention)
                        |>attention_from_user_to_user
                            user
                            other_user

                    Cell.from_colored_text
                        (values_to_text interactions)
                        (values_to_color interactions)
                )
            )
        
        Adjacency_matrix_helpers.add_username_headers
            handle_to_hame
            all_sorted_users
            rows_of_interactions
        |>Googlesheet_writing.write_table
            sheet_service
            googlesheet
    
    let relative_values_to_text
        (interactions: float list)
        =
        System.String.Format(
            NumberFormatInfo.InvariantInfo,
            "{0:0.##}\n{1:0.##},{2:0.##}",
            interactions[0]*100.0,
            interactions[1]*100.0,
            interactions[2]*100.0
        )
    let absolute_values_to_text
        (interactions: int list)
        =
        System.String.Format(
            NumberFormatInfo.InvariantInfo,
            "{0:0}\n{1:0},{2:0}",
            interactions[0],
            interactions[1],
            interactions[2]
        )  
    
    let write_combined_interactions_to_googlesheet
        sheet_service
        googlesheet
        handle_to_hame
        all_sorted_users
        all_interaction_types
        =
        let values_to_color =
            compound_interactions_to_intensity_colors_functions
                all_interaction_types
        
        update_googlesheet_with_compound_interactions
            sheet_service
            googlesheet
            handle_to_hame
            all_sorted_users
            absolute_values_to_text
            // values_to_color
            // all_interaction_types