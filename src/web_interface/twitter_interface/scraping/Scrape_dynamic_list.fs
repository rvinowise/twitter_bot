namespace rvinowise.twitter

open System
open FSharp.Data
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open rvinowise.html_parsing
open canopy.parallell.functions

module Scrape_dynamic_list =
    
    
    let skim_displayed_items
        browser
        item_css
        =
        let items = elements item_css browser
        items
        |>Seq.map Html_parsing.parseable_node_from_scraped_node
        

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
    
    let consume_items_of_dynamic_list
        browser
        (action: Html_node -> 'Parsed_item)
        item_selector
        =
        let rec skim_and_scroll_iteration
            (skimmed_sofar_items: Map<HtmlNode, 'Parsed_item>)
            =
            let visible_skimmed_items =
                skim_displayed_items browser item_selector
                |>Set.ofSeq
                
            let new_skimmed_items =
                skimmed_sofar_items
                |>Map.keys
                |>Set.ofSeq
                |>Set.difference visible_skimmed_items
            
            if (not new_skimmed_items.IsEmpty) then
                Actions(browser)
                    .SendKeys(Keys.PageDown).SendKeys(Keys.PageDown).SendKeys(Keys.PageDown)
                    .Perform()
                sleep 1
                
                let parsed_new_items =
                    new_skimmed_items
                    |>Seq.map (fun item->
                        item, action item
                    )
                
                skimmed_sofar_items
                |>add_new_items_to_map parsed_new_items
                |>skim_and_scroll_iteration
                    
            else
                skimmed_sofar_items
        
        skim_and_scroll_iteration
            Map.empty
        
    
    let collect_all_items_of_dynamic_list
        browser
        item_selector
        =
        item_selector
        |>consume_items_of_dynamic_list
            browser
            ignore
    
        
            
    
        


    
    



    


