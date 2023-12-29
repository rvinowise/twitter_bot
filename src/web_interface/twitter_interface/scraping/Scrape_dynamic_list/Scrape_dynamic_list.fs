namespace rvinowise.twitter

open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.web_scraping

open FsUnit
open Xunit


module Scrape_dynamic_list =
    
    
            
        
    let rec skim_and_scroll_iteration
        load_new_item_batch
        (process_item: Thread_context -> Html_node -> Thread_context option)
        (previous_items: list<Html_node>) // sorted 0=top
        (previous_context: Thread_context)
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
                Read_list_updates.process_item_batch_providing_previous_items
                    process_item
                    previous_context
                    new_skimmed_items
                    
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
        let load_next_items =
            fun () -> Browser.send_keys [Keys.PageDown;Keys.PageDown;Keys.PageDown] browser
        
        let load_new_item_batch previous_items =
            wait_for_loading()
        
            let visible_items =
                Scrape_visible_part_of_list.scrape_items
                    browser
                    html_parsing_context
                    is_item_needed
                    item_selector
            
            let new_skimmed_items =
                Read_list_updates.new_items_from_visible_items
                    Read_list_updates.cell_identity_from_postid
                    previous_items
                    visible_items
                
            load_next_items()
            
            new_skimmed_items
        
        skim_and_scroll_iteration
            load_new_item_batch
            process_item
            []
            Thread_context.Empty_context
            scrolling_repetitions
            scrolling_repetitions
        |>ignore
    
    let collect_items_of_dynamic_list
        browser
        html_parsing_context
        wait_for_loading
        is_item_needed
        item_selector
        =
            
        let rec skim_and_scroll_iteration
            (all_items: list<Html_node>)
            =
            wait_for_loading()
            
            let new_skimmed_items =
                Scrape_visible_part_of_list.scrape_items
                    browser
                    html_parsing_context
                    is_item_needed
                    item_selector
                |>Read_list_updates.new_items_from_visible_items
                      Read_list_updates.cell_identity_from_html
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
        html_context
        wait_for_loading
        item_selector
        =
        item_selector
        |>collect_items_of_dynamic_list
            browser
            html_context
            wait_for_loading
            all_items_are_needed
        
    

    
    



    


