namespace rvinowise.twitter

open AngleSharp
open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.web_scraping

open FsUnit
open Xunit


module Scrape_visible_part_of_list =
    
    
    let scrape_web_items
        browser
        item_css
        =
        let items = Browser.elements item_css browser
        items
        |>List.map (fun web_element ->
            try
                web_element
                |>Web_element.attribute_value "outerHTML"
                |>Html_string
                |>Some
            with
            | :? StaleElementReferenceException as exc ->
                Log.error $"skim_displayed_items error: {exc.Message}"|>ignore
                None
        )|>List.choose id
        
    let scrape_items
        browser
        html_context
        is_item_needed
        item_selector
        =
        scrape_web_items
            browser
            item_selector
        |>List.map (Html_node.from_html_string_and_context html_context)
        |>List.filter is_item_needed
    
    let scrape_all_items
        browser
        html_context
        item_selector
        =
        scrape_web_items
            browser
            item_selector
        |>List.map (Html_node.from_html_string_and_context html_context)
        |>Array.ofList 
    
    let scrape_container
        browser
        html_context
        container_selector
        =
        let container = Browser.element container_selector browser
        
        try
            container
            |>Web_element.attribute_value "outerHTML"
            |>Html_node.from_text_and_context html_context
            |>Some
        with
        | :? StaleElementReferenceException as exc ->
            Log.error $"read_container_with_items error: {exc.Message}"|>ignore
            None
    
    let items_from_container
        is_item_needed
        item_selector
        page_html
        =
        page_html
        |>Html_node.descendants item_selector
        |>List.filter is_item_needed
        |>Array.ofList  
    
    
    
        
    