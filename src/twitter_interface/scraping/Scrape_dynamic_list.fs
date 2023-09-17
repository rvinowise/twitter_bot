namespace rvinowise.twitter

open System
open FSharp.Data
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open rvinowise.twitter
open canopy.parallell.functions

module Scrape_dynamic_list =
    
    
    let skim_displayed_items
        browser
        item_css
        =
        let items = elements item_css browser
        items
        |>Seq.map Parsing.html_node_from_web_element
        
            
    let new_elements_are_skimmed
        (skimmed_sofar_elements: HtmlNode Set)
        (new_skimmed_items: HtmlNode Set)
        =
        skimmed_sofar_elements
        |>Set.difference new_skimmed_items
        |>Set.isEmpty|>not
            
    
    
    let consume_items_of_dynamic_list
        browser
        (action: HtmlNode -> 'a)
        item_selector
        =
        let rec skim_and_scroll_iteration
            (skimmed_sofar_items: HtmlNode Set)
            =
            let current_skimmed_items =
                skim_displayed_items browser item_selector
                |>Set.ofSeq
                
            let new_skimmed_items =
                skimmed_sofar_items
                |>Set.difference current_skimmed_items
            
            if (not new_skimmed_items.IsEmpty) then
                Actions(browser)
                    .SendKeys(Keys.PageDown).SendKeys(Keys.PageDown).SendKeys(Keys.PageDown)
                    .Perform()
                sleep 1
                
                new_skimmed_items
                |>Seq.map action
                
                new_skimmed_items
                |>Set.union skimmed_sofar_items
                |>skim_and_scroll_iteration
                    
            else
                skimmed_sofar_items
        
        skim_and_scroll_iteration
            Set.empty
        
    
    let collect_all_items_of_dynamic_list
        browser
        item_selector
        =
        item_selector
        |>consume_items_of_dynamic_list
            browser
            (fun _->())
    
        
            
    
        


    
    



    


