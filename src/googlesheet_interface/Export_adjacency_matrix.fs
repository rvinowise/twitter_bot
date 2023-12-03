namespace rvinowise.twitter

open System
open System.Collections.Generic
open Google.Apis.Sheets.v4.Data
open Xunit

open rvinowise.twitter.database.tables



module Export_adjacency_matrix =
        
    
    
    let row_of_header
        (user_names: Map<User_handle, string>)
        all_sorted_users
        =
        all_sorted_users
        |>List.map (Googlesheet.username_from_handle user_names)
        |>List.map (fun user->user :> obj)
        |>List.append ["" :> obj]
    

    
        
    
    let prepare_a_row_of_interactions_with_all_users
        all_sorted_users
        (known_interactions: Map<User_handle, int>)
        =
        all_sorted_users
        |>List.map(fun other_user->
            known_interactions
            |>Map.tryFind other_user
            |>Option.defaultValue 0
        )
    
    let row_of_interactions_for_user
        (user_names: Map<User_handle, string>)
        all_sorted_users
        (user_interactions: Map<User_handle, int>)
        user 
        =
        [
            (Googlesheet_for_twitter.hyperlink_to_twitter_user user) :>obj
            (Googlesheet.username_from_handle user_names user) :> obj
        ]
        @
        (
            user_interactions
            |>prepare_a_row_of_interactions_with_all_users
                all_sorted_users
            |>List.map(fun amount -> amount :> obj)
        )
        
    
    let min_and_max_values
        (user_interactions: Map<User_handle, Map<User_handle, int>>)
        =
        user_interactions
        |>Map.values
        |>Seq.map (fun (interactions:Map<User_handle, int>) ->
            let interaction_values = 
                interactions
                |>Map.values
            Seq.min interaction_values,
            Seq.max interaction_values
        )|>List.ofSeq
        |>List.unzip
        |> fun (mins,maxs) ->
            mins|>Seq.min,
            maxs|>Seq.max
    
    let update_googlesheet
        database
        googlesheet
        (read_interactions: User_handle->seq<User_handle*int>)
        all_users
        =
        
        let user_names =
            Social_activity_database.read_user_names_from_handles
                database
        
        let row_of_header =
            row_of_header
                user_names
                all_users
        
        let user_interactions =
            all_users
            |>List.map (fun user->
                user,
                read_interactions user
                |>Map.ofSeq
            )|>Map.ofList
        
        let min_value,max_value =
            min_and_max_values user_interactions
            
        let min_value_color = {
            red=1
            green=0
            blue=0
            alpha=0
        }
        let max_value_color = {
            red=1
            green=0
            blue=0
            alpha=1
        }
        
        let rows_of_users =
            all_users
            |>List.map (fun user->
                row_of_interactions_for_user
                    user_names
                    all_users
                    user_interactions[user]
                    user
            )
        
        (row_of_header::rows_of_users)
        |>Googlesheet.obj_lists_to_google_obj
        |>Googlesheet.input_into_sheet
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
            [
                "yangranat"
                "MikhailBatin"
                "Nst_Egorova"
                "RichardDawkins"
            ]
            |>List.map User_handle
        
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