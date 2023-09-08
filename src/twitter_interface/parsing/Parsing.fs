namespace rvinowise.twitter

open System
open System.Globalization
open FSharp.Data
open OpenQA.Selenium
open canopy.classic
open OpenQA.Selenium.Support.UI
open FParsec
open Xunit
open rvinowise.twitter


module Html_element =
    let text (node: HtmlNode) =
        node.DirectInnerText

    let attribute name (node: HtmlNode) =
        node.Attribute(name)
    
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