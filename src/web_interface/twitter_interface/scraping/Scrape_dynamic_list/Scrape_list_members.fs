namespace rvinowise.twitter

open System
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open canopy.types
open rvinowise.html_parsing
open rvinowise.web_scraping

module Scrape_list_members =
   
    let wait_for_list_loading browser =
        "div[role='progressbar'] svg circle"
        |>Browser.wait_till_disappearance browser 10 |>ignore
    
    let focus_on_list_for_scrolling
        browser
        =
        browser
        |>Browser.send_key Keys.Tab
        // try
        //     "div[data-testid='app-bar-close']"
        //     |>Browser.focus_element browser
        // with
        // | :? ElementNotInteractableException as exc ->
        //     Log.error $"""failed at focusing element:
        //     {exc.Message}"""
        //     |>ignore
        
    let scrape_twitter_list_members
        browser
        html_context
        list_id 
        = 
        Log.info $"reading members of list {list_id}" 
        
        browser
        |>Browser.open_url
            $"{Twitter_settings.base_url}/i/lists/{list_id}/members"
        
        let table_css = "div[aria-label='Timeline: List members']"
        if
            Browser.try_element_reliably browser table_css
            |>Option.isSome
        then
            focus_on_list_for_scrolling browser

            let users =
                $"{table_css} div[data-testid='UserCell']"
                |>Scrape_dynamic_list.collect_all_html_items_of_dynamic_list
                      browser
                      html_context
                      (fun () -> wait_for_list_loading browser)
                |>List.map Parse_twitter_user.parse_twitter_user_cell
                
            Log.important $"list {list_id} has {Seq.length users} members"
            users
        else
            Log.error "Timeline: Member List didn't appear, can't read users"|>ignore
            []

    
    let scrape_twitter_list_members_amount
        browser
        html_context
        list_id
        = 
        browser
        |>Browser.open_url
            $"{Twitter_settings.base_url}/i/lists/{list_id}"
        
        $"a[href='/i/lists/{list_id}/members']"
        |>Browser.try_element browser 
        |>function
        |Some amount_node ->
            amount_node
            |>Html_node.from_scraped_node_and_context html_context
            |>Html_node.descendents_without_deepening "span"
            |>List.head
            |>Html_node.inner_text
            |>Parsing_twitter_datatypes.parse_abbreviated_number
        |None ->
            $"""List {list_id} doesn't have the claimed amount of members"""
            |>Log.error|>ignore
            0


    let scrape_twitter_list_members_and_amount
        browser
        html_context
        list_id
        =
        let supposed_members =
            scrape_twitter_list_members_amount
                browser
                html_context
                list_id
        
        let members =
            scrape_twitter_list_members
                browser
                html_context
                list_id
        
        if
            List.length members <> supposed_members
        then
            $"list {list_id} should have {supposed_members} members, but {List.length members} were scraped "
            |>Log.error|>ignore
            
        members