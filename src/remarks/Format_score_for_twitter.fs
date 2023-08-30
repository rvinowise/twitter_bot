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
    
    
    
    let score_change_line_as_text
        (score_change_line: int*Twitter_user*int*int)
        =
        let place,user,new_score,score_change = score_change_line
        sprintf
            "%d. %s: %d | %s\n\r" 
            place
            user.name
            //(user.handle|>User_handle.value)
            new_score
            (Utils.int_to_string_signed score_change)

    
    let get_score_header_with_timespan
        (start_time:DateTime)
        (end_time:DateTime)
        total_score
        score_change
        =
        sprintf
            """
            followers on %s (change from %s)
            Total: %d | %s
            """
            (end_time.ToString("yyyy-MM-dd HH:mm"))
            (start_time.ToString("yyyy-MM-dd HH:mm"))
            total_score
            (Utils.int_to_string_signed(score_change))
             

         
    
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
    
    let calculate_total_score_of_team
        (scores: (int*int) list )
        =
        let total_growth=
            scores
            |>List.map (fun (_,growth) -> growth)
            |>List.sum
        let total_score=
            scores
            |>List.map (fun (score,_) -> score)
            |>List.sum
        total_score,total_growth
    
    
    let score_line_as_text
        (score_line: int*Twitter_user*int)
        =
        let place,user,score = score_line
        sprintf "%d) %s %s: %d\n\r"
            place user.name (user.handle|>User_handle.value) score
    
    
    let arrange_unsplittable_texts_into_post_thread
        (unsplittables: string list)
        =
        unsplittables
        |>Seq.fold (
            fun 
                (last_post:string, previous_posts:string list)
                unsplittable
                ->
            let increased_length = 
                last_post
                |>String.length
                |>(+) (String.length unsplittable)
            if increased_length <= Twitter_settings.max_post_length then
                last_post+unsplittable
                ,
                previous_posts
            else
                unsplittable
                ,
                last_post::previous_posts
        )
            ("",[])
        
    
    let scoreboard_as_unsplittable_chunks
        start_datetime end_datetime
        score_changes
        =
        let header =
            get_score_header_with_timespan
                start_datetime end_datetime
        
        
        score_changes
        |>List.map score_change_line_as_text
        
    
    
