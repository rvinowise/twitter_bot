namespace rvinowise.twitter

open System
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open canopy.parallell.functions
open rvinowise.twitter

module Scrape_community_members =
    
    
    let read_community_members browser community_id = 
        Log.info $"reading members of community {community_id} ..." 
        let community_members_url = 
            $"{Twitter_settings.base_url}/i/communities/{community_id}/members"
        url community_members_url browser
        
        let user_web_entries = elements "li[role='listitem']" browser
        let users = 
            user_web_entries
            |>List.map (fun user_entry ->
                let name_field = user_entry.FindElement(By.CssSelector("a[role='link'] div > div > span > span"))
                let handle_field = user_entry.FindElement(By.CssSelector("a[role='link']"))
                let user_name = name_field.Text
                let user_url = handle_field.GetAttribute("href")
                let user_handle = User_handle.handle_from_url user_url
                {Twitter_user.name=user_name; handle=user_handle}
            )
        Log.important $"community has {List.length users} members... "
        users
        //|>List.take 4
        
    
    