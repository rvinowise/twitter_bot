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
        
    let combined_attention_from_user_to_user
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
     
    let add_headers
        handle_to_hame
        all_sorted_handles
        (total_known_attention: Map<User_handle,int> list)
        (table: Cell list list)
        =
        let top_row_of_users =
            Adjacency_matrix_helpers.top_row_of_users
                handle_to_hame
                all_sorted_handles    
        
        let all_known_attention_column =
            all_sorted_handles
            |>List.map(fun attentive_user ->
                let known_attention =
                    total_known_attention
                    |>List.map(fun attention_of_type ->
                        attention_of_type
                        |>Map.tryFind attentive_user
                        |>Option.defaultValue 0
                    )
                {
                    Cell.color = Adjacency_matrix_helpers.grey_header
                    style = Text_style.regular
                    value =
                        known_attention
                        |>absolute_values_to_text
                        |>Cell_value.Text
                }
            )
            |>List.append [
                {
                    Cell.color=Adjacency_matrix_helpers.grey_header
                    value = Cell_value.Text "total known\nattention"
                    style = Text_style.vertical 
                }
            ]
        
        let left_column_of_users =
            all_sorted_handles
            |>List.map (Googlesheet_for_twitter.handle_to_user_hyperlink handle_to_hame)
            |>List.map (fun url -> {
                Cell.value = Cell_value.Formula url
                color = Color.white
                style = Text_style.regular
            })
            |>List.append [
                {
                    Cell.value = Cell_value.Formula """=HYPERLINK("https://github.com/rvinowise/twitter_bot","src")"""
                    color = Color.white
                    style = Text_style.regular
                }
            ]
        
        table
        |>List.append [top_row_of_users]
        |>Table.transpose Cell.empty
        |>List.append [all_known_attention_column]
        |>List.append [left_column_of_users]
        |>Table.transpose Cell.empty
    
    
    
    let rows_of_combined_attention_for_user
        values_to_text
        values_to_color
        attention_matrices
        all_sorted_users
        =
        all_sorted_users
        |>List.map (fun user ->
            all_sorted_users
            |>List.map (fun other_user ->
                let combined_attention =
                    attention_matrices
                    |>List.map (fun matrix_data -> matrix_data.absolute_attention)
                    |>combined_attention_from_user_to_user
                        user
                        other_user

                Cell.from_colored_text
                    (combined_attention |>List.map float |>values_to_color )
                    (values_to_text combined_attention)
            )
        )
    
    let update_googlesheet_with_combined_interactions
        handle_to_hame
        values_to_text
        values_to_color
        sheet_service
        googlesheet
        all_sorted_users
        (attention_matrices: Attention_in_matrix list)
        =
        
        let total_known_attention =
            attention_matrices
            |>List.map(_.total_known_attention)
            
        rows_of_combined_attention_for_user
            values_to_text
            values_to_color
            attention_matrices
            all_sorted_users
        |>add_headers
            handle_to_hame
            all_sorted_users
            total_known_attention
        |>Googlesheet_writing.write_table
            sheet_service
            googlesheet
    
      
    
    let write_combined_interactions_to_googlesheet
        handle_to_hame
        sheet_service
        googlesheet
        all_sorted_users
        matrices_data
        =
        let values_to_color =
            matrices_data
            |>List.map(fun matrix_data ->
                matrix_data.design.color,
                matrix_data.absolute_attention
                |>Adjacency_matrix_helpers.interactions_with_everybody
                |>Seq.map float
                |>Adjacency_matrix_helpers.border_values_of_attention
            )
            |>compound_interactions_to_intensity_colors_functions
        
        update_googlesheet_with_combined_interactions
            handle_to_hame
            absolute_values_to_text
            values_to_color
            sheet_service
            googlesheet
            all_sorted_users
            matrices_data