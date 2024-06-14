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
    
    
    
    
            
        
    


