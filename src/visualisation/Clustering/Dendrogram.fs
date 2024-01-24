namespace rvinowise.twitter


open Xunit
open FsUnit

open Plotly.NET
open rvinowise.twitter.database_schema
open rvinowise.twitter.database_schema.tables

open FSharp.Stats
open FSharp.Stats.ML.Unsupervised

module Dendrogram =
    
    let find_clusters
        attention
        =
        HierarchicalClustering.generate
            DistanceMetrics.euclideanNaNSquared
            HierarchicalClustering.Linker.centroidLwLinker
             