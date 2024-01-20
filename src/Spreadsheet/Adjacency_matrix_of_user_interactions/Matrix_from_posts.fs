namespace rvinowise.twitter

open System
open Npgsql
open rvinowise.twitter
open Xunit


type User_attention_of_type = {
    absolute_attention: Map<User_handle, int>    
    total_attention: int    
    relative_attention: Map<User_handle, float>    
}

module Matrix_from_posts =
    
    let reverse_nested_maps (map: Map<'a,Map<'b,'T>>) = 
        [
            for KeyValue(a, m) in map do
                for KeyValue(b, v) in m do
                    yield b, (a, v)
        ]
        |> Seq.groupBy fst 
        |> Seq.map (fun (b, ats) -> b, ats |> Seq.map snd |> Map.ofSeq) 
        |> Map.ofSeq 

    let merge_maps
        (merge_values : 'Key -> 'Value -> 'Value -> 'Value)
        (added_map : Map<'Key, 'Value>)
        (base_map : Map<'Key, 'Value>)
        =
        Map.fold (fun total_map key added_value ->
            match
                Map.tryFind key total_map
            with
            |Some existing_value ->
                total_map
                |>Map.add key (
                    merge_values key existing_value added_value 
                ) 
            |None ->
                total_map
                |>Map.add key added_value 
        )
            base_map
            added_map
    
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
    
    let sort_accounts_by_integration_into_network
        all_matrix_members
        relative_attention
        =
        let zero_integration_for_everybody =
            all_matrix_members
            |>List.map (fun account -> account,0.0)
        
        let paid_attention =
            relative_attention
            |>accounts_to_their_paid_attention
        
        let received_attention =
            relative_attention
            |>accounts_to_their_received_attention
        
        zero_integration_for_everybody
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

    let detailed_attention_from_account
        (database: NpgsqlConnection)
        before_date
        account
        =
        
        User_attention_from_posts.attention_types
        |>List.map(fun (attention_type,read_attention) ->
            let absolute_attention =
                read_attention
                    database
                    before_date
                    account
                |>Map.ofSeq
                
            let total_known_attention =
                absolute_attention
                |>Map.values
                |>Seq.reduce (+)
                
            
            attention_type,
            {
                absolute_attention = absolute_attention
                        
                total_attention = total_known_attention
                
                relative_attention =
                    absolute_attention
                    |>Map.map(fun target absolute_attention ->
                        (float absolute_attention)/(float total_known_attention)
                    )
            }
        )
        
        
    let write_matrices_to_sheet
        sheet_service
        (central_db: NpgsqlConnection)
        (local_db: NpgsqlConnection)
        doc_id
        matrix_title
        matrix_datetime
        handle_to_name
        =
        let matrix_members =
            Adjacency_matrix_database.read_members_of_matrix
                central_db
                matrix_title
        
        let user_detailed_attention =
            matrix_members
            |>List.map (fun account ->
                account,
                detailed_attention_from_account
                    local_db
                    matrix_datetime
                    account
            )
        
        let combined_relative_attention =
            user_detailed_attention
            |>List.map(fun (attentive_user, attention_details) ->
                attentive_user,
                attention_details
                |>List.map (fun (_,attention_detail) ->
                    attention_detail.relative_attention   
                )
                |>List.fold(fun total_map attention_map ->
                    merge_maps
                        (fun target old_value new_value -> old_value + new_value)
                        attention_map
                        total_map
                )
                    Map.empty
            )|>Map.ofList
        
        let sorted_members_of_matrix =
            sort_accounts_by_integration_into_network
                matrix_members
                combined_relative_attention
            |>List.map fst
        
//        let titles_and_interactions =
//            [
//                Adjacency_matrix_helpers.likes_design;
//                Adjacency_matrix_helpers.reposts_design;
//                Adjacency_matrix_helpers.replies_design
//            ]
//            |>List.map(fun design ->
//                design.attention_type,
//                
//                let users_attention =
//                    User_attention_database.read_attention_within_matrix
//                        database
//                        matrix_title
//                        design.attention_type
//                        matrix_datetime
//                
//                let users_total_attention =
//                    User_attention_database.read_total_attention_from_users
//                        database 
//                        design.attention_type
//                        matrix_datetime
//                
//                let users_relative_attention =
//                    Adjacency_matrix_helpers.absolute_to_relative_attention
//                        users_attention
//                        users_total_attention
//                
//                Adjacency_matrix_helpers.attention_matrix_for_colored_interactions
//                    design.color
//                    users_relative_attention
//            )
        ()
        
            
        // Write_matrix_to_sheet.try_write_separate_interactions_to_sheet
        //     sheet_service
        //     doc_id
        //     titles_and_interactions
        //     handle_to_name
        //     sorted_members_of_matrix
            
//        Write_matrix_to_sheet.try_write_combined_interactions_to_sheet
//            sheet_service
//            doc_id
//            titles_and_interactions
//            handle_to_name
//            sorted_members_of_matrix
         
    
    let attention_matrix_to_sheet()=
        
        let doc_id = "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0"
        
        let central_db = Central_database.open_connection()
        let local_db = Local_database.open_connection()
        
        let handle_to_name =
            Twitter_user_database.handle_to_username
                local_db
        
        write_matrices_to_sheet
            (Googlesheet.create_googlesheet_service())
            central_db
            local_db
            doc_id
            Adjacency_matrix.Longevity_members
            DateTime.Now
            handle_to_name
        ()
   
