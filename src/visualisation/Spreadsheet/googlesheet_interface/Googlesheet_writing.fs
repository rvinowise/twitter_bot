namespace rvinowise.twitter

open System
open System.Collections.Generic

open System.Net.Http
open System.Threading.Tasks
open Google
open Google.Apis.Sheets.v4
open Google.Apis.Sheets.v4.Data
open Xunit
open rvinowise.twitter
open rvinowise.twitter.Settings.Influencer_competition



module Googlesheet_writing =
    
    let value_to_google_value
        (value: Cell_value)
        =
        match value with
        |Integer number ->
            ExtendedValue( NumberValue= (number|>float|>Nullable) )
        |Float number
        |Percent number ->
            ExtendedValue( NumberValue= (number|>float|>Nullable) )
        |Text text ->
            ExtendedValue(StringValue = text)
        |Formula text ->
            ExtendedValue(FormulaValue = text)
    
    let is_rather_dark (color:Color) =
        let red_liteness = 1.5
        let green_liteness = 1.6
        let blue_liteness = 0.5
        color.r*red_liteness+
        color.g*green_liteness+
        color.b*blue_liteness < 1.5
    
    let text_color_for_background
        (background_color:Color)
        =
        if is_rather_dark background_color then
            Color.white
        else
            Color.black
        
    
    let cell_to_google_cell
        (cell:Cell)
        =
        CellData(
            UserEnteredFormat = CellFormat(
                BackgroundColor=Color.to_google_color cell.color,
                
                TextRotation=TextRotation(
                    Angle =
                        match cell.style.rotation with
                        |Vertical ->
                            Nullable -90
                        |Horizontal ->
                            Nullable 0
                ),
                
                TextFormat = TextFormat(
                    ForegroundColorStyle = ColorStyle (
                        RgbColor = (
                            cell.color
                            |>text_color_for_background
                            |>Color.to_google_color
                        )
                    )    
                ),
                
                NumberFormat = NumberFormat(
                    Type =
                        match cell.value with
                        |Percent _ -> "PERCENT"    
                        |Integer _
                        |Float _ -> "NUMBER"
                        |_ -> "TEXT"
                )
            ),
            UserEnteredValue = value_to_google_value cell.value
        )
    
   
    
    
    
    let formulas_row_to_google_vertical_cells
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
                    FormulaValue=cell_value
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
    
    
    let lists_of_google_cells_to_google_table
        (rows: Google_cell list list)
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
    

    
    let add_dimensions_to_sheet
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        added_columns_amount
        added_rows_amount
        =
        let batch_request = BatchUpdateSpreadsheetRequest(
            Requests =([
                Request(
                    AppendDimension = AppendDimensionRequest(
                        SheetId=
                            Googlesheet_id.sheet_id_from_title
                                service
                                sheet.doc_id
                                sheet.page_name,
                        Dimension = "COLUMNS",
                        Length = Nullable added_columns_amount
                    )
                )
                Request(
                    AppendDimension = AppendDimensionRequest(
                        SheetId=
                            Googlesheet_id.sheet_id_from_title
                                service
                                sheet.doc_id
                                sheet.page_name,
                        Dimension = "ROWS",
                        Length = Nullable added_rows_amount
                    )
                )
            ]|>List)
        )
        
        let response =
                service.Spreadsheets.BatchUpdate(batch_request, sheet.doc_id).Execute()
        
        response
    
    let set_dimensions_of_sheet
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        columns_amount
        rows_amount
        =
        let batch_request =
            BatchUpdateSpreadsheetRequest(
                Requests = ([
                    Request(
                        UpdateSheetProperties = UpdateSheetPropertiesRequest(
                            Properties = SheetProperties(
                                SheetId=
                                    Googlesheet_id.sheet_id_from_title
                                        service
                                        sheet.doc_id
                                        sheet.page_name,
                                GridProperties = GridProperties(
                                    ColumnCount=Nullable columns_amount,    
                                    RowCount=Nullable rows_amount    
                                )
                                
                            ),
                            Fields="gridProperties"
                        )
                    )
                ]|>List)
            )
        service.Spreadsheets.BatchUpdate(batch_request, sheet.doc_id).Execute()
        |>ignore
    
    let ``try ensure_dimensions_of_sheet``()=
        set_dimensions_of_sheet
            (Googlesheet.create_googlesheet_service())
            {
                Google_spreadsheet.doc_id = "1IghY1FjqODJq5QpaDcCDl2GyerqEtRR79-IcP55aOxI"
                page_name="Likes"
            }
            1000
            4
    
    let check_sheet_dimensions
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        =
        ()
    
    let ``try write_sheet_dimension``()=
        add_dimensions_to_sheet
            (Googlesheet.create_googlesheet_service())
            {
                Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
                page_name="Likes"
            }
            100
            100
        
    
        
    let write_chunk
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        starting_row
        rows
        =
        let max_y =    
            starting_row + List.length rows
            
        $"""writing a chunk of rows {starting_row}-{max_y} to googlesheet {sheet.doc_id}, page {sheet.page_name}"""
        |>Log.info
        
        let max_x = 
            rows
            |>List.head
            |>List.length
            
        let updateCellsRequest =
            Request(
                UpdateCells = UpdateCellsRequest(
                    Range = GridRange(
                        SheetId =
                            Googlesheet_id.sheet_id_from_title
                                service
                                sheet.doc_id
                                sheet.page_name,
                        StartColumnIndex = 0,
                        StartRowIndex = starting_row,
                        EndColumnIndex = max_x,
                        EndRowIndex = max_y
                    ),
                    Rows = (
                        rows
                        |>List.map (List.map cell_to_google_cell)
                        |>lists_of_google_cells_to_google_table
                    ),
                    Fields = "*"
                )
            )
        
        let batch =
            BatchUpdateSpreadsheetRequest(
                Requests = List<Request>([updateCellsRequest])
            )
        service.Spreadsheets.BatchUpdate(batch, sheet.doc_id).Execute()
    
    let rec write_chunk_resiliently
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        starting_row
        rows
        =
        let service,result =
            try
                service,
                write_chunk
                    service
                    sheet
                    starting_row
                    rows
                |>Some
            with
            | :? TaskCanceledException
            | :? HttpRequestException
            | :? GoogleApiException as exc ->
                $"""exception when writing {Seq.length rows} rows starting from {starting_row}
                into sheet "{sheet.doc_id}", "{sheet.page_name}":
                {exc.Message}
                """
                |>Log.error|>ignore
                
                Googlesheet.create_googlesheet_service(),None
        
        match result with
        |None ->
            write_chunk_resiliently
                service
                sheet
                starting_row
                rows
        |Some result ->
            service,result
        
    let write_table 
        (service: SheetsService)
        (sheet: Google_spreadsheet)
        (all_rows: Cell list list)
        =
        $"""writing table to googlesheet {sheet.doc_id}, page {sheet.page_name}"""
        |>Log.important
        
        let rows_amount =
            List.length all_rows
        
        let columns_amount =
            all_rows
            |>List.head
            |>List.length 
        
        set_dimensions_of_sheet
            service
            sheet
            columns_amount
            rows_amount
        
        //assuming 10000 cells per chunk
        let rows_amount_in_chunk =
            10000
            /
            (all_rows
            |>List.head
            |>List.length)
        
        all_rows
        |>List.chunkBySize rows_amount_in_chunk
        |>List.indexed
        |>List.fold (fun service (chunk_index,rows) ->
            write_chunk_resiliently
                service
                sheet
                (chunk_index*rows_amount_in_chunk)
                rows
            |>fst
        )
            service
        |>ignore
           
    
    let ``try write table``() =
        
        let likes_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_name="Likes"
        }
        
        fun y x -> {
            Cell.value = Cell_value.Integer (y*10+x)
            color = Color.white
            style = Text_style.regular
        }
        |>List.init 10
        |>List.map (List.init 10)
        |>write_table
            (Googlesheet.create_googlesheet_service())
            likes_googlesheet