namespace rvinowise.twitter

open System
open System.Configuration
open System.Runtime.InteropServices
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.classic
open Dapper
open Npgsql

module Scores_database =
    open System.Data.Common

    let open_db_connection =
        let connection_string = """Host=127.0.0.1:5432;Username=postgres;Password=" ";Database=web_bot"""
        let dataSource = NpgsqlDataSource.Create(connection_string)
        let db_connection = dataSource.OpenConnection()
        db_connection

    
    let write_score_to_db 
        (datetime: DateTime)
        (user: Twitter_user)
        (score: int)
        =
        open_db_connection.Query<Twitter_user>(
            @"insert into score_line (datetime, user_handle, score)
            values (@datetime, @user_handle, @score)",
            {|
                datetime = datetime
                user_handle = (user.handle|>User_handle.value).ToCharArray(); 
                score = score; 
            |}
        ) |> ignore

    let write_scores_to_db
        datetime
        (users_with_scores: (Twitter_user*int)seq )
        =
        users_with_scores
        |>Seq.iter(fun (user, score)-> 
            write_score_to_db datetime user score
        )
        
    [<CLIMutable>]
    type Db_twitter_user = {
        handle: string
        name: string
    }
    
    
    let write_user_names_to_db
        (users: Twitter_user list)
        =
        users
        |>List.iter(fun user->
            open_db_connection.Query<Db_twitter_user>(
                @"insert into twitter_user (handle, name)
                values (@handle, @name)
                on conflict (handle) do update set name = @name",
                {|
                    handle = (User_handle.value user.handle).ToCharArray()
                    name = user.name
                |}
            ) |> ignore
        )
        
    
    [<CLIMutable>]
    type Score_line = {
        datetime: DateTime
        user_handle: string
        score: int
    }
    
    let read_last_score_time() =
        open_db_connection.Query<DateTime>(
            @"select COALESCE(max(datetime),make_date(1,1,1)) from score_line"
        )|>Seq.head
    let read_scores_for_datetime datetime =
        open_db_connection.Query<Score_line>(
            @"select * from score_line
            where datetime = @datetime",
            {|datetime=datetime|}
        )
        |>Seq.map (fun score_line->
            User_handle score_line.user_handle, score_line.score
        )
    
    let read_last_datetime_on_day (day:DateTime) =
        open_db_connection.Query<DateTime>(
            @"select COALESCE(max(datetime),@day+make_time(23,59,0)) from score_line
            where cast(datetime as date) = @day",
            {|day=day|}
        )|>Seq.head
    
    [<Fact>]
    let ``try read_last_datetime_on_day``()=
        let test = read_last_datetime_on_day (DateTime(2023,8,19))
        ()
    
    let read_last_scores_on_day
        day
        =
        day
        |>read_last_datetime_on_day
        |>read_scores_for_datetime
        
    let read_last_scores () =
        let last_time = read_last_score_time()
        let score_lines = read_scores_for_datetime last_time
        
        last_time, score_lines
        
    let read_user_names_from_handles () =
        open_db_connection.Query<Db_twitter_user>(
            @"select (handle,name) from twitter_user"
        )
        |>Seq.map(fun user->User_handle user.handle, user.name)
        |>Map.ofSeq