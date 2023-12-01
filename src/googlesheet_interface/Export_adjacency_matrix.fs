namespace rvinowise.twitter

open System
open System.Collections.Generic
open Xunit

open rvinowise.twitter.database.tables



module Export_adjacency_matrix =
        
    
    
    let row_of_header
        (user_names: Map<User_handle, string>)
        all_sorted_users
        =
        all_sorted_users
        |>List.map (fun handle ->
            Map.tryFind handle user_names
            |>Option.defaultValue (User_handle.value handle)
            
        )
        |>List.map (fun user->user :> obj)
        |>List.append ["" :> obj]
    

    
    let row_of_user
        (user_names: Map<User_handle, string>)
        (user:User_handle)
        (interactions: int list)
        =
        [
            (Googlesheet_for_twitter.hyperlink_to_twitter_user user) :>obj
            user_names[user] :>obj
        ]@(
            interactions
            |>List.map (fun amount -> amount :> obj)
        )
        
    
    
    let prepare_a_row_of_interactions_with_all_users
        all_sorted_users
        main_user
        (known_interactions: Map<User_handle, int>)
        =
        all_sorted_users
        |>List.map(fun other_user->
            known_interactions
            |>Map.tryFind other_user
            |>Option.defaultValue 0
        )
    
    let row_of_user_interactions
        (user_names: Map<User_handle, string>)
        all_users
        (read_interactions: User_handle->seq<User_handle*int>)
        user 
        =
        ((user_names[user]) :> obj)
        ::
        (
            read_interactions user
            |>Map.ofSeq
            |>prepare_a_row_of_interactions_with_all_users
                all_users
                user
            |>List.map(fun amount -> amount :> obj)
        )
        
    
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
        
        let rows_of_users =
            all_users
            |>List.map (fun user->
                row_of_user_interactions
                    user_names
                    all_users
                    read_interactions
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