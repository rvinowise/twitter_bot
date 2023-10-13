namespace rvinowise.twitter

open System
open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.web_scraping

open FsUnit
open Xunit


module Scrape_dynamic_list =
    
    
    let skim_displayed_items
        browser
        item_css
        =
        use parameters = Scraping_parameters.wait_seconds 60 browser
        let items = Browser.elements item_css browser
        items
        |>List.map (fun web_element ->
            web_element
            |>Web_element.attribute_value "outerHTML"
            |>Html_string 
        )
        
        

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
        let new_items = [|7;6;5;4;3|]
        let all_items = [5;4;3;2;1]
        new_items_from_visible_items
            new_items
            all_items 
        |>should equal [|
            7;6
        |]
        
        let new_items = [|5;4;3|]
        let all_items = [5;4;3;2;1;0]
        new_items_from_visible_items
            new_items
            all_items 
        |>should equal [||]
     
    let parse_dynamic_list
        browser
        (wait_for_loading)
        (parse_item: list<Html_string * 'Parsed_item> -> Html_string -> 'Parsed_item)
        needed_amount
        item_selector
        =
        let rec skim_and_scroll_iteration
            (parsed_sofar_items: list<Html_string * 'Parsed_item>)
            =
            
            //wait_for_loading
            
            
            let visible_skimmed_items =
                skim_displayed_items
                    browser
                    item_selector
                |>List.rev
                |>Array.ofList
                
            let new_skimmed_items =
                parsed_sofar_items
                |>List.map fst
                |>new_items_from_visible_items visible_skimmed_items
                |>List.ofArray
            
            if
                (new_skimmed_items|>Seq.isEmpty|>not) &&
                (Seq.length parsed_sofar_items < needed_amount)
            then
                Browser.send_keys [Keys.PageDown;Keys.PageDown;Keys.PageDown] browser
                
                Browser.sleep 1
                
                let all_parsed_items =
                    parsed_sofar_items
                    |>List.foldBack (fun item all_items ->
                        (item, parse_item all_items item) 
                        ::
                        all_items
                    )
                        new_skimmed_items
                        
                skim_and_scroll_iteration
                    all_parsed_items
                
            else
                parsed_sofar_items
        
        skim_and_scroll_iteration []
        
    
    let dont_parse_html_item _ _ = ()
    
    let collect_all_html_items_of_dynamic_list
        browser
        wait_for_loading
        item_selector
        =
        item_selector
        |>parse_dynamic_list
            browser
            wait_for_loading
            dont_parse_html_item
            Int32.MaxValue
        |>List.map fst
        
    let collect_some_html_items_of_dynamic_list
        browser
        wait_for_loading
        max_amount
        item_selector
        =
        item_selector
        |>parse_dynamic_list
            browser
            wait_for_loading
            dont_parse_html_item
            max_amount
        |>List.map fst
    
        
    

    
    



    


