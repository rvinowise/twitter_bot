namespace rvinowise.twitter

open System
open Npgsql
open rvinowise.html_parsing
open rvinowise.twitter
open Xunit
open FsUnit



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
                total_map
                
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
            ["user1",10;"user2",1;"user3",14;"other_user2",112;"other_user1",100]
            |>Map.ofList
        )
        merged12
        |>should equal (
            ["user1",10;"user2",1;"user3",14;"other_user2",11;]
            |>Map.ofList
        )
        merged13
        |>should equal (
            ["user1",0;"user2",1;"user3",2;"other_user1",100;"other_user2",101;]
            |>Map.ofList
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
        combined_inout_attention
        all_matrix_members
        =
        let zero_integration_for_everybody =
            all_matrix_members
            |>List.map (fun account -> account,0.0)
            |>Map.ofList
        
        merge_maps
            (fun _ old added -> old+added)
            [zero_integration_for_everybody;combined_inout_attention]
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
                design = design
            }    
        )
        
    let write_matrices_to_sheet
        handle_to_name
        sheet_service
        (database: NpgsqlConnection)
        doc_id
        matrix_title
        matrix_datetime
        =
        let computed_attention =
            attention_in_matrices
                database
                matrix_datetime
                matrix_title
        
        let combined_inout_relative_attention =
            computed_attention
            |>List.collect(fun computed_attention ->
                [
                    computed_attention.total_paid_attention
                    computed_attention.total_received_attention
                ]
            )
            |>merge_maps
                (fun _ old_value new_value -> old_value + new_value)
        
        let sorted_matrix_members =
            Adjacency_matrix_database.read_members_of_matrix
                database
                matrix_title
            |>sort_accounts_by_integration_into_network
                combined_inout_relative_attention
            |>List.map fst
              
        Write_matrix_to_sheet.try_write_separate_interactions_to_sheet
            handle_to_name
            sheet_service
            doc_id
            computed_attention
            sorted_matrix_members
            
        Write_matrix_to_sheet.try_write_combined_interactions_to_sheet
            handle_to_name
            sheet_service
            doc_id
            computed_attention
            sorted_matrix_members
         
    
    let attention_matrix_to_sheet()=
        
        let doc_id = "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0" //live
        //let doc_id = "1Rb9cGqTb-3OknU_DWuPMBlMpRAV9PHhOvfc1LlN3h6U" //test
        
        let local_db = Local_database.open_connection()
        
        let handle_to_name =
            Twitter_user_database.handle_to_username
                local_db
        
        write_matrices_to_sheet
            handle_to_name
            (Googlesheet.create_googlesheet_service())
            local_db
            doc_id
            Adjacency_matrix.Longevity_members
            //DateTime.Now
            (Html_parsing.parse_datetime "yyyy-MM-dd HH:mm:ss" "2024-01-22 17:34:39")
        ()
   
