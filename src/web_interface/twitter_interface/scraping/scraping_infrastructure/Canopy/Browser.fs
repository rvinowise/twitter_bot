﻿namespace rvinowise.web_scraping

open System
open System.Diagnostics
open System.IO
open OpenQA.Selenium
open OpenQA.Selenium.Chrome
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers

open canopy.types
open rvinowise.html_parsing
open rvinowise.twitter


type Browser =
    {
        webdriver: ChromeDriver
        profile: Email
    }
    interface IDisposable
        with
        member this.Dispose() =
            Log.debug $"Dispose is called for browser with profile {this.profile}"
            this.webdriver.Quit() 

        
module Browser =
    
    let ensure_webdriver_is_downloaded version =
        let webdriver_download_path =
            if Environment.OSVersion.Platform = PlatformID.Win32NT then
                $"""https://storage.googleapis.com/chrome-for-testing-public/{version}/win64/chromedriver-win64.zip"""
            elif Environment.OSVersion.Platform = PlatformID.Unix then
                $"""https://storage.googleapis.com/chrome-for-testing-public/{version}/linux64/chromedriver-linux64.zip"""
            else
                $"unknown platform {Environment.OSVersion.Platform}, it's not clear, which browser driver to install"
                |>Log.error|>ignore
                $"""https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/{version}/linux64/chromedriver-linux64.zip"""
                
        let local_webdriver_path =
            DirectoryInfo(Settings.browser.path).Parent.FullName
            
        WebDriverManager.DriverManager().SetUpDriver(
            webdriver_download_path,
            Path.Combine(local_webdriver_path,"chromedriver.exe")
        )
        |>sprintf "webdriver installation result: %s"
        |>Log.important
        local_webdriver_path
    
        
    let browser_options
        (local_browser_path: string)
        browser_profile_path
        =
        let options = ChromeOptions()
        options.BinaryLocation <- local_browser_path
        options.AddArgument("--no-sandbox")
        options.AddArgument($"--user-data-dir={browser_profile_path}")
        options.AddArgument("--disable-infobars")
        options.AddArgument("--disable-notifications")
        options.AddArgument("--suppress-message-center-popups")
        options.AddArgument("--ignore-certificate-errors")
        options.AddArgument("--ignore-ssl-errors")
        options.AddArgument("--no-proxy-server")
        
        Settings.browser.options
        |>Array.iter (fun argument_from_settings ->
            options.AddArgument(argument_from_settings)
        )
        let browser_version_as_agent =
            Settings.browser.webdriver_version
            |>fun version -> version.Split '.'
            |>Array.head
            |>fun major_number -> $"{major_number}.0.0.0"
        options.AddArgument($"--user-agent=Chrome/{browser_version_as_agent}")
        options
    
    let start_headless_browser 
        (local_webdriver_path: string)
        (local_browser_path: string)
        browser_profile_path
        =
        let options = browser_options local_browser_path browser_profile_path
        options.AddArgument("--headless=new")
        new ChromeDriver(local_webdriver_path,options,TimeSpan.FromSeconds(180))
        
    let start_visible_browser
        (local_webdriver_path: string)
        (local_browser_path: string)
        browser_profile_path
        =
        let options = browser_options local_browser_path browser_profile_path
        new ChromeDriver(local_webdriver_path, options,TimeSpan.FromSeconds(180))
    
    let run_webdriver profile_path =
        
        let local_webdriver_path =
            ensure_webdriver_is_downloaded Settings.browser.webdriver_version
        let local_browser_path =
            Settings.browser.path
            
        let webdriver = 
            if Settings.browser.headless = true then
                start_headless_browser
                    local_webdriver_path
                    local_browser_path
                    profile_path
            else
                start_visible_browser
                    local_webdriver_path
                    local_browser_path
                    profile_path
    
        webdriver.Manage().Timeouts().ImplicitWait <- (TimeSpan.FromSeconds(3))
        webdriver.Manage().Timeouts().PageLoad <- (TimeSpan.FromSeconds(180))
        
        canopy.parallell.functions.url Twitter_settings.base_url webdriver
        
        webdriver
    

    let rec open_with_profile profile =
        Log.info $"preparing a browser with profile {profile}"
        {
            Browser.webdriver =
                profile
                |>Settings.browser_profile_from_email
                |>run_webdriver
            profile = profile
        }

        
    let open_browser () =
        Settings.browser.profiles
        |>Seq.head
        |>open_with_profile
    
    
    
    let quit (browser: Browser) =
        
        browser.webdriver.Quit()
        Process.GetProcessesByName("chromedriver")
        |>Seq.iter(fun driver_process ->
            Log.debug $"killing process {driver_process.ProcessName}"
            driver_process.Kill()
        )
        
        Log.info $"browser {browser} is closed"
    
    let rec restart_with_profile
        (browser: Browser)
        profile
        =
        try
            quit browser
        with
        | :? InvalidOperationException
        | :? NoSuchWindowException as exc ->
            $"""failed closing the browser: {exc.Message}"""
            |>Log.error|>ignore
        
        try
            open_with_profile profile
        with
        | :? InvalidOperationException as exc ->
            $"""Exception when restarting the browser: {exc.Message}
            trying again"""
            |>Log.error|>ignore
            restart_with_profile browser profile
   
    let restart (browser: Browser) =
       restart_with_profile
           browser
           browser.profile
    
         
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
        |>(_.Perform())
    
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
        browser.webdriver.FindElements(By.CssSelector(css))
        |>Seq.toList
    
    let rec capture_elements_resiliently
        html_context
        css
        (browser: Browser)
        =
        try
            browser
            |>elements css
            |>List.map (Html_node.from_scraped_node_and_context html_context)
        with
        | :? StaleElementReferenceException as exc ->
            $"capturing elements threw an exception: {exc.Message}; trying to capture the elements again"
            |>Log.info
            capture_elements_resiliently
                html_context
                css
                browser
            
        
    
    let try_element
        (browser:Browser)
        css_selector
        =
        browser.webdriver.FindElements(By.CssSelector(css_selector))
        |>Seq.tryHead

        
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
        |>_.SendKeys(Keys.Shift)
    
    let wait_till_disappearance
        (browser: Browser)
        timeout_seconds
        disappearing_css
        =
        //Log.info $"starting wait_till_disappearance"
        let start_time = DateTime.UtcNow
        
        let rec check_disappearance () =
            let waited_time = DateTime.UtcNow - start_time
            let is_timeout = waited_time > TimeSpan.FromSeconds(timeout_seconds)
            if
                is_timeout
            then
                //raise <| TimeoutException()
                $"""reached timeout when waiting for disappearance of "{disappearing_css}" for {timeout_seconds} seconds """
                |>Log.error
                |>ignore
                waited_time
            else
                disappearing_css
                |>try_element browser
                |>function
                |None->
                    waited_time
                |_ ->
                    check_disappearance ()
            
        check_disappearance ()    
        
        
    let try_element_reliably //but slow if the element doesn't appear
        (browser:Browser)
        css_selector
        =
        let oldTime = browser.webdriver.Manage().Timeouts().ImplicitWait;
        browser.webdriver.Manage().Timeouts().ImplicitWait <- TimeSpan.FromMilliseconds(1);
        try try
                let node = element css_selector browser
                Some node
            with | :? CanopyElementNotFoundException ->
                None
        finally
            browser.webdriver.Manage().Timeouts().ImplicitWait <- oldTime;

        
            
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