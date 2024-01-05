namespace rvinowise.twitter

open System
open Microsoft.Extensions.Configuration

type Google_spreadsheet = {
    doc_id: string
    page_id: int
    page_name: string
}
module Google_spreadsheet =
    let default_sheet = {
        Google_spreadsheet.doc_id=""
        page_id=0
        page_name=""
    }

type Browser = {
    path: string
    webdriver_version: string
    profile_path: string
    headless: bool
}

module Settings = 
    
    let configuration_builder = ConfigurationBuilder().AddJsonFile("appsettings.json", false, true);
    let configuration_root = configuration_builder.Build() :> IConfiguration
    
    
    module Influencer_competition =
        
        let competition_section =
                configuration_root.GetSection("influencer_competition")
        module Competitors =
            
            let list,include_from_past =
                if competition_section.Exists() then
                    let competitors_section =
                        competition_section.GetSection("competitors")
                    let list = competitors_section["list"]
                    (*bot can't recognise whether a participant is removed from the list, or the list didn't load fully.
                    to avoid disappearance of participants due to twitter malfunctioning,
                    they are included from the recent past*) 
                    let include_from_past = competitors_section.GetValue<int>("include_from_past_hours",72)
                    list,include_from_past
                else
                    "",0
        
        module Google_sheets =
            
            let followers_amount,posts_amount,read_referrals =
            
                if competition_section.Exists() then
                    let google_tables_section =
                        competition_section.GetSection("google_tables")
                    
                    let followers_amount =
                        let followers_amount_section =
                            google_tables_section.GetSection("followers_amount")
                            
                        {
                            Google_spreadsheet.doc_id = followers_amount_section["doc_id"]
                            page_id = int followers_amount_section["page_id"]
                            page_name = followers_amount_section["page_name"]
                        }
                    
                    let posts_amount =
                        let posts_amount_section =
                            google_tables_section.GetSection("posts_amount")
                        {
                            Google_spreadsheet.doc_id = posts_amount_section["doc_id"]
                            page_id = int posts_amount_section["page_id"]
                            page_name = posts_amount_section["page_name"]
                        }
                    let read_referrals =
                        let posts_amount_section =
                            google_tables_section.GetSection("read_referrals")
                        {
                            Google_spreadsheet.doc_id = posts_amount_section["doc_id"]
                            page_id = int posts_amount_section["page_id"]
                            page_name = posts_amount_section["page_name"]
                        }
                    followers_amount,posts_amount,read_referrals
                else
                    Google_spreadsheet.default_sheet,
                    Google_spreadsheet.default_sheet,
                    Google_spreadsheet.default_sheet
                    

    
    
    //module social_connections    
    let repeat_harvesting_if_older_than =
        if configuration_root.GetSection("repeat_harvesting_if_older_than_days").Exists() then
            let days = 
                configuration_root.GetValue<int>("repeat_harvesting_if_older_than_days",0)
            TimeSpan.FromDays(days)
        else
            TimeSpan.MaxValue
    
    //module Databases
    let db_connection_string = configuration_root.GetValue<string>("db_connection_string","")
    let central_db = configuration_root.GetValue<string>("central_db","")
    
    
    //module Scraping
    
    let browser =
        let browser_section =
            configuration_root.GetSection("browser")
        {
            path = browser_section["path"]
            webdriver_version = browser_section["webdriver_version"]
            profile_path = browser_section["profile_path"]
            headless = browser_section.GetValue<bool>("headless",true)
        }
    let repeat_scrolling_timeline = configuration_root.GetValue<int>("repeat_scrolling_timeline",50)    