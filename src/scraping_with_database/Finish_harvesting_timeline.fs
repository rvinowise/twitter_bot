namespace rvinowise.twitter

open System
open OpenQA.Selenium
open Xunit
open rvinowise.html_parsing
open rvinowise.web_scraping



module Finish_harvesting_timeline =
    
    
    
    
    let reached_last_visited_post
        last_visited_post
        (post:Main_post)
        =
        post.id = last_visited_post
    
    let finish_after_amount_of_invocations amount =
        let mutable items_left = amount 
        let is_finished = fun _ _ _->
            items_left <- items_left - 1
            items_left < 0
        is_finished
        
    let finish_after_time time =
        let start_time = DateTime.Now
        let is_finished = (fun _ _ _ ->
             let current_time = DateTime.Now
             if (current_time - start_time > time) then
                 Log.debug $"scraping time elapsed at {current_time}"
                 true
             else false
        )
        is_finished
    
    let only_finish_when_no_posts_left _ _ _ = false
    
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
                only_finish_when_no_posts_left () ()
        
        is_finished