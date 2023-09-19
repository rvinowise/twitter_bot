namespace rvinowise.twitter

open System
open System.Globalization
open FSharp.Data
open OpenQA.Selenium
open canopy.parallell.functions
open OpenQA.Selenium.Support.UI
open FParsec
open Xunit
open rvinowise.html_parsing
open rvinowise.twitter


module Parsing_twitter_datatypes =
    let final_multiplier_parser: Parser<int,unit> =
        (pstring "K"|>>fun _-> 1000) <|>
        (pstring "M"|>>fun _->1000000) <|>
        (spaces|>>fun _->1)
        
    let abbreviated_number_parser: Parser<int,unit> =
        pfloat .>>. final_multiplier_parser
        |>> (fun (first_number,last_multiplier) ->
            (first_number * float last_multiplier)
            |> int
        )    
    
    let remove_commas (number:string) =
        number.Replace(",", "")
        
    
    let try_parse_abbreviated_number text =
        text
        |>remove_commas
        |>run abbreviated_number_parser
        |>function
        |Success (number,_,_) -> Some number
        |Failure (error,_,_) -> 
            $"""error while parsing number of followers:
            string: {text}
            error: {error}"""
            |>Log.error|>ignore
            None
    
    let parse_abbreviated_number text =
        text
        |>try_parse_abbreviated_number
        |>function
        |Some result -> result
        |None -> raise (FormatException $"there's no abbreviated number in text: '{text}'")
    
    let parse_joined_date text =
        let textual_date =
            pstring "Joined " 
            >>. manyCharsTill anyChar (pstring " ") 
            .>>. pint32
            |>> (fun (month,year) ->
                let month_no =
                    DateTime.ParseExact(month, "MMMM", CultureInfo.CurrentCulture).Month
                DateTime(year,month_no,1)
            )
        run textual_date text
        |>function
        |Success (date,_,_) -> date
        |Failure (error,_,_) ->
            Log.error error |>ignore
            DateTime.MinValue
            
    let parse_twitter_datetime text =
        Html_parsing.parse_datetime "yyyy-MM-dd'T'HH:mm:ss.fff'Z'" text