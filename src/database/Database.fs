namespace rvinowise.twitter

open System
open System.Data
open Dapper
open Npgsql
open rvinowise.twitter

module Database =
    
    
    let set_timezone_of_this_machine
        (connection:NpgsqlConnection)
        =
        let utc_offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).Negate()
        connection.Query<DateTime>(
            $"""set timezone to '{utc_offset}'"""
        )|>ignore
    
    let open_connection (connection_string:string) =
        let data_source = NpgsqlDataSource.Create(connection_string)
        let db_connection = data_source.OpenConnection()
        
        //set_timezone_of_this_machine db_connection
        
        db_connection
        
    let try_close_connection (db_connection:NpgsqlConnection) =
        try
            db_connection.Close()
        with
        | :? NpgsqlException as exc ->
            $"failed to close the database connection: {exc.Message}"
            |>Log.error|>ignore
