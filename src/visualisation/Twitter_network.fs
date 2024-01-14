namespace rvinowise.twitter


open Xunit
open FsUnit

open Plotly.NET
open rvinowise.twitter.database.tables

module Twitter_network =
    
    
    let draw_chart database =
        ()
        
    
    
    let activity_amounts_for_days
        db_connection
        days
        activity_type
        =
        days
        |>Seq.map (fun day ->
            Social_activity_database.read_amounts_closest_to_the_end_of_day
                db_connection
                activity_type
                day
        )
        |>Seq.map(fun amounts_for_users ->
            amounts_for_users
            |>Seq.fold (fun total_amount amount_for_user ->
                total_amount + amount_for_user.amount
            )
                0
        )
        
    [<Fact(Skip="manual")>]
    let ``try draw_chart (focus on days)``() =
        
        let db_connection = Twitter_database.open_connection()
        
        let last_datetime =
            Social_activity_database.read_last_activity_amount_time db_connection
        
        let days_amount = 500
        
        let days =
            [
                for day_from_today in days_amount .. -1 .. 0  ->
                    last_datetime.AddDays(-day_from_today)
            ]
        
        
        [
            Social_activity_amounts.Followers, "Followers"
            Social_activity_amounts.Followees, "Followees"
            Social_activity_amounts.Posts, "Post"
        ]
        |>List.map (fun (activity_type, name) ->
            activity_amounts_for_days
                db_connection
                days
                activity_type
            ,
            name
        )
        |>List.map (fun (amounts, name) ->
            Chart.Line(days, amounts, Name=name)
        )
        |>Chart.combine
        |>Chart.withTitle "Twitter network size"
        |>Chart.withXAxisStyle "time"
        |>Chart.withYAxisStyle "size"
        |>Chart.show        
    
    
    [<Fact(Skip="manual")>]
    let ``try draw_chart (focus on datapoints)``() =
        
        let db_connection = Twitter_database.open_connection()
        
        
        ()    