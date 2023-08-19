namespace rvinowise.twitter

open System
open System.Configuration
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.classic
open rvinowise.twitter
open Dapper

module Format_score_for_twitter =
    
    
    let int_to_string_signed int =
        if int>=0 then
           "+"+string int
        else
           string int
    let score_change_line_as_text
        (score_change_line: int*Twitter_user*int*int)
        =
        let place,user,new_score,score_change = score_change_line
        sprintf
            "%d) %s %s: %d (%s)\n\r" 
            place
            user.name (user.handle|>User_handle.value)
            new_score
            (int_to_string_signed score_change)

    
    let get_score_header_with_timespan
        (start_time:DateTime)
        (end_time:DateTime)
        =
        sprintf
            "followers on %s (change from %s)"
            (end_time.ToString("yyyy-MM-dd HH:mm"))
            (start_time.ToString("yyyy-MM-dd HH:mm"))
             
    let split_score_change_into_text_chunks
        (header_text:string)
        (score_change_lines: (int*Twitter_user*int*int)seq )
        =
        score_change_lines
        |>Seq.fold (
            fun 
                (last_chunk:string, previous_chunks:string list)
                score_line
                ->
            let textual_score_line = score_change_line_as_text score_line
            let increased_length = 
                last_chunk
                |>String.length
                |>(+) (String.length textual_score_line)
            if increased_length <= Twitter_settings.max_post_length then
                last_chunk+textual_score_line
                ,
                previous_chunks
            else
                textual_score_line
                ,
                last_chunk::previous_chunks
        )
            (
                (header_text + "\n\r")
                ,
                []
            )
    
    
    let arrange_by_places_in_competition 
        (score_change_lines: (Twitter_user*int*int) list)
        =
        score_change_lines
        |>List.sortByDescending (fun (user,score,change)->score)
        |>List.mapi(fun place (user, score, change) ->
            (place+1),user,score,change
        )

    
    let score_change_from_two_moments
        (new_user_scores: (Twitter_user*int)seq )
        (previous_user_scores: (User_handle*int)seq )
        =
        let previous_scores =
            previous_user_scores
            |>Map.ofSeq
        let new_scores =
            new_user_scores
            |>Map.ofSeq
        new_scores
        |>Map.toList
        |>List.map(fun (user,score)->
            user,
            score,
            score-
            (previous_scores
            |>Map.tryFind user.handle
            |>Option.defaultValue 0)
        )
    
    let score_as_text_chunks
        start_datetime end_datetime
        score_changes
        =
        let header =
            get_score_header_with_timespan
                start_datetime end_datetime
        
        
        score_changes
        |>split_score_change_into_text_chunks header
        
    
    
