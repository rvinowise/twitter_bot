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




type DissimilarityMetric(
    attention: Map<User_handle, Map<User_handle, float>>
) =
    interface IDissimilarityMetric<User_handle> with

        member this.Calculate(instance1: User_handle, instance2: User_handle) =
            let attention_from1_to2 =
                attention
                |>Map.tryFind instance1
                |>Option.defaultValue Map.empty
                |>Map.tryFind instance2
                |>Option.defaultValue 0.
            let attention_from2_to1 =
                attention
                |>Map.tryFind instance2
                |>Option.defaultValue Map.empty
                |>Map.tryFind instance1
                |>Option.defaultValue 0.
            2. - attention_from1_to2 - attention_from2_to1

module Dendrogram =
    
    
    let image_from_dot_file filename =()
        
    
    let open_image_of_graph (graph_node:Node) =
        let filename = Directory.GetCurrentDirectory() + $"/{graph_node.data.id}"
        let dot_file =
            graph_node.graph
            |>Graph.save_to_file filename
        let root =
            try 
                dot_file
                |>Rubjerg.Graphviz.RootGraph.FromDotFile
            with
                | :? InvalidOperationException as exc ->
                    $"bad .dot file was generated: {exc.Message} {exc.GetType()}"
                    |>Log.error|>ignore
                    reraise()
                
        root.ComputeLayout()       
        root.ToSvgFile($"{filename}.svg")
        root.FreeLayout()
        Process.Start("cmd", $"/c \"{filename}.svg\"") |> ignore
    
    
    let combined_attention
        (attention_types: Map<User_handle,Map<User_handle, float>> seq)
        =
        attention_types
        |>Prepare_attention_matrix.merge_maps(fun attentive_user attention_type1 attention_type2 ->
            Prepare_attention_matrix.merge_maps
                (fun _ value1 value2 -> value1+value2)
                [attention_type1;attention_type2]
        )
    
    let paid_attention_relative_to_matrix
        (relative_attention_everywhere: Map<User_handle, Map<User_handle, float>>)
        (total_paid_attention_in_matrix: Map<User_handle, float>)
        =
        relative_attention_everywhere
        |>Map.map(fun attentive_user targets ->
            let total_attention_inside_matrix =
                total_paid_attention_in_matrix
                |>Map.tryFind attentive_user
                |>Option.defaultValue Double.MinValue
            
            targets
            |>Map.map(fun target amount_relative_to_everything ->
                amount_relative_to_everything / total_attention_inside_matrix
            )
        )
     
    let find_clusters
        database
        handle_to_name
        matrix
        datetime
        =
        let full_attention_data =
            Prepare_attention_matrix.attention_in_matrices
                database
                matrix
                datetime
        //let full_attention_data = [List.head full_attention_data] //test only likes
            
        let all_users =
            Adjacency_matrix_database.read_members_of_matrix
                database
                matrix
            |>HashSet
   
            
        let clustering =
            full_attention_data
            |>List.map(fun attention_type ->
                paid_attention_relative_to_matrix
                    attention_type.relative_attention
                    attention_type.total_paid_attention
            )
            |>combined_attention
            |>DissimilarityMetric
            |>AverageLinkage<User_handle>
            |>AgglomerativeClusteringAlgorithm<User_handle>
            |> _.GetClustering(all_users)
       
      
        clustering
        |>Graph_from_clustering.create_graph
            handle_to_name
            (
                Prepare_attention_matrix.combined_inout_relative_attention
                    full_attention_data
                // full_attention_data.Head.total_received_attention
            )
        |>open_image_of_graph
        
        //clustering.SaveD3DendrogramFile("C:/prj/clusters.json", formatting= Formatting.Indented)
        
        ()
   
    [<Fact>]
    let ``try visualise clusters``() =
        let database = Local_database.open_connection()
        let handle_to_name =
            Twitter_user_database.handle_to_username
                database
                
        find_clusters
            database
            handle_to_name
            Adjacency_matrix.Longevity_members
            (Datetime.parse_datetime "yyyy-MM-dd HH:mm:ss" "2024-01-25 18:36:45")
    
    
     
    let ``show test graph``()=
        let root =
            "Longevity_members"
            |>Graph.empty
            
        let cluster1 =
            root
            |>Graph.provide_vertex "Batin"
            
        cluster1
        |>Graph.with_edge (
            (Graph.provide_vertex "Batin" cluster1)
        )
        |>Graph.with_edge (
            (Graph.provide_vertex "Rybin" root)
        )
            
        
        let cluster2 =
            root
            |>Graph.provide_vertex "Medvedev"
        
        cluster2
        |>Graph.with_edge (
            (Graph.provide_vertex "Medvedev" cluster2)
        )
        |>Graph.with_edge (
            (Graph.provide_vertex "Prapion" root)
        )
        
            
        root
        |>open_image_of_graph