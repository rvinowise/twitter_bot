namespace rvinowise.twitter

open System
open OpenQA.Selenium
open Xunit
open canopy.types
open rvinowise.html_parsing
open rvinowise.twitter.Parse_segments_of_post
open rvinowise.web_scraping

module Harvest_posts_from_timeline =
    
    
    
    
    let harvest_timeline_cell
        database
        html_parsing_context
        thread
        html_cell
        =
        let parsed_cell =
            Parse_post_from_timeline.try_parse_cell
                html_parsing_context
                thread
                html_cell
        
        match parsed_cell with
        |Post (post,thread) ->
            Twitter_post_database.write_main_post    
                database
                post
            thread
        |Hidden_post thread ->
            thread
        |Some (Error error) ->
            Timeline_thread.No_thread
            
        
    
    let harvest_timeline
        browser
        (tab: Timeline_tab)
        user
        =
        Browser.open_url $"{Twitter_settings.base_url}/{User_handle.value user}/{tab}" browser
        Reveal_user_page.surpass_content_warning browser
        use database = Twitter_database.open_connection()
        "div[data-testid='cellInnerDiv']"
        |>Scrape_dynamic_list.parse_dynamic_list_with_previous_item
            browser
            (fun () -> Scrape_posts_from_timeline.wait_for_timeline_loading browser)
            (fun item ->
                item
                |>Html_node.from_html_string
                |>Scrape_posts_from_timeline.cell_contains_post)
            (harvest_timeline_cell database)
            Previous_cell.No_cell
        
    
    
    
    [<Fact(Skip="manual")>]
    let ``try harvest_timeline``()=
        let posts = 
            harvest_timeline
                (
                    Settings.auth_tokens
                    |>Seq.head
                    |>Browser.prepare_authentified_browser
                )
                Timeline_tab.Posts
                (User_handle "HilzFuld")
            
        let results = List.map snd posts
        let errors =
            results
            |>List.filter Result.isError
        ()
    
    
    let harvest_posts_from_timeline
        (timeline_tab: Timeline_tab)
        user
        =
        let posts = 
            harvest_timeline
                (Browser.open_browser())
                timeline_tab
                user
        
        let results = List.map snd posts
        let errors =
            results
            |>List.filter Result.isError
        let good_posts = 
            results
            |>List.choose Result.toOption
            
        use database = Twitter_database.open_connection()
        good_posts
        |>List.iter (fun post->
            Twitter_post_database.write_main_post    
                database
                post
        )
        
        if timeline_tab = Timeline_tab.Likes then
            good_posts
            |>List.iter (fun liked_post ->
                Twitter_post_database.write_like
                    database
                    user
                    liked_post.id
            )
    
        
    
    [<Fact>]//(Skip="manual")
    let ``try harvest_posts_from_timeline``()=
       "dicortona"
        |>User_handle
        |>harvest_posts_from_timeline Timeline_tab.Likes
   