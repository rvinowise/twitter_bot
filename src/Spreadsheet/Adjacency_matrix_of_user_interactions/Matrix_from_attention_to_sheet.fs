namespace rvinowise.twitter

open System
open Npgsql
open rvinowise.twitter
open Xunit

(* user attention from local databases are combined in the central database.
this module exports them into the google sheet as a matrix *)

module Matrix_from_attention_to_sheet =
    

    let accounts_to_their_received_attention
        (users_attention: Map<User_handle, Map<User_handle, float>>)
        =
        users_attention
        |>Map.toSeq
        |>Seq.fold(fun received_attention (attentive_user, attention_from_user) ->
            attention_from_user
            |>Map.fold(fun received_attention attention_target attention_amount ->
                let old_received_attention =
                    received_attention
                    |>Map.tryFind attention_target
                    |>Option.defaultValue 0.0
                
                received_attention
                |>Map.add attention_target (old_received_attention+attention_amount)
            )
                received_attention
        )
            Map.empty
    
    let accounts_to_their_paid_attention
        (users_attention: Map<User_handle, Map<User_handle, float>>)
        =
        users_attention
        |>Map.toSeq
        |>Seq.map(fun (attentive_user,paid_attention) ->
            attentive_user,
            paid_attention
            |>Map.toSeq
            |>Seq.map snd
            |>Seq.reduce (+)
        )|>Map.ofSeq
    
    let accounts_sorted_by_received_attention
        (users_attention: Map<User_handle, Map<User_handle, float>>)
        =
        users_attention
        |>accounts_to_their_received_attention
        |>Map.toList
        |>List.sortByDescending snd
        |>List.map fst
    
    let accounts_sorted_by_integration_into_network
        database
        matrix_title
        matrix_datetime
        =
        let all_matrix_members =
            Adjacency_matrix_database.read_members_of_matrix
                database
                matrix_title
            |>List.map (fun account -> account,0.0)
            
            
        let users_attention =
            User_attention_database.read_combined_attention_within_matrix
                database
                matrix_title
                matrix_datetime
        
        let users_total_attention =
            User_attention_database.read_total_combined_attention_from_users
                database 
                matrix_datetime
        
        let relative_attention =
            Adjacency_matrix_helpers.absolute_to_relative_attention
                users_attention
                users_total_attention
        
        let paid_attention =
            relative_attention
            |>accounts_to_their_paid_attention
        
        let received_attention =
            relative_attention
            |>accounts_to_their_received_attention
        
        all_matrix_members
        |>List.append (Map.toList paid_attention)
        |>List.append (Map.toList received_attention)
        |>List.fold(fun network_integrations (user,value) ->
            let old_network_integration =
                network_integrations
                |>Map.tryFind user
                |>Option.defaultValue 0.0
            
            network_integrations
            |>Map.add user (value+old_network_integration)
        )
            Map.empty
        
        |>Map.toList
        |>List.sortByDescending snd

    let write_matrices_to_sheet
        sheet_service
        (database: NpgsqlConnection)
        doc_id
        matrix_title
        matrix_datetime
        handle_to_name
        =
        let titles_and_interactions =
            [
                Adjacency_matrix_helpers.likes_design;
                Adjacency_matrix_helpers.reposts_design;
                Adjacency_matrix_helpers.replies_design
            ]
            |>List.map(fun design ->
                design.attention_type,
                
                let users_attention =
                    User_attention_database.read_attention_within_matrix
                        database
                        matrix_title
                        design.attention_type
                        matrix_datetime
                
                let users_total_attention =
                    User_attention_database.read_total_attention_from_users
                        database 
                        design.attention_type
                        matrix_datetime
                
                let users_relative_attention =
                    Adjacency_matrix_helpers.absolute_to_relative_attention
                        users_attention
                        users_total_attention
                
                Adjacency_matrix_helpers.attention_matrix_for_colored_interactions
                    design.color
                    users_relative_attention
            )
        
        let sorted_members_of_matrix =
            accounts_sorted_by_integration_into_network
                database
                matrix_title
                matrix_datetime  
            |>List.map fst
            
        // Write_matrix_to_sheet.try_write_separate_interactions_to_sheet
        //     sheet_service
        //     doc_id
        //     titles_and_interactions
        //     handle_to_name
        //     sorted_members_of_matrix
            
        Write_matrix_to_sheet.try_write_combined_interactions_to_sheet
            sheet_service
            doc_id
            titles_and_interactions
            handle_to_name
            sorted_members_of_matrix
         
    
    let ``interactions_to_sheet``()=
        
        let doc_id = "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0"
        
        let central_db = Central_database.open_connection()
        let local_db = Local_database.open_connection()
        
        let handle_to_name =
            Twitter_user_database.handle_to_username
                local_db
        
        write_matrices_to_sheet
            (Googlesheet.create_googlesheet_service())
            central_db
            doc_id
            Adjacency_matrix.Longevity_members
            DateTime.Now
            handle_to_name
        ()
   
