namespace rvinowise.twitter

open System
open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.web_scraping

open FsUnit
open Xunit


type Scraping_parameters(browser: Browser) =
    
    let mutable old_timeout = browser.webdriver.Manage().Timeouts().ImplicitWait
    
    member this.timeout_seconds(seconds) =
        old_timeout <- browser.webdriver.Manage().Timeouts().ImplicitWait
        browser.webdriver.Manage().Timeouts().ImplicitWait <- TimeSpan.FromSeconds(seconds)
        this
    
    interface IDisposable
        with
        member this.Dispose() =
            browser.webdriver.Manage().Timeouts().ImplicitWait <- old_timeout
            
            
            
module Scraping_parameters =
    
    let wait_seconds seconds browser =
        (new Scraping_parameters(browser)).timeout_seconds seconds
