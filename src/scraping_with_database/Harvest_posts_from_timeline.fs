namespace rvinowise.twitter

open System
open OpenQA.Selenium
open Xunit
open rvinowise.html_parsing
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
        let parsed_cell =
            Parse_timeline_cell.parse_timeline_cell
                previous_cell
                html_cell
        
        match parsed_cell with
        |Parsed_timeline_cell.Adjacent_post post ->
            if
                is_finished post
            then
                None
            else
                write_post post
                Some parsed_cell
        |Distant_connected_post _ ->
            Some previous_cell
        |Error error ->
            Log.error $"failed to parse a cell from the timeline: {error}"|>ignore
            Some Parsed_timeline_cell.No_cell
        
        |Final_post _ ->
            None
        |Fail_loading_timeline ->
            Log.error "failed to load the timeline"|>ignore
            None
        |No_cell ->
            "a function which parses a timeline cell can't return No_cell as a result"
            |>Harvesting_exception
            |>raise
            
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
        |Parsed_timeline_cell.Adjacent_post post ->
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
            
    let is_advertisement cell_node =
        cell_node
        |>Html_node.descendants "div[data-testid='placementTracking']"
        <> []
    
    let cell_contains_post cell_node =
        cell_node
        |>is_advertisement|>not
        &&
        cell_node
        |>Html_node.try_descendant "article[data-testid='tweet']"
        |>Option.isSome
    
    let wait_for_timeline_loading browser =
        //Browser.sleep 1
        "div[role='progressbar']"
        |>Browser.wait_till_disappearance browser 60 |>ignore
        
    let parse_timeline
        (process_item: Parsed_timeline_cell -> Html_node -> Parsed_timeline_cell option)
        browser
        parsing_context
        scrolling_repetitions
        =
        
        let wait_for_loading = (fun () -> wait_for_timeline_loading browser)
        let is_item_needed =
            is_advertisement>>not
        
        Twitter_settings.timeline_cell_css
        |>Scrape_dynamic_list.parse_dynamic_list_with_previous_item
            wait_for_loading
            is_item_needed
            process_item
            browser
            parsing_context
            scrolling_repetitions
            
    
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
        
        parse_timeline
            harvest_cell_with_counter
            browser
            html_parsing_context
            50
            
        Log.info $"""
        {item_count} posts have been harvested from tab "{Timeline_tab.human_name tab}" of user "{User_handle.value user}".
        """
        item_count
        
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
        
        
        

    let reached_last_visited_post
        last_visited_post
        (post:Main_post)
        =
        post.id = last_visited_post
    
    let finish_after_amount_of_invocations amount =
        let mutable item_count = 0 
        let is_finished = fun _ ->
            item_count <- item_count + 1
            item_count >= amount
        is_finished
        
    let finish_after_time time =
        let start_time = DateTime.Now
        let is_finished = (fun _ ->
             let current_time = DateTime.Now
             if (current_time - start_time > time) then
                 Log.debug $"scraping time elapsed at {current_time}"
                 true
             else false
        )
        is_finished
    
    let only_finish_when_no_posts_left =
        (fun _ -> false)
    
    let finish_when_last_newest_post_reached
        database tab user
        =
        let newest_last_visited_post =
            Twitter_post_database.read_newest_last_visited_post
                database
                tab
                user
        let work_description =
            $"""harvesting new posts on timeline "{Timeline_tab.human_name tab}" of user "{User_handle.value user}"""
        
        let is_finished =
            match newest_last_visited_post with
            |Some last_post ->
                Log.info
                    $"""{work_description}
                    will stop when post "{Post_id.value last_post}" is reached"""
                (reached_last_visited_post last_post)
            |None->
                Log.info
                    $"""{work_description}
                    will stop when the timeline is ended"""
                only_finish_when_no_posts_left
        
        is_finished
    
    let reveal_timeline browser tab user=
        Browser.open_url $"{Twitter_settings.base_url}/{User_handle.value user}/{tab}" browser
        Reveal_user_page.surpass_content_warning browser
    
        
    
    let check_insufficient_scraping browser tab user posts_found =
        let minimum_posts_ratio =
            [
                (*70% of scraped likes, relative to the reported amount by twitter, is OK
                possibly because liked Ads are skipped*)
                Timeline_tab.Likes, 0.7
                
                (*posts_and_replies tab includes the posts to which the reply is made,
                so, it normally has more posts than the targeted user wrote*)
                Timeline_tab.Posts_and_replies, 1
            ]|>Map.ofList
        
        let posts_supposed_amount =
            Scrape_user_social_activity.try_scrape_posts_amount browser
        match posts_supposed_amount with
        |Some amount ->
            if
                (float posts_found)/(float amount) < (minimum_posts_ratio[tab])
            then
                $"""insufficient scraping of timeline {Timeline_tab.human_name tab} of user {User_handle.value user}:
                twitter reports {posts_supposed_amount} posts, but only {posts_found} posts were found,
                which is less than {minimum_posts_ratio[tab]*100.0} %%
                """
                |>Log.error|>ignore
        |None ->
            $"can't read posts amount from timeline {Timeline_tab.human_name tab} of user {User_handle.value user}"
            |>Log.error|>ignore
            
        
        //Log.error $"""insufficient scraping of timeline {tab}: found {posts_found}, but twitter reports {} """    
            
    
    let harvest_updates_on_timeline_of_user
        browser
        database
        (tab: Timeline_tab)
        user
        =
        Log.info $"""started harvesting all new posts on timeline "{Timeline_tab.human_name tab}" of user "{User_handle.value user}" """
        let html_parsing_context = AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default
        
        reveal_timeline browser tab user 
        
        let is_finished =
            only_finish_when_no_posts_left
            //finish_when_last_newest_post_reached database tab user
        
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
        |>check_insufficient_scraping browser tab user
        
        
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
    
    
    [<Fact(Skip="manual")>]//
    let ``try harvest_all_last_actions_of_users (specific tabs)``()=
        
        resilient_step_of_harvesting_timelines
            (Browser.open_browser())
            (Twitter_database.open_connection())
            [
                User_handle "davidasinclair", Timeline_tab.Posts_and_replies
//                    User_handle "BasedBeffJezos", Timeline_tab.Posts_and_replies
//                    User_handle "PeterDiamandis", Timeline_tab.Posts_and_replies
//                    User_handle "sama", Timeline_tab.Posts_and_replies
//                    User_handle "lexfridman", Timeline_tab.Posts_and_replies
//                    
//                    
//                    User_handle "richardDawkins", Timeline_tab.Likes
//                    User_handle "sapinker", Timeline_tab.Likes
//                    User_handle "PeterSinger", Timeline_tab.Likes
//                    User_handle "danieldennett", Timeline_tab.Likes
//
//                    User_handle "CosmicSkeptic", Timeline_tab.Likes
//                    User_handle "seanmcarroll", Timeline_tab.Likes
//                    User_handle "seanmcarroll", Timeline_tab.Posts_and_replies
                
                
            ]
        
    [<Fact(Skip="manual")>]//
    let ``try harvest_all_last_actions_of_users (both tabs)``()=
        let user_timelines =
            [
                 "davidasinclair"
                 "BasedBeffJezos"
                 "PeterDiamandis"
                 "sama"
                 "fedichev"
            ]
            |>List.map User_handle
            |>List.collect (fun user ->
                [
                    user,Timeline_tab.Posts_and_replies;
                    user,Timeline_tab.Likes
                ]
            )
        
        resilient_step_of_harvesting_timelines
            (Browser.open_browser())
            (Twitter_database.open_connection())
            user_timelines
