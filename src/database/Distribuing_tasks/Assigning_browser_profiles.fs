namespace rvinowise.twitter

open System
open Dapper
open DeviceId.Encoders
open DeviceId.Formatters
open Npgsql
open Xunit
open rvinowise.twitter
open rvinowise.twitter.database



module Assigning_browser_profiles =
   
    
    let release_browser_profile
        (central_db: NpgsqlConnection)
        worker_id
        //profile_email
        =
        central_db.Query<Email>(
            $"
            update {tables.browser_profile} 
            set 
                {tables.browser_profile.worker} = '',
                {tables.browser_profile.last_used} = now()
            where 
                {tables.browser_profile.worker} = @worker
            returning {tables.browser_profile.email}
            ",
            {|
                worker=worker_id
                when_taken=DateTime.Now
            |}
        )|>Seq.tryHead
    
    [<Fact>]
    let ``try release_browser_profile``()=
        let result =
            release_browser_profile
                (Central_task_database.open_connection())
                "Black_box"
        ()        
     
    let take_next_free_profile
        (central_db: NpgsqlConnection)
        worker_id
        =
        
        let free_profile =
            central_db.Query<Email>(
                $"
                update {tables.browser_profile} 
                set 
                    {tables.browser_profile.worker} = @worker,
                    {tables.browser_profile.last_used} = @last_used
                where 
                    {tables.browser_profile.email} = (
                        select {tables.browser_profile.email} from {tables.browser_profile}
                        where 
                            {tables.browser_profile.last_used} = (SELECT MIN({tables.browser_profile.last_used}) FROM {tables.browser_profile})
                            and {tables.browser_profile.worker} = ''
                        limit 1
                    )
                returning {tables.browser_profile.email}    
                ",
                {|
                    worker=worker_id
                    when_taken=DateTime.Now
                |}
            )|>Seq.tryHead
        
        match free_profile with
        |Some profile ->
            Log.info $"next free profile {profile} is taken from the central database"   
        |None ->
            Log.info "the central database didn't return next free profile"
            
        free_profile
        
    let switch_profile
        (central_db: NpgsqlConnection)
        worker_id
        =
        release_browser_profile
            central_db
            worker_id
        |>ignore
        
        take_next_free_profile
            central_db
            worker_id