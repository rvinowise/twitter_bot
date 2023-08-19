namespace rvinowise.twitter

open OpenQA.Selenium
open System
open System.Collections.ObjectModel
open System.Configuration
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.classic
open Dapper
open FParsec

module Scroll_wheel =
    
    exception WebDriverException of string 
    let wheel_element
        deltaY (*= 120*)
        element
        =
        let script =
            $"""
            var element = document.querySelector("%s{element}");
            var deltaY = 120;
            var box = element.getBoundingClientRect();
            var clientX = box.left + (0 || box.width / 2);
            var clientY = box.top + (0 || box.height / 2);
            var target = element.ownerDocument.elementFromPoint(clientX, clientY);

            for (var e = target; e; e = e.parentElement) {{
                if (e === element) {{
                    target.dispatchEvent(new MouseEvent('mouseover', {{view: window, bubbles: true, cancelable: true, clientX: clientX, clientY: clientY}}));
                    target.dispatchEvent(new MouseEvent('mousemove', {{view: window, bubbles: true, cancelable: true, clientX: clientX, clientY: clientY}}));
                    target.dispatchEvent(new WheelEvent('wheel',     {{view: window, bubbles: true, cancelable: true, clientX: clientX, clientY: clientY, deltaY: deltaY}}));
                    return;
                }}
            }}    
            return 'Element is not interactable';
            """
        
        let error = js script
        //let test0 = error.ToString()
        let test1 = string error
        error
//        if error then
//            raise (WebDriverException error)
//        else ()

