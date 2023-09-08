namespace rvinowise.twitter

open System
open OpenQA.Selenium
open canopy.classic
open OpenQA.Selenium.Support.UI
open canopy.types

module Scraping =

    let wait = WebDriverWait(browser, TimeSpan.FromSeconds(3));
    
    let element_exists selector =
        let old_wait = browser.Manage().Timeouts().ImplicitWait
        browser.Manage().Timeouts().ImplicitWait <- (TimeSpan.FromMilliseconds(0));
        let elements =
            browser.FindElements(By.XPath(
                selector
            ))
        browser.Manage().Timeouts().ImplicitWait <- old_wait
        elements.Count > 0
    
    let wait_for_element_orig
        seconds
        css_selector
        =
        let old_wait = browser.Manage().Timeouts().ImplicitWait
        browser.Manage().Timeouts().ImplicitWait <- (TimeSpan.FromSeconds(seconds))
        let elements =
            browser.FindElements(By.CssSelector(
                css_selector
            ))
        browser.Manage().Timeouts().ImplicitWait <- old_wait
        elements
        |>Seq.tryHead
    
    let try_element
        css_selector
        =
        browser.FindElements(By.CssSelector(css_selector))
        |>Seq.tryHead
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
        css_selector
        =
        css_selector
        |>try_element
        |>function
        |Some field ->
            Some field.Text
        |None -> None
        
    let page_isnt_showing () =
        element_exists "//*[text()='Something went wrong. Try reloading.']"
    
    
    let parent (base_element: IWebElement) =
        base_element.FindElement(By.XPath(".."))

    let descendant css_selector (base_element: IWebElement)  =
        base_element.FindElement(By.CssSelector(css_selector))
    


    let prepare_for_scraping () =
        canopy.configuration.chromeDir <- System.AppContext.BaseDirectory
        canopy.configuration.firefoxDir <- System.AppContext.BaseDirectory
        
        let authorisation_cookie =
            Cookie(
                "auth_token",
                Settings.auth_token,
                ".twitter.com",
                "/",
                DateTime.Now.AddYears(1);
            )
        
        if Settings.headless = true then
            canopy.classic.start canopy.types.BrowserStartMode.ChromeHeadless
        else
            canopy.classic.start canopy.types.BrowserStartMode.Chrome
        
        browser.Manage().Timeouts().ImplicitWait <- (TimeSpan.FromSeconds(3))
        browser.Manage().Timeouts().PageLoad <- (TimeSpan.FromSeconds(180))
        
        canopy.classic.url Twitter_settings.base_url
        browser.Manage().Cookies.AddCookie(authorisation_cookie)
        //Login_to_twitter.login_to_twitter
        
        Log.info "browser is open... "
    


