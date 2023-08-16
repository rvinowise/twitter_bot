namespace rvinowise.twitter

open System
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open Xunit
open canopy.classic
open FSharp.Configuration


//type Settings = AppSettings<"appsettings.json">

module Settings = 
    open System.Configuration
    
    let configuration_builder = (ConfigurationBuilder()).AddJsonFile("appsettings.json", false, true);
    let configuration_root = configuration_builder.Build()
    let Username = configuration_root["Username"]
    let Password = configuration_root["Password"]
    let auth_token = configuration_root["auth_token"]
    let transhumanist_community = configuration_root["transhumanist_community"]

type Twitter_user = {
    handle: string;
    name: string
    url: Uri
}

module Twitter_settings =

    let base_url = "https://twitter.com"

    