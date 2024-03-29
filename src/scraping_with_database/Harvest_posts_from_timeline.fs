﻿namespace rvinowise.twitter

open System
open Npgsql
open OpenQA.Selenium
open Xunit
open rvinowise.html_parsing
open rvinowise.twitter
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
        (is_finished: Main_post -> bool) // is it the first ignored post?
        previous_cell
        html_cell
        =
        let thread_context =
            Parse_timeline_cell.parse_timeline_cell
                previous_cell
                html_cell
        
        match thread_context with
        |Thread_context.Post post ->
            if
                is_finished post
            then
                None
            else
                write_post post
                Some thread_context
        |Hidden_thread_replies _ |Empty_context ->
            Some previous_cell
        

            
    let harvest_timeline_cell_for_first_post
        database
        tab
        user
        previous_cell
        html_cell
        =
        let parsed_cell =
            Parse_timeline_cell.parse_timeline_cell
                previous_cell
                html_cell
        
        match parsed_cell with
        |Thread_context.Post post ->
            if
                post.is_pinned
            then
                Some parsed_cell
            else
                Twitter_post_database.write_newest_last_visited_post
                    database
                    tab
                    user
                    post.id
                
                Log.info $"""
                the newest post on timeline {Timeline_tab.human_name tab} of user {User_handle.value user}
                is {Post_id.value post.id} by {Main_post.author_handle post}"""
                
                None
        | _ ->
            Some parsed_cell
    
    (* it only detects ADs in the visible browser, but in the headless mode --
    normal video-posts are mistaken for the ADs *)
    let is_advertisement cell_node =
        cell_node
        |>Html_node.descendants "div[data-testid='placementTracking']"
        <> []
    
    
    let wait_for_timeline_loading browser =
        //Browser.sleep 1
        "div[role='progressbar']"
        |>Browser.wait_till_disappearance browser 60 |>ignore
        
    let parse_timeline
        (process_item: Thread_context -> Html_node -> Thread_context option)
        browser
        html_context
        scrolling_repetitions
        =
        let is_item_needed = fun _ -> true
        
        let scrape_visible_items () =
            Scrape_visible_part_of_list.scrape_items
                browser
                html_context
                is_item_needed
                Twitter_settings.timeline_cell_css
                
        let load_new_item_batch =
            Scrape_dynamic_list.load_new_item_batch
                (fun () -> wait_for_timeline_loading browser)
                //(fun() -> ())
                scrape_visible_items
                Read_list_updates.cell_id_from_post_id
                (fun () -> Scrape_dynamic_list.load_next_items browser)
        
        let process_item_batch =
            Read_list_updates.process_item_batch_providing_previous_items
                process_item
        
        
        Scrape_dynamic_list.parse_dynamic_list_with_context
            load_new_item_batch
            process_item_batch
            scrolling_repetitions
            
    
    let harvest_timeline_tab_of_user
        browser
        html_parsing_context
        database
        is_finished
        (tab: Timeline_tab)
        user
        =
        let mutable post_count = 0
        let write_post post =
            post_count <- post_count + 1
            match tab with
            |Timeline_tab.Likes ->
                write_liked_post user database post
            |_ ->
                Twitter_post_database.write_main_post    
                    database post
        
        let mutable cell_count = 0
        let harvest_cell_with_counter item =
            cell_count <- cell_count + 1
            harvest_timeline_cell
                write_post
                (is_finished tab user)
                item
        
        parse_timeline
            harvest_cell_with_counter
            browser
            html_parsing_context
            Settings.repeat_scrolling_timeline
            
        Log.info $"""
        {post_count} posts from {cell_count} cells have been harvested from tab "{Timeline_tab.human_name tab}" of user "{User_handle.value user}".
        """
        post_count
        
    let write_newest_post_on_timeline
        browser
        html_parsing_context
        database
        tab
        user
        =
        Log.info $"""Harvesting newest posts on timeline "{Timeline_tab.human_name tab}" of user "{User_handle.value user}"."""
        parse_timeline
            (harvest_timeline_cell_for_first_post database tab user)
            browser
            html_parsing_context
            0
        
        
        

    
    
    
        
    
    let is_scraping_sufficient
        browser
        tab
        user
        scraped_amount
        =
        
        let minimum_posts_percentage =
            [
                (*70% of scraped likes, relative to the reported amount by twitter, is OK
                possibly because liked Ads are skipped*)
                Timeline_tab.Likes, 70
                
                (*posts_and_replies tab includes the posts to which the reply is made,
                so, it normally has more posts than the targeted user wrote*)
                Timeline_tab.Posts_and_replies, 100
            ]|>Map.ofList
        
        let posts_supposed_amount =
            Scrape_user_social_activity.try_scrape_posts_amount browser
        match posts_supposed_amount with
        |Some supposed_amount ->
            let scraped_percent =
                if (supposed_amount>0) then
                    (float scraped_amount)/(float supposed_amount) * 100.0
                else 100.0
            if
                scraped_percent < minimum_posts_percentage[tab]
            then
                $"""insufficient scraping of timeline {Timeline_tab.human_name tab} of user {User_handle.value user}:
                twitter reports {supposed_amount} posts, but only {scraped_amount} posts were found,
                which is {int scraped_percent}%% and less than needed {minimum_posts_percentage[tab]} %%
                """
                |>Log.error|>ignore
                false
            else true
        |None ->
            $"can't read posts amount from timeline {Timeline_tab.human_name tab} of user {User_handle.value user}"
            |>Log.error|>ignore
            false
            
    
    let harvest_updates_on_timeline_of_user
        is_finished
        browser
        database
        (tab: Timeline_tab)
        user
        =
        Log.info $"""started harvesting all new posts on timeline "{Timeline_tab.human_name tab}" of user "{User_handle.value user}" """
        let html_parsing_context = AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default
        
        Reveal_user_page.reveal_timeline
            browser 
            html_parsing_context
            tab
            user 
        |>ignore
        
        write_newest_post_on_timeline
            browser
            html_parsing_context
            database
            tab
            user
        
        harvest_timeline_tab_of_user
            browser
            html_parsing_context
            database
            is_finished
            tab
            user
        |>is_scraping_sufficient browser tab user
        
    let reveal_and_harvest_timeline
        (browser:Browser)
        html_context
        database
        needed_posts_amount
        timeline_tab
        user
        =
        let when_to_stop  =
            Finish_harvesting_timeline.finish_after_amount_of_invocations needed_posts_amount
        
        match
            Reveal_user_page.reveal_timeline
                browser
                html_context
                timeline_tab
                user
        with
        |Revealed ->
            
            let amount =
                harvest_timeline_tab_of_user
                    browser
                    html_context
                    database
                    when_to_stop
                    timeline_tab
                    user
            if
                amount < needed_posts_amount
                &&
                is_scraping_sufficient
                    browser
                    timeline_tab
                    user
                    amount
                |>not
            then
                Insufficient amount
            else
                Success amount
        |Page_revealing_result.Failed Protected ->
            Log.info $"Timelines of user {User_handle.value user} are protected from strangers."
            Harvesting_timeline_result.Hidden_timeline Protected
        |Page_revealing_result.Failed failure_reason ->
            Harvesting_timeline_result.Hidden_timeline failure_reason
        
        
    
    let rec resiliently_harvest_user_timeline
        (browser: Browser)
        html_context
        database
        needed_posts_amount
        timeline_tab
        user
        =
        let result =
            try
                reveal_and_harvest_timeline
                    browser
                    html_context
                    database
                    needed_posts_amount
                    timeline_tab
                    user
            with
            | :? ArgumentException
            | :? WebDriverException
            | :? PostgresException
            | :? Harvesting_exception as exc ->
                Harvesting_timeline_result.Exception exc 
        
        match result with
        |Success _
        |Hidden_timeline Protected ->
            browser, result
        |Insufficient amount ->
            $"""
            restarting browser and skipping the timeline because of an insufficient amount of scraped posts.
            """
            |>Log.error|>ignore
            
            Assigning_browser_profiles.switch_profile
                    (Central_database.resiliently_open_connection())
                    (This_worker.this_worker_id database)
                    browser,
            result
            
        |Hidden_timeline failing_of_our_browser ->
            
            match failing_of_our_browser with
            |Loading_denied ->
                Log.info $"""
                Timeline {Timeline_tab.human_name timeline_tab} of user {User_handle.value user} didn't load.
                Switching browser profiles"""
            |No_login |_ ->
                Log.info $"""
                browser {browser.profile} is not logged in to twitter.
                Switching browser profiles"""
            
            let new_browser = 
                Assigning_browser_profiles.switch_profile
                    (Central_database.resiliently_open_connection())
                    (This_worker.this_worker_id database)
                    browser
            resiliently_harvest_user_timeline
                new_browser
                html_context
                database
                needed_posts_amount
                timeline_tab
                user
        |Exception exc ->
            Log.error $"""
                can't harvest timeline {Timeline_tab.human_name timeline_tab} of user {User_handle.value user}:
                {exc.GetType()}
                {exc.Message}.
                Restarting scraping browser and skipping the timeline"""|>ignore
            Assigning_browser_profiles.switch_profile
                    (Central_database.resiliently_open_connection())
                    (This_worker.this_worker_id database)
                    browser,
            result
            
        
    let rec resilient_step_of_harvesting_timelines
        is_finished
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
                        is_finished
                        browser
                        database
                        head_timeline
                        head_user
                    |>ignore
                        
                    browser,rest_tabs
                with
                | :? WebDriverException as exc ->
                    Log.error $"""
                    can't harvest timeline {Timeline_tab.human_name head_timeline} of user {User_handle.value head_user}:
                    {exc.Message}.
                    Restarting scraping browser"""|>ignore
                    browser|>Browser.restart,
                    timeline_tabs
        
            match rest_tabs with
            |[]->()
            |timeline_tabs ->
                resilient_step_of_harvesting_timelines
                    is_finished
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
            (Finish_harvesting_timeline.finish_when_last_newest_post_reached database)
            browser
            database
    
    

    let ``try harvest_all_last_actions_of_users (specific tabs)``()=
        
        resiliently_harvest_user_timeline
            (Browser.open_browser())
            (AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default)
            (Local_database.open_connection())
            Int32.MaxValue
            Timeline_tab.Posts_and_replies
            (User_handle "KeithComito")            
        |>ignore

    let ``try harvest_all_last_actions_of_users (both tabs)``()=
        let user_timelines =
            [
                 "DrBrianKeating"
            ]
            |>List.map User_handle
            |>List.collect (fun user ->
                [
                    user,Timeline_tab.Posts_and_replies;
                    user,Timeline_tab.Likes
                ]
            )
        
        resilient_step_of_harvesting_timelines
            Finish_harvesting_timeline.only_finish_when_no_posts_left
            (Browser.open_browser())
            (Local_database.open_connection())
            user_timelines
