namespace rvinowise.twitter

open System
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open Xunit
open canopy.classic
open FSharp.Configuration




module Twitter_settings =

    let base_url = "https://www.twitter.com"

    let max_post_length = 280
