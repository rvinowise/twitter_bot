﻿namespace rvinowise.twitter

open System
open System.IO
open Microsoft.Extensions.Configuration

type Google_spreadsheet = {
    doc_id: string
    page_name: string
}
module Google_spreadsheet =
    let default_sheet = {
        Google_spreadsheet.doc_id=""
        page_name=""
    }

type Email = Email of string
module Email =
    let value (email:Email) =
        let (Email value) = email
        value
    
    let name (email:Email) =
        email
        |>value
        |>fun value -> value.Split '@'
        |>Array.head

type Browser_settings = {
    path: string
    webdriver_version: string
    headless: bool
    profiles_root: string
    profiles: Email array
    options: string array
}

module Settings = 
    
    let configuration_builder =
        ConfigurationBuilder().
            SetBasePath(DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName).
            AddJsonFile("appsettings.json", false, true);
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
                            page_name = followers_amount_section["page_name"]
                        }
                    
                    let posts_amount =
                        let posts_amount_section =
                            google_tables_section.GetSection("posts_amount")
                        {
                            Google_spreadsheet.doc_id = posts_amount_section["doc_id"]
                            page_name = posts_amount_section["page_name"]
                        }
                    let read_referrals =
                        let posts_amount_section =
                            google_tables_section.GetSection("read_referrals")
                        {
                            Google_spreadsheet.doc_id = posts_amount_section["doc_id"]
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
    let local_database = configuration_root.GetValue<string>("local_database","")
    let central_database = configuration_root.GetValue<string>("central_database","")
    
    
    //module Scraping
    
    
    
    let browser =
        let browser_section =
            configuration_root.GetSection("browser")
        
        {
            path = browser_section["path"]
            webdriver_version = browser_section["webdriver_version"]
            headless = browser_section.GetValue<bool>("headless",true)
            profiles_root = browser_section.GetValue<string>("profiles_root","")
            profiles =
                browser_section.GetSection("profiles").Get<string[]>()
                |>Array.map Email
            options =
                let additional_parameters = browser_section.GetSection("options").Get<string[]>()
                if isNull additional_parameters then
                    [||]
                else
                    additional_parameters
                    
        }
    let repeat_scrolling_timeline = configuration_root.GetValue<int>("repeat_scrolling_timeline",50)
    
    
    let browser_profile_from_email (email: Email) =
        [|
            browser.profiles_root;
            Email.name email
        |]
        |>Path.Combine
        
        
        
        