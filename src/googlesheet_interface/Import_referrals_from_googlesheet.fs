namespace rvinowise.twitter

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open Google.Apis.Auth.OAuth2
open Google.Apis.Services
open Google.Apis.Sheets.v4.Data
open Xunit

open Google.Apis.Sheets.v4
open rvinowise.twitter



module Import_referrals_from_googlesheet =
    
    
    let read_recruitings_datetimes
        (googlesheet_service: Google.Apis.Sheets.v4.SheetsService)
        sheet
        =
        let datetime_column = "C"
        let recruiting_datetimes =
            googlesheet_service.Spreadsheets.Values.Get(
                sheet.doc_id,
                $"{sheet.page_name}!{datetime_column}2:{datetime_column}4000"
            ).Execute().Values
            |>Googlesheets.google_column_as_array
        
        
        recruiting_datetimes
        |>Array.map(fun obj -> obj :?> string)
        |>Array.map(fun string_datetime ->
            let mutable datetime_of_scores = DateTime.MinValue 
            let is_date_parsed =
                DateTime.TryParseExact(
                    string_datetime,
                    "yyyy-MM-dd H:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    &datetime_of_scores
                )
            datetime_of_scores
        )
    
    module Parse_social_user =
        let remove_url (text:string) =
            text.Substring(text.LastIndexOf(@"/")+1)
        
        let remove_impossible_symbols (text:string) =
            text.Replace("@","")
        
        let user_from_raw_text user =
            user
            |>remove_url
            |>remove_impossible_symbols
    
    
    let import_recruitings_starting_from_index
        (googlesheet_service: Google.Apis.Sheets.v4.SheetsService)
        (social_database: Social_competition_database)
        (sheet:Google_spreadsheet)
        (recruiting_datetimes:DateTime array)
        starting_index
        =
        Log.info $"import_recruitings_from_index {starting_index}"
        let recruitings =
            let starting_row =
                starting_index+2 //skip header and use 1-st based counting
            googlesheet_service.Spreadsheets.Values.Get(
                sheet.doc_id,
                $"""{sheet.page_name}!D{starting_row}:F4000"""
            ).Execute().Values
            |>Googlesheets.google_range_as_arrays
            |>Seq.mapi(fun column_i row ->
                (
                    recruiting_datetimes[starting_index+column_i],
                    //link_referral
                    Array.tryItem 0 row
                    |>Option.defaultValue "" :?>string
                    |>Parse_social_user.user_from_raw_text
                    |>User_handle,
                    //recruit
                    Array.tryItem 1 row
                    |>Option.defaultValue "" :?>string 
                    |>Parse_social_user.user_from_raw_text,
                    //claimed_referral
                    Array.tryItem 2 row
                    |>Option.defaultValue "" :?>string 
                    |>Parse_social_user.user_from_raw_text
                )
            )
        social_database.write_recruiting_referrals
            recruitings

    let import_referrals
        (googlesheet_service: Google.Apis.Sheets.v4.SheetsService)
        (social_database: Social_competition_database)
        (sheet:Google_spreadsheet)
        =
        Log.info $"start import_referrals from {sheet}"
        let recruiting_datetimes =
            read_recruitings_datetimes googlesheet_service sheet
            
        let new_recruitings_starting_index =
            let last_db_recruiting_date =
                social_database.read_last_recruiting_datetime()
            recruiting_datetimes
            |>Array.tryFindIndex(fun datetime->
                datetime > last_db_recruiting_date
            )
        
        match new_recruitings_starting_index with
        |Some index ->
            import_recruitings_starting_from_index
                googlesheet_service
                social_database
                sheet
                recruiting_datetimes
                index
        |None ->
            Log.info "no new recruitings by referrals"
            ()
        
        
    
    [<Fact>]//(Skip="manual")
    let ``try import_referrals_from_googlesheet``() =
        let test =
            import_referrals
                (Googlesheets.create_googlesheet_service())
                (new Social_competition_database())
                {
                    Google_spreadsheet.doc_id = "137ExyTBgr-IL0TlxIv-V4EvWee_BDbwyh5U0M06IwsU"
                    page_id=0
                    page_name="Sheet1"
                }
        ()