namespace rvinowise.twitter

open System.Threading.Tasks
open OpenQA.Selenium
open canopy.parallell.functions
open rvinowise.html_parsing
open rvinowise.twitter


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
    
    
    let harvest_following
        root_users
        =
        try
            root_users
            |>Seq.zip Settings.auth_tokens
            |>Array.ofSeq
            |>Array.Parallel.iter(fun (bot_token, user_to_harvest)->
                use db_connection = Database.open_connection() 
                use browser = Scraping.prepare_authentified_browser bot_token
                use parsing_context = Html_parsing.parsing_context()
                Harvest_followers_network.harvest_following_network_around_user
                    parsing_context
                    browser
                    db_connection
                    Settings.repeat_harvesting_if_older_than
                    (User_handle user_to_harvest)
            )
        with
        | :? WebDriverException as exc ->
            Log.error $"""can't harvest acquaintances: {exc.Message}"""|>ignore
    
        
    let announce_competition_successes ()=
        try
            use browser =
                Settings.auth_tokens
                |>Seq.head
                |>Scraping.prepare_authentified_browser 
            Anounce_score.scrape_and_announce_user_state browser.browser
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
            //Scraping.set_canopy_configuration_directories()
            harvest_following rest
        |_->
            //Scraping.set_canopy_configuration_directories()
            announce_competition_successes()
        
        Log.important "bot finished execution."
        0