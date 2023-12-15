

namespace rvinowise.twitter

open System
open Dapper
open Npgsql
open rvinowise.twitter.database
open rvinowise.twitter.database.tables

module Social_activity_database =
    
    
    let write_activity_amount_of_user
        (db_connection: NpgsqlConnection)
        (amount_of_what:Social_activity_amounts)
        (datetime:DateTime)
        user
        (amount:int)
        =
        db_connection.Query<User_handle>(
            $"insert into {amount_of_what} (datetime, user_handle, amount)
            values (@datetime, @user_handle, @amount)
            on conflict (datetime, user_handle) do update set amount = @amount",
            {|
                datetime = datetime
                user_handle = User_handle.db_value user
                amount = amount
            |}
        ) |> ignore
        
    
    let write_optional_activity_amount_of_user
        (db_connection: NpgsqlConnection)
        (amount_of_what:Social_activity_amounts)
        (datetime:DateTime)
        user
        (amount:int option)
        =
        amount
        |>function
        |Some amount->
            write_activity_amount_of_user
                db_connection
                amount_of_what
                datetime
                user
                amount
        |None->()
    
    let write_optional_social_activity_of_user
        (db_connection: NpgsqlConnection)
        (datetime:DateTime)
        user
        (activity:User_social_activity)
        =
        activity
        |>User_social_activity.followees_amount
        |>write_optional_activity_amount_of_user
                db_connection
                Social_activity_amounts.Followees
                datetime
                user
        activity
        |>User_social_activity.followers_amount
        |>write_optional_activity_amount_of_user
                db_connection
                Social_activity_amounts.Followers
                datetime
                user           
        activity
        |>User_social_activity.posts_amount
        |>write_optional_activity_amount_of_user
                db_connection
                Social_activity_amounts.Posts
                datetime
                user
    
    let write_optional_social_activity_of_users
        (db_connection: NpgsqlConnection)
        (datetime:DateTime)
        (activities: seq<User_handle*User_social_activity>)
        =
        activities
        |>Seq.iter(fun (user,activity)->
            write_optional_social_activity_of_user
               db_connection
               datetime
               user
               activity
        )
            
    let write_followers_amounts_to_db
        (db_connection: NpgsqlConnection)
        (datetime:DateTime)
        (users_with_amounts: (User_handle*int)seq )
        =
        Log.info $"""writing followers amounts to DB at {datetime}"""
        users_with_amounts
        |>Seq.iter(fun (user, score)-> 
            write_activity_amount_of_user
                db_connection
                Social_activity_amounts.Followers
                datetime 
                user
                score
        )
    
    let write_posts_amounts_to_db
        (db_connection: NpgsqlConnection)
        (datetime:DateTime)
        (users_with_amounts: (User_handle*int)seq )
        =
        Log.info $"""writing posts amounts to DB at {datetime}"""
        users_with_amounts
        |>Seq.iter(fun (user, score)-> 
            write_activity_amount_of_user
                db_connection
                Social_activity_amounts.Posts
                datetime 
                user
                score
        ) 
    
    
    let write_recruiting_referrals
        (db_connection: NpgsqlConnection)
        recruitings
        =
        recruitings
        |>Seq.iter(fun (submitted_at,link_referral,recruit,claimed_referral) ->
            db_connection.Query<unit>(
                @"insert into referral (submitted_at, recruit, link_referral, claimed_referral)
                values (@submitted_at, @recruit, @link_referral, @claimed_referral)
                on conflict (recruit) do
                update set
                    submitted_at=@submitted_at,
                    link_referral = @link_referral,
                    claimed_referral = @claimed_referral",
                {|
                    submitted_at = submitted_at
                    link_referral = link_referral
                    recruit = recruit
                    claimed_referral = claimed_referral
                |}
            ) |> ignore
        )
    
    let read_last_followers_amount_time
        (db_connection: NpgsqlConnection)
        =   
        db_connection.Query<DateTime>(
            @"select COALESCE(max(datetime),make_date(1000,1,1)) from followers_amount"
        )|>Seq.head
    
    
        
    
    let read_last_amounts_closest_to_moment
        (db_connection: NpgsqlConnection)
        (record_with_amounts: Social_activity_amounts)
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
    
    let read_amounts_closest_to_the_end_of_day
        (db_connection: NpgsqlConnection)
        amounts_table
        (day:DateTime)
        =
        day.Date.AddHours(23).AddMinutes(59)
        |>read_last_amounts_closest_to_moment db_connection
              amounts_table
    
    
    
        
    
    
    
        
    let read_last_competitors
        (db_connection: NpgsqlConnection)
        (since_datetime:DateTime)
        =
        db_connection.Query<string>(
            @"select user_handle from followers_amount
            where datetime >= @since_datetime
            group by user_handle",
            {|since_datetime=since_datetime|}
        )
    
    let read_last_amounts_closest_to_moment_for_users
        (db_connection: NpgsqlConnection)
        (record_with_amounts: Social_activity_amounts)
        (last_moment: DateTime)
        (users: string Set)
        =
        let last_amounts =
            read_last_amounts_closest_to_moment db_connection
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
            
    
    let read_last_recruiting_datetime
        (db_connection: NpgsqlConnection)
        =
        db_connection.Query<DateTime>(
            @"select COALESCE(max(submitted_at),make_date(1000,1,1)) from referral"
        )|>Seq.head
        
    


   