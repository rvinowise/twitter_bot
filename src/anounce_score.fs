namespace rvinowise.twitter


module Anounce_score =
    
    open System
    open OpenQA.Selenium
    open canopy.classic
    open FSharp.Configuration
    open Xunit

    
    let prepare_for_scraping () =
        canopy.configuration.chromeDir <- System.AppContext.BaseDirectory
        canopy.configuration.firefoxDir <- System.AppContext.BaseDirectory
        
        let authorisation_cookie:Cookie =
            Cookie(
                "auth_token",
                Settings.auth_token,
                ".twitter.com",
                "/",
                DateTime.Now.AddYears(1);
            )
        
        if Settings.headless = true then
            canopy.classic.start canopy.types.BrowserStartMode.ChromeHeadless
        else
            canopy.classic.start canopy.types.BrowserStartMode.Chrome
        
        browser.Manage().Timeouts().ImplicitWait <- (TimeSpan.FromSeconds(3))
        browser.Manage().Timeouts().PageLoad <- (TimeSpan.FromSeconds(180))
        
        canopy.classic.url Twitter_settings.base_url
        browser.Manage().Cookies.AddCookie(authorisation_cookie)
        //Login_to_twitter.login_to_twitter
        
        Log.info "browser is open... "
    
    type User_data = {
        name: string
    }
    let scrape_state_of_competitors member_list_id =
        prepare_for_scraping ()
        
        let user_states =
            member_list_id
            |>Scrape_list_members.scrape_twitter_list_members
            |>Seq.map (fun user->
                user,
                user
                |>Twitter_user.handle
                |>Scrape_user_states.scrape_user_page
            )
            |>List.ofSeq
        browser.Quit()
        user_states
    
    
    let scrape_and_announce_user_state()=
        let new_state =
            scrape_state_of_competitors Settings.transhumanist_list
        
        let current_time = DateTime.Now
        
        new_state
        |>Social_database.write_user_states_to_db current_time
        
        Export_scores_to_googlesheet.update_googlesheets ()
        |>ignore
        
//        Export_scores_to_csv.export_score_changes
//            start_time current_time
//            score_changes
        Log.info "finish scraping and announcing scores."
        ()

        
    [<Fact>]//(Skip="manual")
    let ``try scrape_and_announce_score``()=
        scrape_and_announce_user_state ()