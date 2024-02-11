namespace rvinowise.twitter

open System
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open canopy.types
open rvinowise.html_parsing
open rvinowise.twitter.database_schema
open rvinowise.twitter.database_schema.tables
open rvinowise.web_scraping

open FsUnit
open Xunit

module Harvest_list_members =
   
    (* 2024-02-11
    taken single users by query "transhumanism" "transhumanist" "immortalism" "immortalist"
    
    members of lists by the query:
    "e/acc"
    
    *)
    
    let lists =
        [
            "https://twitter.com/i/lists/228873043"
            "https://twitter.com/i/lists/1507135025921159170"
            "https://twitter.com/i/lists/200195165"
            "https://twitter.com/i/lists/1032699809453498369"
            "https://twitter.com/i/lists/1597741824415768578"
            
            
        ]
        |>List.map(fun list_url ->
            list_url.Split "/"
            |>Array.last
        )
    
    
    
    
    let harvest_list_members
        browser
        parsing_context
        database
        list
        =
        let members =
            list
            |>Scrape_list_members.scrape_twitter_list_members
                browser
                parsing_context
            |>List.ofSeq
            
        members
        |>List.map Twitter_profile_from_catalog.handle
        |>Harvest_user.resiliently_harvest_top_pages_of_users
            browser
            parsing_context
            database
            
        members
  
    
    let table_header =
        [
            Cell.from_plain_text "Member"
            Cell.from_plain_text "Appears in lists"
            Cell.from_plain_text "Followers"
            Cell.from_plain_text "Followees"
            Cell.from_plain_text "Posts"
        ]
        
    let prepare_row_of_member
        database
        (member_profile: Twitter_user)
        (lists_amount)
        =
        [
            Googlesheet_for_twitter.cell_for_twitter_user
                member_profile
            
            Cell.from_colored_number (lists_amount, Color.white)
            
            Social_activity_database.read_last_amount_for_user
                database
                Social_activity_amounts_type.Followers
                member_profile.handle
            |>_.amount
            |>Cell.from_plain_integer
            
            Social_activity_database.read_last_amount_for_user
                database
                Social_activity_amounts_type.Followees
                member_profile.handle
            |>_.amount
            |>Cell.from_plain_integer
            
            Social_activity_database.read_last_amount_for_user
                database
                Social_activity_amounts_type.Posts
                member_profile.handle
            |>_.amount
            |>Cell.from_plain_integer 
        ]
        
    
    let export_members_to_spreadsheet
        database
        googlesheet_service
        googlesheet
        members_with_appearances
        =
        members_with_appearances
        |>Seq.map (fun (user,lists_amount) ->
            prepare_row_of_member database user lists_amount
        )
        |>Seq.append [table_header]
        |>List.ofSeq
        |>Googlesheet_writing.write_table
            googlesheet_service
            googlesheet
    

    let ``try export members``()=
        let database = Local_database.open_connection() 
        let googlesheet_service = Googlesheet.create_googlesheet_service()
        
        [
            {
                handle=User_handle "rvinowise"
                name="Victor"
            },10
            {
                handle=User_handle "dicortona"
                name="Dicortona"
            },11
            {
                handle=User_handle "nonexistent_user"
                name="nonexistent_user"
            },12
        ]
        |>export_members_to_spreadsheet
            database
            googlesheet_service
            {
                Google_spreadsheet.doc_id = "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0"
                page_name="Members"
            }
    

    let harvest_lacking_social_activity_of_members
        browser
        parsing_context
        database
        members
        =
        let olders_accepted_date = DateTime.UtcNow - TimeSpan.FromDays(30)
        let unscraped_members=
            members
            |>List.filter(fun account ->
                let last_scraping_date,activity_amounts =
                    Harvest_user.last_date_of_known_social_activity
                        database
                        account
                last_scraping_date < olders_accepted_date
            )
        $"{unscraped_members.Length} members out of {members.Length} don't have scraped activity newer than {olders_accepted_date}, harvesting them"
        |>Log.info
            
        unscraped_members
        |>Harvest_user.resiliently_harvest_top_pages_of_users
            browser
            parsing_context
            database
        
        
    let ``harvest lists``() =
        let database = Local_database.open_connection() 
        let browser =
            Assigning_browser_profiles.open_browser_with_free_profile
                (Central_database.resiliently_open_connection())
                (This_worker.this_worker_id database)
        
        let html_context = AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default
        let googlesheet_service = Googlesheet.create_googlesheet_service()
        
        let members_with_lists_amount = 
            lists
            |>Seq.collect(fun list_id ->
                Scrape_list_members.scrape_twitter_list_members_and_amount
                    browser
                    html_context
                    list_id
            )
            |>Seq.map _.user
            |>Seq.countBy id
            |>List.ofSeq
        
        let members = members_with_lists_amount|>List.map fst
            
        Log.important $"found {List.length members} distinct members in given lists"
        
        members
        |>List.map _.handle
        |>harvest_lacking_social_activity_of_members
            browser
            html_context
            database
    
        export_members_to_spreadsheet
            database
            googlesheet_service
            {
                Google_spreadsheet.doc_id = "1IghY1FjqODJq5QpaDcCDl2GyerqEtRR79-IcP55aOxI"
                page_name="Members"
            }
            members_with_lists_amount
    




