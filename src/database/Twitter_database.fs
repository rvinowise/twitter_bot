namespace rvinowise.twitter

open System
open System.Data
open Dapper
open Npgsql
open rvinowise.twitter
open rvinowise.twitter.database_schema.tables




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
    |Longevity_members
    |Twitter_network
    with
    override this.ToString() =
        match this with
        |Longevity_members -> "Longevity members"
        |Twitter_network -> "Twitter network"

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
      
      
      
module Twitter_database =
    let sql_account_should_be_inside_matrix
        account
        =
        $"""
        --the account should be part of the desired matrix
        exists ( 
            select ''
            from {account_of_matrix}
            where 
                --find the matrix by title
                {account_of_matrix}.{account_of_matrix.title} = @matrix_title
                
                --the target of attention should be part of the matrix
                and {account_of_matrix}.{account_of_matrix.account} = {account}
        )
        """