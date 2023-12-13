namespace rvinowise.twitter

open rvinowise.web_scraping
open Xunit



module Export_adjacency_matrix =
        
    
    
    let row_of_header
        (user_names: Map<User_handle, string>)
        all_sorted_users
        =
        all_sorted_users
        |>List.map (Googlesheet.username_from_handle user_names)
        |>List.map (fun user->user :> obj)
        |>List.append ["" :> obj]
    
        
    
    let row_of_interactions_with_all_users
        (value_to_color: int -> rvinowise.twitter.Color)
        all_sorted_users
        (known_interactions: Map<User_handle, int>)
        =
        all_sorted_users
        |>List.map(fun other_user->
            let interaction_intencity =
                known_interactions
                |>Map.tryFind other_user
                |>Option.defaultValue 0
            interaction_intencity,
            value_to_color interaction_intencity
        )
        
    
    let min_and_max_values_from_maps
        (user_interactions: Map<User_handle, Map<User_handle, int>>)
        =
        user_interactions
        |>Map.values
        |>Seq.map (fun (interactions:Map<User_handle, int>) ->
            let interaction_values = 
                interactions
                |>Map.values
            if Seq.isEmpty interaction_values then
                0,0
            else
                Seq.min interaction_values,
                Seq.max interaction_values
        )|>List.ofSeq
        |>List.unzip
        |> fun (mins,maxs) ->
            mins|>Seq.min,
            maxs|>Seq.max
    

    let min_and_max_values_from_lists
        (lists: int list list)
        =
        lists
        |>List.map (fun inner_list ->
            List.min inner_list,    
            List.max inner_list
        )
        |>List.unzip
        |> fun (mins,maxs) ->
            mins|>Seq.min,
            maxs|>Seq.max
    
    let interactions_with_other_users
        (read_interactions: User_handle->seq<User_handle*int>)
        (other_sorted_users: User_handle list)
        user
        =
        let interactions_with_all_users =
            read_interactions user
        
        other_sorted_users
        |>List.map(fun other_sorted_user ->
            interactions_with_all_users
            |>Seq.tryPick(fun (user,interaction_amount) ->
                if user = other_sorted_user then
                    Some interaction_amount
                else
                    None
            )
            |>Option.defaultValue 0
        )
    
    let maps_of_user_interactions 
        (read_interactions: User_handle->seq<User_handle*int>)
        (all_users: User_handle seq)
        =
        all_users
        |>Seq.map(fun user ->
            user,
            user
            |>read_interactions
            |>Map.ofSeq
        )|>Map.ofSeq
    
    let min_value_color = {
        red=1
        green=1
        blue=1
    }
    let max_value_color = {
        red=1
        green=0
        blue=0
    }
    
    let interactions_to_intencity_colors
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
        
        let interaction_to_intensity_color =
            Color.cell_color_for_value
                min_value_color
                max_value_color
                (Seq.min interactions_with_others)
                (Seq.max interactions_with_others)
        
        let self_interaction_to_intensity_color =
            Color.cell_color_for_value
                {red=1;green=1;blue=1}
                {red=0;green=0;blue=0}
                (Seq.min interactions_with_oneself)
                (Seq.max interactions_with_oneself)
        
        interaction_to_intensity_color,
        self_interaction_to_intensity_color
        
        
    let user_interactions_to_colored_values
        interaction_to_intensity_color
        self_interaction_to_intensity_color
        (all_sorted_users: User_handle list)
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
        user
        (colored_interactions:Map<User_handle, int*Color>)
        =
        all_sorted_users
        |>List.map(fun other_user ->
            colored_interactions
            |>Map.tryFind other_user
            |>Option.defaultValue (0, )
            |>Googlesheet_writing.colored_number_to_google_cell
        )
        
        
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
            maps_of_user_interactions
                read_interactions
                all_sorted_users
        
//        let user_interactions =
//            all_sorted_users
//            |>List.map (fun user->
//                interactions_with_other_users
//                    read_interactions
//                    all_sorted_users
//                    user
//            )
        
        let
            interaction_to_intencity_color,
            self_interaction_to_intencity_color
                =
                interactions_to_intencity_colors user_interactions
        
        
        let colored_interactions =    
            user_interactions
            |>user_interactions_to_colored_values
                interaction_to_intencity_color
                self_interaction_to_intencity_color
                all_sorted_users
            
            
        let header_of_users =
            all_sorted_users
            |>List.map (Googlesheet.username_from_handle user_names)
            |>Googlesheet_writing.text_row_to_google_vertical_cells
        
        let left_column_of_users =
            all_sorted_users
            |>List.map (Googlesheet.username_from_handle user_names)
            |>List.append [""]
            |>Googlesheet_writing.text_row_to_google_cells
        
 
        
        let rows_of_interactions =
            all_sorted_users
            |>List.map (fun user ->
                colored_interactions
                |>Map.find user
                |>row_of_interactions_for_user all_sorted_users user
            )
        
        (header_of_users::rows_of_interactions)
        |>Table.transpose Googlesheet.empty_cell
        |>List.append [left_column_of_users]
        |>Table.transpose Googlesheet.empty_cell
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
            all_users