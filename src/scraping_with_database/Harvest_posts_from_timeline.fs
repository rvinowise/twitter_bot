namespace rvinowise.twitter

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
        (is_finished: Main_post -> bool)
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
                is_finished post
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
            
    
    let skip_timelines =
        [
            "rejuicey", None
            "FedorovIdeas", None
            "SayForeverOrg", None
            "SF_Longevity", None
            "AlexLarionov", None
            "madiyar_123", None
            "FeelixNoel", None
            "DanilaImmortal", None
            "Cancel_Aging", Some Timeline_tab.Posts_and_replies
        ]
        |>Map.ofList
    
    let timeline_should_be_harvested
        (timeline_tab:Timeline_tab)
        user
        =
        skip_timelines
        |>Map.tryFind (User_handle.value user)
        |>function
        |None -> true
        |Some skipped_tab ->
            match skipped_tab with
            |None -> false
            |Some particular_skipped_tab ->
                particular_skipped_tab <> timeline_tab
            
        
    
    let harvest_timeline
        (tab: Timeline_tab)
        is_finished
        browser
        database
        user
        =
        Browser.open_url $"{Twitter_settings.base_url}/{User_handle.value user}/{tab}" browser
        Reveal_user_page.surpass_content_warning browser
        let html_parsing_context = BrowsingContext.New AngleSharp.Configuration.Default
        
        let write_post =
            match tab with
            |Likes ->
                write_liked_post user database
            |_ ->
                Twitter_post_database.write_main_post    
                    database
        
        let wait_for_loading = (fun () -> Scrape_posts_from_timeline.wait_for_timeline_loading browser)
        let is_item_needed =
            Html_node.from_html_string>>
            Scrape_posts_from_timeline.cell_contains_post
        let mutable item_count = 0
        let process_item item =
            item_count <- item_count+1
            harvest_timeline_cell write_post is_finished item
        
        let last_cell =
            "div[data-testid='cellInnerDiv']"
            |>Scrape_dynamic_list.parse_dynamic_list_with_previous_item
                browser
                html_parsing_context
                wait_for_loading
                is_item_needed
                process_item
                Previous_cell.No_cell
        
        Log.info $"""
        {item_count} posts have been harvested from tab "{Timeline_tab.human_name tab}" of user "{User_handle.value user}".
        the last timeline cell is {Previous_cell.human_name last_cell}
        """

    [<Fact>]//(Skip="manual")
    let ``try harvest_posts_from_timeline``()=
        "madiyar_123"
        |>User_handle
        |>harvest_timeline
              Timeline_tab.Posts_and_replies
              (fun _ -> false)
              (Browser.open_browser())
              (Twitter_database.open_connection())
        ()
   
    let reached_last_visited_post
        last_visited_post
        (post:Main_post)
        =
        post.id = last_visited_post
    
    let harvest_new_posts_of_user
        (tab: Timeline_tab)
        browser
        database
        user
        =
        Log.info $"""started harvesting all new posts on timeline "{Timeline_tab.human_name tab}" of user "{User_handle.value user}" """
        let is_finished =
            let last_visited_post =
                Twitter_post_database.read_last_visited_post
                    database
                    tab
                    user
            match last_visited_post with
            |Some last_post ->
                Log.info $"""harvesting new posts on timeline "{Timeline_tab.human_name tab}" of user "{User_handle.value user}" will stop when post "{last_visited_post}" is reached"""
                (reached_last_visited_post last_post)
            |None->
                Log.info $"""harvesting new posts on timeline "{Timeline_tab.human_name tab}" of user "{User_handle.value user}" will stop when the timeline is ended"""
                (fun _ -> false)
        
        harvest_timeline
            tab
            is_finished
            browser
            database
            user
        
        ()
        
    
    let harvest_all_last_actions_of_user
        browser
        database
        user
        =
        Log.info $"""started harvesting all last actions of user "{User_handle.value user}" """
        Reveal_user_page.reveal_user_page browser user
        // harvest_new_posts_of_user
        //     Timeline_tab.Posts
        //     browser
        //     database
        //     user
        if timeline_should_be_harvested Timeline_tab.Posts_and_replies user then
            harvest_new_posts_of_user
                Timeline_tab.Posts_and_replies
                browser
                database
                user
        if timeline_should_be_harvested Timeline_tab.Likes user then
            harvest_new_posts_of_user
                Timeline_tab.Likes
                browser
                database
                user
    
    let harvest_all_last_actions_of_users
        browser
        database
        users
        =
        users
        |>List.iter (
            harvest_all_last_actions_of_user
                browser
                database
        )