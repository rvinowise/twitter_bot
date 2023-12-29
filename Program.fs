namespace rvinowise.twitter

open System.Threading.Tasks
open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.web_scraping


module Program =

    
    
    let harvest_following
        root_users
        =
        try
            root_users
            |>Seq.zip Settings.auth_tokens
            |>Array.ofSeq
            |>Array.Parallel.iter(fun (bot_token, user_to_harvest)->
                use db_connection = Twitter_database.open_connection() 
                use browser = Browser.prepare_authentified_browser bot_token
                use parsing_context = Html_parsing.parsing_context()
                Harvest_followers_network.harvest_following_network_around_user
                    browser
                    parsing_context
                    db_connection
                    Settings.repeat_harvesting_if_older_than
                    (User_handle user_to_harvest)
            )
        with
        | :? WebDriverException as exc ->
            Log.error $"""can't harvest acquaintances: {exc.Message}"""|>ignore
    
        
    let announce_competition_successes ()=
        try
            let browser = Browser.open_browser()
            announce_score.scrape_and_announce_user_state browser
            Browser.close_browser browser
        with
        | :? WebDriverException as exc ->
            Log.error $"""can't scrape state of twitter-competitors: {exc.Message}"""|>ignore
        | exc ->
            Log.error $"exception during scraping and announcing twitter scores: {exc.Message}"|>ignore
        
        try
            Import_referrals_from_googlesheet.import_referrals
                (Googlesheet.create_googlesheet_service())
                (Twitter_database.open_connection())
                Settings.Google_sheets.read_referrals
        with
        | :? TaskCanceledException as exc ->
            Log.error $"""can't read referrals from googlesheet: {exc.Message}"""|>ignore
            ()
        | exc ->
            Log.error $"exception during importing referrals from googlesheet: {exc.Message}"|>ignore
    
    let announce_user_interactions() =
        try
            Announce_user_interactions.scrape_and_announce_user_interactions
                (Browser.open_browser())
        with
        | :? WebDriverException as exc ->
            Log.error $"""can't announce interactions between users (as an adjacency matrix) : {exc.Message}"""|>ignore
            ()
            
    
    [<EntryPoint>]
    let main args =
        let args = args|>List.ofArray
        match args with
        | "following"::rest ->
            //Scraping.set_canopy_configuration_directories()
            harvest_following rest
        | "interactions"::rest ->
            announce_user_interactions()
        |_->
            //Scraping.set_canopy_configuration_directories()
            announce_competition_successes()
        
        Log.important "bot finished execution."
        0