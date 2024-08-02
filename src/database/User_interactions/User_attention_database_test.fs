

namespace rvinowise.twitter.test

open System
open Dapper
open Faithlife.Utility.Dapper
open NUnit.Framework.Internal
open Npgsql

open Xunit
open FsUnit

open rvinowise.html_parsing
open rvinowise.twitter
open rvinowise.twitter.database_schema
open rvinowise.twitter.database_schema.tables



module User_attention_database_test =
    
    
    let ``cached attention gives the same result as newly calculated attention``
        database
        matrix_members
        before_datetime
        obtain_attention
        =
        User_attention_database.delete_all_cached_attention
            database
        
        let calculated_attention =
            obtain_attention
                Attention_type.Likes
                database
                before_datetime
                matrix_members
        
        (Seq.length calculated_attention)
        |>should equal (Seq.length matrix_members)
                
        if Seq.isEmpty calculated_attention then
             "the testing database didn't have enough user attention to test it"
             |>InvalidTestFixtureException
             |>raise
        
        let cached_attention =
            obtain_attention
                Attention_type.Likes
                database
                before_datetime
                matrix_members
                
        cached_attention
        |>should equal calculated_attention
    
    [<Fact(Skip="integration")>]
    let ``cached user-to-user attention gives the same result as newly calculated attention``()=
        let database = Testing_database.open_connection()
        let before_datetime = DateTime.UtcNow
        
        let matrix_members =
            Adjacency_matrix_database.read_members_of_matrix
                database
                Adjacency_matrix.Twitter_network
            |>Set.ofList
        
        ``cached attention gives the same result as newly calculated attention``
            database
            matrix_members
            before_datetime
            User_attention_database.read_cached_or_calculated_attention_in_matrix
            
    [<Fact(Skip="integration")>]
    let ``cached total attention gives the same result as newly calculated attention``()=
        let database = Testing_database.open_connection()
        let before_datetime = DateTime.UtcNow
        
        let matrix_members =
            Adjacency_matrix_database.read_members_of_matrix
                database
                Adjacency_matrix.Twitter_network
        
        ``cached attention gives the same result as newly calculated attention``
            database
            matrix_members
            before_datetime
            User_attention_database.read_cached_or_calculated_total_attention
        
    [<Fact(Skip="integration")>]
    let ``cached attention should be created after first calculation``()=
        let database = Testing_database.open_connection()
        User_attention_database.delete_all_cached_attention
            database
        
        let before_datetime = DateTime.UtcNow
        
        let matrix_members =
            Adjacency_matrix_database.read_members_of_matrix
                database
                Adjacency_matrix.Twitter_network
            |>Set.ofList
            
        let cached_attention_before_calculation =
            matrix_members
            |>Seq.map(fun attentor ->
                attentor,
                User_attention_database.read_cached_user_attention
                    Attention_type.Likes
                    database
                    before_datetime
                    attentor
                |>Map.ofSeq
            )|>Map.ofSeq
        
        cached_attention_before_calculation
        |>Map.values
        |>Seq.iter (should be Empty)
        
        let calculated_attention =
            matrix_members
            |>Seq.map(fun attentor ->
                attentor,
                User_attention_database.calculate_and_cache_user_attention
                    Attention_type.Likes
                    database
                    before_datetime
                    attentor
                |>Map.ofSeq
            )|>Map.ofSeq
                
                
        if calculated_attention.IsEmpty then
             "the testing database didn't have enough user attention to test it"
             |>InvalidTestFixtureException
             |>raise
        
        let cached_attention_after_calculation =
            matrix_members
            |>Seq.map(fun attentor ->
                attentor,
                User_attention_database.read_cached_user_attention
                    Attention_type.Likes
                    database
                    before_datetime
                    attentor
                |>Map.ofSeq
            )|>Map.ofSeq
                
        cached_attention_after_calculation
        |>should equal calculated_attention