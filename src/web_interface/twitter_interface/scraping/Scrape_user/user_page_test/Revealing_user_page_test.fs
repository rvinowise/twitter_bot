namespace rvinowise.twitter.test

open System
open Xunit
open FsUnit
open rvinowise.html_parsing
open rvinowise.twitter
open rvinowise.web_scraping



module revealing_user_page =
    
    
    [<Fact(Skip="integration")>]
    let ``nonexisting user``() =
        //https://twitter.com/0xPolygonLabs
        let nonexisting_user = User_handle "0xPolygonLabs"
        let browser = Browser.open_browser()
        match 
            Reveal_user_page.reveal_timeline
                browser
                (AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default)
                Timeline_tab.Posts_and_replies
                nonexisting_user
        with
        |Page_revealing_result.Failed Nonexisting_user ->
            Log.debug "Page_revealing_result.Failed Nonexisting_user"
            ()
        |wrong_result ->
            raise (Harvesting_exception $"the user {nonexisting_user} shouldn't exist, but it's page is opened with the result {wrong_result}")
        
        browser.webdriver.Quit();