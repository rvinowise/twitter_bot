namespace rvinowise.twitter

open System
open System.Configuration
open System.IO
open System.Text.RegularExpressions
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.classic
open Dapper
open FSharp.Data

module Export_scores_to_csv =
    
    type Score_changes =
        CsvProvider<
            "score_changes.csv",
            Separators = ";",
            Schema="place=int,handle->User_handle,name=string,growth=int,end_score=int,start_score=int",
            HasHeaders=true
            >
    
    
    let a_score_change_line_as_scv_row
        (score_change_line)
        =
        let (place,user,new_score, growth) = score_change_line
        let previous_score = new_score - growth
        Score_changes.Row(
            place,
            user.handle|>User_handle.value,
            user.name,
            growth,
            new_score,
            previous_score
        )
    
    let total_score_as_csv_row
        score growth
        =
        Score_changes.Row(
            0,
            "",
            "Total",
            growth,
            score,
            score-growth
        )
    
    
        
    let export_score_changes
        (start_time: DateTime)
        (end_time: DateTime)
        (scores: (int*Twitter_user*int*int) list )
        =
        
        let csv_score_lines = 
            scores
            |>List.map a_score_change_line_as_scv_row
            
        let csv_scores_with_total = new Score_changes(
            (scores
            |>List.map(fun (_,_,score,growth) -> score,growth)
            |>Format_score_for_twitter.calculate_total_score_of_team
            ||>total_score_as_csv_row)
            ::csv_score_lines
        )
        
        
        let csv_scores_with_total = csv_scores_with_total.SaveToString()
        
        let end_time_string = end_time.ToString("yyyy-MM-dd HH:mm")
        let start_time_string = start_time.ToString("yyyy-MM-dd HH:mm")
        let csv_scores = Regex("end_score").Replace(
            csv_scores_with_total,
            end_time_string,
            1
        )
        let csv_scores = Regex("start_score").Replace(
            csv_scores,
            start_time_string,
            1
        )
        let filename = $"""{end_time.ToString("yyyy-MM-dd HH-mm")} ~ {start_time.ToString("yyyy-MM-dd HH-mm")}.csv"""
        
        let file = System.IO.FileInfo(Settings.scores_export_path+"/"+filename)
        //let file = System.IO.FileInfo("C:/prj/twitter_scraper/output2.csv")
        file.Directory.Create()
        System.IO.File.WriteAllText(file.FullName, csv_scores)
        
    
    let makeup_user_from_handle
        (handle:User_handle)
        =
        {Twitter_user.handle=handle; name="Mr."+(handle|>User_handle.value)}
        
    
    [<Fact>]
    let ``try export_score_changes``()=
        let last_time,last_scores = Scores_database.read_last_scores()
        let previous_time = DateTime.Parse("2023-08-17 22:41:52.616493")
        let previous_scores =
            Scores_database.read_scores_for_datetime previous_time
        
        let last_scores =
            last_scores
            |>Seq.map (fun (handle,score) ->
                makeup_user_from_handle handle
                ,
                score
            )
        Format_score_for_twitter.score_change_from_two_moments
            last_scores previous_scores
        |>Format_score_for_twitter.arrange_by_places_in_competition
        |>export_score_changes
            previous_time last_time