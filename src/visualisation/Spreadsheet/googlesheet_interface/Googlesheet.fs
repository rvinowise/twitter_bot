﻿namespace rvinowise.twitter

open System
open System.Collections.Generic
open System.IO
open System.Threading.Tasks
open Google.Apis.Auth.OAuth2
open Google.Apis.Services

open Google.Apis.Sheets.v4
open Google.Apis.Sheets.v4.Data
open Xunit
open rvinowise.twitter

type Google_cell = Google.Apis.Sheets.v4.Data.CellData

module Google_cell =
    let url
        (google_cell: Google_cell)
        =
        google_cell.Hyperlink
        
module Googlesheet =
    
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
            $"""can't read the file with google credentials:
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
        
        
    let sheet_row_clean = [
        for empty in 0 .. 50 -> ""
    ]
    
    
    let input_obj_into_sheet
        sheet
        rows
        =
        let range = $"{sheet.page_name}!A1:ZZ";
        let valueInputOption = "USER_ENTERED";
        let dataValueRange = ValueRange(
            Range = range,
            Values = rows
        )

        let requestBody =
            BatchUpdateValuesRequest(
                ValueInputOption = valueInputOption,
                Data = List<ValueRange>[dataValueRange]
            )
        
        try
            create_googlesheet_service().Spreadsheets.Values.BatchUpdate(
                requestBody, sheet.doc_id
            ).Execute()|>ignore
            ()
        with
        | :? TaskCanceledException as exc ->
            Log.error $"""couldn't write to googlesheet: {exc.Message}"""|>ignore
            ()
    
    let string_lists_to_google_obj
        (lists: string list list)
        =
        lists
        |>List.map (fun inner_list ->
            inner_list
            |>List.map (fun value -> value :> obj)
            |>List :> IList<_>
        )
        |>List :> IList<_>
    
    let obj_lists_to_google_obj
        (lists: obj list list)
        =
        lists
        |>List.map (fun inner_list ->
            inner_list
            |>List :> IList<_>
        )
        |>List :> IList<_>
    
    let ``try input_into_sheet``()=
        input_obj_into_sheet
            {
                Google_spreadsheet.doc_id="1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU"
                page_name="test"
            }
            ([
                ["test";"123"]
            ]|>string_lists_to_google_obj)
    
    
        
    let clean_sheet
        (sheet: Google_spreadsheet)
        =
        let clean_row = 
            sheet_row_clean
        
        let clean_sheet =
            [ for _ in 0 .. 500 -> clean_row]
            |>string_lists_to_google_obj
        
        input_obj_into_sheet sheet clean_sheet

        
    let ``try clean_sheet``() =  
        clean_sheet
            Settings.Influencer_competition.Google_sheets.followers_amount
            
            
    let username_from_handle
        (user_handles_to_names: Map<User_handle, string>)
        handle
        =
        user_handles_to_names
        |>Map.tryFind handle
        |>Option.defaultValue (User_handle.value handle)
        
    let empty_cell =
        CellData(
            UserEnteredValue = ExtendedValue(
                StringValue= ""
            )
        )
        

module Googlesheet_id =
    
    let mutable cached_ids = Map.empty<string, Map<string,int>>
    
    
    let map_sheet_titles_to_ids
        (service: SheetsService)
        document
        =
        let metadata = service.Spreadsheets.Get(document).Execute()
        
        metadata.Sheets
        |>Seq.map(fun sheet ->
            sheet.Properties.Title,
            (int sheet.Properties.SheetId)
        )
        |>Map.ofSeq
    
    let try_sheet_id_from_title
        (service: SheetsService)
        document
        title
        =
        match
            cached_ids
            |>Map.tryFind document
        with
        |Some document_cache ->
            document_cache
            |>Map.tryFind title
        |None->
            let document_cache =
                map_sheet_titles_to_ids
                    service
                    document
            
            cached_ids <-
                cached_ids
                |>Map.add
                    document
                    document_cache
            
            document_cache
            |>Map.tryFind title
            
            
    let sheet_id_from_title
        (service: SheetsService)
        document
        title
        =
        match
            try_sheet_id_from_title
                service
                document
                title
        with
        |Some sheet_id -> sheet_id
        |None -> raise (KeyNotFoundException())
            