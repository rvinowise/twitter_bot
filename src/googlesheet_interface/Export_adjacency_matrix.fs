namespace rvinowise.twitter

open System
open System.Collections.Generic
open Xunit

open rvinowise.twitter.database.tables



module Export_adjacency_matrix =
        
        

        
    
    let sheet_row_of_header
        (user_names: Map<User_handle, string>)
        all_sorted_users
        =
        all_sorted_users
        |>List.map (Map.find user_names)
        |>List.map (fun user->user :> obj)
        |>List.append ["" :> obj]
        |>List :> IList<obj>
    

    
    let sheet_row_of_user
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
        
        |>List :> IList<obj>
    
    
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
    
    let update_googlesheet
        database
        (read_interactions: User_handle->seq<User_handle*int>)
        all_users
        =
        
        let user_names =
            Social_activity_database.read_user_names_from_handles
                database
                
        
        all_users
        |>List.map (fun user->
            user,
            read_interactions user
            |>Map.ofSeq
            |>prepare_a_row_of_interactions_with_all_users
                all_users
                user
            |>sheet_row_of_user
                user_names
                user
        )
            