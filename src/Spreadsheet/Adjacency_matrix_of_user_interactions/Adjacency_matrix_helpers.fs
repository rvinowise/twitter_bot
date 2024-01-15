namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit
open System

type Interaction_colorscheme = {
    min_color: Color
    max_color: Color
}

type Border_values = {
    min:float
    max:float
    average:float
}

type Relative_attention_matrix = {
    attention_to_users: Map<User_handle, Map<User_handle, float>>
    border_attention_to_others: Border_values
    border_attention_to_oneself: Border_values
    color: Color
}

type Interaction_cell = {
    likes: int
    reposts: int
    replies: int
}

type Interaction_design = {
    color: Color
    attention_type: string
}
    

module Adjacency_matrix_helpers =
    
    let likes_design = {
        color = {r=1;g=0;b=0}
        attention_type = "Likes"
    }
    let reposts_design = {
        color = {r=0;g=1;b=0}
        attention_type = "Reposts"
    }
    let replies_design = {
        color = {r=0;g=0;b=1}
        attention_type = "Replies"
    }
    let combined_interactions_title = "Everything"
    
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
            |>Option.defaultValue 0.0
        )
        
    let border_values_of_attention attention =
        if Seq.isEmpty attention then
            {
                Border_values.min = 0
                max = 0
                average = 0
            }
        else
            {
                Border_values.min=
                    Seq.min attention
                max =
                    Seq.max attention
                average = (
                    attention
                    |>Seq.map float
                    |>Seq.average
                )
            }
    
    let absolute_attention_to_percents
        (absolute_attention: Map<User_handle, Map<User_handle, int>>)
        (total_attention: Map<User_handle, int>)
        =
        absolute_attention
        |>Map.map(fun attentor attention ->
            attention
            |>Map.map (fun target absolute_attention ->
                let user_total_attention = //there are no zero values of attention at this point
                    total_attention
                    |>Map.find attentor
                (float absolute_attention) / (float user_total_attention)
            )
        )
     
    let attention_matrix_for_colored_interactions
        color
        attention_map
        =
        {
            attention_to_users = attention_map
            color = color
            border_attention_to_oneself=
                attention_map
                |>interactions_with_oneself
                |>border_values_of_attention
            border_attention_to_others=
                attention_map
                |>interactions_with_others
                |>border_values_of_attention
        }
     
    let inline clamp minimum maximum value = value |> max minimum |> min maximum
    
    
    let enhance_average_coeffitient
        enhancing_accuracy
        amplifier_of_average
        (key_values: Border_values)
        (pure_value_coefficient:float)
        =
        let max_from_zero = key_values.max-key_values.min
        
        let average_value_coefficient =
            float(key_values.average-key_values.min)
            /
            float(max_from_zero)
            
        let maximal_difference =
            if average_value_coefficient <= 0.5 then
                Math.Pow(-average_value_coefficient+2.0, enhancing_accuracy)
            else
                Math.Pow(average_value_coefficient+1.0, enhancing_accuracy)
            
        let difference_with_average =
            Math.Pow(
                abs(average_value_coefficient-pure_value_coefficient)+1.0,
                enhancing_accuracy
            ) /
            maximal_difference
        
        let enhancing_because_average =
            amplifier_of_average*difference_with_average
        
        pure_value_coefficient + enhancing_because_average
        |>clamp 0 1
    
    let coefficient_between_values
        (key_values: Border_values)
        value_between
        =
        if value_between <= key_values.min then
            0.0
        elif value_between >= key_values.max then
            1.0
        else
            let our_value_from_zero = value_between-key_values.min
            let max_from_zero = key_values.max-key_values.min
                
            float(our_value_from_zero)/float(max_from_zero)
            
    
    let coefficient_between_values_enhanced
        key_values
        value_between
        =
        coefficient_between_values
            key_values
            value_between
        |>enhance_average_coeffitient
            3
            0.4
            key_values
            
    let cell_color_for_value
        (min_color:Color)
        (max_color:Color)
        (key_values: Border_values)
        value_between
        =
        let multiplier_to =
            coefficient_between_values
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
        (read_attentions_from_user: User_handle->seq<User_handle*int>)
        (all_users: User_handle Set)
        =
        all_users
        |>Seq.map(fun user ->
            user,
            user
            |>read_attentions_from_user
            |>Seq.filter (fun (user,_) -> Set.contains user all_users)
            |>Map.ofSeq
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
    
    let row_of_attention_for_user
        interaction_color
        self_interaction_color
        all_sorted_users
        attentive_user
        (all_attention:Map<User_handle, float>)
        =
        all_sorted_users
        |>List.map(fun other_user ->
            let relative_value =
                all_attention
                |>Map.tryFind other_user
                |>Option.defaultValue 0.0
            {
                Cell.value =
                    (relative_value * 100.0)
                    |>Cell_value.Float
                color =
                    if attentive_user = other_user then
                        self_interaction_color
                            relative_value
                    else
                        interaction_color
                            relative_value
                        
                style = Text_style.regular
            }
        )
    
    
    let header_of_users handle_to_hame all_sorted_handles =
        all_sorted_handles
        |>List.map (Googlesheet_for_twitter.handle_to_user_hyperlink handle_to_hame)
        |>List.map (fun url -> {
            Cell.value = Cell_value.Formula url
            color = Color.white
            style = Text_style.vertical
        })
        
    let left_column_of_users handle_to_hame all_sorted_handles =
        all_sorted_handles
        |>List.map (Googlesheet_for_twitter.handle_to_user_hyperlink handle_to_hame)
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


    let add_headers_to_adjacency_matrix
        handle_to_hame
        all_sorted_handles
        rows_of_interactions
        =
            
        let header_of_users =
            header_of_users
                handle_to_hame
                all_sorted_handles
        
        let left_column_of_users =
            left_column_of_users
                handle_to_hame
                all_sorted_handles
            
        (header_of_users::rows_of_interactions)
        |>Table.transpose Cell.empty
        |>List.append [left_column_of_users]
        |>Table.transpose Cell.empty
        

    
    
    
    
    