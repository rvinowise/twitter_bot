

namespace rvinowise.twitter

open System
open Dapper
open Xunit
open rvinowise.twitter.social_database

type Social_competition_database() =
    
    let db_connection = Database.open_connection()
        
                
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
        (amount_of_what: Scraped_user_state->Result<int, string>) 
        (states: (User_handle*Scraped_user_state) list)
        =
        states
        |>List.choose (fun (user,state) ->
            match amount_of_what state with
            |Result.Ok number -> Some (user,number)
            |Result.Error message -> None
        )
        
    member this.write_user_states_to_db
        datetime
        (state:(Twitter_user*Scraped_user_state) list)
        =
        state
        |>List.map fst
        |>this.update_user_names_in_db
        
        state
        |>List.map (fun (user, state)->user.handle, state)
        |>this.amounts_from_scraping_state Scraped_user_state.followers_amount
        |>this.write_followers_amounts_to_db datetime
        
        state
        |>List.map (fun (user, state)->user.handle, state)
        |>this.amounts_from_scraping_state Scraped_user_state.posts_amount
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
    
    [<Fact(Skip="manual")>]
    let ``try writing with time-zoned datetime``()=
        [
            User_handle "rvinowise", 12
        ]|>social_db.write_followers_amounts_to_db DateTime.Now
    
    [<Fact(Skip="manual")>]
    let ``try writing to bd row with the error column``()=
        [
            User_handle "rvinowise", 10
        ]|>social_db.write_followers_amounts_to_db DateTime.Now
    
    [<Fact>] //(Skip="manual")
    let ``try write user states to db``()=
        [
            {
                Twitter_user.handle=User_handle "rvinowise"
                name="Victor Rybin test"
            },{
                Scraped_user_state.followers_amount=Result.Ok 7890
                posts_amount=Result.Ok 7890
            };
            
            {
                Twitter_user.handle=User_handle "rvinowise2"
                name="Victor Rybin no posts"
            },{
                Scraped_user_state.followers_amount=Result.Ok 200
                posts_amount=Result.Error "no field with posts amount"
            };
            
            {
                Twitter_user.handle=User_handle "rvinowise3"
                name="Victor Rybin no data"
            },{
                Scraped_user_state.followers_amount=Result.Error "no field with followers_amount"
                posts_amount=Result.Error "no field with posts amount"
            };
            
            {
                Twitter_user.handle=User_handle "rvinowise4"
                name="Victor Rybin no followers"
            },{
                Scraped_user_state.followers_amount=Result.Error "no field with followers_amount"
                posts_amount=Result.Ok 410
            }
        ]|>social_db.write_user_states_to_db DateTime.Now
    
    [<Fact>]//(Skip="manual")
    let ``try read_scores_closest_to_the_end_of_day``()=
        let scores =
            social_db.read_amounts_closest_to_the_end_of_day
                Table_with_amounts.Followers
                (DateTime(2023,08,28))
        ()
    
    
    [<Fact>]//(Skip="manual")
    let ``try read time with offset from UTC``()=
        let result = 
            Database.open_connection().Query<Amount_for_user>(
                $"""select * from followers_amount
                    where amount = 7890
                """
            )
        ()
    
    [<Fact(Skip="manual")>]
    let ``try read_user_names_from_handles``()=
        let test = social_db.read_user_names_from_handles()
        ()
    
    [<Fact>]
    let ``try read_last_competitors``()=
        let test =
            social_db.read_last_competitors
                (DateTime.Now - TimeSpan.FromDays(3))
        ()