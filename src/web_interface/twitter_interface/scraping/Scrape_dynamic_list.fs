namespace rvinowise.twitter

open System
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open rvinowise.html_parsing
open canopy.parallell.functions

open FsUnit
open Xunit


module Scrape_dynamic_list =
    
    
    let skim_displayed_items
        (browser:IWebDriver)
        item_css
        =
        browser.Manage().Timeouts().ImplicitWait <- TimeSpan.FromSeconds(60); //test
        let items = elements item_css browser
        items
        |>List.map (fun web_element -> Html_string (web_element.GetAttribute("outerHTML")))
        

    let add_new_items_to_map
        added_items
        map
        =
        added_items
        |>Seq.fold (fun all_items (new_item,parsed_new_item) -> 
            all_items
            |>Map.add new_item parsed_new_item
        )
            map
    
    (* 5;4;3;2;1 -> 7;6;5;4;3 -> 6;7 *)
    let new_items_from_visible_items
        (new_items: 'a array)
        all_items
        =
        match all_items with
        |last_known_item::_ ->
            
            new_items
            |>Array.tryFindIndex ((=)last_known_item)
            |>function
            |Some i_first_previously_known_item ->
                new_items
                |>Array.splitAt i_first_previously_known_item
                |>fst
            |None->
                Log.error $"""lists are not overlapping, some elements might be missed.
                full_list={all_items};
                new_list={new_items}"""|>ignore
                new_items
            
        |[]->new_items
        
   
    [<Fact>]
    let ``try unique_items_of_new_collection``() =
        let all_items = [5;4;3;2;1]
        let new_items = [|7;6;5;4;3|]
        new_items_from_visible_items
            new_items
            all_items 
        |>should equal [|
            7;6
        |]
        
        let all_items = [5;4;3;2;1;0]
        let new_items = [|5;4;3|]
        new_items_from_visible_items
            new_items
            all_items 
        |>should equal [||]
     
    let consume_items_of_dynamic_list
        browser
        (action: list<Html_string * 'Parsed_item> -> Html_string -> 'Parsed_item)
        max_amount
        item_selector
        =
        let rec skim_and_scroll_iteration
            (skimmed_sofar_items: list<Html_string * 'Parsed_item>)
            =
            let visible_skimmed_items =
                skim_displayed_items
                    browser
                    item_selector
                |>Array.ofList
                
            let new_skimmed_items =
                skimmed_sofar_items
                |>List.map fst
                |>new_items_from_visible_items visible_skimmed_items
                |>List.ofArray
            
            if
                (new_skimmed_items|>Seq.isEmpty|>not) &&
                (Seq.length skimmed_sofar_items < max_amount)
            then
                Actions(browser)
                    .SendKeys(Keys.PageDown).SendKeys(Keys.PageDown).SendKeys(Keys.PageDown)
                    .Perform()
                sleep 1
                
                let all_parsed_items =
                    skimmed_sofar_items
                    |>List.foldBack (fun item all_items ->
                        (item, action all_items item) 
                        ::
                        all_items
                    )
                        new_skimmed_items
                        
                skim_and_scroll_iteration
                    all_parsed_items
                
            else
                skimmed_sofar_items
        
        skim_and_scroll_iteration []
        
    
    let dont_parse_html_item all_items item = ()
    
    let collect_all_items_of_dynamic_list
        browser
        item_selector
        =
        item_selector
        |>consume_items_of_dynamic_list
            browser
            dont_parse_html_item
            Int32.MaxValue
        |>List.map fst
        |>Set.ofSeq
        
    let collect_some_items_of_dynamic_list
        browser
        max_amount
        item_selector
        =
        item_selector
        |>consume_items_of_dynamic_list
            browser
            dont_parse_html_item
            max_amount
        |>List.map fst
        |>Set.ofSeq         
    
        


    
    



    


