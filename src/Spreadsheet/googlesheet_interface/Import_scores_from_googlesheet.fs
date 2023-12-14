namespace rvinowise.twitter

open System
open System.Globalization
open System.IO
open Google.Apis.Auth.OAuth2
open Google.Apis.Services
open Npgsql
open Xunit

open Google.Apis.Sheets.v4
open rvinowise.twitter



module Import_scores_from_googlesheet =
    
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
    
    
    let row_to_user_correspondence
        (sheet:Google_spreadsheet)
        =
        googlesheet_service.Spreadsheets.Values.Get(
            sheet.doc_id,
            $"{sheet.page_name}!B3:B4000"
        ).Execute().Values
        |>Googlesheet.google_column_as_array
        |>Array.map (string>>User_handle.trim_potential_atsign>>User_handle)
    
    
    let import_scores_on_day
        (db_connection: NpgsqlConnection)
        (sheet:Google_spreadsheet)
        (row_to_user_correspondence: User_handle array)
        (column_of_day: char)
        =
        let date_and_scores =
            googlesheet_service.Spreadsheets.Values.Get(
                sheet.doc_id,
                $"{sheet.page_name}!{column_of_day}2:{column_of_day}4000"
            ).Execute().Values
            |>Googlesheet.google_column_as_array
        
        let string_datetime_of_scores =
            date_and_scores
            |>Array.head
            |>string
        let mutable datetime_of_scores = DateTime.MinValue 
        let is_date_parsed =
            DateTime.TryParseExact(
                string_datetime_of_scores,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                &datetime_of_scores
            )
        let last_moment_of_that_date = datetime_of_scores.AddHours(23).AddMinutes(59) 
            
        let scores_on_day =
            date_and_scores
            |>Array.tail
            |>Array.map (fun obj-> Convert.ToInt32(obj))
        
        let users_scores=
            row_to_user_correspondence
            |>Array.mapi (fun index user ->
                let score =
                    scores_on_day
                    |>Array.tryItem index
                    |>Option.defaultValue 0
                user,score
            )
        
        Social_activity_database.write_followers_amounts_to_db
            db_connection
            last_moment_of_that_date
            users_scores
            
    let import_scores_on_days
        (sheet:Google_spreadsheet)
        columns_of_days
        =
        use db_connection = Twitter_database.open_connection()
        let sorted_users = row_to_user_correspondence sheet
        columns_of_days
        |>List.iter (
            import_scores_on_day db_connection sheet sorted_users
        )
    
    let import_scores_between_two_date_columns
        (sheet:Google_spreadsheet)
        start_column
        end_column
        =
        import_scores_on_days
            sheet
            [start_column..end_column]
    
    [<Fact(Skip="manual")>]//
    let ``try import_scores_from_googlesheet``() =
        import_scores_on_days
            {
                Google_spreadsheet.doc_id = "1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU"
                page_id=2000980006
                page_name="import"
            }
            ['F' .. 'N']
