namespace rvinowise.twitter

open System
open Xunit
open rvinowise.web_scraping

module Announce_user_interactions =
    
    let scrape_and_announce_user_interactions browser =
        let database = Twitter_database.open_connection()
        
        let before_scraping_users = DateTime.Now
        let all_users =
            Settings.Competitors.list
            |>Scrape_list_members.scrape_twitter_list_members browser
            |>List.map (Twitter_profile_from_catalog.user >> Twitter_user.handle)
            
            
        let users =
            all_users
            |>List.splitAt(
                let last_harvested_user=
                    all_users
                    |>List.findIndex (fun user -> user = User_handle "OttoMller12")
                last_harvested_user+1
            )|>snd
        
            
        Log.info $"reading list of users took {DateTime.Now-before_scraping_users}"
        
        let before_harvesting_actions = DateTime.Now
        
        Harvest_posts_from_timeline.harvest_all_last_actions_of_users
            browser 
            database
            users
            
        Log.info $"harvesting actions of users took {DateTime.Now-before_harvesting_actions}"
//            
//        
//        let before_updating_googlesheet = DateTime.Now
//        Export_adjacency_matrix.update_googlesheet
//            database
//            
//        Log.info $"updating user interactions in googlesheet took {DateTime.Now-before_updating_googlesheet}"

        Log.info "finish scraping and announcing user interactions."
        ()

    [<Fact>]//(Skip="manual")
    let ``try scrape_and_announce_user_interactions``()=
        Browser.open_browser()
        |>scrape_and_announce_user_interactions

