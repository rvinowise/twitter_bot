namespace rvinowise.twitter

open System.Globalization
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
    
    
    let add_headers_to_attention_cells
        handle_to_hame
        first_cell
        all_sorted_handles
        (attention_in_matrix: Attention_in_matrix)
        (table: Cell list list)
        =
        let received_attention_row =
            all_sorted_handles
            |>List.map(fun target ->
                {
                    Cell.color = Adjacency_matrix_helpers.grey_header
                    style = Text_style.regular
                    value =
                        attention_in_matrix.total_received_attention
                        |>Map.tryFind target
                        |>Option.defaultValue 0.0
                        |>fun number ->
                            System.String.Format(
                                NumberFormatInfo.InvariantInfo,
                                "{0:0.##}%",
                                number*100.0
                            )
                        |>Cell_value.Text
                }
            )
        let top_row_of_users =
            Adjacency_matrix_helpers.top_row_of_users
                handle_to_hame
                all_sorted_handles    
        
        let paid_attention_column =
            all_sorted_handles
            |>List.map(fun target ->
                {
                    Cell.color = Adjacency_matrix_helpers.grey_header
                    style = Text_style.regular
                    value =
                        attention_in_matrix.total_paid_attention
                        |>Map.tryFind target
                        |>Option.defaultValue 0.0
                        |>fun number ->
                            System.String.Format(
                                NumberFormatInfo.InvariantInfo,
                                "{0:0.##}%",
                                number*100.0
                            )
                        |>Cell_value.Text
                }
            )
            |>List.append [
                {
                    Cell.color = Adjacency_matrix_helpers.grey_header
                    style = Text_style.vertical
                    value = Cell_value.Text "% attention paid\nwithin matrix"
                }
                Cell.from_colored_text Adjacency_matrix_helpers.grey_header ""
            ]
        
        let all_known_attention_column =
            all_sorted_handles
            |>List.map(fun target ->
                let known_attention =
                    attention_in_matrix.total_known_attention
                    |>Map.tryFind target
                    |>Option.defaultValue 0
                {
                    Cell.color = Adjacency_matrix_helpers.grey_header
                    style = Text_style.regular
                    value = Cell_value.Integer known_attention
                }
            )
            |>List.append [
                {
                    Cell.color = Adjacency_matrix_helpers.grey_header
                    style = Text_style.vertical
                    value = Cell_value.Text "total known\nattention"
                }
                Cell.from_colored_text Adjacency_matrix_helpers.grey_header ""
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
                first_cell
                {
                    Cell.value = Cell_value.Text "% attention received\nwithin matrix"
                    color = Adjacency_matrix_helpers.grey_header
                    style = Text_style.regular
                }
            ]
        
        table
        |>List.append [received_attention_row]
        |>List.append [top_row_of_users]
        |>Table.transpose Cell.empty
        |>List.append [paid_attention_column]
        |>List.append [all_known_attention_column]
        |>List.append [left_column_of_users]
        |>Table.transpose Cell.empty
    
    
    let row_of_attention_for_user
        interaction_color
        self_interaction_color
        all_sorted_users
        attentive_user
        (absolute_attentions:Map<User_handle, int>)
        (relative_attentions:Map<User_handle, float>)
        =
        all_sorted_users
        |>List.map(fun other_user ->
            let absolute_value =
                absolute_attentions
                |>Map.tryFind other_user
                |>Option.defaultValue 0
            
            let relative_value =
                relative_attentions
                |>Map.tryFind other_user
                |>Option.defaultValue 0.0
            
            let cell_text =
                System.String.Format(
                    NumberFormatInfo.InvariantInfo,
                    "{0:0}\n{1:0.##}%",
                    absolute_value,
                    relative_value*100.0
                )
            {
                Cell.value = Cell_value.Text cell_text
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
       
    let update_googlesheet
        handle_to_hame
        sheet_service
        googlesheet
        first_cell
        all_sorted_users
        (attention_in_matrix: Attention_in_matrix)
        =
        let attention_to_intensity_color =
            attention_in_matrix.relative_attention
            |>Adjacency_matrix_helpers.interactions_with_others
            |>Adjacency_matrix_helpers.border_values_of_attention
            |>Adjacency_matrix_helpers.cell_color_for_value
                Color.white
                attention_in_matrix.design.color
        
        let self_attention_to_intensity_color =
            attention_in_matrix.relative_attention
            |>Adjacency_matrix_helpers.interactions_with_oneself
            |>Adjacency_matrix_helpers.border_values_of_attention
            |>Adjacency_matrix_helpers.cell_color_for_value
                Color.white
                {r=0.5;g=0.5;b=0.5}
                
        
        let rows_of_attention =
            all_sorted_users
            |>List.map (fun user ->
                let absolute_attention_of_user =
                    attention_in_matrix.absolute_attention
                    |>Map.tryFind user
                    |>Option.defaultValue Map.empty
                let relative_attention_of_user =
                    attention_in_matrix.relative_attention
                    |>Map.tryFind user
                    |>Option.defaultValue Map.empty
                
                row_of_attention_for_user
                      attention_to_intensity_color
                      self_attention_to_intensity_color
                      all_sorted_users
                      user
                      absolute_attention_of_user
                      relative_attention_of_user
            )
        
        rows_of_attention
        |>add_headers_to_attention_cells
            handle_to_hame
            first_cell
            all_sorted_users
            attention_in_matrix
        |>Googlesheet_writing.write_table
            sheet_service
            googlesheet
    
    
