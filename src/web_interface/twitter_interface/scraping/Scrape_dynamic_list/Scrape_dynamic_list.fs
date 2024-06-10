namespace rvinowise.twitter

open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.web_scraping

open FsUnit
open Xunit


module Scrape_dynamic_list =
    
    
            
        
    let rec scraping_list_iteration
        load_new_item_batch
        process_item_batch
        (previous_items: list<'Item>) // sorted 0=top
        (previous_context: Thread_context)
        scrolling_repetitions
        repetitions_left
        =
        
        let new_skimmed_items =
            load_new_item_batch previous_items
                
        match new_skimmed_items with
        |[]->
            if repetitions_left = 0 then
                Result_of_timeline_cell_processing.Should_stop Stopping_reason.No_more_posts_appeared
            else
                scraping_list_iteration
                    load_new_item_batch
                    process_item_batch
                    previous_items
                    previous_context
                    scrolling_repetitions
                    (repetitions_left-1)
        
        |new_skimmed_items ->
            let result =
                process_item_batch
                    previous_context
                    new_skimmed_items
                    
            match result with
            |Result_of_timeline_cell_processing.Scraped_post next_context ->
                scraping_list_iteration
                    load_new_item_batch
                    process_item_batch
                    new_skimmed_items
                    next_context
                    scrolling_repetitions
                    scrolling_repetitions
            |Result_of_timeline_cell_processing.Should_stop stopping_reason ->
                Result_of_timeline_cell_processing.Should_stop stopping_reason
            
   
    let load_next_items browser =
        Browser.send_keys [Keys.PageDown;Keys.PageDown;Keys.PageDown] browser
     
    let load_new_item_batch
        wait_for_loading
        scrape_items
        item_id
        load_next_items
        previous_items
        =
        wait_for_loading()
    
        let visible_items = scrape_items()
        
        let new_skimmed_items =
            Read_list_updates.new_items_from_visible_items
                item_id
                previous_items
                visible_items
            
        load_next_items()
        
        new_skimmed_items 
    
    let parse_dynamic_list_with_context
        load_new_item_batch
        process_item_batch
        scrolling_repetitions
        =
        scraping_list_iteration
            load_new_item_batch
            process_item_batch
            []
            Thread_context.Empty_context
            scrolling_repetitions
            scrolling_repetitions
    
    
    let collect_all_html_items_of_dynamic_list
        browser
        html_context
        wait_for_loading
        item_selector
        =
        let scrape_visible_items () =
            Scrape_visible_part_of_list.scrape_items
                browser
                html_context
                (fun _ -> true)
                item_selector 
            
        let load_new_item_batch =
            load_new_item_batch
                wait_for_loading
                scrape_visible_items
                Read_list_updates.cell_id_from_list_user
                (fun () -> load_next_items browser)
        
        let items = ResizeArray<Html_node>()
        let remember_item _ item =
            items.Add item
            Result_of_timeline_cell_processing.Scraped_post Empty_context
            
        scraping_list_iteration
            load_new_item_batch
            (Read_list_updates.process_item_batch_providing_previous_items remember_item)
            []
            Thread_context.Empty_context
            1
            1
        |>ignore
        
        items

    
    



    


