namespace rvinowise.twitter

open System
open OpenQA.Selenium
open canopy.parallell.functions
open canopy.types

module Scraping =
    
    
    let try_element
        (browser:IWebDriver)
        css_selector
        =
        browser.FindElements(By.CssSelector(css_selector))
        |>Seq.tryHead
        
    let try_element_reliable //but slow if the element doesn't appear
        (browser:IWebDriver)
        css_selector
        =
        let oldTime = browser.Manage().Timeouts().ImplicitWait;
        browser.Manage().Timeouts().ImplicitWait <- TimeSpan.FromMilliseconds(1);
        try try
                let node = element css_selector
                Some node
            with | :? CanopyElementNotFoundException ->
                None
        finally
            browser.Manage().Timeouts().ImplicitWait <- oldTime;

        
            
    let try_text
        (browser:IWebDriver)
        css_selector
        =
        css_selector
        |>try_element browser
        |>function
        |Some field ->
            Some field.Text
        |None -> None
        
    
    
    let parent (base_element: IWebElement) =
        base_element.FindElement(By.XPath(".."))

    let descendant css_selector (base_element: IWebElement)  =
        base_element.FindElement(By.CssSelector(css_selector))
    

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
        
    let set_canopy_configuration_directories () =
        canopy.configuration.chromeDir <- System.AppContext.BaseDirectory
        canopy.configuration.firefoxDir <- System.AppContext.BaseDirectory
        
        


