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



module Table =
    
    let append_column
        (table: 'Item list list)
        (column: 'Item list)
        =
        column
        ::
        (
            table
            |>List.transpose
        )
        |>List.transpose    
       
    let pad
        padding
        (table: 'Item list list)
        =
        let max_length =
            table
            |>List.maxBy(fun cells ->
                cells|>List.length
            )|>List.length
        
        table
        |>List.map(fun cells ->
            let lacking_cells =
                max_length - (cells|>List.length)
            
            (fun _ -> padding)
            |>List.init lacking_cells
            |>List.append cells
        )
        
    let transpose padding (table: 'Item list list) =
        table
        |>pad padding
        |>List.transpose