namespace rvinowise.twitter



module Program =

    [<EntryPoint>]
    let main args =
        //printfn "Arguments passed to function : %A" args
        let args = args|>List.ofArray
        match args with
        | "import"::rest ->
            Log.important<|sprintf
                "updating scores from google sheet %s %d %s"
                Settings.score_table_for_import.doc_id
                Settings.score_table_for_import.page_id
                Settings.score_table_for_import.page_name
            
            match rest with
            |start_column::end_column::rest ->
                Import_scores_from_googlesheet.import_scores_between_two_date_columns
                    Settings.score_table_for_import
                        (start_column|>Seq.head)
                        (end_column|>Seq.head)
            |_-> Log.important "specify Start_column and End_column with dates of scores, e.g. 'import E P'"
        |_->
            Anounce_score.scrape_and_announce_score()
        
        Log.important "bot finished execution."
        0