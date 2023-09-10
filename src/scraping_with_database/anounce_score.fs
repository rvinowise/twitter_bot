namespace rvinowise.twitter


module Anounce_score =
    
    open System
    open OpenQA.Selenium
    open canopy.classic
    open FSharp.Configuration
    open Xunit

    
   
    let scrape_and_announce_user_state()=
        
        let competitors =
            Settings.Competitors.list
            |>Scrape_list_members.scrape_twitter_list_members
        
        let activity_of_competitors =
            competitors
            |>Seq.map (fun (handle,_) ->
                Reveal_user_page.reveal_user_page handle
                handle,
                Scrape_user_social_activity.scrape_user_social_activity
                    handle
            )
            
        use db_connection = Database.open_connection()
        
        competitors
        |>Social_user_database.update_user_names_in_db
            db_connection
        
        activity_of_competitors
        |>Social_activity_database.write_optional_social_activity_of_users
              db_connection
              DateTime.Now
        
        
        
        Export_scores_to_googlesheet.update_googlesheets db_connection

        Log.info "finish scraping and announcing scores."
        ()

        
    [<Fact>]//(Skip="manual")
    let ``try scrape_and_announce_score``()=
        scrape_and_announce_user_state ()