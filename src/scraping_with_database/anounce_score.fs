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
                |>Scrape_user_states.scrape_user_page
            )
            |>List.ofSeq
        browser.Quit()
        user_states
    
    
    let scrape_and_announce_user_state()=
        let new_state =
            scrape_state_of_competitors Settings.Competitors.list
        
        let current_time = DateTime.Now
        
        use social_database = new Social_competition_database()
        new_state
        |>social_database.write_user_states_to_db current_time
        
        Export_scores_to_googlesheet.update_googlesheets social_database

        Log.info "finish scraping and announcing scores."
        ()

        
    [<Fact>]//(Skip="manual")
    let ``try scrape_and_announce_score``()=
        scrape_and_announce_user_state ()