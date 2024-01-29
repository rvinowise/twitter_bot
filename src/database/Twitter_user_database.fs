

namespace rvinowise.twitter

open Dapper
open Npgsql
open rvinowise.twitter
open rvinowise.twitter.database_schema
open rvinowise.twitter.database_schema.tables

module Twitter_user_database =
    
    
    let read_usernames_map
        (db_connection: NpgsqlConnection)
        =
        db_connection.Query<Twitter_user>(
            $"select * from {user_name}"
        )
        |>Seq.map(fun user->user.handle, user.name)
        |>Map.ofSeq
    
    let read_usernames_map_from_briefing
        (db_connection: NpgsqlConnection)
        =
        db_connection.Query<Twitter_user>(
            $"""
            select distinct on ({user_briefing.handle}) 
                {user_briefing.handle}, {user_briefing.name}
            from {user_briefing}
            group by {user_briefing.handle}, {user_briefing.name}, {user_briefing.created_at}
            order by {user_briefing.handle}, {user_briefing.created_at} desc
            
            """
        )
        |>Seq.map(fun user->user.handle, user.name)
        |>Map.ofSeq
    
    let handle_to_username
        (database: NpgsqlConnection)
        =
        let usernames =
            read_usernames_map_from_briefing
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
                insert into {user_name} (
                   {user_name.handle}, 
                   {user_name.name}
                )
                values (@handle, @name)
                on conflict ({user_name.handle}) 
                do update set {user_name.name} = @name
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
        
        db_connection.Query<unit>(
            $"insert into {user_briefing} (
                {user_briefing.handle},
                {user_briefing.name},
                {user_briefing.bio},
                {user_briefing.location},
                {user_briefing.date_joined},
                {user_briefing.web_site},
                {user_briefing.profession}
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
                {user_briefing.created_at}, 
                {user_briefing.handle}
            ) 
            do update set 
            (
                {user_briefing.name}, 
                {user_briefing.bio}, 
                {user_briefing.location},
                {user_briefing.date_joined},
                {user_briefing.web_site},
                {user_briefing.profession}
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

    let sql_account_should_be_inside_matrix
        account
        =
        $"""
        --the account should be part of the desired matrix
        exists ( 
            select ''
            from {account_of_matrix}
            where 
                --find the matrix by title
                {account_of_matrix}.{account_of_matrix.title} = @matrix_title
                
                --the target of attention should be part of the matrix
                and {account_of_matrix}.{account_of_matrix.account} = {account}
        )
        """