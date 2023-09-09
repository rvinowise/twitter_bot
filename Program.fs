namespace rvinowise.twitter

open System.Threading.Tasks
open OpenQA.Selenium



module Program =

    
    
    let import_scores_from_console
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
    
    let announce_competition_successes()=
        try
            Anounce_score.scrape_and_announce_user_state()
        with
        | :? WebDriverException as exc ->
            Log.error $"""can't scrape state of twitter-competitors: {exc.Message}"""|>ignore
            ()
        
        try
            Import_referrals_from_googlesheet.import_referrals
                (Googlesheets.create_googlesheet_service())
                (new Social_competition_database())
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
            import_scores_from_console rest
        |_->
            announce_competition_successes()
        
        Log.important "bot finished execution."
        0