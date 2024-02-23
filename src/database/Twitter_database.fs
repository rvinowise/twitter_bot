namespace rvinowise.twitter

open System
open System.Data
open Dapper
open Npgsql
open rvinowise.twitter




type Harvesting_timeline_result =
    |Success of int
    |Insufficient of int
    |Hidden_timeline of Timeline_hiding_reason 
    |Exception of Exception 


module Harvesting_timeline_result =
    let db_value (result: Harvesting_timeline_result) =
        match result with
        |Success amount-> "Success"
        |Insufficient amount-> "Insufficient"
        |Hidden_timeline reason -> Timeline_hiding_reason.db_value reason
        |Exception exc -> $"Exception: {exc.Message}"

    let articles_amount (result: Harvesting_timeline_result) =
        match result with
        |Success amount
        |Insufficient amount ->
            amount
        |Hidden_timeline _
        |Exception _ ->
            0
        
type Scraping_user_status =
    |Free
    |Taken
    |Completed of Harvesting_timeline_result
    
    
module Scraping_user_status =
    let db_value (status: Scraping_user_status) =
        match status with
        |Free -> "Free"
        |Taken -> "Taken"
        |Completed result -> Harvesting_timeline_result.db_value result

    let from_db_value value =
        match value with
        |"Free" -> Scraping_user_status.Free
        |"Taken" -> Scraping_user_status.Taken
        |unknown_type -> raise (TypeAccessException $"unknown type of Scraping_user_status: {unknown_type}")


type Adjacency_matrix =
    |Twitter_network
    |Longevity_members
    |Philosophy_members
    |Transhumanist_members
    |AI_members
    with
    override this.ToString() =
        match this with
        |Twitter_network -> "Twitter network"
        |Longevity_members -> "Longevity members"
        |Philosophy_members -> "Philosophy members"
        |Transhumanist_members -> "Transhumanist_members"
        |AI_members -> "AI_members"

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
      
      
      