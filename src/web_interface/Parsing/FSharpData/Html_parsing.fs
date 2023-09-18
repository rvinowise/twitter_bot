namespace rvinowise.html_parsing.fsharpdata

open System
open System.Globalization
open FSharp.Data
open AngleSharp
open OpenQA.Selenium
open FParsec
open Xunit

open rvinowise.twitter


(*this module encapsulates an external library for parsing HTML *)

type Html_node = FSharp.Data.HtmlNode
//type Html_node = AngleSharp.Dom.IElement
type Web_node = OpenQA.Selenium.IWebElement

module Html_element =
    let text (node: Html_node) =
        node.DirectInnerText
    


module Html_parsing =
    
    
    let last_url_segment (text:string) =
        text.Substring(text.LastIndexOf(@"/")+1)
    
    let descendants css (node:Html_node) =
        HtmlNode.cssSelect node css
    
    let direct_children (node:Html_node) =
        node.Elements()
        
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
    
//    let direct_children css (parent:HtmlNode) =
//        HtmlNode. parent
        
    
    let first_descendants css (node:Html_node) =
        
        let needed_children =
            node
            |>Html_parsing.direct_children css
    
    let try_descendant css (node:Html_node) =
        node
        |>descendants css
        |>function
        |[]->None
        |[single] -> Some single
        |many ->
            Log.error $"expected one element, but there's {Seq.length many}"|>ignore
            many|>List.head|>Some
    
    let which_have_direct_child
        (child:Html_node)
        (parents: HtmlNode seq)
        =
        parents
        |>Seq.filter(fun parent->
            parent.Elements()
            |>List.contains child
        )
            
    let descend levels_amount (node:Html_node) =
        [1..levels_amount]
        |>List.fold (fun node _ ->
            node
            |>direct_children
            |>should_be_single
        )
            node
            
    let parseable_node_from_scraped_node
        (web_node: Web_node): Html_node
        =
        web_node.GetAttribute("outerHTML")
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
        
        
    let segment_of_composed_text_as_text (segment:Html_node) =
        match segment.Name() with
        |"img" -> //an emoji
            segment.AttributeValue "alt"
        |"span" when segment.InnerText() = "" -> " " //FSharpData removes spaces from spans 😳
        |_->segment.InnerText()
    
    let readable_text_from_html_segments (root:Html_node) =
        root
        |>direct_children
        |>Seq.map segment_of_composed_text_as_text
        |>String.concat ""
        
    let parse_datetime format text =
        DateTime.ParseExact(
            text,
            format,
            CultureInfo.InvariantCulture
        )