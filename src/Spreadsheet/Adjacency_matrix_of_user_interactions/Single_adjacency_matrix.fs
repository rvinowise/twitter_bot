namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit



module Single_adjacency_matrix =
        

    
    
    let users_attention_to_colored_values
        attention_to_intensity_color
        self_attention_to_intensity_color
        (users_attention: Map<User_handle, Map<User_handle, int>>)
        =
        users_attention
        |>Map.map(fun user attention_amounts ->
            attention_amounts
            |>Map.map (fun other_user interaction_amount ->
                if
                    other_user = user
                then
                    interaction_amount,
                    self_attention_to_intensity_color interaction_amount
                else
                    interaction_amount,
                    attention_to_intensity_color interaction_amount
            )
        )
    
   
        
    
    let cell_of_attention
        color
        (matrix_border_percents: Border_values)
        relative_attention
        =
        {
            value = Cell_value.Float (relative_attention * 100.0)
                
            color =
                Adjacency_matrix_helpers.cell_color_for_value
                    Color.white
                    color
                    matrix_border_percents
                    relative_attention
                    
            Cell.style = Text_style.regular
        }
        
    let update_googlesheet
        sheet_service
        googlesheet
        handle_to_hame
        all_sorted_users
        (attention_matrix: Relative_attention_matrix)
        =
        let attention_to_intensity_color =
            Adjacency_matrix_helpers.cell_color_for_value
                Color.white
                attention_matrix.color
                attention_matrix.border_attention_to_others 
        
        let self_attention_to_intensity_color =
            Adjacency_matrix_helpers.cell_color_for_value
                Color.white
                {r=0.5;g=0.5;b=0.5}
                attention_matrix.border_attention_to_oneself
        
        let rows_of_attention =
            all_sorted_users
            |>List.map (fun user ->
                attention_matrix.attention_to_users
                |>Map.tryFind user
                |>Option.defaultValue Map.empty
                |>Adjacency_matrix_helpers.row_of_attention_for_user
                      attention_to_intensity_color
                      self_attention_to_intensity_color
                      all_sorted_users
                      user
            )
        
        Adjacency_matrix_helpers.add_headers_to_adjacency_matrix
            handle_to_hame
            all_sorted_users
            rows_of_attention
        |>Googlesheet_writing.write_table
            sheet_service
            googlesheet
    
    
