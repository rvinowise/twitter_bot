namespace rvinowise.twitter

open System
open Dapper
open DeviceId.Encoders
open DeviceId.Formatters
open Npgsql
open OpenQA.Selenium
open Xunit
open rvinowise.twitter
open rvinowise.twitter.database_schema
open rvinowise.web_scraping



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
                when_taken=DateTime.UtcNow
            |}
        )|>Seq.tryHead
    

    let ``try release_browser_profile``()=
        let result =
            release_browser_profile
                (Central_database.open_connection())
                "Black_box"
        ()        
     
    let take_next_free_profile
        (central_db: NpgsqlConnection)
        possible_profiles
        worker_id
        =
        
        let free_profile =
            central_db.Query<Email>(
                $"
                update {tables.browser_profile} 
                set 
                    {tables.browser_profile.worker} = @worker,
                    {tables.browser_profile.last_used} = now()
                where 
                    {tables.browser_profile.email} = (
                        select {tables.browser_profile.email} from {tables.browser_profile}
                        where 
                            {tables.browser_profile.worker} = ''
                            and {tables.browser_profile.email} = any (@possible_profiles)
                        order by {tables.browser_profile.last_used}
                        limit 1
                    )
                returning {tables.browser_profile.email}    
                ",
                {|
                    worker=worker_id
                    possible_profiles=
                        possible_profiles
                        |>Seq.map Email.value
                        |>Array.ofSeq 
                |}
            )|>Seq.tryHead
        
        match free_profile with
        |Some profile ->
            Log.info $"next free profile {profile} is taken from the central database"   
        |None ->
            Log.info "the central database didn't return next free profile"
            
        free_profile
   
    let read_profile_taken_by_this_worker
        (central_db: NpgsqlConnection)
        worker_id
        =
        central_db.Query<Email>(
            $"
            select 
                {tables.browser_profile.email}
            from
                {tables.browser_profile}
            where 
                {tables.browser_profile.worker} = @worker
            ",
            {|
                worker=worker_id
            |}
        )|>Seq.tryHead
        

    let ``try take_next_free_profile``()=
        let result =
            Local_database.open_connection()
            |>This_worker.this_worker_id
            |>take_next_free_profile
                (Central_database.open_connection())
                [
                    Email "victortwitter@yandex.com"
                    Email "nonexistent@yandex.com"
                    Email "tehprom.moscow@gmail.com"
                ]
        ()
        
    let switch_profile_in_central_database
        (central_db: NpgsqlConnection)
        possible_profiles
        worker_id
        =
        release_browser_profile
            central_db
            worker_id
        |>ignore
        
        take_next_free_profile
            central_db
            possible_profiles
            worker_id
            
    let rec open_browser_with_free_profile 
        (central_db: NpgsqlConnection)
        worker_id
        =
        try
            read_profile_taken_by_this_worker
                central_db
                worker_id
            |>function
            |Some profile ->
                $"worker {worker_id} was already holding the profile {profile}, keep using it"
                |>Log.info
                Some profile
            |None ->
                take_next_free_profile
                    central_db
                    Settings.browser.profiles
                    worker_id
            |>function
            |Some email ->
                email
                |>Browser.open_with_profile 
            |None ->
                "can't open a browser with a free profile from the central database, because it didn't return any free profile"
                |>Log.error
                |>Harvesting_exception
                |>raise 
        with
        | :? WebDriverException as exc ->
            $"failed to open browser: {exc.Message},
            trying again"
            |>Log.error|>ignore
            open_browser_with_free_profile
                central_db
                worker_id
            
        
    let rec switch_profile
        (central_db: NpgsqlConnection)
        worker_id
        (browser:Browser)
        =
        try 
            switch_profile_in_central_database
                central_db
                Settings.browser.profiles
                worker_id
            |>function
            |Some profile_email ->
                profile_email
                |>Browser.restart_with_profile browser
            |None ->
                "can't switch browser profiles, because the central database didn't yield the next free one"
                |>Log.error|>ignore
                browser
        with
        | :? OpenQA.Selenium.WebDriverException as exc ->
            $"failed switching profiles: {exc.Message}, trying again"
            |>Log.error|>ignore
            switch_profile
                central_db
                worker_id
                browser