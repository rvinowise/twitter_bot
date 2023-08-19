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

module Database =
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
        
    
    let test_scores = [
        {Twitter_user.name="Batin1";handle=User_handle "Batin";},14;
        {Twitter_user.name="Batin2";handle=User_handle "Batin2";},35;
        {Twitter_user.name="Rybin1";handle=User_handle "Rybin";},67;
    ]
    
    [<Fact>]
    let ``write scores to db``()=
        test_scores
        |>write_scores_to_db DateTime.Now
        
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
    let read_scores_for_time datetime =
        open_db_connection.Query<Score_line>(
            @"select * from score_line
            where datetime = @datetime",
            {|datetime=datetime|}
        )
        |>Seq.map (fun score_line->
            User_handle score_line.user_handle, score_line.score
        )
        
    let read_last_scores () =
        let last_time = read_last_score_time()
        let score_lines = read_scores_for_time last_time
        
        last_time, score_lines