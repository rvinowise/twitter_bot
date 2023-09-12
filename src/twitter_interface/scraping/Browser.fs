namespace rvinowise.twitter

open System
open OpenQA.Selenium


type Browser(cookies:Cookie seq) =
    
        
    let prepare_browser (cookies:Cookie seq) =
        let browser = 
            if Settings.headless = true then
                canopy.parallell.functions.start canopy.types.BrowserStartMode.ChromeHeadless
            else
                canopy.parallell.functions.start canopy.types.BrowserStartMode.Chrome
    
        browser.Manage().Timeouts().ImplicitWait <- (TimeSpan.FromSeconds(3))
        browser.Manage().Timeouts().PageLoad <- (TimeSpan.FromSeconds(180))
        
        canopy.parallell.functions.url Twitter_settings.base_url browser
        
        cookies
        |>Seq.iter(
            browser.Manage().Cookies.AddCookie
        )
        browser
    
    let mutable private_browser = prepare_browser cookies
    member this.browser = private_browser
        
    member this.restart()=
        this.browser.Quit()
        private_browser <- prepare_browser cookies
    
    interface IDisposable
        with
        member this.Dispose() =
            this.browser.Quit()
            
    static member from_cookie(cookie) =
        new Browser(cookie)