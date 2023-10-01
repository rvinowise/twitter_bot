namespace rvinowise.twitter

open System
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open rvinowise.web_scraping

module Scrape_list_members =
    (*
    this module doesn't separate Scraping from Parsing, so,
    it has additional complexity to avoid stale elements
    (they can become stale while we're parsing them).
    A simpler approach is used in "Scrape_dynamic_list":
    it scrapes elements in one read first,
    and then it analyses their preserved HTML content.
    *)
    
    
    let rec keep_skimming_elements_while_valid
        skimmed_elements
        (user_cells: Web_element list)
        =
        match user_cells with
        |[]->skimmed_elements,false
        |user_cell::rest_cells->
            let skimmed_element =
                try
                    let name_field =
                        user_cell.FindElement(
                            By.CssSelector("div>div:nth-child(2)>div>div>div>div>a[role='link']>div>div>span>span:nth-child(1)")
                        )
                    let handle_field =
                        user_cell.FindElement(
                            By.CssSelector("div>div:nth-child(2)>div>div>div>div>a[role='link']")
                        )
                    let user_name = name_field.Text
                    let user_url = handle_field.GetAttribute("href")
                    let user_handle = User_handle.handle_from_url user_url
                    Some (user_handle,user_name)
                    
                with
                | :? StaleElementReferenceException as exc ->
                    None
            match skimmed_element with
            |None -> skimmed_elements,true
            |Some skimmed_element ->
                keep_skimming_elements_while_valid
                    (skimmed_element::skimmed_elements)
                    rest_cells
    
    let rec skim_displayed_elements_till_they_are_not_stale
        previous_skimmed_elements
        (table_with_elements: IWebElement)
        =
        let user_cells = table_with_elements.FindElements(By.CssSelector("div[data-testid='UserCell']"))
        
        let skimmed_elements,had_stale_elements =
            user_cells
            |>List.ofSeq
            |>keep_skimming_elements_while_valid []
        
        let all_skimmed_elements = skimmed_elements@previous_skimmed_elements
        if had_stale_elements then
            skim_displayed_elements_till_they_are_not_stale
                all_skimmed_elements
                table_with_elements
        else
           all_skimmed_elements
            
    let new_elements_are_skimmed
        skimmed_sofar_elements
        new_skimmed_members
        =
        skimmed_sofar_elements
        |>Set.difference new_skimmed_members
        |>Set.isEmpty|>not
            
    let consume_all_elements_of_dynamic_list                 
        browser
        table_with_elements
        =
        Browser.send_key Keys.Tab browser
        
        let rec skim_and_scroll_iteration
            table_with_elements
            skimmed_sofar_elements
            =
            let new_skimmed_members =
                table_with_elements
                |>skim_displayed_elements_till_they_are_not_stale [] 
                |>Set.ofSeq
                
            if new_elements_are_skimmed 
                skimmed_sofar_elements
                new_skimmed_members
            then
                Browser.send_keys [Keys.PageDown;Keys.PageDown;Keys.PageDown] browser
                
                Browser.sleep 1
                
                let progress_bar_css = "div[role='progressbar']"
                
                Browser.wait_for_disappearance progress_bar_css 20 browser
                
                new_skimmed_members
                |>Set.ofSeq
                |>Set.union skimmed_sofar_elements
                |>skim_and_scroll_iteration table_with_elements
                    
            else
                skimmed_sofar_elements
        
        skim_and_scroll_iteration
            table_with_elements
            Set.empty
            
    let scrape_twitter_list_members browser list_id = 
        Log.info $"reading members of list {list_id} ... " 
        let members_url = 
            $"{Twitter_settings.base_url}/i/lists/{list_id}/members"
        Browser.open_url members_url browser
        
        let table_with_members =
            Browser.try_element browser "div[aria-label='Timeline: List members']" 
        
        match table_with_members with
        |Some table_with_members ->
            let users = consume_all_elements_of_dynamic_list browser table_with_members
            Log.important $"list has {Seq.length users} members... "
            users
        |None->
            Log.error "Timeline: List members didn't appear, can't read users... "|>ignore
            Set.empty

    
  


    
    



    


