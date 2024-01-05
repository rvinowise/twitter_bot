namespace rvinowise.twitter

open System
open Dapper
open DeviceId.Encoders
open DeviceId.Formatters
open Npgsql
open Xunit
open rvinowise.twitter
open rvinowise.twitter.database

open DeviceId
   




module Central_task_database =
    
    
    let open_connection () =
        let db_connection = Database.open_connection Settings.central_db
        
        Twitter_database.set_twitter_type_handlers()
        
        db_connection    
    

    
    let take_next_free_job
        (central_db: NpgsqlConnection)
        worker_id
        =
        
        let free_user =
            central_db.Query<User_handle>(
                $"
                update users_to_scrape 
                set 
                    {tables.users_to_scrape.status} = '{Scraping_user_status.db_value Scraping_user_status.Taken}',
                    {tables.users_to_scrape.taken_by} = @worker,
                    {tables.users_to_scrape.when_taken} = @when_taken
                where 
                    handle = (
                        select {tables.users_to_scrape.handle} from {tables.users_to_scrape}
                        where 
                            {tables.users_to_scrape.created_at} = (SELECT MAX({tables.users_to_scrape.created_at}) FROM {tables.users_to_scrape})
                            and {tables.users_to_scrape.status} = '{Scraping_user_status.db_value Scraping_user_status.Free}'
                            and {tables.users_to_scrape.taken_by} = ''
                        limit 1
                    )
                returning handle    
                ",
                {|
                    worker=worker_id
                    when_taken=DateTime.Now
                |}
            )|>Seq.tryHead
        
        match free_user with
        |Some user ->
            Log.info $"next free user {User_handle.value user} is taken from the central database"   
        |None ->
            Log.info "the central database didn't return next free user"
            
        free_user
   
            
    let rec resiliently_take_next_free_job worker_id =
        
        let taken_job,is_successful =
            try
                use central_db = open_connection()    
                let taken_job =
                    take_next_free_job
                        central_db
                        worker_id
                taken_job, true
            with
            | :? NpgsqlException as exc ->
                $"""Exception {exc.GetType()} when trying to take the next free job from the central datbase: {exc.Message}.
                trying again with reistablishing the connection"""
                |>Log.error|>ignore
                None, false
                
        if is_successful then
            taken_job
        else
            resiliently_take_next_free_job worker_id
       
    [<Fact(Skip="manual")>]
    let ``try take_next_free_user``()=
        let result =
            take_next_free_job
                (open_connection())
            
        ()
        
    let read_next_free_job
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
    
    let read_worker_of_job
        (db_connection: NpgsqlConnection)
        (user: User_handle)
        =
        db_connection.Query<string>(
            $"select {tables.users_to_scrape.taken_by} from {tables.users_to_scrape}
            where 
                {tables.users_to_scrape.created_at} = (
                        SELECT MAX({tables.users_to_scrape.created_at}) FROM {tables.users_to_scrape}
                    )
                and {tables.users_to_scrape.handle} = @user
                and not {tables.users_to_scrape.taken_by} = ''
            ",
            {|
                user=user
            |}
        )|>Seq.tryHead
    
    [<Fact>]//(Skip="manual")
    let ``try read_worker_of_job``()=
        let result =
            read_worker_of_job
                (open_connection())
                (User_handle "LapierreLab")
        ()
        
    let write_job_status
        (db_connection:NpgsqlConnection)
        (user: User_handle)
        (status: Scraping_user_status)
        =
        Log.info $"writing status {Scraping_user_status.db_value status} for user {User_handle.value user} in the central database"
        
        db_connection.Query<User_handle>(
            $"update {tables.users_to_scrape} 
            set 
                {tables.users_to_scrape.status}  = @status
            where 
                {tables.users_to_scrape.handle} = @user",
            {|
                status = status
                handle = user
            |}
        ) |> ignore
    
    
    let check_job_worker_is_same
        (db_connection:NpgsqlConnection)
        worker
        (user: User_handle)
        =
        let last_worker =
            read_worker_of_job
                db_connection
                user
        match last_worker with
        |None ->
            $"the job of scraping user {User_handle.value user} wasn't taken by anybody, but it is being updated by worker {worker} "
            |>Log.error|>ignore
        |Some last_worker ->
            if last_worker <> worker then
                $"the job of scraping user {User_handle.value user} was taken by worker {last_worker}, but another worker {worker} is updating it"
                |>Log.error|>ignore
        
    let update_user_status
        (db_connection:NpgsqlConnection)
        worker
        (user: User_handle)
        (status: Scraping_user_status)
        =
        check_job_worker_is_same
            db_connection
            worker
            user
            
        write_job_status
            db_connection
            user
            status
    
    let write_final_result
        (db_connection:NpgsqlConnection)
        worker
        (user_task: User_handle)
        posts_amount
        likes_amount
        (result:Scraping_user_status)
        =
        check_job_worker_is_same
            db_connection
            worker
            user_task
            
        db_connection.Query<User_handle>(
            $"update {tables.users_to_scrape} 
            set
                {tables.users_to_scrape.status}  = '{result}',
                {tables.users_to_scrape.posts_amount} = @posts_amount,
                {tables.users_to_scrape.likes_amount} = @likes_amount,
                {tables.users_to_scrape.when_completed} = @when_completed
            where 
                {tables.users_to_scrape.handle} = @handle",
            {|
                handle = user_task
                posts_amount = posts_amount
                likes_amount = likes_amount
                when_completed = DateTime.Now
            |}
        ) |> ignore
    
    
        
    let rec resiliently_write_final_result
        worker
        (user: User_handle)
        posts_amount
        likes_amount
        result
        =
        let is_successful =
            try
                use central_db = open_connection()
                
                write_final_result
                    central_db
                    worker
                    (user: User_handle)
                    posts_amount
                    likes_amount
                    result
                true
            with
            | :? NpgsqlException as exc ->
                $"""Exception {exc.GetType()} when trying to set task as complete in the central datbase: {exc.Message}.
                trying again with reistablishing the connection"""
                |>Log.error|>ignore
                false

                
        if not is_successful then
            resiliently_write_final_result
                worker
                (user: User_handle)
                posts_amount
                likes_amount
                result
                

    let read_last_user_jobs_with_status
        (database:NpgsqlConnection)
        (status: Scraping_user_status)
        =
        
        database.Query<string>(
            $"select {tables.users_to_scrape.handle} from {tables.users_to_scrape}
            where 
                {tables.users_to_scrape.created_at} = (
                        SELECT MAX({tables.users_to_scrape.created_at}) FROM {tables.users_to_scrape}
                    )
                and {tables.users_to_scrape.status} = '{status}'
            "
        )|>Seq.tryHead
    
    type User_to_scrape = {
        handle: User_handle
        posts_amount: int
        likes_amount: int
        taken_by: string
    }
    
    let read_results_of_last_jobs
        (database:NpgsqlConnection)
        =
        
        database.Query<string>(
            $"select 
                {tables.users_to_scrape.handle},
                {tables.users_to_scrape.posts_amount},
                {tables.users_to_scrape.likes_amount},
                {tables.users_to_scrape.taken_by},
                
            from {tables.users_to_scrape}
            where 
                {tables.users_to_scrape.created_at} = (
                        SELECT MAX({tables.users_to_scrape.created_at}) FROM {tables.users_to_scrape}
                    )
                and {tables.users_to_scrape.status} = '{Scraping_user_status.Completed}'
            "
        )|>Seq.tryHead
    
    let write_user_for_scraping
        (database:NpgsqlConnection)
        created_at
        user 
        =
        
        database.Query<User_handle>(
            $"insert into {tables.users_to_scrape} (
                {tables.users_to_scrape.created_at},
                {tables.users_to_scrape.handle},
                {tables.users_to_scrape.status}
            )
            values (
                @created_at,
                @handle,
                '{Scraping_user_status.db_value Scraping_user_status.Free}'
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
        