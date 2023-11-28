namespace rvinowise.twitter

open System
open System.Collections.Generic
open Xunit

open rvinowise.twitter.database.tables



module Export_adjacency_matrix =
        
        

        
    
    let sheet_row_of_header
        (now: DateTime)
        (days_from_today: int)
        =
        [
            "Place" :>obj
            "Handle" :>obj
            "Name" :>obj
            "Growth" :>obj
            now.ToString("yyyy-MM-dd HH:mm")
        ]@[
            for day_from_today in 1 .. days_from_today ->
                (now.Date.AddDays(-day_from_today)).ToString("yyyy-MM-dd")
        ]
        |>List :> IList<obj>
    

    
    let sheet_row_of_competitor
        (place:int)
        (competitor:Twitter_user)
        (score_history: int list)
        =
        let user_row_index = place+2
        let current_growth_formula = $"=E{user_row_index}-F{user_row_index}"
        [
            place :>obj
            (Googlesheet_for_twitter.hyperlink_to_twitter_user competitor.handle) :>obj
            competitor.name :>obj
            current_growth_formula :>obj
        ]@(
            score_history
            |>List.map (fun score -> score :> obj)
        )
        
        |>List :> IList<obj>
    