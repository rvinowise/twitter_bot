namespace rvinowise.twitter

open System
open Dapper
open Npgsql



module Social_following_database =

    
    let write_following
        (db_connection:NpgsqlConnection)
        follower
        followee
        =
        db_connection.Query<User_handle>(
            @"insert into followers (follower, followee)
            values (@follower, @followee)",
            {|
                follower = User_handle.value follower
                followee = User_handle.value followee
            |}
        ) |> ignore
    
    let write_followees_of_user
        (db_connection:NpgsqlConnection)
        user
        followees
        =
        followees
        |>Seq.iter (write_following db_connection user)
        
    let write_social_connections_of_user
        (db_connection:NpgsqlConnection)
        user
        followees
        followers
        =
        followees
        |>Seq.iter (write_following db_connection user)
        followers
        |>Seq.iter (fun follower->
            write_following db_connection follower user
        )
        
   
    let mark_user_as_visited_now    
        (db_connection:NpgsqlConnection)
        user
        =
        db_connection.Query<User_handle>(
            @"insert into user_visited_by_following_scraper (handle)
            values (@handle)",
            {|
                handle = user
            |}
        ) |> ignore
        
        
    let was_user_harvested_recently
        (db_connection:NpgsqlConnection)
        (since_when: DateTime)
        user
        =
        let was_visited =
            db_connection.Query<string>(
                @"select handle from user_visited_by_following_scraper
                where handle = @handle and
                visited_at >= @since_when",
                {|handle=user; since_when=since_when|}
            )|>Seq.length > 0
        if was_visited then
            Log.info $"user {User_handle.value user} was already harvested after {since_when}"
        was_visited