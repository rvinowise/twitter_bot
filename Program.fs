namespace rvinowise.twitter



module Program =

    [<EntryPoint>]
    let main args =
        //printfn "Arguments passed to function : %A" args
        Anounce_score.``scrape and announce score``()
        0