namespace rvinowise.twitter

open AngleSharp
open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.web_scraping

open FsUnit
open Xunit


module Read_list_updates_as_simple_html =
    
    
    let read_web_items_from_browser
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
        
    let read_items_from_browser
        browser
        html_context
        is_item_needed
        item_selector
        =
        read_web_items_from_browser
            browser
            item_selector
        |>List.map (Html_node.from_html_string_and_context html_context)
        |>List.filter is_item_needed
        |>Array.ofList    
    
    
    
    let read_container_from_browser
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
    
    let read_items_from_container
        is_item_needed
        page_html
        item_selector
        =
        page_html
        |>Html_node.descendants item_selector
        |>List.filter is_item_needed
        |>Array.ofList  
    
    
    
    let new_items_from_visible_items
        items_are_equal
        visible_items
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
                Log.error $"""lists are not overlapping"""
                // full_list={old_items};
                // new_list={visible_items}"""
                |>ignore
                visible_items
            
        |None->
            visible_items
        
    
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
     
    
    
    let skim_new_visible_items
        browser
        html_context
        is_item_needed
        item_selector
        previous_items
        =
        let visible_skimmed_items =
            read_items_from_browser
                browser
                html_context
                is_item_needed
                item_selector
            
        previous_items
        |>new_nodes_from_visible_nodes
              visible_skimmed_items
        |>List.ofArray
        
    