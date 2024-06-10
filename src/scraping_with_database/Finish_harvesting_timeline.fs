namespace rvinowise.twitter

open System
open OpenQA.Selenium
open Xunit
open rvinowise.html_parsing
open rvinowise.web_scraping



module Finish_harvesting_timeline =
    
    (*
    these functions are invoked every time a next post is scraped,
    and they determine whether to stop on the current post (and not write it to the db)
    or continue writing and scraping further posts
    *)
    
    
    let reached_last_visited_post
        last_visited_post
        (post:Main_post)
        =
        post.id = last_visited_post
    
    let finish_after_amount_of_invocations amount =
        let mutable items_left = amount 
        let is_finished () =
            items_left <- items_left - 1
            if items_left < 0 then
                Some Enough_posts_scraped
            else
                None
        is_finished
    
    let finish_when_encountered_familiar_posts
        familiar_posts_amount
        is_post_needed
        was_post_scraped_before
        =
        let mutable familiar_posts_streak = 0;
        let is_finished
            (post: Main_post)
            =
            if
                is_post_needed post
            then
                if
                    was_post_scraped_before
                        post.id
                then
                    familiar_posts_streak <- familiar_posts_streak + 1
                    $"post {post.id} is recognised as scraped earlier, amount of familiar posts = {familiar_posts_streak}"
                    |>Log.info
                    if 
                        familiar_posts_streak >= familiar_posts_amount
                    then
                        Some Familiar_posts_encountered
                    else
                        None
                else
                    if familiar_posts_streak > 0 then
                        $"previous {familiar_posts_streak} posts were recognised as scraped earlier, but the following post {post.id} is new, it's strange."
                        |>Log.info
                    
                    familiar_posts_streak <- 0
                    None
            else
                None
        is_finished
    
    let finish_when_encountered_familiar_published_posts
        database
        familiar_posts_amount
        =
        let is_post_needed post =
            (
                post.is_pinned
                ||
                post.reposter.IsSome
            )|>not
        
        finish_when_encountered_familiar_posts
            familiar_posts_amount
            is_post_needed
            (Twitter_post_database.is_post_original_scraped database)
            
        
    
    let finish_when_encountered_familiar_liked_posts
        database
        user
        familiar_posts_amount
        =
        finish_when_encountered_familiar_posts
            familiar_posts_amount
            (fun _ -> true)
            (Twitter_post_database.is_like_scraped database user)
    
        
    let finish_after_time time =
        let start_time = DateTime.UtcNow
        let is_finished _ _ _ =
            let current_time = DateTime.UtcNow
            if (current_time - start_time > time) then
                Log.debug $"scraping time elapsed at {current_time}"
                Some Stopping_reason.Timeout
            else None
            
        is_finished
    
    let only_finish_when_no_posts_left _ = false
    
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