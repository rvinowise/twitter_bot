namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit



module Adjacency_matrix_compound =()
        
    
    (*
    
    
    let interactions_to_intensity_colors
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
                min_value_color
                max_value_color
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
    
    
    let hyperlink_of_user
        usernames
        handle
        =
        {
            handle=handle
            name=
                usernames
                |>Map.tryFind handle
                |>Option.defaultValue (User_handle.value handle)
        }
        |>Googlesheet_for_twitter.hyperlink_to_twitter_user
   
    
        
    let update_googlesheet
        database
        googlesheet
        (read_interactions: User_handle->seq<User_handle*int>)
        all_sorted_users
        =
        let user_names =
            Social_activity_database.read_user_names_from_handles
                database
        
        let user_interactions =
            all_sorted_users
            |>Set.ofSeq
            |>maps_of_user_interactions
                read_interactions
            
        let
            interaction_to_intensity_color,
            self_interaction_to_intensity_color
                =
                interactions_to_intensity_colors user_interactions
        
        
        let colored_interactions =    
            user_interactions
            |>user_interactions_to_colored_values
                interaction_to_intensity_color
                self_interaction_to_intensity_color
            
            
        let header_of_users =
            all_sorted_users
            |>List.map (hyperlink_of_user user_names)
            |>List.map (fun url -> {
                Cell.value = Cell_value.Formula url
                color = Color.white
                style = Text_style.vertical
            })
            
        let left_column_of_users =
            all_sorted_users
            |>List.map (hyperlink_of_user user_names)
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
                |>row_of_interactions_for_user
                      all_sorted_users
            )
        
        (header_of_users::rows_of_interactions)
        |>Table.transpose Cell.empty
        |>List.append [left_column_of_users]
        |>Table.transpose Cell.empty
        |>Googlesheet_writing.write_table
            (Googlesheet.create_googlesheet_service())
            googlesheet
        
    
    [<Fact>]//(Skip="manual")
    let ``try update_googlesheet``() =
        //https://docs.google.com/spreadsheets/d/1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY/edit#gid=0
        let likes_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=0
            page_name="Likes"
        }
        let reposts_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=2108706810
            page_name="Reposts"
        }
        let replies_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=2007335692
            page_name="Replies"
        }
        let everything_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=1019851571
            page_name="Everything"
        }
            
        let database = Twitter_database.open_connection()
        
        let all_users =
            Settings.Competitors.list
            |>Scrape_list_members.scrape_twitter_list_members
                (Browser.open_browser())
            |>List.map (Twitter_profile_from_catalog.user >> Twitter_user.handle)
        
        update_googlesheet
            database
            likes_googlesheet
            (User_interaction.read_likes_by_user database)
            all_users
        update_googlesheet
            database
            reposts_googlesheet
            (User_interaction.read_reposts_by_user database)
            all_users
        update_googlesheet
            database
            replies_googlesheet
            (User_interaction.read_replies_by_user database)
            all_users *)