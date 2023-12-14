namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit


type Interaction_colorscheme = {
    min_color: Color
    max_color: Color
}

module Adjacency_matrix =
    
    
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
    
    
    
   
    
        
    
    
    
    
    
    [<Fact>]//(Skip="manual")
    let ``try update_googlesheet``() =
        //https://docs.google.com/spreadsheets/d/1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY/edit#gid=0
        let likes_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=0
            page_name="Likes"
        }
        let likes_colorscheme = {
            min_color={r=1; g=1;b=1}
            max_color={r=1; g=0;b=0}
        }
        
        let reposts_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=2108706810
            page_name="Reposts"
        }
        let reposts_colorscheme = {
            min_color={r=1; g=1;b=1}
            max_color={r=0; g=1;b=0}
        }
        
        let replies_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=2007335692
            page_name="Replies"
        }
        let replies_colorscheme = {
            min_color={r=1; g=1;b=1}
            max_color={r=0; g=0;b=1}
        }
        
        let everything_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=1019851571
            page_name="Everything"
        }
            
        let database = Twitter_database.open_connection()
        
        let all_sorted_users =
            Settings.Competitors.list
            |>Scrape_list_members.scrape_twitter_list_members
                (Browser.open_browser())
            |>List.map (Twitter_profile_from_catalog.user >> Twitter_user.handle)
        
        let all_users = Set.ofList all_sorted_users
        let user_names =
            Social_activity_database.read_user_names_from_handles
                database
        
        
        let likes_interactions =
            all_users
            |>maps_of_user_interactions
                (User_interaction.read_likes_by_user database)    
        
        let reposts_interactions =
            all_users
            |>maps_of_user_interactions
                (User_interaction.read_reposts_by_user database)
                
        let replies_interactions =
            all_users
            |>maps_of_user_interactions
                (User_interaction.read_replies_by_user database)  
        
        let update_googlesheet_with_interaction_type =
            update_googlesheet
                all_sorted_users
                user_names
        
        update_googlesheet_with_interaction_type
            likes_googlesheet
            likes_colorscheme
            likes_interactions
        update_googlesheet_with_interaction_type
            reposts_googlesheet
            reposts_colorscheme
            reposts_interactions
        update_googlesheet_with_interaction_type
            replies_googlesheet
            replies_colorscheme
            replies_interactions
            
        update_googlesheet_with_total_interactions
            everything_googlesheet
            likes_interactions
            reposts_interactions
            replies_interactions