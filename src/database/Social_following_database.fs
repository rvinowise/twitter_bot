namespace rvinowise.twitter

open System
open Dapper
open Npgsql



module Social_post_database =

    
    let write_post
        (db_connection:NpgsqlConnection)
        (post: Main_post)
        =
        db_connection.Query<User_handle>(
            @"insert into post (follower, followee)
            values (@follower, @followee)",
            {|
                //follower = User_handle.value follower
                //followee = User_handle.value followee
            |}
        ) |> ignore
    
    