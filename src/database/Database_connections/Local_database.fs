namespace rvinowise.twitter

open System
open System.Data
open Dapper
open Npgsql
open rvinowise.twitter


module Local_database =
        
    let open_connection () =
        let db_connection = Database.open_connection Settings.local_database
        
        Twitter_database_type_mappers.set_twitter_type_handlers()
        
        db_connection
        
module Testing_database =
        
    let open_connection () =
        let db_connection =
            Database.open_connection
                "Host=localhost:5432;Username=postgres;Password=' ';Database=twitter;Timeout=300;CommandTimeout=300"
        
        Twitter_database_type_mappers.set_twitter_type_handlers()
        
        db_connection