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
        html_parsing_context
        previous_cell
        html_cell
        =
        let parsed_cell =
            Parse_post_from_timeline.try_parse_cell
                html_parsing_context
                previous_cell
                html_cell
        
        match parsed_cell with
        |Parsed_timeline_cell.Post (post, previous_cell) ->
            write_post post
            previous_cell
        |Hidden_post previous_cell ->
            previous_cell
        |Error error ->
            Log.error $"failed to harvest a cell from the timeline: {error}"|>ignore
            Previous_cell.No_cell
            
        
    
    let harvest_timeline
        browser
        (tab: Timeline_tab)
        user
        =
        Browser.open_url $"{Twitter_settings.base_url}/{User_handle.value user}/{tab}" browser
        Reveal_user_page.surpass_content_warning browser
        let html_parsing_context = BrowsingContext.New AngleSharp.Configuration.Default
        use database = Twitter_database.open_connection()
        
        let write_post =
            match tab with
            |Likes ->
                write_liked_post user database
            |_ ->
                Twitter_post_database.write_main_post    
                    database
                
        "div[data-testid='cellInnerDiv']"
        |>Scrape_dynamic_list.parse_dynamic_list_with_previous_item
            browser
            html_parsing_context
            (fun () -> Scrape_posts_from_timeline.wait_for_timeline_loading browser)
            (fun item ->
                item
                |>Html_node.from_html_string
                |>Scrape_posts_from_timeline.cell_contains_post)
            (harvest_timeline_cell write_post html_parsing_context)
            Previous_cell.No_cell
        

        
    
    [<Fact>]//(Skip="manual")
    let ``try harvest_posts_from_timeline``()=
        "MikhailBatin"
        |>User_handle
        |>harvest_timeline
              (Browser.open_browser())
              Timeline_tab.Posts

    
    let harvest_new_posts
        browser
        user