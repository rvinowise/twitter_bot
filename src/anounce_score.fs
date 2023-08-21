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
        
        browser.Manage().Timeouts().ImplicitWait <- (TimeSpan.FromSeconds(3));
        
        canopy.classic.url Twitter_settings.base_url
        browser.Manage().Cookies.AddCookie(authorisation_cookie)
        //Login_to_twitter.login_to_twitter
        
        printfn "browser is open... "
    
    let scrape_followers_of_competitors member_list_id =
        member_list_id
        |>Scrape_followers.scrape_twitter_list_members
        |>Scrape_followers.scrape_followers_of_users    
    
    
    
    [<Fact>]
    let ``try post long scoreboard as a thread on twitter``()=
        let long_scores =
            [
            "useruseruseruseruser1",10,0
            "useruseruseruseruser2",11,1
            "useruseruseruseruser3",12,2
            "useruseruseruseruser4",13,3
            "useruseruseruseruser5",14,4
            "useruseruseruseruser6",15,5
            "useruseruseruseruser7",16,6
            "useruseruseruseruser8",17,7
            "useruseruseruseruser9",18,8
            "useruseruseruseruser10",19,9
            "useruseruseruseruser11",20,10
            ]
            |>List.map (fun (user,score,change)->
                {Twitter_user.handle=User_handle user;name=user},
                score,
                change
            )
        
        prepare_for_scraping ()
        
        long_scores
        |>Format_score_for_twitter.arrange_by_places_in_competition
        |>Format_score_for_twitter.scoreboard_as_unsplittable_chunks
            (DateTime.Now-TimeSpan.FromDays(1))
            DateTime.Now
        |>Post_on_twitter.post_thread_or_single_post
    
    
    
    let scrape_and_announce_score()=
        //let start_time,previous_scores = Scores_database.read_last_scores ()
        
        prepare_for_scraping ()
        
        let new_scores =
            scrape_followers_of_competitors Settings.transhumanist_list
            |>List.ofSeq
        browser.Quit()
    
//        let score_changes =
//            Format_score_for_twitter.score_change_from_two_moments
//                new_scores previous_scores
//            |>Format_score_for_twitter.arrange_by_places_in_competition
                
        let current_time = DateTime.Now
//        Format_score_for_twitter.scoreboard_as_unsplittable_chunks
//            start_time
//            current_time
//            score_changes
//        |>Post_on_twitter.post_thread_or_single_post
        
        new_scores
        |>Seq.map(fun (user,score)->user.handle,score)
        |>Scores_database.write_scores_to_db current_time
        new_scores
        |>List.map fst
        |>Scores_database.write_user_names_to_db
        
        Export_scores_to_googlesheet.update_googlesheet_with_last_scores
            Settings.score_table
            |>ignore
        
//        Export_scores_to_csv.export_score_changes
//            start_time current_time
//            score_changes
        printfn "finish scraping and announcing scores."
        ()

        
    [<Fact>]
    let ``try scrape_and_announce_score``()=
        scrape_and_announce_score ()