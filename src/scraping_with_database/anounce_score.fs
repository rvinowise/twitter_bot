namespace rvinowise.twitter


module Anounce_score =
    
    open System
    open OpenQA.Selenium
    open canopy.classic
    open FSharp.Configuration
    open Xunit

    
   
    let scrape_state_of_competitors member_list_id =
        Scraping.prepare_for_scraping ()
        
        let user_states =
            member_list_id
            |>Scrape_list_members.scrape_twitter_list_members
            |>Seq.map (fun user->
                user,
                user
                |>Twitter_user.handle
                |>Scrape_user_social_activity.scrape_user_social_activity
            )
            |>List.ofSeq
        browser.Quit()
        user_states
    
    
    let scrape_and_announce_user_state()=
        let new_state =
            scrape_state_of_competitors Settings.Competitors.list
        
        use social_database = new Social_competition_database()
        
        social_database.write_user_activity_to_db
              DateTime.Now
              new_state
        
        Export_scores_to_googlesheet.update_googlesheets social_database

        Log.info "finish scraping and announcing scores."
        ()

        
    [<Fact>]//(Skip="manual")
    let ``try scrape_and_announce_score``()=
        scrape_and_announce_user_state ()