namespace rvinowise.twitter

open canopy.classic
open canopy.parallell.functions
open rvinowise.twitter


module Post_on_twitter =


    let post_text browser text =
        Log.info "posting as a single message..."
        url (Twitter_settings.base_url+"/compose/tweet") browser
        element "[aria-label='Tweet text']" browser << text
        click (element "[data-testid='tweetButton']" browser) browser
        //check_unlock_more_popup

    let list_without_last elements =
        elements|>List.rev|>List.tail|>List.rev

    let post_posts_as_thread
        browser
        (chunks:string list) 
        =
        Log.info "posting as a thread..."
        url (Twitter_settings.base_url+"/compose/tweet") browser

        chunks
        |>list_without_last
        |>Seq.iteri(fun index chunk ->
            element $"[data-testid='tweetTextarea_{index}']" browser << chunk
            browser
            |>click (element "div[aria-label='Add post']" browser)
        )

        element $"[data-testid='tweetTextarea_{List.length chunks - 1}']" browser << (List.last chunks)
        browser
        |>click (element "[data-testid='tweetButton']" browser)


    
    let post_thread_or_single_post
        browser
        (posts: string list)
        =
        match posts with
        |[single_post]->post_text browser single_post
        |many_posts->
            many_posts
            |>List.rev
            |>post_posts_as_thread browser

    
        
