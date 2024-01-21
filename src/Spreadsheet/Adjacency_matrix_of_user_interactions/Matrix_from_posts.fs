namespace rvinowise.twitter

open System
open Npgsql
open rvinowise.twitter
open Xunit


type Attention_in_matrix = {
    absolute_attention: Map<User_handle, Map<User_handle, int>>
    total_known_attention: Map<User_handle, int>    
    relative_attention: Map<User_handle, Map<User_handle, float>>
    total_paid_attention: Map<User_handle, float>
    total_received_attention: Map<User_handle, float>
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

    
    let merge_maps_by_appending
        (merge_values : 'Key -> 'Value -> 'Value -> 'Value)
        (maps : Map<'Key, 'Value> seq)
        =
        maps
    
    let merge_maps_by_folding
        (merge_values : 'Key -> 'Value -> 'Value -> 'Value)
        (maps : Map<'Key, 'Value> seq)
        =
        maps
        |>Seq.fold (fun total_map added_map ->
            added_map
            |>Map.fold (fun total_map key added_value ->
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
                Map.empty
                
        )
            Map.empty
    
    let merge_maps = merge_maps_by_folding
    
    [<Fact>]
    let ``try merge_maps, small maps, adding values``()=
        let map1 = ["user1",0;"user2",1;"user3",2]|>Map.ofList
        let map2 = ["user1",10;"other_user2",11;"user3",12]|>Map.ofList
        let map3 = ["other_user1",100;"other_user2",101]|>Map.ofList
    
        let merged123 =
            merge_maps
                (fun _ value1 value2 -> value1 + value2)
                [map1;map2;map3]
    
        let merged12 =
            merge_maps
                (fun _ value1 value2 -> value1 + value2)
                [map1;map2]
        
        let merged13 =
            merge_maps
                (fun _ value1 value2 -> value1 + value2)
                [map1;map3]
    
        merged123
        |>should equal (
                
        )
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
    
    let sort_accounts_by_received_attention
        (users_attention: Map<User_handle, Map<User_handle, float>>)
        =
        users_attention
        |>accounts_to_their_received_attention
        |>Map.toList
        |>List.sortByDescending snd
        |>List.map fst
    
    let sort_accounts_by_integration_into_network
        all_matrix_members
        combined_paid_attention
        combined_received_attention
        =
        let zero_integration_for_everybody =
            all_matrix_members
            |>List.map (fun account -> account,0.0)
        
        zero_integration_for_everybody
        |>List.append (Map.toList combined_paid_attention)
        |>List.append (Map.toList combined_received_attention)
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

    
    let attention_in_matrices
        database
        matrix_datetime
        matrix_title
        =
        [
            Adjacency_matrix_helpers.likes_design,
            User_attention_from_posts.read_likes_inside_matrix,
            User_attention_from_posts.read_total_known_likes_of_matrix_members;
            
            Adjacency_matrix_helpers.reposts_design,
            User_attention_from_posts.read_reposts_inside_matrix,
            User_attention_from_posts.read_total_known_reposts_of_matrix_members;
            
            
            Adjacency_matrix_helpers.replies_design,
            User_attention_from_posts.read_replies_inside_matrix,
            User_attention_from_posts.read_total_known_replies_of_matrix_members;
        ]
        |>List.map(fun (design, read_attention_inside_matrix, read_total_known_attention) ->
            let total_known_attention =
                read_total_known_attention
                    database
                    matrix_datetime
                    matrix_title
                |>Map.ofSeq
            
            let absolute_attention_in_matrix =
                read_attention_inside_matrix
                    database
                    matrix_datetime
                    matrix_title
            
            let relative_paid_attention_in_matrix =
                Adjacency_matrix_helpers.absolute_to_relative_attention
                    absolute_attention_in_matrix
                    total_known_attention
            
            
            let total_paid_attention_in_matrix =
                relative_paid_attention_in_matrix
                |>accounts_to_their_paid_attention
            
            let total_received_attention_in_matrix =
                relative_paid_attention_in_matrix
                |>accounts_to_their_received_attention
            
            
            {
                absolute_attention = absolute_attention_in_matrix
                total_known_attention = total_known_attention
                relative_attention = relative_paid_attention_in_matrix
                total_paid_attention = total_paid_attention_in_matrix
                total_received_attention = total_received_attention_in_matrix
            }    
        )
        
    let write_matrices_to_sheet
        sheet_service
        (database: NpgsqlConnection)
        doc_id
        matrix_title
        matrix_datetime
        handle_to_name
        =
        let matrix_members =
            Adjacency_matrix_database.read_members_of_matrix
                database
                matrix_title
        
        let computed_attention =
            attention_in_matrices
                database
                matrix_datetime
                matrix_title
        
        let combined_relative_attention =
            computed_attention
            |>List.map(fun computed_attention ->
                merge_maps
                    (fun target old_value new_value -> old_value + new_value)
                    computed_attention.total_paid_attention
                    computed_attention.total_received_attention
                
            )
        
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
            local_db
            doc_id
            Adjacency_matrix.Longevity_members
            DateTime.Now
            handle_to_name
        ()
   
