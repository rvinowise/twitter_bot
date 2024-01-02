namespace rvinowise.twitter

open System
open Microsoft.Extensions.Configuration

//type Config = AppSettings<"app.config">
type Google_spreadsheet = {
    doc_id: string
    page_id: int
    page_name: string
}

type Browser = {
    path: string
    webdriver_version: string
    profile_path: string
    headless: bool
}

module Settings = 
    
    
    let configuration_builder = (ConfigurationBuilder()).AddJsonFile("appsettings.json", false, true);
    let configuration_root = configuration_builder.Build() :> IConfiguration
    let auth_tokens =
        configuration_root.GetSection("auth_tokens").Get<string[]>()
    
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
    let repeat_harvesting_if_older_than =
        let days = 
            configuration_root.GetValue<int>("repeat_harvesting_if_older_than_days",1)
        TimeSpan.FromDays(days)
    let db_connection_string = configuration_root["db_connection_string"]
    let central_db = configuration_root["central_db"]
    
    module Google_sheets =
        
        let followers_amount =
            let followers_amount_section =
                configuration_root.GetSection("google_tables").GetSection("followers_amount")
            {
                Google_spreadsheet.doc_id = followers_amount_section["doc_id"]
                page_id = int followers_amount_section["page_id"]
                page_name = followers_amount_section["page_name"]
            }
        
        let posts_amount =
            let posts_amount_section =
                configuration_root.GetSection("google_tables").GetSection("posts_amount")
            {
                Google_spreadsheet.doc_id = posts_amount_section["doc_id"]
                page_id = int posts_amount_section["page_id"]
                page_name = posts_amount_section["page_name"]
            }
        let read_referrals =
            let posts_amount_section =
                configuration_root.GetSection("google_tables").GetSection("read_referrals")
            {
                Google_spreadsheet.doc_id = posts_amount_section["doc_id"]
                page_id = int posts_amount_section["page_id"]
                page_name = posts_amount_section["page_name"]
            }
    

    let browser =
        let browser_section =
            configuration_root.GetSection("browser")
        {
            path = browser_section["path"]
            webdriver_version = browser_section["webdriver_version"]
            profile_path = browser_section["profile_path"]
            headless = browser_section["headless"] = "true"
        }
        
        