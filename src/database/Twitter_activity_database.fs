

namespace rvinowise.twitter

open System
open Dapper
open Npgsql
open Xunit
open rvinowise.twitter.database_schema
open rvinowise.twitter.database_schema.tables

module Social_activity_database =
    
    
    let write_activity_amount_of_user
        (db_connection: NpgsqlConnection)
        (amount_of_what:Social_activity_amounts_type)
        (datetime:DateTime)
        account
        (amount:int)
        =
        db_connection.Query<User_handle>(
            $"
            insert into {tables.activity_amount} 
            (
                {tables.activity_amount.account},
                {tables.activity_amount.datetime}, 
                {tables.activity_amount.activity}, 
                {tables.activity_amount.amount}
            )
            values (@account, @datetime, @activity, @amount)
            on conflict (
                {tables.activity_amount.account},
                {tables.activity_amount.datetime},
                {tables.activity_amount.activity}
            ) 
            do update set {tables.activity_amount.amount} = @amount
            ",
            {|
                datetime = datetime
                account = User_handle.db_value account
                activity = string amount_of_what
                amount = amount
            |}
        ) |> ignore
        
    
    let write_optional_activity_amount_of_user
        (db_connection: NpgsqlConnection)
        (amount_of_what:Social_activity_amounts_type)
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
                Social_activity_amounts_type.Followees
                datetime
                user
        activity
        |>User_social_activity.followers_amount
        |>write_optional_activity_amount_of_user
                db_connection
                Social_activity_amounts_type.Followers
                datetime
                user           
        activity
        |>User_social_activity.posts_amount
        |>write_optional_activity_amount_of_user
                db_connection
                Social_activity_amounts_type.Posts
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
                Social_activity_amounts_type.Followers
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
                Social_activity_amounts_type.Posts
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
                $"
                insert into referral (submitted_at, recruit, link_referral, claimed_referral)
                values (@submitted_at, @recruit, @link_referral, @claimed_referral)
                on conflict (recruit) do
                update set
                    submitted_at=@submitted_at,
                    link_referral = @link_referral,
                    claimed_referral = @claimed_referral
                ",
                {|
                    submitted_at = submitted_at
                    link_referral = link_referral
                    recruit = recruit
                    claimed_referral = claimed_referral
                |}
            ) |> ignore
        )
    
    let read_last_activity_amount_time
        (db_connection: NpgsqlConnection)
        =   
        db_connection.Query<DateTime>(
            $"
            select COALESCE(max({tables.activity_amount.datetime}),make_date(1000,1,1)) 
            from {tables.activity_amount}
            "
        )|>Seq.head
    
    
        
    
    let read_last_amounts_closest_to_moment
        (db_connection: NpgsqlConnection)
        (amount_of_what: Social_activity_amounts_type)
        (moment:DateTime)
        =
        db_connection.Query<Amount_for_account>(
            $"""
            select 
                {tables.activity_amount.datetime},
                {tables.activity_amount.account},
                {tables.activity_amount.amount}
            from
                (
                    select
                        row_number() over(partition by {tables.activity_amount.account} order by {tables.activity_amount.datetime} DESC) as my_row_number,
                        *
                    from {tables.activity_amount}
                    where 
                        {tables.activity_amount.activity} = @amount_of_what
                        and {tables.activity_amount.datetime} <= @last_moment
                ) as amounts_by_time
            where my_row_number = 1
            """,
            {|
                last_moment=moment
                amount_of_what = string amount_of_what
            |}
        )
    
    let read_last_amount_for_user
        (db_connection: NpgsqlConnection)
        (amount_of_what: Social_activity_amounts_type)
        (account: User_handle)
        =
        db_connection.Query<Amount_for_account>(
            $"""select 
                    {tables.activity_amount.account},
                    {tables.activity_amount.datetime},
                    {tables.activity_amount.amount}
                from 
                    {tables.activity_amount}
                where 
                    {tables.activity_amount.account} = @account
                    and {tables.activity_amount.activity} = @amount_of_what
                order by {tables.activity_amount.datetime} DESC limit 1
             """,
            {|
                account = account
                amount_of_what = string amount_of_what
            |}
        )|>Seq.tryHead
        |>Option.defaultValue (Amount_for_account.empty account)
            
            
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
        db_connection.Query<User_handle>(
            $"
            select {tables.activity_amount.account} from {tables.activity_amount}
            where {tables.activity_amount.datetime} >= @since_datetime
            group by {tables.activity_amount.account}
            ",
            {|
                since_datetime=since_datetime
            |}
        )
   
    
    let ``try read_last_competitors``()=
        let test =
            read_last_competitors
                (Central_database.open_connection())
                (DateTime.Now-TimeSpan.FromDays(5))
        ()
     
    let read_last_amounts_closest_to_moment_for_users
        (db_connection: NpgsqlConnection)
        (record_with_amounts: Social_activity_amounts_type)
        (last_moment: DateTime)
        (users: User_handle Set)
        =
        let last_amounts =
            read_last_amounts_closest_to_moment db_connection
                record_with_amounts
                last_moment
        last_amounts
        |>Seq.filter(fun amount_row ->
            users
            |>Set.contains amount_row.account 
        )|>Seq.map(fun amount_row->
            amount_row.account,
            amount_row.amount
        )|>Map.ofSeq
            
    
    let read_last_recruiting_datetime
        (db_connection: NpgsqlConnection)
        =
        db_connection.Query<DateTime>(
            $"select COALESCE(max(submitted_at),make_date(1000,1,1)) from referral"
        )|>Seq.head
        
    


   