namespace rvinowise.twitter

open OpenQA.Selenium
open Xunit
open canopy.classic
open System
open rvinowise.twitter


module Post_on_twitter =


    let post_text text =
        printfn "posting as a single message..."
        url (Twitter_settings.base_url+"/compose/tweet")
        element "[aria-label='Tweet text']" << text
        element "[data-testid='tweetButton']" |>click
        //check_unlock_more_popup

    let list_without_last elements =
        elements|>List.rev|>List.tail|>List.rev

    let post_posts_as_thread 
        (chunks:string list) 
        =
        printfn "posting as a thread..."
        url (Twitter_settings.base_url+"/compose/tweet")

        chunks
        |>list_without_last
        |>Seq.iteri(fun index chunk ->
            element $"[data-testid='tweetTextarea_{index}']" << chunk
            element "div[aria-label='Add post']"|>click
        )

        element $"[data-testid='tweetTextarea_{List.length chunks - 1}']" << (List.last chunks)
        element "[data-testid='tweetButton']" |>click


    
    let post_thread_or_single_post 
        (posts: string list)
        =
        match posts with
        |[single_post]->post_text single_post
        |many_posts->
            many_posts
            |>List.rev
            |>post_posts_as_thread 

    
        
