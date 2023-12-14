namespace rvinowise.twitter

open System
open AngleSharp
open BenchmarkDotNet.Engines
open OpenQA.Selenium
open Xunit
open canopy.types
open rvinowise.html_parsing
open rvinowise.twitter.Parse_timeline_cell
open rvinowise.twitter.Parse_article
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
            Parse_timeline_cell.try_parse_cell
                previous_cell
                html_cell
        
        match parsed_cell with
        |Parsed_timeline_cell.Post (post, cell_summary) ->
            if
                is_first_ignored_post post
            then
                None
            else
                write_post post
                Some cell_summary
        |Hidden_post previous_cell ->
            Some previous_cell
        |Error error ->
            Log.error $"failed to harvest a cell from the timeline: {error}"|>ignore
            Some Previous_cell.No_cell
    
    let harvest_timeline_cell_for_first_post
        database
        tab
        user
        previous_cell
        html_cell
        =
        let parsed_cell =
            Parse_timeline_cell.try_parse_cell
                previous_cell
                html_cell
        
        match parsed_cell with
        |Parsed_timeline_cell.Post (post, cell_summary) ->
            if
                post.is_pinned
            then
                Some cell_summary
            else
                Twitter_post_database.write_newest_last_visited_post
                    database
                    tab
                    user
                    post.id
                
                let author =
                    post
                    |>Main_post.header
                    |>Post_header.author
                
                Log.info $"""
                the newest post on timeline {Timeline_tab.human_name tab} of user {User_handle.value user}
                is {Post_id.value post.id} by {User_handle.value author.handle}"""
                
                None
                
        |Hidden_post previous_cell ->
            Some previous_cell
        |Error error ->
            Log.error $"failed to harvest a cell from the timeline: {error}"|>ignore
            Some Previous_cell.No_cell
            
            
    let parse_timeline
        (process_item: Previous_cell -> Html_node -> Previous_cell option)
        browser
        parsing_context
        scrolling_repetitions
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
            5
            
        Log.info $"""
        {item_count} posts have been harvested from tab "{Timeline_tab.human_name tab}" of user "{User_handle.value user}".
        """
   
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
    
            
    
    let harvest_updates_on_timeline_of_user
        browser
        database
        (tab: Timeline_tab)
        user
        =
        Log.info $"""started harvesting all new posts on timeline "{Timeline_tab.human_name tab}" of user "{User_handle.value user}" """
        
        let is_finished = (fun _ -> false)
        
        // let start_time = DateTime.Now
        // let is_finished = (fun _ ->
        //      let current_time = DateTime.Now
        //      if (current_time - start_time > TimeSpan.FromMinutes(5)) then
        //          Log.debug $"scraping time elapsed at {current_time}"
        //          true
        //      else false
        // )
        //TEST
//            let newest_last_visited_post =
//                Twitter_post_database.read_newest_last_visited_post
//                    database
//                    tab
//                    user
//            let work_description =
//                $"""harvesting new posts on timeline "{Timeline_tab.human_name tab}" of user "{User_handle.value user}"""
//            
//            match newest_last_visited_post with
//            |Some last_post ->
//                Log.info
//                    $"""{work_description}
//                    will stop when post "{Post_id.value last_post}" is reached"""
//                (reached_last_visited_post last_post)
//            |None->
//                Log.info
//                    $"""{work_description}
//                    will stop when the timeline is ended"""
//                (fun _ -> false)
        
        let html_parsing_context = BrowsingContext.New AngleSharp.Configuration.Default
        
        Browser.open_url $"{Twitter_settings.base_url}/{User_handle.value user}/{tab}" browser
        Reveal_user_page.surpass_content_warning browser
        
        // write_newest_post_on_timeline
        //     browser
        //     html_parsing_context
        //     database
        //     tab
        //     user
        
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
    
    
    [<Fact(Skip="manual")>]//
    let ``try harvest_all_last_actions_of_users``()=
        //Likes of petrenko_ai are skipped
        //Likes of SciFi_by_Allen are skipped
        //"Likes" of user "EnriqueSegarra_"
        //"Posts_and_replies" of user "GStolyarovII" ?
        //"Posts_and_replies" of user "Timrael" ?
        //Likes of user ValleeRl (half way -- fail with a twitter-event)
        //"Posts_and_replies" of user "fedichev" ?
        //"Posts_and_replies" of user "turchin". (scraped enough, but not all) 70%
        //"Posts_and_replies" of user "vadbars". (scraped enough, but not all) 70%
        //"Likes" of user "irat1onal".
        
        let result =
            resilient_step_of_harvesting_timelines
                (Browser.open_browser())
                (Twitter_database.open_connection())
                [
                    User_handle "petrenko_ai", Timeline_tab.Likes
                    User_handle "SciFi_by_Allen", Timeline_tab.Likes
                    User_handle "EnriqueSegarra_", Timeline_tab.Likes
                    User_handle "GStolyarovII", Timeline_tab.Posts_and_replies
                    User_handle "Timrael", Timeline_tab.Posts_and_replies
                    User_handle "ValleeRl", Timeline_tab.Likes
                    User_handle "fedichev", Timeline_tab.Posts_and_replies
                    User_handle "turchin", Timeline_tab.Posts_and_replies
                    User_handle "vadbars", Timeline_tab.Posts_and_replies
                    User_handle "irat1onal", Timeline_tab.Likes
                    
                    //User_handle "tehprom269887", Timeline_tab.Likes
                    
                ]
        ()