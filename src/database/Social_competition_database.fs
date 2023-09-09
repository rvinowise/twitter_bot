

namespace rvinowise.twitter

open System
open Dapper
open Npgsql
open Xunit
open rvinowise.twitter.social_database

type Social_competition_database(db_connection: NpgsqlConnection) =
    
    new() = new Social_competition_database(Database.open_connection())
        
    member this.write_followers_amount_to_db 
        (datetime: DateTime)
        (user: User_handle)
        (amount: int)
        =
        db_connection.Query<User_handle>(
            @"insert into followers_amount (datetime, user_handle, amount)
            values (@datetime, @user_handle, @amount)
            on conflict (datetime, user_handle) do update set amount = @amount",
            {|
                datetime = datetime
                user_handle = User_handle.db_value user
                amount = amount; 
            |}
        ) |> ignore

    member this.write_followers_amounts_to_db
        (datetime:DateTime)
        (users_with_amounts: (User_handle*int)seq )
        =
        Log.info $"""writing followers amounts to DB at {datetime}"""
        users_with_amounts
        |>Seq.iter(fun (user, score)-> 
            this.write_followers_amount_to_db datetime user score
        )
    
    
    
    
    member this.write_posts_amounts_to_db
        (datetime:DateTime)
        (users_with_amounts: (User_handle*int)seq )
        =
        Log.info $"""writing posts amounts to DB at {datetime}"""
        users_with_amounts
        |>Seq.iter(fun (user, amount)-> 
            db_connection.Query<User_handle>(
                @"insert into posts_amount (datetime, user_handle, amount)
                values (@datetime, @user_handle, @amount)
                on conflict (datetime, user_handle) do update set amount = @amount",
                {|
                    datetime = datetime
                    user_handle = User_handle.db_value user
                    amount = amount; 
                |}
            ) |> ignore
        )    
    
    member this.update_user_names_in_db
        (users: Twitter_user list)
        =
        Log.info $"writing {List.length users} user names to DB"
        users
        |>List.iter(fun user->
            db_connection.Query<Db_twitter_user>(
                @"insert into twitter_user (handle, name)
                values (@handle, @name)
                on conflict (handle) do update set name = @name",
                {|
                    handle = User_handle.db_value user.handle
                    name = user.name
                |}
            ) |> ignore
        )
        
    member this.amounts_from_scraping_state
        (amount_of_what: User_social_activity->Result<int, string>) 
        (states: (User_handle*User_social_activity) list)
        =
        states
        |>List.choose (fun (user,state) ->
            match amount_of_what state with
            |Result.Ok number -> Some (user,number)
            |Result.Error message -> None
        )
        
    member this.write_user_activity_to_db
        datetime
        (activity:(Twitter_user*User_social_activity) list)
        =
        activity
        |>List.map fst
        |>this.update_user_names_in_db
        
        activity
        |>List.map (fun (user, state)->user.handle, state)
        |>this.amounts_from_scraping_state User_social_activity.followers_amount
        |>this.write_followers_amounts_to_db datetime
        
        activity
        |>List.map (fun (user, state)->user.handle, state)
        |>this.amounts_from_scraping_state User_social_activity.posts_amount
        |>this.write_posts_amounts_to_db datetime
    
    
    member this.write_recruiting_referrals
        recruitings
        =
        recruitings
        |>Seq.iter(fun (submitted_at,link_referral,recruit,claimed_referral) ->
            db_connection.Query<unit>(
                @"insert into referral (submitted_at, recruit, link_referral, claimed_referral)
                values (@submitted_at, @recruit, @link_referral, @claimed_referral)",
                {|
                    submitted_at = submitted_at
                    link_referral = link_referral
                    recruit = recruit
                    claimed_referral = claimed_referral
                |}
            ) |> ignore
        )
    
    member this.read_last_followers_amount_time() =
        db_connection.Query<DateTime>(
            @"select COALESCE(max(datetime),make_date(1000,1,1)) from followers_amount"
        )|>Seq.head
    
    
        
    
    member this.read_last_amounts_closest_to_moment
        (record_with_amounts: Table_with_amounts)
        (moment:DateTime)
        =
        db_connection.Query<Amount_for_user>(
            $"""select *
            from
                (
                    select
                        row_number() over(partition by user_handle order by datetime DESC) as my_row_number,
                        *
                    from {record_with_amounts}
                    where datetime <= @last_moment
                ) as amounts_by_time
            where my_row_number = 1""",
            {|
                record_with_amounts=record_with_amounts;
                last_moment=moment
            |}
        )
    
    member this.read_amounts_closest_to_the_end_of_day
        amounts_table
        (day:DateTime) =
        day.Date.AddHours(23).AddMinutes(59)
        |>this.read_last_amounts_closest_to_moment amounts_table
    
    
    
        
    member this.read_user_names_from_handles () =
        db_connection.Query<Db_twitter_user>(
            @"select * from twitter_user"
        )
        |>Seq.map(fun user->User_handle user.handle, user.name)
        |>Map.ofSeq
    
    
        
    member this.read_last_competitors
        (since_datetime:DateTime)
        =
        db_connection.Query<string>(
            @"select user_handle from followers_amount
            where datetime >= @since_datetime
            group by user_handle",
            {|since_datetime=since_datetime|}
        )
    
    member this.read_last_amounts_closest_to_moment_for_users
        (record_with_amounts: Table_with_amounts)
        (last_moment: DateTime)
        (users: string Set)
        =
        let last_amounts =
            this.read_last_amounts_closest_to_moment
                record_with_amounts
                last_moment
        last_amounts
        |>Seq.filter(fun amount_row ->
            users
            |>Set.contains amount_row.user_handle 
        )|>Seq.map(fun amount_row->
            User_handle amount_row.user_handle,
            amount_row.amount
        )|>Map.ofSeq
            
    
    member this.read_last_recruiting_datetime()=
        db_connection.Query<DateTime>(
            @"select COALESCE(max(submitted_at),make_date(1000,1,1)) from referral"
        )|>Seq.head
        
    
    interface IDisposable with
        member this.Dispose() =
            db_connection.Close()

type Social_database_test() =
    
    let social_db = new Social_competition_database()
    

   