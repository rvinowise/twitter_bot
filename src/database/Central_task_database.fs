namespace rvinowise.twitter

open System
open Dapper
open Npgsql
open Xunit
open rvinowise.twitter
open rvinowise.twitter.database


   
        

module Central_task_database =
    
    
    
    let open_connection () =
        let db_connection = Database.open_connection Settings.central_db
        
        Twitter_database.set_twitter_type_handlers()
        
        db_connection    

    let this_working_session_id = Guid.NewGuid ()
        
    
    let take_next_free_user
        (db_connection: NpgsqlConnection)
        =
        
        let free_user =
            db_connection.Query<User_handle>(
                @"
                update users_to_scrape 
                set 
                    status = 'taken',
                    taken_by = @worker
                where 
                    handle = (
                        select handle from users_to_scrape
                        where 
                            created_at = (SELECT MAX(created_at) FROM users_to_scrape)
                            and status = 'free'
                            and taken_by = ''
                        limit 1
                    )
                returning handle    
                ",
                {|
                    worker=this_working_session_id
                |}
            )|>Seq.tryHead
        
        match free_user with
        |Some user ->
            Log.info $"next free user {User_handle.value user} is taken from the central database"   
        |None ->
            Log.info "the central database didn't return next free user"
            
        free_user
        
    [<Fact>]
    let ``try take_next_free_user``()=
        let result =
            take_next_free_user
                (open_connection())
            
        ()
        
    let read_next_free_user
        (db_connection: NpgsqlConnection)
        =
        db_connection.Query<User_handle>(
            @"select * from users_to_scrape
            where 
                created_at = (SELECT MAX(created_at) FROM users_to_scrape)
                and status = 'free'
                and taken_by = ''
            "
        )
        
    let write_user_status
        (db_connection:NpgsqlConnection)
        (user: User_handle)
        (status: Scrape_user_status)
        =
        Log.info $"writing status {Scrape_user_status.db_value status} for user {User_handle.value user} in the central database"
        
        db_connection.Query<User_handle>(
            @"update users_to_scrape 
            set 
                status = @status
            where 
                handle = @user",
            {|
                status = status
                handle = user
            |}
        ) |> ignore
        
    let write_taken_user
        (db_connection:NpgsqlConnection)
        (user: User_handle)
        =
        Log.info $"take user {User_handle.value user} for scraping as a task"
        
        db_connection.Query<User_handle>(
            @"update users_to_scrape 
            set 
                status = @status
                taken_by = @worker
            where 
                handle = @user",
            {|
                status="taken"
                worker=this_working_session_id
                handle = user
            |}
        ) |> ignore


    let write_user_for_scraping
        (database:NpgsqlConnection)
        created_at
        user 
        =
        
        database.Query<User_handle>(
            @"insert into users_to_scrape (
                created_at,
                handle,
                status
            )
            values (
                @created_at,
                @handle,
                'free'
            )
            ",
            {|
                created_at = created_at
                handle = user
            |}
        ) |> ignore
    
    let write_users_for_scraping
        database
        users
        =
        Log.info $"writing {Seq.length users} users as targets for scraping"
        let created_at = DateTime.Now
        users
        |>Seq.iter (write_user_for_scraping database created_at)
        