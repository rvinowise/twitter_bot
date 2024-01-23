namespace rvinowise.twitter

open System.Globalization
open rvinowise.twitter
open rvinowise.web_scraping
open Xunit
open System




type Border_values = {
    min:float
    max:float
    average:float
}

type Attention_matrix_design = {
    color: Color
    attention_type: Attention_type
}

type Attention_in_matrix = {
    absolute_attention: Map<User_handle, Map<User_handle, int>>
    total_known_attention: Map<User_handle, int>    
    relative_attention: Map<User_handle, Map<User_handle, float>>
    total_paid_attention: Map<User_handle, float>
    total_received_attention: Map<User_handle, float>
    design: Attention_matrix_design
}

type Attention_cell = {
    likes: int
    reposts: int
    replies: int
}


    

module Adjacency_matrix_helpers =
    
    let likes_design = {
        color = {r=1;g=0;b=0}
        attention_type = Attention_type.Likes
    }
    let reposts_design = {
        color = {r=0;g=1;b=0}
        attention_type = Attention_type.Reposts
    }
    let replies_design = {
        color = {r=0;g=0;b=1}
        attention_type = Attention_type.Replies
    }
    let combined_interactions_title = "Everything"
    
    
    let interactions_with_everybody interactions =
        interactions
        |>Map.toSeq
        |>Seq.collect(fun (user,interactions) ->
            Map.values interactions
        )
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
        
    let border_values_of_attention
        (attention: float seq)
         =
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
                    |>Seq.average
                )
            }
    
    let absolute_to_relative_attention
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
    
    
    
        
    let user_attention_to_colored_values
        attention_to_intensity_color
        self_attention_to_intensity_color
        (user_attention: Map<User_handle, Map<User_handle, int>>)
        =
        user_attention
        |>Map.map(fun user interactions ->
            interactions
            |>Map.map (fun other_user attention_amount ->
                if
                    other_user = user
                then
                    attention_amount,
                    self_attention_to_intensity_color attention_amount
                else
                    attention_amount,
                    attention_to_intensity_color attention_amount
            )
        )
    
    
    
    
    let top_row_of_users handle_to_hame all_sorted_handles =
        all_sorted_handles
        |>List.map (Googlesheet_for_twitter.handle_to_user_hyperlink handle_to_hame)
        |>List.map (fun url -> {
            Cell.value = Cell_value.Formula url
            color = Color.white
            style = Text_style.vertical
        })
        
    let left_column_of_users
        handle_to_hame 
        all_sorted_handles
        =
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

    let grey_header = {r=0.9;g=0.9;b=0.9}

    
        
    
    let add_username_headers
        handle_to_hame
        all_sorted_handles
        rows_of_interactions
        =
            
        let top_row_of_users =
            top_row_of_users
                handle_to_hame
                all_sorted_handles
        
        let left_column_of_users =
            left_column_of_users
                handle_to_hame
                all_sorted_handles
            
        (top_row_of_users::rows_of_interactions)
        |>Table.transpose Cell.empty
        |>List.append [left_column_of_users]
        |>Table.transpose Cell.empty
        

    
    
    
    
    