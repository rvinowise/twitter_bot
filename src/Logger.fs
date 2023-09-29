namespace rvinowise.twitter

open Serilog
open Xunit

    

module Log =
    let logger =
        LoggerConfiguration().WriteTo
            .File(
                "../log/log.txt",
                rollOnFileSizeLimit = true, 
                fileSizeLimitBytes = 1000000000
            )
            .CreateLogger();
    
    let info = logger.Information
    let important message =
        logger.Information message
        printfn $"%s{message}"
    
    let error message =
        logger.Error message
        printfn $"%s{message}"
        message
    
    [<Fact(Skip="manual")>]
    let ``try logging``()=
        for i in 0..1000 do
            info <| sprintf "%s : %i" "hello" 3
//        log ("test1")
//        log ("test2")