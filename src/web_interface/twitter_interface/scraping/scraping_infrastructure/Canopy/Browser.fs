namespace rvinowise.web_scraping

open System
open OpenQA.Selenium
open OpenQA.Selenium.Chrome
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open WebDriverManager.DriverConfigs.Impl
open WebDriverManager.Helpers

open canopy.types
open rvinowise.twitter


type Browser(cookies:Cookie seq) =
    
    let ensure_webdriver_is_downloaded version =
        let browser_download_path =
            $"""https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/{version}/win64/chromedriver-win64.zip"""
        let local_browser_path =
            $"""{AppDomain.CurrentDomain.BaseDirectory}Chrome\{version}\X64\"""
        WebDriverManager.DriverManager().SetUpDriver(
            browser_download_path,
            local_browser_path+"chromedriver.exe"
        )
        |>sprintf "browser installation result: %s"
        |>Log.important
        local_browser_path
        
    let start_headless_browser (local_browser_path:string) =
        let options4 = ChromeOptions()
        options4.AddArgument("--disable-extensions")
        options4.AddArgument("disable-infobars")
        options4.AddArgument("test-type")
        options4.AddArgument("--headless")
        new ChromeDriver(local_browser_path,options4)
        
    let start_visible_browser (local_browser_path:string) =
        new ChromeDriver(local_browser_path)
    
    let prepare_webdriver (cookies:Cookie seq) =
        
        let local_browser_path = ensure_webdriver_is_downloaded "117.0.5938.92"
        
        let browser = 
            if Settings.headless = true then
                start_headless_browser local_browser_path
            else
                start_visible_browser local_browser_path
    
        browser.Manage().Timeouts().ImplicitWait <- (TimeSpan.FromSeconds(3))
        browser.Manage().Timeouts().PageLoad <- (TimeSpan.FromSeconds(180))
        
        canopy.parallell.functions.url Twitter_settings.base_url browser
        
        cookies
        |>Seq.iter(
            browser.Manage().Cookies.AddCookie
        )
        browser
    
    let mutable private_webdriver = prepare_webdriver cookies
    member this.webdriver = private_webdriver
        
    member this.restart()=
        this.webdriver.Quit()
        private_webdriver <- prepare_webdriver cookies
    
    interface IDisposable
        with
        member this.Dispose() =
            this.webdriver.Quit()
            
    static member from_cookie(cookie) =
        new Browser(cookie)
        
        
module Browser =
    let webdriver (browser:Browser) =
        browser.webdriver
    
    
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
        canopy.parallell.functions.url url_string browser.webdriver
        
    let send_key key (browser:Browser) =
        let actions = Actions(browser.webdriver)
        actions.SendKeys(key).Perform()
    let send_keys keys (browser:Browser) =
        keys
        |>Seq.fold (fun (actions:Actions) ->
            actions.SendKeys
        )
            (Actions(browser.webdriver))
        |>(fun actions -> actions.Perform())
    
    let click_element (element:Web_element) (browser:Browser) =
        canopy.parallell.functions.click element browser.webdriver
        
    let sleep seconds =
        canopy.parallell.functions.sleep seconds
        
    let wait_for_disappearance css seconds (browser:Browser) =
        WebDriverWait(browser.webdriver, TimeSpan.FromSeconds(seconds)).
                    Until(ExpectedConditions.InvisibilityOfElementLocated(By.CssSelector(css)))
                |>ignore
    
    let element
        css
        (browser: Browser)
        =
        canopy.parallell.functions.element css browser.webdriver
    
    let elements
        css
        (browser: Browser)
        =
        canopy.parallell.functions.elements css browser.webdriver
    
    
    let try_element
        (browser:Browser)
        css_selector
        =
        browser.webdriver.FindElements(By.CssSelector(css_selector))
        |>Seq.tryHead
//        try
//            browser
//            |>element css_selector
//            |>Some
//        with
//        | :? CanopyElementNotFoundException as exc ->
//            None
        
    let element_exists
        (browser:Browser)
        css_selector
        =
        (try_element browser css_selector)
        |>Option.isSome
    
    let focus_element
        (browser:Browser)
        css_selector
        =
        browser
        |>element css_selector
        |>fun element->element.SendKeys(Keys.Shift)
    
    let wait_till_disappearance
        (browser: Browser)
        timeout_seconds
        disappearing_css
        =
        Log.info $"starting wait_till_disappearance"
        let start_time = DateTime.UtcNow
        
        let rec check_disappearance () =
            Log.info $"starting check_disappearance"
            let waited_time = DateTime.UtcNow - start_time
            let is_timeout = waited_time > TimeSpan.FromSeconds(timeout_seconds)
            Log.info $"is_timeout is determined as: {is_timeout}"
            if
                is_timeout
            then
                raise <| TimeoutException()
            else
                disappearing_css
                |>try_element browser
                |>function
                |None->
                    Log.info $"element {disappearing_css} isn't found, return waited_time={waited_time}"
                    waited_time
                |_ ->
                    Log.info $"disappearing_css {disappearing_css} is found; waited_time={waited_time}"
                    check_disappearance ()
            
        check_disappearance ()    
        
        
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
        canopy.parallell.functions.read css_selector browser.webdriver