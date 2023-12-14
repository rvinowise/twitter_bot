namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit



module Adjacency_matrix_single =
        


    
    
    let interactions_to_intensity_colors
        (user_interactions: Map<User_handle, Map<User_handle, int>>)
        (colorscheme:Interaction_colorscheme)
        =
        let interactions_with_others =
            user_interactions
            |>Map.toSeq
            |>Seq.collect(fun (user,interactions) ->
                interactions
                |>Map.remove user
                |>Map.values
            )
        let interactions_with_oneself =
            user_interactions
            |>Map.toSeq
            |>Seq.map(fun (user,interactions) ->
                interactions
                |>Map.tryFind user
                |>Option.defaultValue 0
            )
        
        let min_interaction = Seq.min interactions_with_others
        let max_interaction = Seq.max interactions_with_others
        
        let interaction_to_intensity_color =
            Color.cell_color_for_value
                colorscheme.min_color
                colorscheme.max_color
                min_interaction
                max_interaction
        
        let self_interaction_to_intensity_color =
            Color.cell_color_for_value
                {r=1;g=1;b=1}
                {r=0.5;g=0.5;b=0.5}
                (Seq.min interactions_with_oneself)
                (Seq.max interactions_with_oneself)
        
        interaction_to_intensity_color,
        self_interaction_to_intensity_color
        
        
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
        user_names
        googlesheet
        colorscheme
        user_interactions
        =
        let
            interaction_to_intensity_color,
            self_interaction_to_intensity_color
                =
                interactions_to_intensity_colors user_interactions colorscheme
        
        let colored_interactions =    
            user_interactions
            |>user_interactions_to_colored_values
                interaction_to_intensity_color
                self_interaction_to_intensity_color
            
            
        let header_of_users =
            all_sorted_users
            |>List.map (Googlesheet_for_twitter.hyperlink_of_user user_names)
            |>List.map (fun url -> {
                Cell.value = Cell_value.Formula url
                color = Color.white
                style = Text_style.vertical
            })
            
        let left_column_of_users =
            all_sorted_users
            |>List.map (Googlesheet_for_twitter.hyperlink_of_user user_names)
            |>List.map (fun url -> {
                Cell.value = Cell_value.Formula url
                color = Color.white
                style = Text_style.regular
            })
            |>List.append [{
                Cell.value = Cell_value.Formula """=HYPERLINK("https://github.com/rvinowise/twitter_bot","src")"""
                color = Color.white
                style = Text_style.regular
            }]
        
        
        let rows_of_interactions =
            all_sorted_users
            |>List.map (fun user ->
                colored_interactions
                |>Map.find user
                |>Adjacency_matrix.row_of_interactions_for_user
                      all_sorted_users
            )
        
        (header_of_users::rows_of_interactions)
        |>Table.transpose Cell.empty
        |>List.append [left_column_of_users]
        |>Table.transpose Cell.empty
        |>Googlesheet_writing.write_table
            (Googlesheet.create_googlesheet_service())
            googlesheet
        
    
    
