namespace rvinowise.twitter

open System
open Xunit
open rvinowise.web_scraping

module announce_score =
    
   
    let scrape_and_announce_user_state browser html_context =
        
        
        let before_scraping_competitors = DateTime.Now
        let competitors =
            Settings.Competitors.list
            |>Scrape_list_members.scrape_twitter_list_members
                  browser
                  html_context
            |>Seq.map Twitter_profile_from_catalog.user
        
        Log.info $"reading list of competitors took {DateTime.Now-before_scraping_competitors}"
        
        let before_scraping_activity = DateTime.Now
        let activity_of_competitors =
            competitors
            |>Seq.map (fun user ->
                let handle = user.handle
                Reveal_user_page.reveal_user_page browser handle
                handle,
                Scrape_user_social_activity.scrape_user_social_activity browser
                    handle
            )
        Log.info $"reading activity of competitors took {DateTime.Now-before_scraping_activity}"
            
        use db_connection = Twitter_database.open_connection()
        
        let before_updating_names = DateTime.Now
        competitors
        |>Social_user_database.update_user_names_in_db
            db_connection
        Log.info $"updating names of competitors in DB took {DateTime.Now-before_updating_names}"
        
        let before_writing_activity = DateTime.Now
        activity_of_competitors
        |>Social_activity_database.write_optional_social_activity_of_users
              db_connection
              DateTime.Now
        Log.info $"writing activity of competitors to DB took {DateTime.Now-before_writing_activity}"
        
        let before_updating_googlesheet = DateTime.Now
        Export_scores_to_googlesheet.update_googlesheets db_connection
        Log.info $"updating scores in googlesheet took {DateTime.Now-before_updating_googlesheet}"

        Log.info "finish scraping and announcing scores."
        ()

    [<Fact(Skip="manual")>]//
    let ``try scrape_and_announce_user_state``()=
        Browser.open_browser()
        |>scrape_and_announce_user_state

