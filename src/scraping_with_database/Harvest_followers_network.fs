﻿namespace rvinowise.twitter

open System
open System.Runtime.InteropServices.JavaScript
open canopy.classic
open Xunit
open rvinowise.twitter.social_database

module Harvest_followers_network =
    
    
    let harvest_top_of_user_page
        db_connection
        user
        =
        Log.info $"harvesting bio of user {User_handle.value user}"
        let user_briefing =
            Scrape_user_briefing.scrape_user_briefing user
        
        Social_user_database.write_user_briefing
            db_connection
            user_briefing

        let user_activity =
            Scrape_user_social_activity.scrape_user_social_activity
                user
                
        Social_activity_database.write_optional_social_activity_of_user
              db_connection
              DateTime.Now
              user
              user_activity
        
    let log_start_of_step_of_harvesting_acquaintances_network
        unknown_users_around
        =
        Log.info """step_of_harvesting_acquaintances_network started with unknown_users_around= """
        unknown_users_around
        |>Seq.map User_handle.value
        |>String.concat ", "
        |>Log.info  
    
    let harvest_user
        db_connection
        user
        =
        Log.important $"harvesting user {user}"
        harvest_top_of_user_page
            db_connection
            user

        let followees,followers =
            user
            |>Scrape_followers_network.scrape_acquaintances_of_user
        
        Social_following_database.write_social_connections_of_user
            db_connection   
            user
            followees
            followers
        
        Social_following_database.mark_user_as_visited_now
            db_connection
            user
            
        followees,followers
        
    let rec step_of_harvesting_acquaintances_network
        db_connection
        repeat_if_older_than
        unknown_users_around
        =
        log_start_of_step_of_harvesting_acquaintances_network unknown_users_around
        
        match unknown_users_around with
        |[] ->
            Log.info "harvesting acquaintances network has finished because there's no unknown users around"; ()
        |observed_user::rest_unknown_users->
            let followees, followers =
                harvest_user
                    db_connection
                    observed_user
            
            let new_unknown_users_around =
                followers
                |>Set.union followees
                |>Set.filter(fun user->
                    rest_unknown_users
                    |>List.contains user
                    |>not
                )
                |>Seq.append rest_unknown_users
                |>List.ofSeq
            
            
            new_unknown_users_around
            |>List.filter (
                Social_following_database.was_user_visited_recently
                    db_connection
                    (DateTime.Now-repeat_if_older_than)
                >>not
            )
            |>step_of_harvesting_acquaintances_network
                db_connection
                repeat_if_older_than

                
    let harvest_following_network_around_user
        db_connection
        repeat_if_older_than
        root_user
        =
        step_of_harvesting_acquaintances_network
            db_connection
            repeat_if_older_than
            [root_user]
            
    [<Fact>]
    let ``try harvest_following_network_around_user``()=
        Scraping.prepare_for_scraping()
        step_of_harvesting_acquaintances_network
            (Database.open_connection())
            (TimeSpan.FromDays(-1))
            [User_handle "dicortona"]