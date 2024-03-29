﻿namespace rvinowise.twitter

open rvinowise.twitter



type Cell_value =
    |Text of string
    |Formula of string
    |Integer of int
    |Float of float
    |Percent of float

module Cell_value=
    let is_empty value =
        match value with
        |Text value
        |Formula value
            -> value = "" 
        |Integer _
        |Float _
        |Percent _
            -> false 

type Text_orientation =
    |Horizontal
    |Vertical

type Text_writing =
    |Regular
    |Bold
    |Cursive

type Text_style = {
    rotation: Text_orientation
    writing: Text_writing
}

module Text_style =
    let regular = {
        rotation = Text_orientation.Horizontal
        writing = Text_writing.Regular
    }
    let vertical = {
        rotation = Text_orientation.Vertical
        writing = Text_writing.Regular
    }


type Cell = {
    color: Color
    value: Cell_value
    style: Text_style
}
type Frame =
|Cell of Cell
|Table of Frame list list




module Cell =
    
    let empty = {
        Cell.color = Color.white
        value = Cell_value.Text ""
        style = Text_style.regular
    }
    
    let is_empty (cell:Cell) =
        cell.value
        |>Cell_value.is_empty
    
    let from_colored_number
        (number,color)
        =
        {
            Cell.color = color
            value = Cell_value.Integer number
            style = Text_style.regular
        }
    
    let from_colored_text
        color
        text
        =
        {
            Cell.color = color
            value = Cell_value.Text text
            style = Text_style.regular
        }
    
    let from_plain_text text = {
        Cell.color = Color.white
        value = Cell_value.Text text
        style = Text_style.regular
    }
    let from_plain_integer number = {
        Cell.color = Color.white
        value = Cell_value.Integer number
        style = Text_style.regular
    }
    
    let escape_special_characters (text:string) =
        text.Replace("\"","'")
    
    let from_url text url =
        let escaped_text = escape_special_characters text    
        {
            Cell.color = Color.white
            
            value =
                Cell_value.Formula $"""=HYPERLINK("{url}", "{escaped_text}")"""
            style = Text_style.regular
        }
    
    let from_formula text url =
        let escaped_text = escape_special_characters text  
        {
            Cell.color = Color.white
            value =
                Cell_value.Formula $"""=HYPERLINK("{url}", "{escaped_text}")"""
            style = Text_style.regular
        }
        
    let from_table_with_colored_numbers
        (rows: (int*Color) list list)
        =
        rows
        |>List.map (fun cells ->
            cells
            |>List.map from_colored_number
        )

module Frame =
    let empty =
        Frame.Cell Cell.empty
        
    let from_colored_number
        (number,color)
        =
         Cell.from_colored_number (number,color)
         |>Frame.Cell

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
        
        
    let trim_table
        is_empty
        (rows: 'Item list list)
        =
        rows
        |>List.map(fun cells ->
            cells
            |>List.rev
            |>List.skipWhile is_empty
            |>List.rev
        )
        |>List.rev
        |>List.skipWhile List.isEmpty
        |>List.rev