namespace rvinowise.twitter

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
    

            
    let write_published_post //"published" means from the timeline "posts_and_replies"
        database
        publisher
        post
        =
        Twitter_post_database.write_main_post    
            database
            post
        if
            post
            |>Main_post.header
            |>Post_header.author
            |>Twitter_user.handle
            |>(=)publisher
        then
            Twitter_post_database.write_original_post_marker
                database
                post.id
    
    
    
    let harvest_timeline_cell
        (write_post: Main_post -> unit)
        (is_finished: Main_post -> Stopping_reason option) // is it the first ignored post?
        previous_cell
        html_cell
        =
        let thread_context =
            Parse_timeline_cell.parse_timeline_cell
                previous_cell
                html_cell
        
        match thread_context with
        |Thread_context.Post post ->
            match is_finished post with
            |Some stopping_reason ->
                Result_of_timeline_cell_processing.Should_stop stopping_reason
            |None ->
                write_post post
                Result_of_timeline_cell_processing.Scraped_post thread_context
        |Hidden_thread_replies _ |Empty_context ->
            Result_of_timeline_cell_processing.Scraped_post previous_cell
        

            
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
        (process_item: Thread_context -> Html_node -> Result_of_timeline_cell_processing)
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
            
    
    let parse_timeline_with_counting
        browser
        html_parsing_context
        is_finished
        parse_post
        =
        let mutable post_count = 0
        
        let parse_post_with_counting post =
            post_count <- post_count + 1
            parse_post post
        
        let mutable cell_count = 0
        let parse_cell_with_counting item =
            cell_count <- cell_count + 1
            harvest_timeline_cell
                parse_post_with_counting
                is_finished
                item
        
        let result =
            parse_timeline
                parse_cell_with_counting
                browser
                html_parsing_context
                Settings.repeat_scrolling_timeline
        
        post_count,cell_count,result
        
        
    
    let all_posts_were_scraped
        browser
        tab
        user
        scraped_posts_amount
        scraped_cells_amount
        =
        
        Log.info $"""
        {scraped_posts_amount} posts from {scraped_cells_amount} cells have been harvested from tab "{Timeline_tab.human_name tab}" of user "{User_handle.value user}".
        """
        
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
                    (float scraped_posts_amount)/(float supposed_amount) * 100.0
                else 100.0
            if
                scraped_percent < minimum_posts_percentage[tab]
            then
                $"""insufficient scraping of timeline {Timeline_tab.human_name tab} of user {User_handle.value user}:
                twitter reports {supposed_amount} posts, but only {scraped_posts_amount} posts were found,
                which is {int scraped_percent}%% and less than needed {minimum_posts_percentage[tab]} %%
                """
                |>Log.error|>ignore
                false
            else true
        |None ->
            $"can't read posts amount from timeline {Timeline_tab.human_name tab} of user {User_handle.value user}"
            |>Log.error|>ignore
            false
            
    
    let write_post_to_db
        database
        timeline_tab
        user
        post
        =
        match timeline_tab with
        |Timeline_tab.Likes ->
            write_liked_post user database post
        |Timeline_tab.Posts_and_replies ->
            write_published_post database user post
            
        |unknown_tab ->
            raise (Harvesting_exception($"unknown tab: {unknown_tab}"))
    
    
    
    let reveal_and_harvest_timeline
        (browser:Browser)
        html_context
        database
        maximum_posts_amount
        timeline_tab
        user
        =
        let familiar_posts_streak = 5;
        
        let is_invoked_many_times =
            Finish_harvesting_timeline.finish_after_amount_of_invocations maximum_posts_amount
        
        let has_encountered_a_streak_of_familiar_posts =
            match timeline_tab with
            |Timeline_tab.Likes ->
                Finish_harvesting_timeline.finish_when_encountered_familiar_liked_posts
                    database
                    user
                    familiar_posts_streak
            |Timeline_tab.Posts_and_replies ->
                Finish_harvesting_timeline.finish_when_encountered_familiar_published_posts
                    database
                    familiar_posts_streak
            |_-> raise (Harvesting_exception("unknown timeline tab"))
        
        let finish_at_familiar_posts_or_too_many_posts
            post
            =
            match is_invoked_many_times() with
            |Some rason_to_stop ->
                $"{maximum_posts_amount} posts were scraped forom timeline {timeline_tab} of user {user}, it's enough"
                |>Log.info
                Some rason_to_stop
            |None ->
                match
                    has_encountered_a_streak_of_familiar_posts post
                with
                |Some reason ->
                    $"{familiar_posts_streak} familiar posts in a row were scraped forom timeline {timeline_tab} of user {user}, it's enough"
                    |>Log.info
                    Some reason
                |None ->
                    None
        
        match
            Reveal_user_page.reveal_timeline
                browser
                html_context
                timeline_tab
                user
        with
        |Revealed ->
            
            let
                posts_amount,
                cells_amount,
                result
                    =
                    parse_timeline_with_counting
                        browser
                        html_context
                        finish_at_familiar_posts_or_too_many_posts
                        (write_post_to_db database timeline_tab user)
            if
                result = Result_of_timeline_cell_processing.Should_stop Stopping_reason.No_more_posts_appeared
                &&
                all_posts_were_scraped
                    browser
                    timeline_tab
                    user
                    posts_amount
                    cells_amount
                |>not
            then
                Insufficient posts_amount
            else
                Success posts_amount
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
        $"start harvesting timeline {timeline_tab} or user {user}"
        |>Log.info
        
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
            
        
    
    let ``try harvest_all_last_actions_of_users (specific tabs)``()=
        
        resiliently_harvest_user_timeline
            (Browser.open_browser())
            (AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default)
            (Local_database.open_connection())
            Int32.MaxValue
            Timeline_tab.Posts_and_replies
            (User_handle "KeithComito")            
        |>ignore

