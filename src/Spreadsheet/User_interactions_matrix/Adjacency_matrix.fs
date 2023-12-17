namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit


type Interaction_colorscheme = {
    min_color: Color
    max_color: Color
}

type Key_values = {
    min:int
    max:int
    average:int
}

type Interaction_type = {
    values: Map<User_handle, Map<User_handle, int>>
    key_values_with_others: Key_values
    key_values_with_oneself: Key_values
    color: Color
}

type Interaction_cell = {
    likes: int
    reposts: int
    replies: int
}


module Adjacency_matrix =
    
    
    
    let interactions_with_others interactions =
        interactions
        |>Map.toSeq
        |>Seq.collect(fun (user,interactions) ->
            interactions
            |>Map.remove user
            |>Map.values
        )
    
    let interactions_with_oneself interactions =
        interactions
        |>Map.toSeq
        |>Seq.map(fun (user,interactions) ->
            interactions
            |>Map.tryFind user
            |>Option.defaultValue 0
        )
        
    let key_values_of_interactions interactions =
        {
            Key_values.min=
                Seq.min interactions
            max =
                Seq.max interactions
            average = (
                interactions
                |>Seq.map float
                |>Seq.average|>int
            )
        }
     
    let interaction_type_for_colored_interactions
        color
        interactions
        =
        {
            values=interactions
            color=color
            key_values_with_oneself=
                interactions
                |>interactions_with_others
                |>key_values_of_interactions
            key_values_with_others=
                interactions
                |>interactions_with_others
                |>key_values_of_interactions
        }
     
    let inline clamp minimum maximum value = value |> max minimum |> min maximum
    
    let coefficient_between_values
        amplifier_of_average
        (key_values: Key_values)
        (value_between: int)
        =
        if value_between <= key_values.min then
            0.0
        elif value_between >= key_values.max then
            1.0
        else
            let our_value_from_zero = value_between-key_values.min
            let max_from_zero = key_values.max-key_values.min
            
            let average_value_coefficient =
                float(key_values.average-key_values.min)
                /
                float(max_from_zero)
                
            let pure_value_coefficient =
                float(our_value_from_zero)
                /
                float(max_from_zero)
            
            let enhancing_because_average =
                if value_between <= key_values.min then
                    0.0
                else
                    amplifier_of_average / (abs(average_value_coefficient-pure_value_coefficient)+amplifier_of_average)
            
            pure_value_coefficient// + enhancing_because_average
            |>clamp 0 1
        
    let cell_color_for_value
        (min_color:Color)
        (max_color:Color)
        amplification_of_average
        (key_values: Key_values)
        (value_between: int)
        =
        let multiplier_to =
            coefficient_between_values
                amplification_of_average
                key_values
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
        all_users
        |>Seq.map(fun user ->
            user,
            user
            |>read_interactions
            |>Seq.filter (fun (user,_) -> Set.contains user all_users)
            |>map_from_seq_preferring_last
        )|>Map.ofSeq
    
    let add_zero_interactions
        (all_users: User_handle Set)
        (user_interactions: Map<User_handle, Map<User_handle, int>>)
        =
        let zero_interactions =
            all_users
            |>Seq.map (fun user -> user,0)
            
        all_users
        |>Seq.map(fun user ->
            user
            ,
            user_interactions
            |>Map.tryFind user
            |>Option.defaultValue Map.empty
            |>Map.toSeq
            |>Seq.append zero_interactions
            |>map_from_seq_preferring_last
        )|>Map.ofSeq
            
    
    
        
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
        

    
    
    
    
    