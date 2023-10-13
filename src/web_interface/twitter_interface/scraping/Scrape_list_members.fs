namespace rvinowise.twitter

open System
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open rvinowise.html_parsing
open rvinowise.web_scraping

module Scrape_list_members =
   
    let wait_for_list_loading browser =
        "div[role='progressbar'] svg circle"
        |>Browser.wait_till_disappearance browser 10 |>ignore
        
        
    let scrape_twitter_list_members browser list_id = 
        Log.info $"reading members of list {list_id} ... " 
        let members_url = 
            $"{Twitter_settings.base_url}/i/lists/{list_id}/members"
        Browser.open_url members_url browser
        
        let table_css = "div[aria-label='Timeline: List members']"
        let table_with_members =
            Browser.try_element browser table_css
        
        match table_with_members with
        |Some table_with_members ->
            Browser.send_key Keys.Tab browser
            let users =
                $"{table_css} div[data-testid='UserCell']"
                |>Scrape_dynamic_list.collect_all_html_items_of_dynamic_list
                      browser
                      (wait_for_list_loading browser)
                |>List.map (
                    Html_node.from_html_string
                    >>Parse_twitter_user.parse_twitter_user_cell
                )
                
            Log.important $"list has {Seq.length users} members... "
            users
        |None->
            Log.error "Timeline: Member List didn't appear, can't read users... "|>ignore
            []

    
  


    
    



    


