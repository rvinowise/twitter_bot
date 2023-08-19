namespace rvinowise.twitter

open System
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open Xunit
open canopy.classic
open FSharp.Configuration


//type Config = AppSettings<"app.config">

module Settings = 
    open System.Configuration
    
    let configuration_builder = (ConfigurationBuilder()).AddJsonFile("appsettings.json", false, true);
    let configuration_root = configuration_builder.Build()
    let Username = configuration_root["Username"]
    let Password = configuration_root["Password"]
    let auth_token = configuration_root["auth_token"]
    let transhumanist_community = configuration_root["transhumanist_community"]
    let transhumanist_list = configuration_root["transhumanist_list"]
    let headless =  (configuration_root["headless"]) = "true"
    let db_connection_string = configuration_root["db_connection_string"]
    let scores_export_path = configuration_root["scores_export_path"]

