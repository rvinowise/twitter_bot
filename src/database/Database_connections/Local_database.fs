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