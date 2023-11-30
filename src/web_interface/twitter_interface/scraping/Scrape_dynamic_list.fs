namespace rvinowise.twitter

open System
open AngleSharp
open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.web_scraping

open FsUnit
open Xunit


module Scrape_dynamic_list =
    
    
    let skim_displayed_items
        (is_item_needed: Html_string -> bool) 
        browser
        item_css
        =
        use parameters = Scraping_parameters.wait_seconds 60 browser
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
        |>List.filter is_item_needed
        
        

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
        (items_are_equal: 'a -> 'a -> bool)
        (visible_items: 'a array)
        old_items
        =
        let last_known_item =
            old_items
            |>List.tryLast
        match last_known_item with
        |Some last_known_item ->
            visible_items
            |>Array.tryFindIndex (items_are_equal last_known_item)
            |>function
            |Some i_first_previously_known_item ->
                visible_items
                |>Array.splitAt i_first_previously_known_item
                |>snd
                |>Array.tail
            |None->
                Log.error $"""lists are not overlapping, some elements might be missed.
                full_list={old_items};
                new_list={visible_items}"""|>ignore
                visible_items
            
        |None->
            visible_items
        
   
    [<Fact>]
    let ``try unique_items_of_new_collection``() =
        let new_items = [|7;6;5;4;3|]
        let all_items = [5;4;3;2;1]
        new_items_from_visible_items
            (=)
            new_items
            all_items
        |>should equal [|
            7;6
        |]
        
        let new_items = [|5;4;3|]
        let all_items = [5;4;3;2;1;0]
        new_items_from_visible_items
            (=) 
            new_items
            all_items
        |>should equal [||]
    
    
    let html_has_same_text
        (node1: Html_node)
        (node2: Html_node)
        =
        node1
        |>Html_node.remove_parts_not_related_to_text
        |>Html_node.to_string
        |>(=) (
            node2
            |>Html_node.remove_parts_not_related_to_text
            |>Html_node.to_string
        )
        
            
    
    let new_nodes_from_visible_nodes
        new_nodes
        previous_nodes
        =
        new_items_from_visible_items
            html_has_same_text
            new_nodes
            previous_nodes
     
    
    let skim_visible_items
        browser
        is_item_needed
        item_selector
        previous_items
        html_parsing_context
        =
        let visible_skimmed_items =
            skim_displayed_items
                is_item_needed
                browser
                item_selector
            |>List.map (Html_node.from_html_string_and_context html_parsing_context)
            |>Array.ofList
            
        previous_items
        |>new_nodes_from_visible_nodes
              visible_skimmed_items
        |>List.ofArray
    
    let process_items_providing_previous_items
        (context: 'Previous_context)
        items
        (process_item: 'Previous_context -> Html_node -> 'Previous_context * bool)
        =
        let rec iteration_of_batch_processing
            context
            items
            =
            match items with
            |next_item::rest_items ->
                let new_context, is_finished =
                    process_item context next_item
                if is_finished then
                    context,is_finished
                else
                    iteration_of_batch_processing
                        context
                        rest_items
            |[]->context,false
            
        
        iteration_of_batch_processing
            context
            items
            
    
    let parse_dynamic_list_with_previous_item
        browser
        html_parsing_context
        wait_for_loading
        is_item_needed
        (process_item: 'Previous_context -> Html_node -> 'Previous_context * bool )
        (empty_context: 'Previous_context)
        item_selector
        =
        
            
        let rec skim_and_scroll_iteration
            (previous_items: list<Html_node>) // sorted 0=top
            (previous_context: 'Previous_context)
            =
            
            wait_for_loading()
            
            let new_skimmed_items =
                skim_visible_items
                    browser
                    is_item_needed
                    item_selector
                    previous_items
                    html_parsing_context
            
            if
                not new_skimmed_items.IsEmpty
            then
                Browser.send_keys [Keys.PageDown;Keys.PageDown;Keys.PageDown] browser
                
                let previous_context, has_finished =
                    process_items_providing_previous_items
                        previous_context
                        new_skimmed_items
                        process_item
                        
                if (not has_finished) then
                    skim_and_scroll_iteration
                        new_skimmed_items
                        previous_context
        
        skim_and_scroll_iteration [] empty_context
   
    
    let collect_items_of_dynamic_list
        browser
        wait_for_loading
        is_item_needed
        item_selector
        =
        let html_parsing_context = BrowsingContext.New AngleSharp.Configuration.Default
            
        let rec skim_and_scroll_iteration
            (all_items: list<Html_node>)
            =
            
            wait_for_loading()
            
            let new_skimmed_items =
                skim_visible_items
                    browser
                    is_item_needed
                    item_selector
                    all_items
                    html_parsing_context
            
            if
                not new_skimmed_items.IsEmpty
            then
                Browser.send_keys [Keys.PageDown;Keys.PageDown;Keys.PageDown] browser
                
                new_skimmed_items@all_items
                |>skim_and_scroll_iteration
            else
                all_items
        
        skim_and_scroll_iteration []     
    
    let dont_parse_html_item _ _ = ()
    
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
        
    let collect_some_html_items_of_dynamic_list
        browser
        wait_for_loading
        item_selector
        =
        item_selector
        |>collect_items_of_dynamic_list
            browser
            wait_for_loading
            all_items_are_needed
        
    

    
    



    


