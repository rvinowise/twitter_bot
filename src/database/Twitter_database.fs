namespace rvinowise.twitter

open System
open System.Data
open Dapper
open Npgsql
open rvinowise.twitter






type Adjacency_matrix =
    |Twitter_network
    |Longevity_members
    |Philosophy_members
    |Transhumanist_members
    |AI_members
    |Antiislam_members
    // with
    // override this.ToString() =
    //     match this with
    //     |Twitter_network -> "Twitter network"
    //     |Longevity_members -> "Longevity members"
    //     |Philosophy_members -> "Philosophy members"
    //     |Transhumanist_members -> "Transhumanist_members"
    //     |AI_members -> "AI_members"


module Adjacency_matrix =
    
    let matrix_to_name =
        [
            Twitter_network,"Twitter network"
            Longevity_members,"Longevity members"
            Philosophy_members,"Philosophy members"
            Transhumanist_members,"Transhumanist_members"
            AI_members,"AI_members"
            Antiislam_members,"Antiislam_members"
        ]
        |>Map.ofSeq
    
    let name (matrix: Adjacency_matrix) =
        matrix_to_name
        |>Map.find matrix
    
    let try_matrix_from_string name =
        matrix_to_name
        |>Map.tryFindKey (fun _ title -> title=name)
        

type Attention_type =
    |Likes
    |Replies
    |Reposts
    with
    override this.ToString() =
        match this with
        |Likes -> "Likes"
        |Replies -> "Replies"
        |Reposts -> "Reposts"
      
      
      