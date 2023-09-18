﻿namespace rvinowise.html_parsing

open System
open System.Globalization
open System.Threading.Tasks
open AngleSharp.Html.Parser
open AngleSharp.Io
open FSharp.Data
open AngleSharp
open OpenQA.Selenium
open FParsec
open Xunit

open rvinowise.twitter


(*this module encapsulates an external library for parsing HTML *)

type Html_node = AngleSharp.Dom.IElement
type Web_node = OpenQA.Selenium.IWebElement

module Html_node =
    let attribute_value attribute (node:Html_node) =
        node.GetAttribute(attribute)
    
    let inner_text (node:Html_node) =
        node.TextContent

    let descendants css (node:Html_node) =
        node.QuerySelectorAll css
        |>List.ofSeq
    
    let direct_children (node:Html_node) =
        List.ofSeq node.Children
    
    

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
    
    let descendant css (node:Html_node) =
        node
        |>descendants css
        |>should_be_single
    
    
    
            
    
    let try_descendant css (node:Html_node) =
        node
        |>descendants css
        |>function
        |[]->None
        |[single] -> Some single
        |many ->
            Log.error $"expected one element, but there's {Seq.length many}"|>ignore
            many|>List.head|>Some
    
    
    let first_descendants_with_css css (node:Html_node) =
        
        let rec iteration_of_finding_children
            css
            (nodes: Html_node list)
            =
            let needed_children =
                nodes
                |>List.collect (descendants (">"+css))
            match needed_children with
            |[] ->
                nodes
                |>List.collect direct_children
                |>iteration_of_finding_children
                    css
            |found_children -> found_children
    
        iteration_of_finding_children
            css
            [node]
            
    
    let which_have_direct_child
        (child:Html_node)
        (parents: Html_node seq)
        =
        parents
        |>Seq.filter(fun parent->
            parent.Children
            |>Seq.contains child
        )
    
    
     
    let descend levels_amount (node:Html_node) =
        [1..levels_amount]
        |>List.fold (fun node _ ->
            node
            |>direct_children
            |>should_be_single
        )
            node




module Html_parsing =
    
    let init_context () =
        BrowsingContext.New(AngleSharp.Configuration.Default)
    
    let last_url_segment (text:string) =
        text.Substring(text.LastIndexOf(@"/")+1)
    
  
    let parseable_node_from_scraped_node
        (context:IBrowsingContext )
        (web_node: Web_node): Html_node
        =
        let html_text = web_node.GetAttribute("outerHTML")
        let htmlParser = context.GetService<IHtmlParser>();
        let document = htmlParser.ParseDocument html_text
        document.Body
        
    

    let segment_of_composed_text_as_text (segment:Html_node) =
        match segment.ClassName with
        |"img" -> //an emoji
            Html_node.attribute_value "alt" segment
        |"span" when Html_node.inner_text segment = "" -> " " //FSharpData removes spaces from spans 😳
        |_->Html_node.inner_text segment
    
    let readable_text_from_html_segments (root:Html_node) =
        root
        |>Html_node.direct_children
        |>Seq.map segment_of_composed_text_as_text
        |>String.concat ""
        
    let parse_datetime format text =
        DateTime.ParseExact(
            text,
            format,
            CultureInfo.InvariantCulture
        )