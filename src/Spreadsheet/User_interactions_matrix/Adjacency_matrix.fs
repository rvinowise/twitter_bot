namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit


type Interaction_colorscheme = {
    min_color: Color
    max_color: Color
}

module Adjacency_matrix =
    
    let likes_color = {r=1;g=0;b=0}
    let reposts_color = {r=0;g=1;b=0}
    let replies_color = {r=0.2;g=0.2;b=1}
    
    let enhancing_average_values = 0.2
    
    let coefficient_between_values
        (min_value: int)
        (max_value: int)
        (average_value: int)
        (value_between: int)
        =
        let our_value_from_zero = value_between-min_value
        let max_from_zero = max_value-min_value
        
        let average_value_coefficient =
            float(average_value-min_value)
            /
            float(max_from_zero)
            
        let pure_value_coefficient =
            float(our_value_from_zero)
            /
            float(max_from_zero)
        
        let enhancing_because_average =
            if value_between = min_value then
                0.0
            else
                abs(average_value_coefficient-pure_value_coefficient) * enhancing_average_values
        
        pure_value_coefficient + enhancing_because_average
        
    let cell_color_for_value
        (min_color:Color)
        (max_color:Color)
        (min_value: int)
        (max_value: int)
        (average_value: int)
        (value_between: int)
        =
        if value_between > 0 then
            () //test
            
        let multiplier_to =
            coefficient_between_values
                min_value
                max_value
                average_value
                value_between
        
        Color.mix_two_colors
            max_color
            multiplier_to
            min_color
    
    let map_from_seq_preferring_last
        items
        =
        items
        |>Seq.fold(fun map (key, value) ->
            map
            |>Map.add key value
        )
            Map.empty
    
    let maps_of_user_interactions 
        (read_interactions: User_handle->seq<User_handle*int>)
        (all_users: User_handle Set)
        =
        let zero_interactions =
            all_users
            |>Seq.map (fun user -> user,0)
        
        all_users
        |>Seq.map(fun user ->
            user,
            user
            |>read_interactions
            |>Seq.filter (fun (user,_) -> Set.contains user all_users)
            |>Seq.append zero_interactions
            |>map_from_seq_preferring_last
        )|>Map.ofSeq
    
    
    let min_max_average_values
        (items: int seq)
        =
        Seq.min items,
        Seq.max items,
        items
        |>Seq.map float
        |>Seq.average
        |>int
    
    let border_and_average_interactions
        (user_interactions: Map<User_handle, Map<User_handle, int>>)
        =
        let interactions =
            user_interactions
            |>Map.toSeq
            |>Seq.collect(fun (user,interactions) ->
                interactions
                |>Map.values
            )
        min_max_average_values interactions
    
    let border_and_average_interactions_with_others
        (user_interactions: Map<User_handle, Map<User_handle, int>>)
        =
        let interactions_with_others =
            user_interactions
            |>Map.toSeq
            |>Seq.collect(fun (user,interactions) ->
                interactions
                |>Map.remove user
                |>Map.values
            )
            
        min_max_average_values interactions_with_others
    
    let border_and_average_interactions_with_oneself
        (user_interactions: Map<User_handle, Map<User_handle, int>>)
        =
        let interactions_with_oneself =
            user_interactions
            |>Map.toSeq
            |>Seq.map(fun (user,interactions) ->
                interactions
                |>Map.tryFind user
                |>Option.defaultValue 0
            )
            
        min_max_average_values interactions_with_oneself
    
    let interactions_to_intensity_colors_functions
        (user_interactions: Map<User_handle, Map<User_handle, int>>)
        (colorscheme:Interaction_colorscheme)
        =
        
        let interaction_to_intensity_color =
            user_interactions
            |>border_and_average_interactions_with_others
            |||>cell_color_for_value
                colorscheme.min_color
                colorscheme.max_color
        
        let self_interaction_to_intensity_color =
            user_interactions
            |>border_and_average_interactions_with_oneself
            |||>cell_color_for_value
                {r=1;g=1;b=1}
                {r=0.5;g=0.5;b=0.5}
        
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
    
    let row_of_interactions_for_user
        all_sorted_users
        (colored_interactions:Map<User_handle, int*Color>)
        =
        all_sorted_users
        |>List.map(fun other_user ->
            colored_interactions
            |>Map.find other_user
            |>Cell.from_colored_number
        )
    
    
    let header_of_users all_sorted_users =
        all_sorted_users
        |>List.map (Googlesheet_for_twitter.hyperlink_to_twitter_user)
        |>List.map (fun url -> {
            Cell.value = Cell_value.Formula url
            color = Color.white
            style = Text_style.vertical
        })
        
    let left_column_of_users all_sorted_users =
        all_sorted_users
        |>List.map (Googlesheet_for_twitter.hyperlink_to_twitter_user )
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


    let compose_adjacency_matrix
        all_sorted_users
        rows_of_interactions
        =
            
        let header_of_users =
            header_of_users
                all_sorted_users
        
        let left_column_of_users =
            left_column_of_users
                all_sorted_users
            
        (header_of_users::rows_of_interactions)
        |>Table.transpose Cell.empty
        |>List.append [left_column_of_users]
        |>Table.transpose Cell.empty
        

    
    
    
    
    