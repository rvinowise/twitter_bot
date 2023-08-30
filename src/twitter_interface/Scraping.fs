namespace rvinowise.twitter

open System
open OpenQA.Selenium
open canopy.classic

module Scraping =
    open OpenQA.Selenium.Support.UI

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
        try
            let elem = element css_selector
            Some elem
        with
        |exc ->
            None
            
        
    let page_isnt_showing () =
        element_exists "//*[text()='Something went wrong. Try reloading.']"
    
    
    let parent (base_element: IWebElement) =
        base_element.FindElement(By.XPath(".."))

    let descendant css_selector (base_element: IWebElement)  =
        base_element.FindElement(By.CssSelector(css_selector))
    



    


