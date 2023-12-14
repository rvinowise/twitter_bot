namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit


type User_interaction = {
    values: Map<User_handle, Map<User_handle, int>>
    colorscheme: Interaction_colorscheme
}

type Interaction_cell = {
    likes: int
    reposts: int
    replies: int
}

module Adjacency_matrix_compound =
        
    
    let cell_color_for_value
            (color_from:Color)
            (color_to:Color)
            (value_from: int)
            (value_to: int)
            (value_between: int)
            =
            let multiplier_to =
                coefficient_between_values
                    value_from
                    value_to
                    value_between
            let multiplier_from = 1.0-multiplier_to
            
            {
                r=color_from.r * multiplier_from + color_to.r*multiplier_to
                g=color_from.g * multiplier_from + color_to.g*multiplier_to
                b=color_from.b * multiplier_from + color_to.b*multiplier_to
            }
    
    let compound_interactions_to_intensity_colors
        (interactions: User_interaction list)
        =
        
        
    let update_googlesheet_with_total_interactions
        googlesheet
        all_sorted_users
        user_names
        likes_interactions
        reposts_interactions
        replies_interactions
        =
        let
            interaction_to_intensity_color,
            self_interaction_to_intensity_color
                =
                compound_interactions_to_intensity_colors user_interactions colorscheme