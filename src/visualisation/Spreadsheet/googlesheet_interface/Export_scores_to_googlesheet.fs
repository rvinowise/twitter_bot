namespace rvinowise.twitter

open System
open System.Collections.Generic
open Xunit

open rvinowise.twitter.database_schema
open rvinowise.twitter.database_schema.tables



module Export_scores_to_googlesheet =
        
        
    
            
    let sheet_row_of_total_score
        =
        [
            "" :>obj
            "" :>obj
            "Total" :>obj
        ]@
        [
            for letter in 'D' .. 'Z' ->
                $"=SUM({letter}3:{letter}1000)" :>obj
        ]|>List :> IList<obj>
        
    
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
            (Googlesheet_for_twitter.hyperlink_to_twitter_user_handle competitor.handle) :>obj
            competitor.name :>obj
            current_growth_formula :>obj
        ]@(
            score_history
            |>List.map (fun score -> score :> obj)
        )
        
        |>List :> IList<obj>
    
    let sheet_row_of_competitor_from_total_map
        (competitor:Twitter_user)
        place
        (users_to_history: Map<User_handle, int list>)
        =
        users_to_history
        |>Map.find competitor.handle 
        |>sheet_row_of_competitor
            place
            competitor
    
    let get_history_of_amounts_for_users
        db_connection
        (amount_of_what)
        days_from_today
        =
        let last_datetime =
            Social_activity_database.read_last_activity_amount_time db_connection
        let competitors =
            Social_activity_database.read_last_competitors db_connection
                (last_datetime - TimeSpan.FromHours(Settings.Influencer_competition.Competitors.include_from_past))
            |>Set.ofSeq

        let last_amounts =
            Social_activity_database.read_last_amounts_closest_to_moment_for_users
                    db_connection
                    amount_of_what
                    last_datetime
                    competitors
        
        let map_user_to_amounts =
            last_amounts
            |>Map.map (fun _ amount -> [amount])
        
        last_datetime
        ,
        last_amounts
        |>Map.toSeq
        |>Seq.sortByDescending snd
        |>Seq.mapi(fun place (user,score) ->
            (place+1),user,score          
        )
        ,
        [
            for day_from_today in 1 .. days_from_today ->
                let historical_day = last_datetime.AddDays(-day_from_today)
                Social_activity_database.read_amounts_closest_to_the_end_of_day
                    db_connection
                    amount_of_what
                    historical_day
        ]
        |>List.fold(fun map_user_to_full_score_history scores_on_day ->
            
            let user_to_score_on_that_day =
                scores_on_day
                |>Seq.map(fun row -> row.account, row.amount)
                |>Map.ofSeq
            
            map_user_to_full_score_history
            |>Map.map(fun user score_history ->
                let score_on_that_day =
                    user_to_score_on_that_day
                    |>Map.tryFind user
                    |>Option.defaultValue 0
                score_on_that_day::score_history
            )
        )   
            map_user_to_amounts
        |>Map.map (fun _ score_history ->
            score_history
            |>List.rev    
        )
    
    
 
    
    
    
            
    let input_history_of_amounts_to_sheet
        db_connection
        (table_with_amounts)
        (sheet:Google_spreadsheet)
        =
        let days_in_past = 20
        
        let last_datetime,current_scores,score_hisory =
            get_history_of_amounts_for_users
                db_connection
                table_with_amounts
                days_in_past
        
        Log.info $"inputting fields into google spread sheet for {days_in_past} days in the past since {last_datetime}..."
        let constant_data =
            [
                sheet_row_of_total_score;
                (sheet_row_of_header last_datetime days_in_past)
            ]
        let user_handles_to_names =
            Twitter_user_database.read_usernames_map
                db_connection
        let user_scores=
            current_scores
            |>Seq.map(fun (place,user_handle,_) ->
                sheet_row_of_competitor_from_total_map
                    {
                        Twitter_user.handle = user_handle
                        name=(Googlesheet.username_from_handle user_handles_to_names user_handle)
                    }
                    place
                    score_hisory
            )|>List.ofSeq// :> IList<obj>
        
        let all_rows =
            constant_data
            @user_scores
            |>List
        Log.info $"inputting into google sheet: {sheet}"
            
        Googlesheet.input_obj_into_sheet sheet all_rows
    
    
    let update_googlesheet
        social_database
        table_with_amounts
        (sheet:Google_spreadsheet)
        =
        Log.info $"updating google sheet, page '{sheet.page_name}'" 
        Googlesheet.clean_sheet sheet
        input_history_of_amounts_to_sheet
            social_database
            table_with_amounts
            sheet
    
    let update_googlesheets database =
        update_googlesheet
            database
            Social_activity_amounts_type.Followers
            Settings.Influencer_competition.Google_sheets.followers_amount
        update_googlesheet
            database
            Social_activity_amounts_type.Posts
            Settings.Influencer_competition.Google_sheets.posts_amount
    
    let ``try update_googlesheets``() =
        Local_database.open_connection()
        |>update_googlesheets
    
   
    let ``try input_posts_amount_to_sheet``() =
        update_googlesheet
            (Local_database.open_connection())
            Social_activity_amounts_type.Posts
            {
                Google_spreadsheet.doc_id = "1E_4BeKi0gOkaqsDkuY_0DeHssEcbLOBBzYmdneQo5Uw"
                page_name="posts_amount"
            }
