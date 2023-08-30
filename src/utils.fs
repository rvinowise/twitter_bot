namespace rvinowise.twitter

open System
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open Xunit
open canopy.classic
open FSharp.Configuration
open rvinowise.twitter


module Utils =
    let int_to_string_signed int =
        if int>=0 then
           "+"+string int
        else
           string int