namespace rvinowise.twitter

open System
open System.Data.Common
open Dapper
open Faithlife.Utility.Dapper
open DeviceId.Encoders
open DeviceId.Formatters
open Npgsql
open NpgsqlTypes
open Xunit
open rvinowise.html_parsing
open rvinowise.twitter
open rvinowise.twitter.database_schema
open rvinowise.twitter.database_schema.tables

open DeviceId
   

module Distributing_jobs_database =
    
    let sql_taking_only_jobs_from_last_batch =
        $"""
        {user_to_scrape.created_at} = (SELECT MAX({user_to_scrape.created_at}) FROM {user_to_scrape})
        """

    let take_next_free_job
        (central_db: NpgsqlConnection)
        worker_id
        =
        
        let free_user =
            central_db.Query<User_handle>(
                $"
                update {user_to_scrape} 
                set 
                    {user_to_scrape.status} = '{Scraping_user_status.db_value Scraping_user_status.Taken}',
                    {user_to_scrape.taken_by} = @worker,
                    {user_to_scrape.when_taken} = @when_taken
                where 
                    {user_to_scrape.account} = (
                        select {user_to_scrape.account} from {user_to_scrape}
                        where 
                            {sql_taking_only_jobs_from_last_batch}
                            and {user_to_scrape.status} = '{Scraping_user_status.db_value Scraping_user_status.Free}'
                            and {user_to_scrape.taken_by} = ''
                        limit 1
                    )
                    and {sql_taking_only_jobs_from_last_batch}
                returning {user_to_scrape.account}   
                ",
                {|
                    worker=worker_id
                    when_taken=DateTime.UtcNow
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
                use central_db = Central_database.open_connection()    
                let taken_job =
                    take_next_free_job
                        central_db
                        worker_id
                taken_job, true
            with
            | :? NpgsqlException
            | :? TimeoutException as exc ->
                $"""Exception {exc.GetType()} when trying to take the next free job from the central datbase: {exc.Message}.
                trying again with reistablishing the connection"""
                |>Log.error|>ignore
                None, false
                
        if is_successful then
            taken_job
        else
            resiliently_take_next_free_job worker_id
       

    let ``try take_next_free_user``()=
        let result =
            take_next_free_job
                (Central_database.open_connection())
            
        ()
        
    let read_next_free_job
        (db_connection: NpgsqlConnection)
        =
        db_connection.Query<User_handle>(
            $"select * from {user_to_scrape}
            where 
                {sql_taking_only_jobs_from_last_batch}
                and {user_to_scrape.status} = 'Free'
                and {user_to_scrape.taken_by} = ''
            "
        )
    
    let read_worker_of_job
        (db_connection: NpgsqlConnection)
        (user: User_handle)
        =
        db_connection.Query<string>(
            $"select {user_to_scrape.taken_by} from {user_to_scrape}
            where 
                {sql_taking_only_jobs_from_last_batch}
                and {user_to_scrape.account} = @user
                and not {user_to_scrape.taken_by} = ''
            ",
            {|
                user=user
            |}
        )|>Seq.tryHead
    

    let ``try read_worker_of_job``()=
        let result =
            read_worker_of_job
                (Central_database.open_connection())
                (User_handle "LapierreLab")
        ()
        
    let write_job_status
        (db_connection:NpgsqlConnection)
        (user: User_handle)
        (status: Scraping_user_status)
        =
        Log.info $"writing status {Scraping_user_status.db_value status} for user {User_handle.value user} in the central database"
        
        db_connection.Query<User_handle>(
            $"update {user_to_scrape} 
            set 
                {user_to_scrape.status}  = @status
            where 
                {sql_taking_only_jobs_from_last_batch}
                and {user_to_scrape.account} = @user",
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
        (result:Parsing_timeline_result)
        =
        check_job_worker_is_same
            db_connection
            worker
            user_task
            
        db_connection.Query<User_handle>(
            $"
            update {user_to_scrape} 
            set
                {user_to_scrape.status}  = @status,
                {user_to_scrape.posts_amount} = @posts_amount,
                {user_to_scrape.likes_amount} = @likes_amount,
                {user_to_scrape.when_completed} = @when_completed
            where
                {sql_taking_only_jobs_from_last_batch}
                and {user_to_scrape.account} = @handle
            ",
            {|
                handle = user_task
                posts_amount = posts_amount
                likes_amount = likes_amount
                when_completed = DateTime.UtcNow
                status=Scraping_user_status.Completed result
            |}
        ) |> ignore
    
    
        
    let rec resiliently_write_final_result
        worker
        (user: User_handle)
        posts_amount
        likes_amount
        (last_result: Parsing_timeline_result)
        =
        let is_successful =
            try
                use central_db = Central_database.open_connection()
                
                write_final_result
                    central_db
                    worker
                    (user: User_handle)
                    posts_amount
                    likes_amount
                    last_result
                true
            with
            | :? NpgsqlException
            | :? TimeoutException
            | :? System.Net.Sockets.SocketException as exc ->
                $"""Exception {exc.GetType()} when trying to set a task as complete in the central datbase: {exc.Message}.
                trying again with reistablishing the connection"""
                |>Log.error|>ignore
                false
                
        if not is_successful then
            resiliently_write_final_result
                worker
                (user: User_handle)
                posts_amount
                likes_amount
                last_result
                

    let read_last_user_jobs_with_status
        (database:NpgsqlConnection)
        (status: Scraping_user_status)
        =
        
        database.Query<User_handle>(
            $"select {user_to_scrape.account} from {user_to_scrape}
            where 
                {sql_taking_only_jobs_from_last_batch}
                and {user_to_scrape.status} = @status
            ",
            {|
                status = status
            |}
        )
    
    let read_jobs_completed_by_worker
        (database:NpgsqlConnection)
        (worker: string)
        =
        let success_marker =
            Success 0
            |>Scraping_user_status.Completed
            |>Scraping_user_status.db_value
            
        database.Query<User_to_scrape_row>(
            $"select 
                {user_to_scrape.account},
                {user_to_scrape.when_completed}
            from {user_to_scrape}
            where 
                {sql_taking_only_jobs_from_last_batch}
                and {user_to_scrape.status} = '{success_marker}'
                and {user_to_scrape.taken_by} = @worker
            ",
            {|
                worker = worker
            |}
        )|>Seq.map(fun job ->
            job.account,job.when_completed    
        )
    
        
    
    
    let read_last_completed_jobs
        (database:NpgsqlConnection)
        =
        database.Query<User_to_scrape_row>(
            $"select 
                {user_to_scrape.account},
                {user_to_scrape.taken_by},
                {user_to_scrape.status},
                {user_to_scrape.posts_amount},
                {user_to_scrape.likes_amount},
                {user_to_scrape.when_taken},
                {user_to_scrape.when_completed}
                
            from {user_to_scrape}
            where 
                {sql_taking_only_jobs_from_last_batch}
                and {user_to_scrape.status} = 'Success'
            "
        )|>Seq.map(fun job ->
            job.account,job.when_completed    
        )
    
    let read_last_unscraped_accounts
        (database:NpgsqlConnection)
        =
        
        database.Query<User_to_scrape_row>(
            $"select 
                {user_to_scrape.account},
                {user_to_scrape.taken_by},
                {user_to_scrape.status},
                {user_to_scrape.posts_amount},
                {user_to_scrape.likes_amount},
                {user_to_scrape.when_taken},
                {user_to_scrape.when_completed}
                
            from {user_to_scrape}
            where 
                {sql_taking_only_jobs_from_last_batch}
                and not {user_to_scrape.status} = 'Success'
            "
        )|>Seq.map _.account
        
    let write_users_for_scraping
        (database:NpgsqlConnection)
        accounts
        =
        Log.info $"writing {Seq.length accounts} accounts as targets for scraping"
        let batch_datetime = DateTime.UtcNow
        
        database.BulkInsert(
            $"""
            insert into {user_to_scrape} (
                {user_to_scrape.created_at},
                {user_to_scrape.account},
                {user_to_scrape.status}
            )
            values (
                @created_at,
                @handle,
                '{Scraping_user_status.db_value Scraping_user_status.Free}'
            )
            ...
            """,
            accounts
            |>Seq.map(fun handle ->
                {|
                    created_at = batch_datetime
                    handle = handle
                |}
            )
        ) |> ignore
    

    
    
    [<Fact>]
    let ``try datetime shift in local_db``()=
        let database = Local_database.open_connection()
        let datetime =
            Datetime.parse_datetime "yyyy-MM-dd HH:mm:ss.ffffff" "2024-01-24 14:29:00.250168"
        
        database.Query<Post_header_row>(
            $"select * from {post.header}
            where 
                {post.header.when_scraped} = @datetime
            ",
            {|
                datetime =  datetime 
            |}
        )
        |>List.ofSeq
        |>ignore
        
        ()
        
    [<Fact>]
    let ``try datetime shift in central_db``()=
        let database = Central_database.open_connection()
        let datetime =
            Datetime.parse_datetime "yyyy-MM-dd HH:mm:ss.ffffff" "2024-01-22 20:34:39.745473"
        
        database.Query<User_to_scrape_row>(
            $"select * from {user_to_scrape}
            where 
                {user_to_scrape.created_at} = @datetime
            ",
            {|
                datetime =  datetime 
            |}
        )
        |>List.ofSeq
        |>ignore
        
        ()