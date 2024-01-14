

namespace rvinowise.twitter

open System
open Dapper
open Faithlife.Utility.Dapper
open Npgsql
open Xunit
open rvinowise.html_parsing
open rvinowise.twitter
open rvinowise.twitter.database
open rvinowise.twitter.database.tables




module Adjacency_matrix =
    
    
    
    let read_sorted_members_of_matrix
        (database:NpgsqlConnection)
        matrix_title
        =
        database.Query<User_handle>(
            $"select
                {account_of_matrix.account}
            from
                {account_of_matrix}
            where
                {account_of_matrix.title} = @title",
            {|
                title=matrix_title
            |}
        )
        |>List.ofSeq    
    
    let ``try inner_sql_reading_closest_scraped_date``()=
        let test =
            read_sorted_members_of_matrix
                (Central_database.open_connection())
                "Longevity members"
                
        ()
    
    