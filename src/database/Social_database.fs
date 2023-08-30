namespace rvinowise.twitter

open System
open System.Configuration
open System.Runtime.InteropServices
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.classic
open Dapper
open Npgsql
open rvinowise.twitter

module Social_database =
    [<CLIMutable>]
    type Db_twitter_user = {
        handle: string
        name: string
    }
    
    type Table_with_amounts =
        |Followers
        |Posts
        with
        override this.ToString() =
            match this with
            |Followers -> "followers_amount"
            |Posts -> "posts_amount"
                
    //let followers_amount_table = "followers_amount"
    //let posts_amount_table = "posts_amount"
    
    let write_followers_amount_to_db 
        (datetime: DateTime)
        (user: User_handle)
        (amount: int)
        =
        Database.open_connection.Query<User_handle>(
            @"insert into followers_amount (datetime, user_handle, amount)
            values (@datetime, @user_handle, @amount)
            on conflict (datetime, user_handle) do update set amount = @amount",
            {|
                datetime = datetime
                user_handle = User_handle.db_value user
                amount = amount; 
            |}
        ) |> ignore

    let write_followers_amounts_to_db
        (datetime:DateTime)
        (users_with_amounts: (User_handle*int)seq )
        =
        Log.info $"""writing followers amounts to DB on {datetime}"""
        users_with_amounts
        |>Seq.iter(fun (user, score)-> 
            write_followers_amount_to_db datetime user score
        )
    
    [<Fact(Skip="manual")>]
    let ``try writing with time-zoned datetime``()=
        [
            User_handle "rvinowise", 12
        ]|>write_followers_amounts_to_db DateTime.Now
    
    [<Fact(Skip="manual")>]
    let ``try writing to bd row with the error column``()=
        [
            User_handle "rvinowise", 10
        ]|>write_followers_amounts_to_db DateTime.Now
    
    let write_posts_amounts_to_db
        (datetime:DateTime)
        (users_with_amounts: (User_handle*int)seq )
        =
        Log.info $"""writing posts amounts to DB on {datetime}"""
        users_with_amounts
        |>Seq.iter(fun (user, amount)-> 
            Database.open_connection.Query<User_handle>(
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
    
    let update_user_names_in_db
        (users: Twitter_user list)
        =
        Log.info $"writing {List.length users} user names to DB"
        users
        |>List.iter(fun user->
            Database.open_connection.Query<Db_twitter_user>(
                @"insert into twitter_user (handle, name)
                values (@handle, @name)
                on conflict (handle) do update set name = @name",
                {|
                    handle = User_handle.db_value user.handle
                    name = user.name
                |}
            ) |> ignore
        )
        
    let amounts_from_scraping_state
        (amount_of_what: Scraped_user_state->Result<int, string>) 
        (states: (User_handle*Scraped_user_state) list)
        =
        states
        |>List.choose (fun (user,state) ->
            match amount_of_what state with
            |Result.Ok number -> Some (user,number)
            |Result.Error message -> None
        )
        
    let write_user_states_to_db
        datetime
        (state:(Twitter_user*Scraped_user_state) list)
        =
        state
        |>List.map fst
        |>update_user_names_in_db
        
        state
        |>List.map (fun (user, state)->user.handle, state)
        |>amounts_from_scraping_state Scraped_user_state.followers_amount
        |>write_followers_amounts_to_db datetime
        
        state
        |>List.map (fun (user, state)->user.handle, state)
        |>amounts_from_scraping_state Scraped_user_state.posts_amount
        |>write_posts_amounts_to_db datetime
    
    
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
        ]|>write_user_states_to_db DateTime.Now
    
    [<CLIMutable>]
    type Amount_for_user = {
        datetime: DateTime
        user_handle: string
        amount: int
    }
    
    let read_last_followers_amount_time() =
        Database.open_connection.Query<DateTime>(
            @"select COALESCE(max(datetime),make_date(1000,1,1)) from followers_amount"
        )|>Seq.head
    
    
        
    
    let read_last_amounts_closest_to_moment
        (record_with_amounts: Table_with_amounts)
        (moment:DateTime)
        =
        Database.open_connection.Query<Amount_for_user>(
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
        amounts_table
        (day:DateTime) =
        day.Date.AddHours(23).AddMinutes(59)
        |>read_last_amounts_closest_to_moment amounts_table
    
    
    [<Fact>]//(Skip="manual")
    let ``try read_scores_closest_to_the_end_of_day``()=
        let scores =
            read_amounts_closest_to_the_end_of_day
                Table_with_amounts.Followers
                (DateTime(2023,08,28))
        ()
    
    
    [<Fact>]//(Skip="manual")
    let ``try read time with offset from UTC``()=
        let result = 
            Database.open_connection.Query<Amount_for_user>(
                $"""select * from followers_amount
                    where amount = 7890
                """
            )
        ()
        
    let read_user_names_from_handles () =
        Database.open_connection.Query<Db_twitter_user>(
            @"select * from twitter_user"
        )
        |>Seq.map(fun user->User_handle user.handle, user.name)
        |>Map.ofSeq
    
    [<Fact(Skip="manual")>]
    let ``try read_user_names_from_handles``()=
        let test = read_user_names_from_handles()
        ()
        
    let read_last_competitors
        (since_datetime:DateTime)
        =
        Database.open_connection.Query<string>(
            @"select user_handle from followers_amount
            where datetime >= @since_datetime
            group by user_handle",
            {|since_datetime=since_datetime|}
        )
    
    let read_last_amounts_closest_to_moment_for_users
        (record_with_amounts: Table_with_amounts)
        (last_moment: DateTime)
        (users: string Set)
        =
        let last_amounts =
            read_last_amounts_closest_to_moment
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
            
    [<Fact>]
    let ``try read_last_competitors``()=
        let test =
            read_last_competitors
                (DateTime.Now - TimeSpan.FromDays(3))
        ()