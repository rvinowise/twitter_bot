namespace rvinowise.twitter

open System
open System.Globalization
open FSharp.Data
open OpenQA.Selenium
open canopy.parallell.functions
open OpenQA.Selenium.Support.UI
open FParsec
open Xunit
open rvinowise.twitter


module Html_element =
    let text (node: HtmlNode) =
        node.DirectInnerText

    let attribute name (node: HtmlNode) =
        node.Attribute(name)
    

module Parsing_abbreviated_number =
    let final_multiplier: Parser<int,unit> =
        (pstring "K"|>>fun _-> 1000) <|>
        (pstring "M"|>>fun _->1000000) <|>
        (spaces|>>fun _->1)
        
    let abbreviated_number: Parser<int,unit> =
        pfloat .>>. final_multiplier
        |>> (fun (first_number,last_multiplier) ->
            (first_number * float last_multiplier)
            |> int
        )    
    
    let remove_commas (number:string) =
        number.Replace(",", "")
        
    
    let try_parse_abbreviated_number text =
        text
        |>remove_commas
        |>run abbreviated_number
        |>function
        |Success (number,_,_) -> Some number
        |Failure (error,_,_) -> 
            $"""error while parsing number of followers:
            string: {text}
            error: {error}"""
            |>Log.error|>ignore
            None

module Parsing =
    let descendants css (node:HtmlNode) =
        HtmlNode.cssSelect node css
    
   
        
    let should_be_single seq =
        if Seq.length seq = 1 then
            Seq.head seq
        else if Seq.length seq = 0 then
            "expected one element, but there's none"
            |>Log.error
            |>ArgumentException
            |>raise
        else
            Log.error $"expected one element, but there's {Seq.length seq}"|>ignore
            Seq.head seq
    
    
    let descendant css (node:HtmlNode) =
        node
        |>descendants css
        |>should_be_single
            
            
    let descend levels_amount (node:HtmlNode) =
        [1..levels_amount]
        |>List.fold (fun node _ ->
            node
            |>HtmlNode.elements
            |>should_be_single
        )
            node
            
    let html_node_from_web_element
        (web_element: IWebElement)
        =
        web_element.GetAttribute("outerHTML")
        |>HtmlNode.Parse
        |>Seq.head
        
    
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
        
            
    [<Fact>]
    let ``try parse_joined_date`` ()=
        let date = 
            "Joined September 2017"
            |>parse_joined_date
        ()
        
        
    let segment_of_composed_text_as_text (segment:HtmlNode) =
        match segment.Name() with
        |"img" -> //an emoji
            segment.AttributeValue "alt"
        |"span" when segment.InnerText() = "" -> " " //FSharpData removes spaces from spans 😳
        |_->segment.InnerText()
    
    let readable_text_from_html_segments (root:HtmlNode) =
        root
        |>HtmlNode.elements
        |>Seq.map segment_of_composed_text_as_text
        |>String.concat ""
        
    let parse_datetime format text =
        DateTime.ParseExact(
            text,
            format,
            CultureInfo.InvariantCulture
        )