namespace rvinowise.twitter

open System
open OpenQA.Selenium
open Xunit
open canopy.types
open rvinowise.web_scraping

module Harvest_posts_from_timeline =
    
    
    let harvest_posts_from_timeline user =
        let posts = 
            Scrape_posts_from_timeline.scrape_timeline
                (Browser.open_browser())
                Int32.MaxValue
                //Timeline_tab.Posts
                Timeline_tab.Replies
                user
        
        let results = List.map snd posts
        let errors =
            results
            |>List.filter Result.isError
        
        use database = Twitter_database.open_connection()
        results
        |>List.choose Result.toOption
        |>List.map (fun post->
            Twitter_post_database.write_main_post    
                database
                post
        )
    
    [<Fact>]//(Skip="manual")
    let ``try harvest_posts_from_timeline``()=
       "dicortona"
        |>User_handle
        |>harvest_posts_from_timeline
   