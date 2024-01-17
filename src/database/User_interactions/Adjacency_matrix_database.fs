

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




module Adjacency_matrix_database =
    
    let read_members_of_matrix_sorted_by_received_attention
        (database:NpgsqlConnection)
        matrix_title
        =
        database.Query<User_handle>(
            $"""
            select *
            from 
                {account_of_matrix} as account_of_matrix
            inner join (
                    select 
                        {user_attention.target},
                        sum({user_attention.amount}) as total_received_attention
                    from 
                        {user_attention}
                    group by 
                        {user_attention.target}
                ) as received_attention
                on 
                    received_attention.{user_attention.target} = account_of_matrix.{account_of_matrix.account}
            where 
                account_of_matrix.{account_of_matrix.title} = @matrix_title
            order by 
                received_attention.total_received_attention desc
            """,
            {|
                matrix_title=matrix_title
            |}
        )
        |>List.ofSeq
    
    let read_members_of_matrix
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
            read_members_of_matrix
                (Central_database.open_connection())
                "Longevity members"
                
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