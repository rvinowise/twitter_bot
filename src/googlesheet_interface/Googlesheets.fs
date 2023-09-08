namespace rvinowise.twitter

open System
open System.Collections.Generic
open System.IO
open Google.Apis.Auth.OAuth2
open Google.Apis.Services
open Google.Apis.Sheets.v4.Data
open Xunit

open Google.Apis.Sheets.v4
open rvinowise.twitter



module Googlesheets =
    
    let create_googlesheet_service () =
        try
            let stream =
                new FileStream(
                    "google_api_secret.json", 
                    FileMode.Open, FileAccess.Read
                )
            let credential = GoogleCredential.FromStream(stream).CreateScoped([SheetsService.Scope.Spreadsheets])
            new SheetsService(
                BaseClientService.Initializer(
                    HttpClientInitializer = credential, ApplicationName = "web-bot" 
                )
            )
        with
        | exc ->
            $"""can't read file with google credential:
               {exc}
            """
            |>Log.important
            reraise ()
    
    
    let google_column_as_array
        (google_column:IList<IList<obj>>)
        =
        google_column
        |>Seq.map(fun row_values->
            row_values
            |>Seq.tryHead
            |>Option.defaultValue null
        )|>Array.ofSeq
        
    let google_range_as_arrays
        (lists_range:IList<IList<obj>>)
        =
        lists_range
        |>Seq.map(fun row_values->
            row_values
            |>Array.ofSeq
        )|>Array.ofSeq      