namespace rvinowise.twitter

open System
open System.Collections.Generic
open System.IO
open System.Threading.Tasks
open Google.Apis.Auth.OAuth2
open Google.Apis.Services
open Google.Apis.Sheets.v4.Data
open Xunit

open Google.Apis.Sheets.v4
open rvinowise.twitter



module Export_scores_to_googlesheet =
        
        
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
                Googlesheets.create_googlesheet_service(), deleteRequest, sheet.doc_id
            );
        deletion.Execute();
    
    
    let sheet_row_clean = [
        for empty in 0 .. 50 -> ""
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
        try
            Googlesheets.create_googlesheet_service().Spreadsheets.Values.BatchUpdate(
                requestBody, sheet.doc_id
            ).Execute()|>ignore
            ()
        with
        | :? TaskCanceledException as exc ->
            Log.error $"""couldn't write to googlesheet: {exc.Message}"""|>ignore
            ()
    
    let lists_to_google_obj
        (lists: string list list)
        =
        lists
        |>List.map (fun inner_list ->
            inner_list
            |>List.map (fun value -> value :> obj)
            |>List :> IList<_>
        )
        |>List :> IList<_>
    
    
    [<Fact(Skip="manual")>]
    let ``try input_into_sheet``()=
        input_into_sheet
            {
                Google_spreadsheet.doc_id="1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU"
                page_id=293721268
                page_name="test"
            }
            ([
                ["test";"123"]
            ]|>lists_to_google_obj)
    
    
        
    let clean_sheet
        (sheet: Google_spreadsheet)
        =
        let clean_row = 
            sheet_row_clean
        
        let clean_sheet =
            [ for _ in 0 .. 500 -> clean_row]
            |>lists_to_google_obj
        
        input_into_sheet sheet clean_sheet

        
    [<Fact>]//(Skip="manual")
    let ``try clean_sheet``() =
        clean_sheet
            Settings.Google_sheets.followers_amount
            
    let sheet_row_of_total_score
        =
        [
            "" :>obj
            "" :>obj
            "Total" :>obj
        ]@
        [
            for letter in 'D' .. 'Z' ->
                $"=SUM({letter}3:{letter}1000)" :>obj
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
            (User_handle.url_from_handle handle)
            (handle|>User_handle.value)
    
    let sheet_row_of_competitor
        (place:int)
        (competitor:Twitter_user)
        (score_history: int list)
        =
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
    
    let get_history_of_amounts_for_users
        (social_database: Social_competition_database)
        (amount_of_what: social_database.Table_with_amounts)
        days_from_today
        =
        let last_datetime = social_database.read_last_followers_amount_time()
        let competitors =
            social_database.read_last_competitors
                (last_datetime - TimeSpan.FromHours(Settings.Competitors.include_from_past))
            |>Set.ofSeq

        let last_amounts =
            social_database.read_last_amounts_closest_to_moment_for_users
                    amount_of_what
                    last_datetime
                    competitors
        
        let map_user_to_amounts =
            last_amounts
            |>Map.map (fun _ amount -> [amount])
        
        last_datetime
        ,
        last_amounts
        |>Map.toSeq
        |>Seq.sortByDescending snd
        |>Seq.mapi(fun place (user,score) ->
            (place+1),user,score          
        )
        ,
        [
            for day_from_today in 1 .. days_from_today ->
                let historical_day = last_datetime.AddDays(-day_from_today)
                social_database.read_amounts_closest_to_the_end_of_day
                    amount_of_what
                    historical_day
        ]
        |>List.fold(fun map_user_to_full_score_history scores_on_day ->
            
            let user_to_score_on_that_day =
                scores_on_day
                |>Seq.map(fun row -> User_handle row.user_handle,row.amount)
                |>Map.ofSeq
            
            map_user_to_full_score_history
            |>Map.map(fun user score_history ->
                let score_on_that_day =
                    user_to_score_on_that_day
                    |>Map.tryFind user
                    |>Option.defaultValue 0
                score_on_that_day::score_history
            )
        )   
            map_user_to_amounts
        |>Map.map (fun _ score_history ->
            score_history
            |>List.rev    
        )
    
    
 
    
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
    
    [<Fact(Skip="manual")>]
    let ``try date as text``()=
        let datetime = DateTime.Now
        let str = $"last day of scores is {datetime}"
        ()
            
    let input_history_of_amounts_to_sheet
        social_database
        (table_with_amounts)
        (sheet:Google_spreadsheet)
        =
        let days_in_past = 20
        
        let last_datetime,current_scores,score_hisory =
            get_history_of_amounts_for_users
                social_database
                table_with_amounts
                days_in_past
        
        Log.info $"inputting fields into google spread sheet for {days_in_past} days in the past since {last_datetime}..."
        let constant_data =
            [
                sheet_row_of_total_score;
                (sheet_row_of_header last_datetime days_in_past)
            ]
        let user_handles_to_names = social_database.read_user_names_from_handles()
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
        Log.info $"inputting into google sheet: {sheet}"
            
        input_into_sheet sheet all_rows
    
    
    let update_googlesheet
        social_database
        table_with_amounts
        (sheet:Google_spreadsheet)
        =
        Log.info $"updating google sheet, page '{sheet.page_name}'" 
        clean_sheet sheet
        input_history_of_amounts_to_sheet
            social_database
            table_with_amounts
            sheet
    
    let update_googlesheets social_database_connection =
        update_googlesheet
            social_database_connection
            social_database.Table_with_amounts.Followers
            Settings.Google_sheets.followers_amount
        update_googlesheet
            social_database_connection
            social_database.Table_with_amounts.Posts
            Settings.Google_sheets.posts_amount
    
    [<Fact>]//(Skip="manual")
    let ``try update_googlesheets``() =
        new Social_competition_database()
        |>update_googlesheets
    
   
    [<Fact>]//(Skip="manual")
    let ``try input_posts_amount_to_sheet``() =
        update_googlesheet
            (new Social_competition_database())
            social_database.Table_with_amounts.Posts
            {
                Google_spreadsheet.doc_id = "1E_4BeKi0gOkaqsDkuY_0DeHssEcbLOBBzYmdneQo5Uw"
                page_id=1445243022
                page_name="posts_amount"
            }
