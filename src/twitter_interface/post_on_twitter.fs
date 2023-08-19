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

    let post_chunks 
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


    let score_line_as_text
        (score_line: int*Twitter_user*int)
        =
        let place,user,score = score_line
        sprintf "%d) %s %s: %d\n\r"
            place user.name (user.handle|>User_handle.value) score
    
    

    let post_thread_or_single_post (
        last_text: string,
        previous_chunks: string list
        )=
        match previous_chunks with
        |[]->post_text last_text
        |previous_chunks->
            last_text::previous_chunks
            |>List.rev
            |>post_chunks 

    
        
