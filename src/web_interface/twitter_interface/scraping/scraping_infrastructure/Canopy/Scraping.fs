namespace rvinowise.web_scraping

open System
open OpenQA.Selenium
open canopy.parallell.functions
open canopy.types
open rvinowise.twitter


type Web_element = IWebElement

module Web_element =
    
    let attribute_value
        attribute
        (web_element: Web_element)
        =
        web_element.GetAttribute(attribute)
    
    let parent (base_element: Web_element) =
        base_element.FindElement(By.XPath(".."))

    let descendant css_selector (base_element: Web_element)  =
        base_element.FindElement(By.CssSelector(css_selector))    
        
module Scraping =

        
    let set_canopy_configuration_directories () =
        canopy.configuration.chromeDir <- System.AppContext.BaseDirectory
        canopy.configuration.firefoxDir <- System.AppContext.BaseDirectory
        
        


