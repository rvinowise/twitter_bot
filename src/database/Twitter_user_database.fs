

namespace rvinowise.twitter

open Dapper
open Npgsql
open rvinowise.twitter
open rvinowise.twitter.database

module Social_user_database =
    
    let read_user_names_from_handles
        (db_connection: NpgsqlConnection)
        =
        db_connection.Query<Db_twitter_user>(
            @"select * from user_name"
        )
        |>Seq.map(fun user->User_handle user.handle, user.name)
        |>Map.ofSeq
        
    let update_user_names_in_db
        (db_connection:NpgsqlConnection)
        (users: Twitter_user seq)
        =
        Log.info $"writing {Seq.length users} user names to DB"
        users
        |>Seq.iter(fun user ->
            db_connection.Query<Db_twitter_user>(
                @"insert into user_name (handle, name)
                values (@handle, @name)
                on conflict (handle) do update set name = @name",
                {|
                    handle = User_handle.db_value user.handle
                    name = user.name
                |}
            ) |> ignore
        )
    
    
    let write_user_briefing
        (db_connection:NpgsqlConnection)
        (user: User_briefing)
        =
        Log.info $"writing briefing of {User_handle.value user.handle} to DB"
        
        db_connection.Query<Db_twitter_user>(
            @"insert into user_briefing (
                handle,
                name,
                bio,
                location,
                date_joined,
                web_site,
                profession
            )
            values (
                @handle,
                @name,
                @bio,
                @location,
                @date_joined,
                @web_site,
                @profession
            )",
            {|
                handle = User_handle.db_value user.handle
                name = user.name
                bio=user.bio
                location=user.location
                date_joined=user.date_joined
                web_site=user.web_site
                profession=user.profession
            |}
        ) |> ignore
