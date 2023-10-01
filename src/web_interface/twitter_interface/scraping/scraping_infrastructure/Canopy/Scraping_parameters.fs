namespace rvinowise.twitter

open System
open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.web_scraping

open FsUnit
open Xunit


type Scraping_parameters(browser: Browser) =
    
    
    member this.timeout_seconds(seconds) =
        browser.browser.Manage().Timeouts().ImplicitWait <- TimeSpan.FromSeconds(seconds)
    
    interface IDisposable
        with
        member this.Dispose() =
            (browser :> IDisposable).Dispose()
            
            
            
module Scraping_parameters =
    
    let wait_seconds seconds browser =
        new Scraping_parameters(browser)