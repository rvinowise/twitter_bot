namespace rvinowise.twitter

open System
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open Xunit
open canopy.parallell.functions
open FSharp.Configuration
open rvinowise.twitter


module Utils =
    let int_to_string_signed int =
        if int>=0 then
           "+"+string int
        else
           string int
           
           
    let has_different_items items =
        if Seq.length items < 2 then
            false
        else
            items
            |>Seq.forall((=) (Seq.head items))
            |>not