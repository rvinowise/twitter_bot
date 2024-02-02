namespace rvinowise.twitter

open System
open System.Collections.Generic

open AngleSharp.Css.Dom
open Google.Apis.Sheets.v4
open Google.Apis.Sheets.v4.Data
open Google.Apis.Util
open Xunit
open rvinowise.twitter



        

module Parse_google_cell =
    
    let color_from_google_cell (google_cell:Google_cell) =
        if isNull google_cell.EffectiveFormat then
            Color.white
        else
            google_cell.EffectiveFormat.BackgroundColor
            |>Color.from_google_color
    
    
    let visible_text_from_cell
        (google_cell: Google_cell)
        =
        if isNull google_cell.EffectiveValue then
            ""
        else
            google_cell.EffectiveValue.StringValue
       
    
    let url_from_cell
        (google_cell: Google_cell)
        =    
        match google_cell.Hyperlink with
        |"" ->
            Cell_value.Text google_cell.EffectiveValue.StringValue
        |url ->
            Cell_value.Text (string google_cell.Hyperlink)
    
    let value_from_google_cell
        parse_url_cell
        (google_cell: Google_cell)
        =
        let google_value = google_cell.EffectiveValue
        if isNull google_value then
            Cell_value.Text ""
        elif google_value.NumberValue.HasValue then
           Cell_value.Float google_value.NumberValue.Value
        elif google_value.BoolValue.HasValue then
           Cell_value.Text (string google_value.BoolValue.Value)
        else
            parse_url_cell google_cell
    
    let try_twitter_user_handle_from_visible_text
        (google_cell: Google_cell)
        =
        let google_value = google_cell.EffectiveValue
        if google_value.StringValue <> "" then
            User_handle.try_handle_from_text google_value.StringValue
        else
            None
            
    let try_twitter_user_handle_from_url
        (google_cell: Google_cell)
        =
        User_handle.try_handle_from_url google_cell.Hyperlink
    
    let try_twitter_user_handle_from_google_cell
        (google_cell: Google_cell)
        =
        if isNull google_cell.EffectiveValue then
            None
        else
            [
                try_twitter_user_handle_from_visible_text
                try_twitter_user_handle_from_url
            ]
            |>List.tryPick (fun try_find_handle ->
                try_find_handle google_cell
            )
            
            
    
    let visible_value
        (google_cell:Google_cell)
        =
        {
            Cell.color=
                color_from_google_cell
                    google_cell
            value=
                value_from_google_cell
                    (visible_text_from_cell>>Cell_value.Text)
                    google_cell
            style=
                Text_style.regular
        }
    
    let urls
        (google_cell:Google_cell)
        =
        {
            Cell.color=
                color_from_google_cell
                    google_cell
                
            value=
                value_from_google_cell
                    url_from_cell
                    google_cell
                
            style=
                Text_style.regular
        }
    

module Googlesheet_reading =

    
    let range_as_string
        page_name
        (range: (int*int)*(int*int) )
        =
        let (start_x,start_y),(end_x,end_y) =
            range
        $"{page_name}!R{start_y}C{start_x}:R{end_y}C{end_x}"
    
    
    
    
    let read_string_range
        parse_google_cell
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        range
        =
        let request = service.Spreadsheets.Get(sheet.doc_id)
        request.Ranges <- Repeatable [range]
        request.IncludeGridData <- true
        let response = request.Execute()
        
        response.Sheets[0].Data[0].RowData
        |>Seq.map(fun google_row ->
            google_row.Values
            |>Seq.map parse_google_cell
            |>List.ofSeq
        )
        |>List.ofSeq
    
    
    let read_range
        parse_google_cell
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        (range: (int*int)*(int*int) )
        =
        read_string_range
            parse_google_cell
            (service: SheetsService)
            (sheet: Google_spreadsheet)
            (range_as_string sheet.page_name range)
        
    
    let read_table
        parse_google_cell
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        =
        read_string_range
            parse_google_cell
            (service: SheetsService)
            (sheet: Google_spreadsheet)
            sheet.page_name
    
    
    let ``try read_table``()=
        let table =
            read_range
                Parse_google_cell.urls
                (Googlesheet.create_googlesheet_service())
                {
                    Google_spreadsheet.doc_id = "1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU"
                    page_name="Posts amount"
                }
                ((2,3),(2,100))
            |>Table.trim_table
                Cell.is_empty
        ()