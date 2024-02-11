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
    
    // let node_id_for_cluster (cluster: Cluster<User_handle>) =
    //     cluster
    //     |>Seq.map User_handle.value
    //     |>String.concat "_"
    
    let html_url_to_twitter_user
        handle_to_name
        user_handle
        =
        $"""<<a href="{User_handle.url_from_handle user_handle}">{handle_to_name user_handle}</a>>"""
    
    let most_important_user_of_cluster
        user_importance
        (cluster: Cluster<User_handle>) 
        =
        match
            cluster
            |>List.ofSeq    
        with
        |[single_member] -> single_member
        |many_members ->
            many_members
            |>List.sortByDescending (fun user ->
                Map.tryFind user user_importance
                |>Option.defaultValue 0.
            )
            |>List.head
    
    let decorate_if_leaf
        (cluster: Cluster<User_handle>)
        (node: Node)
        =
        if cluster.Count = 1 then
            Graph.fill_with_color "lightgray" node
            |>ignore
        node
    
    let tooltip_for_cluster
        handle_to_name 
        user_integration
        user_received_attention
        user_paid_attention
        cluster
        =
        let most_integrated_user = 
            most_important_user_of_cluster
                user_integration
                cluster
        let most_popular_user = 
            most_important_user_of_cluster
                user_received_attention
                cluster
        let most_attentive_user = 
            most_important_user_of_cluster
                user_paid_attention
                cluster
        $"Most_integrated: {handle_to_name most_integrated_user} @{User_handle.value most_integrated_user}\n
Most_popular: {handle_to_name most_popular_user} @{User_handle.value  most_popular_user}\n
Most_attentive: {handle_to_name most_attentive_user} @{User_handle.value most_attentive_user}"
    
    let provide_node_for_cluster
        handle_to_name
        node_id_for_cluster
        user_integration
        user_popularity
        user_attentiveness
        graph
        cluster
        =
        let user_in_title =
            most_important_user_of_cluster
                user_integration
                cluster
        
        let tooltip =
            tooltip_for_cluster
                handle_to_name 
                user_integration
                user_popularity
                user_attentiveness
                cluster
        
        graph
        |>Graph.provide_labeled_vertex
            (node_id_for_cluster cluster)
            $"\"{handle_to_name user_in_title}\""
        |>Graph.with_attribute "href" $"\"{User_handle.url_from_handle user_in_title}\""
        |>Graph.with_attribute
              "tooltip"
              $"\"{tooltip}\""
        
    let add_cluster_to_graph
        handle_to_name
        node_id_for_cluster
        user_integration
        user_popularity
        user_attentiveness
        graph
        (cluster: Cluster<User_handle>)
        =
        let cluster_node =
            provide_node_for_cluster 
                handle_to_name
                node_id_for_cluster
                user_integration
                user_popularity
                user_attentiveness
                graph
                cluster
                
        let fused_node1 =
            provide_node_for_cluster 
                handle_to_name
                node_id_for_cluster
                user_integration
                user_popularity
                user_attentiveness
                graph
                cluster.Parent1
            |>decorate_if_leaf cluster.Parent1
            
        let fused_node2 =
            provide_node_for_cluster 
                handle_to_name
                node_id_for_cluster
                user_integration
                user_popularity
                user_attentiveness
                graph
                cluster.Parent2
            |>decorate_if_leaf cluster.Parent2
            
        Graph.with_edge cluster_node fused_node1 |>ignore
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
        user_integration
        user_popularity
        user_attentiveness
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
                user_integration
                user_popularity
                user_attentiveness
                graph
                added_cluster
            |>ignore
                    
            graph,clustering_stage
        )
            (
                (Graph.empty "Dendrogram"),
                initial_stage
            )
        |>fst