namespace rvinowise.twitter


open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open Aglomera
open Aglomera.Linkage
open Aglomera.D3
open  Newtonsoft.Json
open Xunit
open FsUnit

open Plotly.NET
open rvinowise.twitter.database_schema
open rvinowise.twitter.database_schema.tables

open FSharp.Stats
open FSharp.Stats.ML.Unsupervised
open Microsoft.FSharp.Quotations

open rvinowise.ui.infrastructure




module Graph_from_clustering =
    
    
    let changes_in_clustering_stage
        (previous_stage: ClusterSet<User_handle>)
        (current_stage: ClusterSet<User_handle>)
        =
        current_stage
        |>Seq.find(fun potentional_new_cluster ->
            previous_stage
            |>Seq.contains potentional_new_cluster
            |>not
        )
    
    let node_id_from_cluster (cluster: Cluster<User_handle>) =
        cluster
        |>Seq.map User_handle.value
        |>String.concat "_"
    
    let node_label_from_cluster
        handle_to_name
        user_importance
        (cluster: Cluster<User_handle>) 
        =
        match
            cluster
            |>List.ofSeq    
        with
        |[single_member] ->
            //$"<<B>{User_handle.value single_member}</B>>"
            $"\"{handle_to_name single_member}\""
        |many_members ->
            many_members
            |>List.sortByDescending (fun user ->
                Map.tryFind user user_importance
                |>Option.defaultValue 0.
            )
            |>List.head
            |>fun user -> $"\"{handle_to_name user}\""
    
    let decorate_if_leaf
        (cluster: Cluster<User_handle>)
        (node: Node)
        =
        if cluster.Count = 1 then
            Graph.fill_with_color "lightgray" node
            |>ignore
        node
        
    let add_cluster_to_graph
        handle_to_name
        node_id_for_cluster
        user_importance
        graph
        (cluster: Cluster<User_handle>)
        =
        let cluster_node =
            graph
            |>Graph.provide_labeled_vertex
                (node_id_for_cluster cluster)
                (node_label_from_cluster handle_to_name user_importance cluster)
            
            
        let fused_node1 =
            graph
            |>Graph.provide_labeled_vertex
                (node_id_for_cluster cluster.Parent1)
                (node_label_from_cluster handle_to_name user_importance cluster.Parent1)
            |>decorate_if_leaf cluster.Parent1
            
        let fused_node2 =
            graph
            |>Graph.provide_labeled_vertex
                (node_id_for_cluster cluster.Parent2)
                (node_label_from_cluster handle_to_name user_importance cluster.Parent2)
            |>decorate_if_leaf cluster.Parent2
            
        Graph.with_edge cluster_node fused_node1
        Graph.with_edge cluster_node fused_node2
    
    
    let node_id_for_cluster () =
        let cluster_to_id = Dictionary<Cluster<User_handle>, int>()
        let mutable free_id = 0
        
        let node_id_for_cluster (cluster: Cluster<User_handle>) =
            match cluster_to_id.TryGetValue cluster with
            |true, assigned_id ->
                string assigned_id
            |_ ->
                let new_assigned_id = free_id
                free_id <- free_id + 1
                cluster_to_id[cluster] <- new_assigned_id

                string new_assigned_id
        
        node_id_for_cluster
    
    let create_graph
        handle_to_name
        user_importance
        (clustering: ClusteringResult<User_handle>)
        =
        let initial_stage =
            Seq.head clustering
        
        let node_id_for_cluster = node_id_for_cluster()
            
        clustering
        |>Seq.skip 1
        |>Seq.fold(fun (graph,previous_stage) (clustering_stage: ClusterSet<User_handle>) ->
            
            let added_cluster =
                changes_in_clustering_stage
                    previous_stage
                    clustering_stage
            
            add_cluster_to_graph
                handle_to_name
                node_id_for_cluster
                user_importance
                graph
                added_cluster
            |>ignore
                    
            graph,clustering_stage
        )
            (
                (Graph.empty "Longevity_members"),
                initial_stage
            )
        |>fst