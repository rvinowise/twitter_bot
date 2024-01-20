

namespace rvinowise.twitter

open System
open Dapper
open Faithlife.Utility.Dapper
open Npgsql
open Xunit
open rvinowise.html_parsing
open rvinowise.twitter
open rvinowise.twitter.Settings.Influencer_competition
open rvinowise.twitter.database_schema
open rvinowise.twitter.database_schema.tables


type Adjacency_matrix =
    |Longevity_members
    |Twitter_network
    with
    override this.ToString() =
        match this with
        |Longevity_members -> "Longevity members"
        |Twitter_network -> "Twitter network"


module Adjacency_matrix_database =
    
    
    let read_members_of_matrix
        (database:NpgsqlConnection)
        (matrix_title: Adjacency_matrix)
        =
        database.Query<User_handle>(
            $"select
                {account_of_matrix.account}
            from
                {account_of_matrix}
            where
                {account_of_matrix.title} = @title",
            {|
                title=(string matrix_title)
            |}
        )
        |>List.ofSeq    
    
    let ``try inner_sql_reading_closest_scraped_date``()=
        let test =
            read_members_of_matrix
                (Central_database.open_connection())
                Adjacency_matrix.Longevity_members
                
        ()
    
    let write_members_of_matrix
        (database:NpgsqlConnection)
        matrix_title
        members
        =
        database.BulkInsert(
            $"""
            insert into {account_of_matrix} (
                {account_of_matrix.account},
                {account_of_matrix.title}
            )
            values (
                @account,
                @title
            )
            ...
            
            """,
            members
            |>List.map(fun handle ->
                {|
                    account = handle
                    title = matrix_title
                |}    
            )
        )|> ignore
        
    let transfer_matrix_members_across_databases () =
        let local_db = Local_database.open_connection()
        let central_db = Central_database.open_connection()
        
        read_members_of_matrix
            central_db
            Adjacency_matrix.Longevity_members
        |>write_members_of_matrix
            local_db
            Adjacency_matrix.Longevity_members
            
    let ``add_matrix_from_sheet``()=
        Googlesheet_reading.read_range
            Parse_google_cell.visible_text_from_cell
            (Googlesheet.create_googlesheet_service())
            {
                Google_spreadsheet.doc_id="1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU"
                page_name = "Followers amount"
            }
            ((2,3),(2,1000))
        |>Table.trim_table String.IsNullOrEmpty
        |>List.collect id
        |>List.map (fun handle ->
            handle
            |>User_handle.trim_potential_atsign
            |>User_handle
        )
        |>write_members_of_matrix
            (Central_database.open_connection())
            "Twitter network"
                
        ()      