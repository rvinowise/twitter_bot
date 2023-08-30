namespace rvinowise.twitter

open System
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open canopy.classic
open rvinowise.twitter

module Scrape_list_members =
    
    
    let read_community_members community_id = 
        Log.info $"reading members of community {community_id} ..." 
        let community_members_url = 
            $"{Twitter_settings.base_url}/i/communities/{community_id}/members"
        url community_members_url
        
        let user_web_entries = elements "li[role='listitem']"
        let users = 
            user_web_entries
            |>List.map (fun user_entry ->
                let name_field = user_entry.FindElement(By.CssSelector("a[role='link'] div > div > span > span"))
                let handle_field = user_entry.FindElement(By.CssSelector("a[role='link']"))
                let user_name = name_field.Text
                let user_url = handle_field.GetAttribute("href")
                let user_handle = User_handle (Uri(user_url).Segments|>Array.last)
                {Twitter_user.name=user_name; handle=user_handle}
            )
        Log.important $"community has {List.length users} members... "
        users
        //|>List.take 4
        
    
    let rec keep_skimming_elements_while_valid
        (skimmed_elements: Twitter_user list)
        (user_cells: IWebElement list)
        =
        match user_cells with
        |[]->skimmed_elements,false
        |user_cell::rest_cells->
            let skimmed_element =
                try
                    let name_field = user_cell.FindElement(
                        By.CssSelector("div>div:nth-child(2)>div>div>div>div>a[role='link']>div>div>span>span:nth-child(1)"))
                    let handle_field = user_cell.FindElement(
                        By.CssSelector("div>div:nth-child(2)>div>div>div>div>a[role='link']"))
                    let user_name = name_field.Text
                    let user_url = handle_field.GetAttribute("href")
                    let user_handle = User_handle (Uri(user_url).Segments|>Array.last;)
                    Some {Twitter_user.name=user_name; handle=user_handle}
                    
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
        (previous_skimmed_elements: Twitter_user list)
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
        (skimmed_sofar_elements: Twitter_user Set)
        (new_skimmed_members: Twitter_user Set)
        =
        skimmed_sofar_elements
        |>Set.difference new_skimmed_members
        |>Set.isEmpty|>not
            
    let consume_all_elements_of_dynamic_list
        table_with_elements
        =
        let actions = Actions(browser)
        actions.SendKeys(Keys.Tab).Perform()
        let rec skim_and_scroll_iteration
            table_with_elements
            (skimmed_sofar_elements: Twitter_user Set)
            =
            let new_skimmed_members =
                table_with_elements
                |>skim_displayed_elements_till_they_are_not_stale [] 
                |>Set.ofSeq
                
            if new_elements_are_skimmed 
                skimmed_sofar_elements
                new_skimmed_members
            then
                actions
                    .SendKeys(Keys.PageDown).SendKeys(Keys.PageDown).SendKeys(Keys.PageDown)
                    .Perform()
                sleep 1
                let progress_bar_css = "div[role='progressbar']"
                WebDriverWait(browser, TimeSpan.FromSeconds(20)).
                    Until(ExpectedConditions.InvisibilityOfElementLocated(By.CssSelector(progress_bar_css)))
                |>ignore
                
                new_skimmed_members
                |>Set.ofSeq
                |>Set.union skimmed_sofar_elements
                |>skim_and_scroll_iteration table_with_elements
                    
            else
                skimmed_sofar_elements
        
        skim_and_scroll_iteration
            table_with_elements
            Set.empty
            
    let scrape_twitter_list_members list_id = 
        Log.info $"reading members of list {list_id} ... " 
        let members_url = 
            $"{Twitter_settings.base_url}/i/lists/{list_id}/members"
        url members_url
        
        let table_with_members = Scraping.try_element "div[aria-label='Timeline: List members']"
        
        match table_with_members with
        |Some table_with_members ->
            let users = consume_all_elements_of_dynamic_list table_with_members
            Log.important $"list has {Seq.length users} members... "
            users
        |None->
            Log.error "Timeline: List members didn't appear, can't read users... "|>ignore
            Set.empty
        //|>Seq.take 3 //

    
  


    
    



    


