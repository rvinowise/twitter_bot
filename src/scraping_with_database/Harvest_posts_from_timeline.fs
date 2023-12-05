﻿namespace rvinowise.twitter

open System
open AngleSharp
open BenchmarkDotNet.Engines
open OpenQA.Selenium
open Xunit
open canopy.types
open rvinowise.html_parsing
open rvinowise.twitter.Parse_post_from_timeline
open rvinowise.twitter.Parse_segments_of_post
open rvinowise.web_scraping

module Harvest_posts_from_timeline =
    
    let write_liked_post
        liker
        database
        post
        =
        Twitter_post_database.write_main_post    
            database
            post
        Twitter_post_database.write_like
            database
            liker
            post.id
    
    let harvest_timeline_cell
        (write_post: Main_post -> unit)
        (is_first_ignored_post: Main_post -> bool)
        previous_cell
        html_cell
        =
        let parsed_cell =
            Parse_post_from_timeline.try_parse_cell
                previous_cell
                html_cell
        
        match parsed_cell with
        |Parsed_timeline_cell.Post (post, previous_cell) ->
            if
                is_first_ignored_post post
            then
                previous_cell,true
            else
                write_post post
                previous_cell,false
        |Hidden_post previous_cell ->
            previous_cell,false
        |Error error ->
            Log.error $"failed to harvest a cell from the timeline: {error}"|>ignore
            Previous_cell.No_cell,false
    
    let harvest_timeline_cell_for_first_post
        database
        user
        tab
        previous_cell
        html_cell
        =
        let parsed_cell =
            Parse_post_from_timeline.try_parse_cell
                previous_cell
                html_cell
        
        match parsed_cell with
        |Parsed_timeline_cell.Post (post, previous_cell) ->
            if
                post.is_pinned
            then
                previous_cell,false
            else
                Twitter_post_database.write_newest_last_visited_post
                    database
                    tab
                    user
                    post
                    
                previous_cell,true
                
        |Hidden_post previous_cell ->
            previous_cell,false
        |Error error ->
            Log.error $"failed to harvest a cell from the timeline: {error}"|>ignore
            Previous_cell.No_cell,false
            
    let parse_timeline
        process_item
        browser
        parsing_context
        =
        
        let wait_for_loading = (fun () -> Scrape_posts_from_timeline.wait_for_timeline_loading browser)
        let is_item_needed =
            Html_node.from_html_string>>
            Scrape_posts_from_timeline.cell_contains_post
        
        "div[data-testid='cellInnerDiv']"
        |>Scrape_dynamic_list.parse_dynamic_list_with_previous_item
            wait_for_loading
            is_item_needed
            process_item
            Previous_cell.No_cell
            browser
            parsing_context
            
    
    let harvest_timeline_tab_of_user
        browser
        html_parsing_context
        database
        is_finished
        (tab: Timeline_tab)
        user
        =
        let write_post =
            match tab with
            |Likes ->
                write_liked_post user database
            |_ ->
                Twitter_post_database.write_main_post    
                    database
        
        
        let mutable item_count = 0
        let harvest_cell_with_counter item =
            item_count <- item_count+1
            harvest_timeline_cell write_post is_finished item
        
        let last_cell =
            parse_timeline
                harvest_cell_with_counter
                browser
                html_parsing_context
        Log.info $"""
        {item_count} posts have been harvested from tab "{Timeline_tab.human_name tab}" of user "{User_handle.value user}".
        the last timeline cell is {Previous_cell.human_name last_cell}
        """
   
//    let write_newest_post_on_timeline
//        browser
//        html_parsing_context
//        database
//        tab
//        user
//        =
//        let remember_as_first_post
//            post
//            =
//            if post.is_pinned then
//                ()//Previous_cell.No_cell, false
//            else
//                Twitter_post_database.write_newest_last_visited_post
//                    database
//                    tab
//                    user
//                    post
//                ()
//                //Previous_cell.No_cell, true
//        
//        let is_post_after_the_first_one post =
//            if post.is_pinned then
//                Previous_cell.No_cell, false
//            else
//                Previous_cell.No_cell, false
        
//        let harvest_first_post
//            previous_cell
//            html_node
//            =
//            
//        
//        parse_timeline
//            (harvest_timeline_cell remember_as_first_post is_post_after_the_first_one)
//            browser
//            html_parsing_context
        
        

    let reached_last_visited_post
        last_visited_post
        (post:Main_post)
        =
        post.id = last_visited_post
    
    
            
            
    
    let harvest_updates_on_timeline_of_user
        browser
        database
        (tab: Timeline_tab)
        user
        =
        Log.info $"""started harvesting all new posts on timeline "{Timeline_tab.human_name tab}" of user "{User_handle.value user}" """
        let is_finished =
            let newest_last_visited_post =
                Twitter_post_database.read_newest_last_visited_post
                    database
                    tab
                    user
            match newest_last_visited_post with
            |Some last_post ->
                Log.info $"""
                harvesting new posts on timeline "{Timeline_tab.human_name tab}" of user "{User_handle.value user}"
                will stop when post "{newest_last_visited_post}" is reached"""
                (reached_last_visited_post last_post)
            |None->
                Log.info $"""
                harvesting new posts on timeline "{Timeline_tab.human_name tab}" of user "{User_handle.value user}"
                will stop when the timeline is ended"""
                (fun _ -> false)
        
        let html_parsing_context = BrowsingContext.New AngleSharp.Configuration.Default
        
        Browser.open_url $"{Twitter_settings.base_url}/{User_handle.value user}/{tab}" browser
        Reveal_user_page.surpass_content_warning browser
        
//        write_newest_post_on_timeline
//            browser
//            html_parsing_context
//            database
        
        harvest_timeline_tab_of_user
            browser
            html_parsing_context
            database
            is_finished
            tab
            user
        
        ()
    
    let rec resilient_step_of_harvesting_timelines
        (browser: Browser)
        database
        (timeline_tabs: (User_handle*Timeline_tab) list)
        =
        match timeline_tabs with
        |[]->()
        |(head_user, head_timeline)::rest_tabs ->
        
            let browser,rest_tabs =
                try
                    harvest_updates_on_timeline_of_user
                        browser
                        database
                        head_timeline
                        head_user
                        
                    browser,rest_tabs
                with
                | :? WebDriverException as exc ->
                    Log.error $"""
                    can't harvest timeline {Timeline_tab.human_name head_timeline} of user {User_handle.value head_user}:
                    {exc.Message}.
                    Restarting scraping browser"""|>ignore
                    browser.restart()
                    browser,timeline_tabs
        
            match rest_tabs with
            |[]->()
            |timeline_tabs ->
                resilient_step_of_harvesting_timelines
                    browser
                    database
                    timeline_tabs
    
    let harvest_all_last_actions_of_users
        browser
        database
        users
        =
        users
        |>List.collect(fun user->
            [
                user,Timeline_tab.Posts_and_replies
                user,Timeline_tab.Likes
            ]
        )
        |>resilient_step_of_harvesting_timelines
            browser
            database
