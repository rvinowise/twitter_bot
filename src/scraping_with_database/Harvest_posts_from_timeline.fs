namespace rvinowise.twitter

open System
open OpenQA.Selenium
open Xunit
open canopy.types
open rvinowise.web_scraping

module Harvest_posts_from_timeline =
    
    
    let harvest_posts_from_timeline
        (timeline_tab: Timeline_tab)
        user
        =
        let posts = 
            Scrape_posts_from_timeline.scrape_timeline
                (Browser.open_browser())
                Int32.MaxValue
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
   