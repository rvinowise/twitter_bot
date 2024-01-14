

namespace rvinowise.twitter

open Dapper
open Npgsql
open rvinowise.twitter
open rvinowise.twitter.database

module Twitter_user_database =
    
    
    let read_usernames_map
        (db_connection: NpgsqlConnection)
        =
        db_connection.Query<Twitter_user>(
            @"select * from {tables.user_name}"
        )
        |>Seq.map(fun user->user.handle, user.name)
        |>Map.ofSeq
    
    
    let handle_to_username
        (database: NpgsqlConnection)
        =
        let usernames =
            read_usernames_map
                database
        let handle_to_hame handle =
            usernames
            |>Map.tryFind handle
            |>Option.defaultValue (User_handle.value handle)
        handle_to_hame
        
    let update_user_names_in_db
        (db_connection:NpgsqlConnection)
        (users: Twitter_user seq)
        =
        Log.info $"writing {Seq.length users} user names to DB"
        users
        |>Seq.iter(fun user ->
            db_connection.Query<Twitter_user>(
                $"""
                insert into {tables.user_name} (
                   {tables.user_name.handle}, 
                   {tables.user_name.name}
                )
                values (@handle, @name)
                on conflict ({tables.user_name.handle}) 
                do update set {tables.user_name.name} = @name
                """,
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
        
        db_connection.Query<Twitter_user>(
            $"insert into {tables.user_briefing} (
                {tables.user_briefing.handle},
                {tables.user_briefing.name},
                {tables.user_briefing.bio},
                {tables.user_briefing.location},
                {tables.user_briefing.date_joined},
                {tables.user_briefing.web_site},
                {tables.user_briefing.profession}
            )
            values (
                @handle,
                @name,
                @bio,
                @location,
                @date_joined,
                @web_site,
                @profession
            )
            on conflict (
                {tables.user_briefing.created_at}, 
                {tables.user_briefing.handle}
            ) 
            do update set 
            (
                {tables.user_briefing.name}, 
                {tables.user_briefing.bio}, 
                {tables.user_briefing.location},
                {tables.user_briefing.date_joined},
                {tables.user_briefing.web_site},
                {tables.user_briefing.profession}
            ) = 
            (
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
