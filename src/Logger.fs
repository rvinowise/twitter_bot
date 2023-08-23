namespace rvinowise.twitter

open System
open System.Configuration
open System.Runtime.InteropServices
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open Serilog
open Serilog.Formatting.Compact
open Xunit
open canopy.classic
open Dapper
open Npgsql

    

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
    
    [<Fact>]
    let ``try logging``()=
        for i in 0..1000 do
            info <| sprintf "%s : %i" "hello" 3
//        log ("test1")
//        log ("test2")