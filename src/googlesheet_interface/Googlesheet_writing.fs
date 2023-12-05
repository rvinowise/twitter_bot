namespace rvinowise.twitter

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



module Googlesheet_writing =
    
    
    
    
    let text_row_to_google_vertical_cells
        (cells: string list)
        =
        cells
        |>List.map (fun cell_value ->
            CellData(
                UserEnteredFormat = CellFormat(
                    TextRotation=TextRotation(
                        Angle= Nullable -90
                    )
                ),
                UserEnteredValue = ExtendedValue(
                    StringValue=cell_value
                )
            )
        )
    
    let text_row_to_google_cells
        (cells: string list)
        =
        cells
        |>List.map (fun cell_value ->
            CellData(
                UserEnteredValue = ExtendedValue(
                    StringValue=cell_value
                )
            )
        )
    
    let colored_numbers_to_google_cells
        (rows: (int*Color) list list)
        =
        rows
        |>List.map (fun cells ->
            cells
            |>List.map (fun (cell_value,color) ->
                CellData(
                    UserEnteredFormat = CellFormat(
                        BackgroundColor=Color.to_google_color color
                    ),
                    UserEnteredValue = ExtendedValue(
                        NumberValue= (cell_value|>float|>Nullable)
                    )
                )
            )
        )
    
    let lists_of_google_cells_to_google_table
        (rows: CellData list list)
        =
        rows
        |>List.map (fun cells_of_row ->
            RowData(
                Values=(
                    cells_of_row
                    |>List :> IList<_>
                )
            )
        )|>List :> IList<_>
    
    
    let input_colors_into_sheet
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        =
        let red = {red=1;green=0;blue=0;alpha=1}
        let green = {red=0;green=1;blue=0;alpha=1}
        let yellow = {red=1;green=1;blue=0;alpha=1}
        let blue = {red=0;green=0;blue=1;alpha=1}
        let white = {red=1;green=1;blue=1;alpha=1}
        
        
        
        let updateCellsRequest =
            Request(
                UpdateCells = UpdateCellsRequest(
                    Range = GridRange(
                        SheetId = sheet.page_id,
                        StartColumnIndex = 0,
                        StartRowIndex = 0,
                        EndColumnIndex = 1000,
                        EndRowIndex = 1000
                    ),
                    Rows =
                        (
                         text_row_to_google_vertical_cells
                            ["long name1"; "long name2"]
                        ::
                        colored_numbers_to_google_cells [
                            [1,white;2,white;3,yellow;3,yellow]
                            [3,white;4,white]
                        ]
                        |>Table.transpose Googlesheet.empty_cell
                        |>List.append [(text_row_to_google_cells ["test1";"test2";"test3";"test4";"test";"test"])]
                        |>Table.transpose Googlesheet.empty_cell
                        |>lists_of_google_cells_to_google_table
                        )
                    ,
                    Fields = "*"
                )
            )
        
        let bussr = BatchUpdateSpreadsheetRequest()
        bussr.Requests <- List<Request>()
        bussr.Requests.Add(updateCellsRequest)
        let result = service.Spreadsheets.BatchUpdate(bussr, sheet.doc_id).Execute()
        ()
    
    let write_table
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        table
        =
        let max_x = 
            table
            |>List.head
            |>List.length
            
        let max_y =    
            List.length table
        
        let updateCellsRequest =
            Request(
                UpdateCells = UpdateCellsRequest(
                    Range = GridRange(
                        SheetId = sheet.page_id,
                        StartColumnIndex = 0,
                        StartRowIndex = 0,
                        EndColumnIndex = max_x,
                        EndRowIndex = max_y
                    ),
                    Rows = lists_of_google_cells_to_google_table table,
                    Fields = "*"
                )
            )
        
        let batch = BatchUpdateSpreadsheetRequest()
        batch.Requests <- List<Request>()
        batch.Requests.Add(updateCellsRequest)
        let result = service.Spreadsheets.BatchUpdate(batch, sheet.doc_id).Execute()
        ()
    
    
    [<Fact>]
    let ``try input_colors_into_sheet``() =
        input_colors_into_sheet
            (Googlesheet.create_googlesheet_service())
            {
                Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
                page_id=2108706810
                page_name="Reposts"
            }
    
    
