namespace rvinowise.twitter

open System
open FSharp.Data
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open rvinowise.twitter
open canopy.parallell.functions

module Scrape_catalog =
    
    
    
    let skim_displayed_items browser
        =
        let user_cells = elements "div[data-testid='UserCell']" browser
        user_cells
        |>Seq.map Parsing.html_node_from_web_element
        
            
    let new_elements_are_skimmed
        (skimmed_sofar_elements: HtmlNode Set)
        (new_skimmed_items: HtmlNode Set)
        =
        skimmed_sofar_elements
        |>Set.difference new_skimmed_items
        |>Set.isEmpty|>not
            
    let consume_all_items_of_catalog
        browser
        catalog
        =
        let rec skim_and_scroll_iteration
            catalog
            (skimmed_sofar_elements: HtmlNode Set)
            =
            let new_skimmed_items =
                skim_displayed_items browser 
                |>Set.ofSeq
                
            if new_elements_are_skimmed 
                skimmed_sofar_elements
                new_skimmed_items
            then
                Actions(browser)
                    .SendKeys(Keys.PageDown).SendKeys(Keys.PageDown).SendKeys(Keys.PageDown)
                    .Perform()
                sleep 1
                
                new_skimmed_items
                |>Set.ofSeq
                |>Set.union skimmed_sofar_elements
                |>skim_and_scroll_iteration catalog
                    
            else
                skimmed_sofar_elements
        
        skim_and_scroll_iteration
            catalog
            Set.empty
            
    let scrape_catalog browser catalog_url = 
        Log.info $"reading elements of catalog {catalog_url} ... " 
        url catalog_url browser
        
        let catalogue =
            Scraping.try_element browser "div:has(>div[data-testid='cellInnerDiv'])"
        
        match catalogue with
        |Some catalogue ->
            let items = consume_all_items_of_catalog browser catalogue
            Log.important $"catalogue has {Seq.length items} items"
            items
        |None->
            Log.error $"{catalog_url} doesn't have a catalogue "|>ignore
            Set.empty

    
  


    
    



    


