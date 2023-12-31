namespace rvinowise.twitter

open System
open System.Collections.Generic

open AngleSharp.Css.Dom
open Google.Apis.Sheets.v4
open Google.Apis.Sheets.v4.Data
open Google.Apis.Util
open Xunit
open rvinowise.twitter



module Googlesheet_reading =
    
    let google_value_to_value
        (google_value: ExtendedValue)
        =
        if google_value.NumberValue.HasValue then
           Cell_value.Float (google_value.NumberValue.Value) 
        elif google_value.BoolValue.HasValue then
           Cell_value.Text (string google_value.BoolValue.Value)
        else
           Cell_value.Text google_value.StringValue

    
    let google_cell_to_cell
        (google_cell:CellData)
        =
        {
            Cell.color=
                if isNull google_cell.EffectiveFormat then
                    Color.white
                else
                    google_cell.EffectiveFormat.BackgroundColor
                    |>Color.from_google_color
                
            value=
                if isNull google_cell.EffectiveValue then
                    Cell_value.Text ""
                else
                    google_cell.EffectiveValue
                    |>google_value_to_value
                
            style=
                Text_style.regular
        }
            
    
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
            |>Seq.map google_cell_to_cell
            |>List.ofSeq
        )
        |>List.ofSeq
        
    [<Fact>]
    let ``try read_table``()=
        let table =
            read_table
                (Googlesheet.create_googlesheet_service())
                {
                    Google_spreadsheet.doc_id = "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0"
                    page_id=0
                    page_name="Members"
                }
        ()