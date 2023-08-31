namespace rvinowise.twitter

open System
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open Xunit
open canopy.classic
open FSharp.Configuration


//type Config = AppSettings<"app.config">
type Google_spreadsheet = {
    doc_id: string
    page_id: int
    page_name: string
}

module Settings = 
    open System.Configuration
    
    
    
    let configuration_builder = (ConfigurationBuilder()).AddJsonFile("appsettings.json", false, true);
    let configuration_root = configuration_builder.Build()
    let auth_token = configuration_root["auth_token"]
    let login = configuration_root["login"]
    let password = configuration_root["password"]
    
    let competitors_section =
            configuration_root.GetSection("competitors")
    module Competitors =
        let list = competitors_section["list"]
        (*bot can't recognise whether a participant is removed from the list, or the list didn't load fully.
        to avoid disappearance of participants due to twitter malfunctioning,
        they are included from the recent past*) 
        let include_from_past = int (competitors_section["include_from_past_hours"])
    let headless =  (configuration_root["headless"]) = "true"
    let db_connection_string = configuration_root["db_connection_string"]
    
    module Google_sheets =
        let followers_amount_section =
            configuration_root.GetSection("google_tables").GetSection("followers_amount")
        let followers_amount = {
            Google_spreadsheet.doc_id = followers_amount_section["doc_id"]
            page_id = int followers_amount_section["page_id"]
            page_name = followers_amount_section["page_name"]
        }
        let posts_amount_section =
            configuration_root.GetSection("google_tables").GetSection("posts_amount")
        let posts_amount = {
            Google_spreadsheet.doc_id = posts_amount_section["doc_id"]
            page_id = int posts_amount_section["page_id"]
            page_name = posts_amount_section["page_name"]
        }
        let score_table_for_import = {
            Google_spreadsheet.doc_id = configuration_root["googlesheet_doc_id_for_import"]
            page_id = int configuration_root["googlesheet_page_id_for_import"]
            page_name = configuration_root["googlesheet_page_name_for_import"]
        }