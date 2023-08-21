namespace rvinowise.twitter



module Program =

    [<EntryPoint>]
    let main args =
        //printfn "Arguments passed to function : %A" args
        Anounce_score.scrape_and_announce_score()
        0