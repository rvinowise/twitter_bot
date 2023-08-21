﻿namespace rvinowise.twitter

open System
open System.Collections.Generic
open System.Configuration
open System.IO
open System.Text.RegularExpressions
open Google.Apis.Auth.OAuth2
open Google.Apis.Services
open Google.Apis.Sheets.v4.Data
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.classic
open Dapper
open FSharp.Data

open Google.Apis.Sheets.v4
open rvinowise.twitter



module Export_scores_to_googlesheet =
    
    let stream = new FileStream(
        "google_api_secret.json", 
        FileMode.Open, FileAccess.Read
    ) 
        
    let credential = GoogleCredential.FromStream(stream).CreateScoped([SheetsService.Scope.Spreadsheets])
    let googlesheet_service = new SheetsService(
        BaseClientService.Initializer(
            HttpClientInitializer = credential, ApplicationName = "web-bot" 
        )
    )
    
    
    let delete_rows
        (sheet: Google_spreadsheet)
        =
        let request = Request (
            DeleteDimension = DeleteDimensionRequest (
                Range = DimensionRange (
                    SheetId = sheet.page_id,
                    Dimension = "ROWS",
                    StartIndex = 0,
                    EndIndex = 1
                )
            )
        )
            
        let deleteRequest = BatchUpdateSpreadsheetRequest (
            Requests = List<Request> [request]
        )

        let deletion =
            SpreadsheetsResource.BatchUpdateRequest(
                googlesheet_service, deleteRequest, sheet.doc_id
            );
        deletion.Execute();
    
    
    let sheet_row_clean = [
        "";"";"";"";"";""
    ]
        
    let input_into_sheet
        sheet
        rows
        =
        let range = $"{sheet.page_name}!A1:ZZ";
        let valueInputOption = "USER_ENTERED";
        let dataValueRange = ValueRange(
            Range = range,
            Values = rows
        )

        let requestBody = BatchUpdateValuesRequest(
            ValueInputOption = valueInputOption,
            Data = List<ValueRange>[dataValueRange]
        )

        googlesheet_service.Spreadsheets.Values.BatchUpdate(
            requestBody, sheet.doc_id
        ).Execute()
    
    let clean_sheet
        (sheet: Google_spreadsheet)
        =
        let clean_row = 
            sheet_row_clean
            |>List.map (fun value -> value :> obj)
            |>List :> IList<obj>
        
        let clean_sheet =
            [ for _ in 0 .. 40000 -> clean_row]
            |>List
        
        input_into_sheet sheet clean_sheet

        
    [<Fact>]
    let ``try clean_sheet``() =
        clean_sheet
            Settings.score_table
            
    let sheet_row_of_total_score
        =
        [
            "" :>obj
            "" :>obj
            "Total" :>obj
        ]@
        [
            for letter in 'D' .. 'Z' ->
                $"=SUM({letter}3:{letter}4000)" :>obj
        ]|>List :> IList<obj>
        
    
    let sheet_row_of_header
        (now: DateTime)
        (days_from_today: int)
        =
        [
            "Place" :>obj
            "Handle" :>obj
            "Name" :>obj
            "Growth" :>obj
            now.ToString("yyyy-MM-dd HH:mm")
        ]@[
            for day_from_today in 1 .. days_from_today ->
                (now.Date.AddDays(-day_from_today)).ToString("yyyy-MM-dd")
        ]
        |>List :> IList<obj>
    
    
    let hyperlink_to_twitter_user handle =
        sprintf
            """=HYPERLINK("%s", "@%s")"""
            (Twitter_user.url_from_handle handle)
            (handle|>User_handle.value)
    
    
    let get_history_of_user_scores
        days_from_today
        =
        let last_datetime,last_scores = Scores_database.read_last_scores ()
        let map_user_to_score =
            last_scores
            |>Seq.map (fun (user,score) -> user,[score])
            |>Map.ofSeq//<User_handle,int list>
        
        last_datetime
        ,
        last_scores
        |>Seq.sortByDescending snd
        |>Seq.mapi(fun place (user,score) ->
            (place+1),user,score          
        )
        ,
        [
            for day_from_today in 1 .. days_from_today ->
                let historical_day = last_datetime.Date.AddDays(-day_from_today).Date //todo take date or no?
                Scores_database.read_last_scores_on_day historical_day
        ]
        |>List.fold(fun map_user_to_full_score_history scores_on_day ->
            
            let user_to_score_on_that_day =
                scores_on_day
                |>Map.ofSeq
            
            map_user_to_full_score_history
            |>Map.map(fun user score_history ->
                let score_on_that_day =
                    user_to_score_on_that_day
                    |>Map.tryFind user
                    |>Option.defaultValue 0
                score_on_that_day::score_history
            )
            
//            scores_on_day
//            |>Seq.fold (fun map_user_to_full_score_history (user,score) ->
//                let user_score_history =
//                    map_user_to_full_score_history
//                    |>Map.tryFind user
//                    |>Option.defaultValue []
//                
//                map_user_to_full_score_history
//                |>Map.add user (score::user_score_history)
//            )
//                map_user_to_full_score_history
        )   
            map_user_to_score
        |>Map.map (fun _ score_history ->
            score_history
            |>List.rev    
        )
    
    let get_current_scores =
        Scores_database.read_last_scores()
            
            
    let sheet_row_of_competitor
        (place:int)
        (competitor:Twitter_user)
        (score_history: int list)
        =
//        let growth_today =
//            (score_history
//            |>List.head)
//            -
//            (score_history
//            |>List.tryItem(1)
//            |>Option.defaultValue 0)
        let user_row_index = place+2
        let current_growth_formula = $"=E{user_row_index}-F{user_row_index}"
        [
            place :>obj
            (hyperlink_to_twitter_user competitor.handle) :>obj
            competitor.name :>obj
            current_growth_formula :>obj
        ]@(
            score_history
            |>List.map (fun score -> score :> obj)
        )
        
        |>List :> IList<obj>
    
    let sheet_row_of_competitor_from_total_map
        (competitor:Twitter_user)
        place
        (users_to_history: Map<User_handle, int list>)
        =
        users_to_history
        |>Map.find competitor.handle 
        |>sheet_row_of_competitor
            place
            competitor
        
    
    let twitter_user_from_handle
        handle
        (user_handles_to_names: Map<User_handle, string>)
        =
        {
            Twitter_user.handle = handle
            name=
                user_handles_to_names
                |>Map.tryFind handle
                |>Option.defaultValue ""
        }
            
    let input_scores_to_sheet
        (sheet:Google_spreadsheet)
        =
        let days_in_past = 100
        
        let last_datetime,current_scores,score_hisory =
            get_history_of_user_scores days_in_past
        
        let constant_data =
            [
                sheet_row_of_total_score;
                (sheet_row_of_header last_datetime days_in_past)
            ]
        let user_handles_to_names = Scores_database.read_user_names_from_handles()
        let user_scores=
            current_scores
            |>Seq.map(fun (place,user_handle,_) ->
                sheet_row_of_competitor_from_total_map
                    (twitter_user_from_handle user_handle user_handles_to_names)
                    place
                    score_hisory
            )|>List.ofSeq// :> IList<obj>
        
        let all_rows =
            constant_data
            @user_scores
            |>List
            
        input_into_sheet sheet all_rows
    
    
    [<Fact>]
    let ``try input_scores_to_sheet``() =
        input_scores_to_sheet
            Settings.score_table
    
    let update_googlesheet_with_last_scores
        sheet
        =
        delete_rows sheet|>ignore
        input_scores_to_sheet sheet
    
    
    let score_changes = [
        {Twitter_user.name="name1"; handle=User_handle "handle1"}, 10,0;
        {Twitter_user.name="name2"; handle=User_handle "handle2"}, 21,1;
        {Twitter_user.name="name3"; handle=User_handle "handle3"}, 32,2;
        {Twitter_user.name="name4"; handle=User_handle "handle4"}, 43,3;
    ]
    
    [<Fact>]
    let ``try update_googlesheet_with_last_scores``() =
        update_googlesheet_with_last_scores
            Settings.score_table
