namespace rvinowise.twitter

open System
open System.Collections.Generic

open AngleSharp.Css.Dom
open Google.Apis.Sheets.v4
open Google.Apis.Sheets.v4.Data
open Google.Apis.Util
open Xunit
open rvinowise.twitter


type Google_cell = Google.Apis.Sheets.v4.Data.CellData
module Google_cell =
    let url
        (google_cell: Google_cell)
        =
        google_cell.Hyperlink
        

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
        Cell_value.Text google_cell.EffectiveValue.StringValue
       
    
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
        (google_cell: CellData)
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
    
    let visible_value
        (google_cell:CellData)
        =
        {
            Cell.color=
                color_from_google_cell
                    google_cell
            value=
                value_from_google_cell
                    visible_text_from_cell
                    google_cell
            style=
                Text_style.regular
        }
    
    let urls
        (google_cell:CellData)
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
        (range: ((int*int)*(int*int)) )
        =
        let (start_x,start_y),(end_x,end_y) =
            range
        $"{page_name}!R{start_x}C{start_y}:R{end_x}C{end_y}"
    
    let read_range
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        (range: ((int*int)*(int*int)) )
        =
        service.Spreadsheets.Values.Get(
            sheet.doc_id,
            (range_as_string sheet.page_name range)
        ).Execute().Values
        |>Seq.map(fun row ->
            row
            |>Seq.map(fun google_cell ->
                {
                    Cell.color=Color.white
                    value=
                        Cell_value.Text (google_cell :?> string)
                    style=Text_style.regular
                }
            )
            |>List.ofSeq
        )
        |>List.ofSeq
    
    
    let read_table
        parse_google_cell
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        =
        let request = service.Spreadsheets.Get(sheet.doc_id)
        request.Ranges <- Repeatable [sheet.page_name]
        request.IncludeGridData <- true
        let response = request.Execute()
        
        response.Sheets[0].Data[0].RowData
        |>Seq.map(fun google_row ->
            google_row.Values
            |>Seq.map parse_google_cell
            |>List.ofSeq
        )
        |>List.ofSeq
        
    [<Fact>]//(Skip="Manual")
    let ``try read_table``()=
        let table =
            read_table
                Parse_google_cell.urls
                (Googlesheet.create_googlesheet_service())
                {
                    Google_spreadsheet.doc_id = "1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU"
                    page_name="Followers amount"
                }
        ()