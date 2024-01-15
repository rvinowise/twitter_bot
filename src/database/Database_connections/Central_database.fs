namespace rvinowise.twitter

open System
open System.Data
open Dapper
open Npgsql
open rvinowise.twitter


module Central_database =
        
    let open_connection () =
        let db_connection = Database.open_connection Settings.central_database
        
        Twitter_database_types.set_twitter_type_handlers()
        
        db_connection
        
        
    let rec resiliently_open_connection () =
        let db_connection =
            try
                open_connection ()
                |>Some
            with
            | :? NpgsqlException as exc ->
                $"""failed opening the connection with the central database: {exc.Message}, trying again."""
                |>Log.error|>ignore
                None
       
        match db_connection with
        |Some db_connection ->
            db_connection
        |None ->
            resiliently_open_connection ()