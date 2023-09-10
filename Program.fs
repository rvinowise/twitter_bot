namespace rvinowise.twitter

open System.Threading.Tasks
open OpenQA.Selenium
open canopy.classic


module Program =

    
    
    let import_scores_from_googlesheet
        parameters
        =
        Log.important<|sprintf
            "updating scores from google sheet %s %d %s"
            Settings.Google_sheets.score_table_for_import.doc_id
            Settings.Google_sheets.score_table_for_import.page_id
            Settings.Google_sheets.score_table_for_import.page_name
        
        match parameters with
        |start_column::end_column::rest ->
            Import_scores_from_googlesheet.import_scores_between_two_date_columns
                Settings.Google_sheets.score_table_for_import
                    (start_column|>Seq.head)
                    (end_column|>Seq.head)
        |_-> Log.important "specify Start_column and End_column with dates of scores, e.g. 'import E P'"
    
    
    let harvest_network_of_acquaintances
        parameters
        =
        Log.important<|sprintf
            $"harvesting network of social connections: {parameters}"
        match parameters with
        |[root_user] ->
            
            use db_connection = Database.open_connection() 
            
            try
                Scraping.prepare_for_scraping ()
                root_user
                |>User_handle
                |>Harvest_followers_network.harvest_following_network_around_user
                    db_connection
                    Settings.repeat_harvesting_if_older_than
                browser.Quit()
            with
            | :? WebDriverException as exc ->
                Log.error $"""can't scrape acquaintances: {exc.Message}"""|>ignore
                () 
        |_ -> Log.important "specify the root user handle"
        
        
    let announce_competition_successes()=
        try
            Scraping.prepare_for_scraping ()
            Anounce_score.scrape_and_announce_user_state()
            browser.Quit()
        with
        | :? WebDriverException as exc ->
            Log.error $"""can't scrape state of twitter-competitors: {exc.Message}"""|>ignore
            ()
        
        try
            use db_connection = Database.open_connection() 
            Import_referrals_from_googlesheet.import_referrals
                (Googlesheets.create_googlesheet_service())
                db_connection
                Settings.Google_sheets.read_referrals
        with
        | :? TaskCanceledException as exc ->
            Log.error $"""can't read referrals from googlesheet: {exc.Message}"""|>ignore
            ()
    
    
    [<EntryPoint>]
    let main args =
        let args = args|>List.ofArray
        match args with
        | "import"::rest ->
            import_scores_from_googlesheet rest
        | "following"::rest ->
            harvest_network_of_acquaintances rest
        |_->
            announce_competition_successes()
        
        Log.important "bot finished execution."
        0