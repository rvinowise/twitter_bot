namespace rvinowise.web_scraping

open System
open OpenQA.Selenium
open OpenQA.Selenium.Chrome
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open WebDriverManager.DriverConfigs.Impl
open WebDriverManager.Helpers

open rvinowise.twitter


type Browser(cookies:Cookie seq) =
        
    let prepare_browser (cookies:Cookie seq) =
        let browser = 
            if Settings.headless = true then
                canopy.parallell.functions.start canopy.types.BrowserStartMode.ChromeHeadless
            else
//                let test = WebDriverManager.DriverManager().SetUpDriver(
//                    ChromeConfig(),
//                    VersionResolveStrategy.Latest
//                )
                let local_path =
                    @"C:\prj\twitter_scraper\bin\Debug\net7.0\Chrome\117.0.5938.92\X64\chromedriver.exe"
                let test = WebDriverManager.DriverManager().SetUpDriver(
                    @"https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/117.0.5938.92/win64/chromedriver-win64.zip",
                    local_path
                )
                new ChromeDriver(local_path)
    
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
        
        
module Browser =
    let browser (browser:Browser) =
        browser.browser
    
    
    let authorisation_cookie auth_token =
        Cookie(
            "auth_token",
            auth_token,
            ".twitter.com",
            "/",
            DateTime.Now.AddYears(1);
        )
    
    let prepare_authentified_browser token =
        Log.info $"preparing a browser with twitter authorisation token: {token} "
        token
        |>authorisation_cookie
        |>Seq.singleton
        |>Browser.from_cookie
        
    let open_url url_string (browser: Browser) =
        canopy.parallell.functions.url url_string browser.browser
        
    let send_key key (browser:Browser) =
        let actions = Actions(browser.browser)
        actions.SendKeys(key).Perform()
    let send_keys keys (browser:Browser) =
        keys
        |>Seq.fold (fun (actions:Actions) ->
            actions.SendKeys
        )
            (Actions(browser.browser))
        |>(fun actions -> actions.Perform())
    
    let click_element (element:Web_element) (browser:Browser) =
        canopy.parallell.functions.click element browser.browser
        
    let sleep seconds =
        canopy.parallell.functions.sleep seconds
        
    let wait_for_disappearance css seconds (browser:Browser) =
        WebDriverWait(browser.browser, TimeSpan.FromSeconds(seconds)).
                    Until(ExpectedConditions.InvisibilityOfElementLocated(By.CssSelector(css)))
                |>ignore
    
    let element
        css
        (browser: Browser)
        =
        canopy.parallell.functions.element css browser.browser
    
    let elements
        css
        (browser: Browser)
        =
        canopy.parallell.functions.elements css browser.browser
    
    let try_element
        (browser:Browser)
        css_selector
        =
        browser.browser.FindElements(By.CssSelector(css_selector))
        |>Seq.tryHead
        
//    let try_element_reliably //but slow if the element doesn't appear
//        (browser:IWebDriver)
//        css_selector
//        =
//        let oldTime = browser.Manage().Timeouts().ImplicitWait;
//        browser.Manage().Timeouts().ImplicitWait <- TimeSpan.FromMilliseconds(1);
//        try try
//                let node = element css_selector
//                Some node
//            with | :? CanopyElementNotFoundException ->
//                None
//        finally
//            browser.Manage().Timeouts().ImplicitWait <- oldTime;

        
            
    let try_text
        (browser:Browser)
        css_selector
        =
        css_selector
        |>try_element browser
        |>function
        |Some field ->
            Some field.Text
        |None -> None
        
    let read css_selector (browser:Browser) =
        canopy.parallell.functions.read css_selector browser.browser