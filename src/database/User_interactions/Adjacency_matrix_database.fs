

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



type Matrix_timeframe = {
    when_jobs_prepared: DateTime
    first_completion: DateTime
    last_completion: DateTime
}

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
            on conflict
            do nothing
            """,
            members
            |>Seq.map(fun handle ->
                {|
                    account = handle
                    title = string matrix_title
                |}    
            )
        )|> ignore
        
    let transfer_matrix_members_across_databases () =
        let local_db = Local_database.open_connection()
        let central_db = Central_database.open_connection()
        
        read_members_of_matrix
            central_db
            Adjacency_matrix.Twitter_network
        |>write_members_of_matrix
            local_db
            Adjacency_matrix.Twitter_network
            
    
        
        
    let check_correctness_of_timeframe
        (timeframe: Matrix_timeframe)
        =
        if not (
            timeframe.when_jobs_prepared < timeframe.first_completion
            &&
            timeframe.first_completion < timeframe.last_completion
        ) then
            $"bad matrix timeframe: {timeframe}"
            |>DataMisalignedException
            |>raise 
        timeframe
    let check_correctness_of_datetime_sequence
        (timeframes: Matrix_timeframe list)
        =
        timeframes
        |>List.skip 1
        |>List.fold(fun previous_timeframe this_timeframe ->
            if not (
                previous_timeframe.when_jobs_prepared < this_timeframe.when_jobs_prepared
            ) then
                $"bad sequence of matrix timeframes: previous_timeframe={previous_timeframe}, this_timeframe={this_timeframe}"
                |>DataMisalignedException
                |>raise
            this_timeframe
            |>check_correctness_of_timeframe
        )
            (
             timeframes
             |>List.head
             |>check_correctness_of_timeframe
            )
        |>ignore
        timeframes
        
    let read_timeframes_of_scraping_jobs
        (database:NpgsqlConnection)
        (matrix_members: User_handle Set)
        =
        database.Query<User_to_scrape_row>(
            $"select *
            from {user_to_scrape}
            where 
                not {user_to_scrape.status} = 'Free'
                and not {user_to_scrape.status} = 'Taken'
            "
        )
        |>Seq.filter(fun (job:User_to_scrape_row) -> Set.contains job.account matrix_members)
        |>Seq.groupBy(fun (job:User_to_scrape_row) -> job.created_at)
        |>Seq.filter(fun (created_at, jobs_maybe_without_needed_users) ->
            jobs_maybe_without_needed_users
            |>Seq.map _.account
            |>Set.ofSeq
            |>Set.intersect matrix_members
            |>Set.count
            |>(=) matrix_members.Count
        )
        |>Seq.sortBy fst
        |>Seq.map(fun (created_at, jobs_with_all_needed_users) ->
            let sorted_jobs = 
                jobs_with_all_needed_users
                |>Seq.map(_.when_completed)
                |>Seq.sort
            {
                Matrix_timeframe.when_jobs_prepared =
                    created_at
                first_completion =
                    sorted_jobs
                    |>Seq.head
                last_completion =
                    sorted_jobs
                    |>Seq.last
            }
        )
        |>Seq.toList
        |>check_correctness_of_datetime_sequence
    
    let read_timeframes_of_matrix
        central_database
        local_database
        matrix_title
        =
        let matrix_members =
            read_members_of_matrix
                local_database
                matrix_title
            |>Set.ofSeq
            
        read_timeframes_of_scraping_jobs
            central_database
            matrix_members
    
    let read_last_timeframe_of_matrix
        central_database
        local_database
        matrix_title
        =
        read_timeframes_of_matrix
            central_database
            local_database
            matrix_title
        |>List.last
        |> _.last_completion
    
    let ``try read_timeframes_of_matrix``()=
        let frames =
            read_timeframes_of_matrix
                (Central_database.resiliently_open_connection())
                (Local_database.open_connection())
                Longevity_members
        ()