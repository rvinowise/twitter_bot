namespace rvinowise.twitter

open System
open System.Collections.ObjectModel
open System.Configuration
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.classic
open rvinowise.twitter
open Dapper
open FParsec

module Scrape_followers =
    open OpenQA.Selenium.Interactions
    open OpenQA.Selenium.Support.UI

    let wait = WebDriverWait(browser, TimeSpan.FromSeconds(3));
    
    let element_exists selector =
        let old_wait = browser.Manage().Timeouts().ImplicitWait
        browser.Manage().Timeouts().ImplicitWait <- (TimeSpan.FromMilliseconds(0));
        let elements =
            browser.FindElements(By.XPath(
                selector
            ))
        browser.Manage().Timeouts().ImplicitWait <- old_wait
        elements.Count > 0
    
    let wait_for_element
        seconds
        css_selector
        =
        let old_wait = browser.Manage().Timeouts().ImplicitWait
        browser.Manage().Timeouts().ImplicitWait <- (TimeSpan.FromSeconds(seconds))
        let elements =
            browser.FindElements(By.CssSelector(
                css_selector
            ))
        browser.Manage().Timeouts().ImplicitWait <- old_wait
        elements
        |>Seq.tryHead
    
    
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
        
        let table_with_members = wait_for_element 180 "div[aria-label='Timeline: List members']"
        
        match table_with_members with
        |Some table_with_members ->
            let users = consume_all_elements_of_dynamic_list table_with_members
            Log.important $"list has {Seq.length users} members... "
            users
        |None->
            Log.error "Timeline: List members didn't appear, can't read users... "
            Set.empty
        //|>Seq.take 3 //

    let remove_commas (number:string) =
        number.Replace(",", "")
        
    let parse_followers_qty_from_string number =
        let letter_multiplier =
            anyString 1
            |>> (fun multiplier_letter ->
                match multiplier_letter with
                |"K"->1000
                |"M"->1000000
                |strange_letter->
                    Log.error $"parsing a number with a letter encountered a weird letter: {strange_letter}"
                    1
            )
        
        let number_with_letter =
            pfloat .>>. (letter_multiplier <|>% 1)
            |>> (fun (first_number,last_multiplier) ->
                (first_number * float last_multiplier)
                |> int
            )
            
        number
        |>remove_commas
        |>run number_with_letter
        |>function
        |Success (number,_,_) -> number
        |Failure (error,_,_) -> 
            Log.error $"error while parsing number of followers: \n\r{error}"
            0
    
    
    
    
    let page_isnt_showing () =
        element_exists "//*[text()='Something went wrong. Try reloading.']"
    
    let scrape_followers_of_user twitter_user =
        url (Twitter_user.url twitter_user)
        
        let followers_qty_field = $"a[href='/{twitter_user.handle|>User_handle.value}/followers'] span span"
        followers_qty_field
        |>wait_for_element 3
        |>function
        |None->
            Log.error $"url '{Twitter_user.url}' doesn't show the Followers field"
            None
        |Some followers_qty_field->
            followers_qty_field
            |>read|>parse_followers_qty_from_string
            |>Some
            
    let scrape_followers_of_users 
        (users: Twitter_user seq) 
        =
        Log.info "reading amounts of followers of members... "
        users
        |>Seq.map (fun twitter_user ->
            twitter_user
            ,
            scrape_followers_of_user twitter_user
        )|>Seq.map (fun (user,score)->
            match score with
            |None->None
            |Some number->Some (user,number)
        )|>Seq.choose id

    
    

  


    
    



    


