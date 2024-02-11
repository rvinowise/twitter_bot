namespace rvinowise.twitter

open System
open Npgsql
open rvinowise.html_parsing
open rvinowise.twitter
open Xunit
open FsUnit



module Prepare_attention_matrix =
    
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
            if paid_attention.IsEmpty then
                0.
            else
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
    
    let sort_accounts_by_their_values
        values_for_members
        all_matrix_members
        =
        let zero_values_for_everybody =
            all_matrix_members
            |>Seq.map (fun account -> account,0.0)
            |>Map.ofSeq
        
        merge_maps
            (fun _ old added -> old+added)
            [zero_values_for_everybody;values_for_members]
        |>Map.toList
        |>List.sortByDescending snd

    
    let attention_in_matrices
        database
        matrix_title
        matrix_datetime
        =
        let matrix_members =
            Adjacency_matrix_database.read_members_of_matrix
                database
                matrix_title
            
        [
            Adjacency_matrix_helpers.likes_design,
            User_attention_database.read_cached_or_calculated_attention_in_matrix
                Attention_type.Likes,
            User_attention_database.read_cached_or_calculated_total_attention
                Attention_type.Likes;
            
            Adjacency_matrix_helpers.reposts_design,
            User_attention_database.read_cached_or_calculated_attention_in_matrix
                Attention_type.Reposts,
            User_attention_database.read_cached_or_calculated_total_attention
                Attention_type.Reposts;
            
            Adjacency_matrix_helpers.replies_design,
            User_attention_database.read_cached_or_calculated_attention_in_matrix
                Attention_type.Replies,
            User_attention_database.read_cached_or_calculated_total_attention
                Attention_type.Replies;
        ]
        |>List.map(fun (design, read_attention, read_total_known_attention) ->
            let total_known_attention =
                read_total_known_attention
                    database
                    matrix_datetime
                    matrix_members
            
            let absolute_attention_in_matrix =
                matrix_members
                |>Set.ofSeq
                |>read_attention
                    database
                    matrix_datetime
            
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
    
    let combined_inout_relative_attention
        attention_types
        =
        attention_types
        |>List.collect(fun attention_type ->
            [
                attention_type.total_paid_attention
                attention_type.total_received_attention
            ]
        )
        |>merge_maps
            (fun _ old_value new_value -> old_value + new_value)
    
    
    
    let info_cell
        (matrix_title)
        (matrix_datetime: DateTime) 
        =
        {
            Cell.value =
                $"""attention before
{matrix_datetime.ToString("yyyy-MM-dd HH:mm")}

matrix:
{matrix_title}

src:
https://github.com/rvinowise/twitter_bot"""
                |>Cell_value.Formula
            color = Color.white
            style = Text_style.regular
        }
        
    
         
    

   
