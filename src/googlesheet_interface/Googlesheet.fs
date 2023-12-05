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
        
        let empty_cell =
            CellData(
                UserEnteredValue = ExtendedValue(
                    StringValue= ""
                )
            )
        
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
                        |>Table.transpose empty_cell
                        |>List.append [(text_row_to_google_cells ["test1";"test2";"test3";"test4";"test";"test"])]
                        |>Table.transpose empty_cell
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
    
    [<Fact>]
    let ``try input_colors_into_sheet``() =
        input_colors_into_sheet
            (create_googlesheet_service())
            {
                Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
                page_id=2108706810
                page_name="Reposts"
            }
    
    
        
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
            ]|>string_lists_to_google_obj)
    
    
        
    let clean_sheet
        (sheet: Google_spreadsheet)
        =
        let clean_row = 
            sheet_row_clean
        
        let clean_sheet =
            [ for _ in 0 .. 500 -> clean_row]
            |>string_lists_to_google_obj
        
        input_into_sheet sheet clean_sheet

        
    [<Fact(Skip="manual")>]//
    let ``try clean_sheet``() =  
        clean_sheet
            Settings.Google_sheets.followers_amount
            
            
    let username_from_handle
        (user_handles_to_names: Map<User_handle, string>)
        handle
        =
        user_handles_to_names
        |>Map.tryFind handle
        |>Option.defaultValue (User_handle.value handle)