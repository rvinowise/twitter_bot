namespace rvinowise.twitter

open OpenQA.Selenium
open Xunit
open canopy.classic
open System


module Post_on_twitter =

    let max_post_length = 280

    let post_text text =
        url (Twitter_settings.base_url+"/compose/tweet")
        element "[aria-label='Tweet text']" << text
        element "[data-testid='tweetButton']" |>click
        //check_unlock_more_popup

    let list_without_last elements =
        elements|>List.rev|>List.tail|>List.rev

    let post_chunks 
        (chunks:string list) 
        =
        url (Twitter_settings.base_url+"/compose/tweet")

        chunks
        |>list_without_last
        |>Seq.iteri(fun index chunk ->
            element $"[data-testid='tweetTextarea_{index}']" << chunk
            element "div[aria-label='Add post']"|>click
        )

        element $"[data-testid='tweetTextarea_{List.length chunks - 1}']" << (List.last chunks)
        element "[data-testid='tweetButton']" |>click


    let score_line_as_text score_line =
        (fst score_line+": "+(score_line|>snd|>string))+"\n\r"

    let split_score_into_text_chunks
        (score_lines: (string*int)seq )
        =
        score_lines
        |>Seq.fold (
            fun 
                (last_chunk:string, previous_chunks:string list)
                score_line
                ->
            let textual_score_line = score_line_as_text score_line
            let increased_length = 
                last_chunk
                |>String.length
                |>(+) (String.length textual_score_line)
            if increased_length <= max_post_length then
                last_chunk+textual_score_line
                ,
                previous_chunks
            else
                textual_score_line
                ,
                last_chunk::previous_chunks
        )
            (
                (string DateTime.Now + "\n\r")
                ,
                []
            )

    let post_thread_or_single_post (
        (last_text: string),
        (previous_chunks: string list)
        )=
        match previous_chunks with
        |[]->post_text last_text
        |previous_chunks->
            last_text::previous_chunks
            |>List.rev
            |>post_chunks 

    let post_subscribers_score
        (score: (string*int)seq ) 
        =
        score
        |>List.ofSeq
        |>List.sortByDescending snd
        |>split_score_into_text_chunks
        |>post_thread_or_single_post