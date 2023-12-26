namespace rvinowise.twitter

open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.web_scraping

open FsUnit
open Xunit


module Scrape_dynamic_list =
    
    let process_item_batch_providing_previous_items
        context
        items
        process_item
        =
        let rec iteration_of_batch_processing
            context
            items
            =
            match items with
            |next_item::rest_items ->
                let new_context =
                    process_item context next_item
                match new_context with
                |None->None
                |Some context ->
                    iteration_of_batch_processing
                        context
                        rest_items
            |[]->Some context
            
        
        iteration_of_batch_processing
            context
            items
            
        
    let rec skim_and_scroll_iteration
        load_new_item_batch
        (process_item: Parsed_timeline_cell -> Html_node -> Parsed_timeline_cell option)
        (previous_items: list<Html_node>) // sorted 0=top
        (previous_context: Parsed_timeline_cell)
        scrolling_repetitions
        repetitions_left
        =
        
        let new_skimmed_items =
            load_new_item_batch previous_items
                
        match new_skimmed_items with
        |[]->
            if repetitions_left = 0 then
                None
            else
                skim_and_scroll_iteration
                    load_new_item_batch
                    process_item
                    previous_items
                    previous_context
                    scrolling_repetitions
                    (repetitions_left-1)
        
        |new_skimmed_items ->
            let next_context =
                process_item_batch_providing_previous_items
                    previous_context
                    new_skimmed_items
                    process_item
                    
            match next_context with
            |Some next_context ->
                skim_and_scroll_iteration
                    load_new_item_batch
                    process_item
                    new_skimmed_items
                    next_context
                    scrolling_repetitions
                    scrolling_repetitions
            |None->
                None
            
    
    let parse_dynamic_list_with_previous_item
        wait_for_loading
        is_item_needed
        process_item
        browser
        html_parsing_context
        scrolling_repetitions
        item_selector
        =
        let skim_new_visible_items =
            Skim_dynamic_list_updates.skim_new_visible_items
                browser
                html_parsing_context
                is_item_needed
                item_selector
                   
        let load_next_items =
            fun () -> Browser.send_keys [Keys.PageDown;Keys.PageDown;Keys.PageDown] browser
        
        let load_new_item_batch previous_items =
            wait_for_loading()
        
            let new_skimmed_items =
                skim_new_visible_items previous_items
                
            load_next_items()
            
            new_skimmed_items
        
        skim_and_scroll_iteration
            load_new_item_batch
            process_item
            []
            Parsed_timeline_cell.No_cell
            scrolling_repetitions
            scrolling_repetitions
        |>ignore
    
    let collect_items_of_dynamic_list
        browser
        wait_for_loading
        is_item_needed
        item_selector
        =
        let html_parsing_context = AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default
            
        let rec skim_and_scroll_iteration
            (all_items: list<Html_node>)
            =
            wait_for_loading()
            
            let new_skimmed_items =
                Skim_dynamic_list_updates.skim_new_visible_items
                    browser
                    html_parsing_context
                    is_item_needed
                    item_selector
                    all_items
            
            if
                not new_skimmed_items.IsEmpty
            then
                Browser.send_keys [Keys.PageDown;Keys.PageDown;Keys.PageDown] browser
                
                all_items
                @
                new_skimmed_items
                |>skim_and_scroll_iteration
            else
                all_items
        
        skim_and_scroll_iteration []     

    
    let all_items_are_needed _ = true
    
    let collect_all_html_items_of_dynamic_list
        browser
        wait_for_loading
        item_selector
        =
        item_selector
        |>collect_items_of_dynamic_list
            browser
            wait_for_loading
            all_items_are_needed
        
    

    
    



    


