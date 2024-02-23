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
    
    members by the query:
    "e/acc" (only first hundreds)
    
    *)
    
    let lists =
        [
            "https://twitter.com/i/lists/1633391356939829250"
            "https://twitter.com/i/lists/1635096901623111680"
            "https://twitter.com/i/lists/1365421579937734662"
            "https://twitter.com/i/lists/1600382333600686080"
            "https://twitter.com/i/lists/1644265080027222017"
            "https://twitter.com/i/lists/1314242486156623874"
            "https://twitter.com/i/lists/1646244348713943045"
            "https://twitter.com/i/lists/1644265080027222017"
            "https://twitter.com/i/lists/1376876611455307778"
            "https://twitter.com/i/lists/1071708221730115584"
            "https://twitter.com/i/lists/1141102617097117697"
            "https://twitter.com/i/lists/1376876611455307778"
            "https://twitter.com/i/lists/1571318287417614337"
            "https://twitter.com/i/lists/1639327880227479552"
            "https://twitter.com/i/lists/1689222239244468429"
            "https://twitter.com/i/lists/232581438"
            "https://twitter.com/i/lists/811648891229609985"
            "https://twitter.com/i/lists/829689034578460675"
            "https://twitter.com/i/lists/868881902610128897"
            "https://twitter.com/i/lists/909905273912926208"
            "https://twitter.com/i/lists/1069698729236619264"
            "https://twitter.com/i/lists/1180437185062944768"
            "https://twitter.com/i/lists/1195611616307548160"
            "https://twitter.com/i/lists/1288027217063182339"
            "https://twitter.com/i/lists/1375472780490244103"
            "https://twitter.com/i/lists/1608507347743264769"
            "https://twitter.com/i/lists/1644000343032750082"
            "https://twitter.com/i/lists/1654854186318610434"
            "https://twitter.com/i/lists/1380936371586666497"
            "https://twitter.com/i/lists/1400410074275889156"
            "https://twitter.com/i/lists/1267967400647032833"
            "https://twitter.com/i/lists/1593137802635534336"
            "https://twitter.com/i/lists/1511758298709696517"
            "https://twitter.com/i/lists/1653648790098771969"
            "https://twitter.com/i/lists/1495504317377392643"
            "https://twitter.com/i/lists/1633601941296099329"
            "https://twitter.com/i/lists/1623617160613355528"
            "https://twitter.com/i/lists/1367836484728979459"
            "https://twitter.com/i/lists/784534271683809280"
            "https://twitter.com/i/lists/996784124718407681"
            "https://twitter.com/i/lists/1643630895033643008"
            "https://twitter.com/i/lists/1458687473475678208"
            "https://twitter.com/i/lists/1637450382543601664"
            "https://twitter.com/i/lists/850475818241413121"
            "https://twitter.com/i/lists/1412009411346944000"
            "https://twitter.com/i/lists/1597814047688335360"
            "https://twitter.com/i/lists/1303577789040394240"
            "https://twitter.com/i/lists/1051247760920346624"
            "https://twitter.com/i/lists/22875613"
            "https://twitter.com/i/lists/1316935740882886660"
            "https://twitter.com/i/lists/197344893"
            "https://twitter.com/i/lists/225995747"
            "https://twitter.com/i/lists/225487259"
            "https://twitter.com/i/lists/22875613"
            "https://twitter.com/i/lists/8793381"
            "https://twitter.com/i/lists/75460849"
            "https://twitter.com/i/lists/217079472"
            "https://twitter.com/i/lists/756019607924867072"
            "https://twitter.com/i/lists/826010580423176192"
            "https://twitter.com/i/lists/816439924647989248"
            "https://twitter.com/i/lists/963762977458589698"
            "https://twitter.com/i/lists/1494808807372955648"
            "https://twitter.com/i/lists/1623119951371403265"
            "https://twitter.com/i/lists/1622389924757479432"
            
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
    

    let harvest_lacking_top_pages_of_members
        browser
        parsing_context
        database
        members
        =
        let oldest_accepted_date = DateTime.UtcNow - TimeSpan.FromDays(30)
        let unscraped_members=
            members
            |>List.filter(fun account ->
                let last_scraping_date,activity_amounts =
                    Harvest_user.last_date_of_known_social_activity
                        database
                        account
                last_scraping_date < oldest_accepted_date
            )
        $"{unscraped_members.Length} members out of {members.Length} don't have scraped activity newer than {oldest_accepted_date}, harvesting them"
        |>Log.info
            
        unscraped_members
        |>Harvest_user.resiliently_harvest_top_pages_of_users
            browser
            parsing_context
            database
        
    
    (* Matrix_template: https://docs.google.com/spreadsheets/d/1qUBXYvHj4bROzELbcgLy-P8li5cjSJR5FH3EK8_PQWU/edit#gid=0 *)        
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
        |>harvest_lacking_top_pages_of_members
            browser
            html_context
            database
    
        export_members_to_spreadsheet
            database
            googlesheet_service
            {
                Google_spreadsheet.doc_id = "18iGlQOaSihzVhAhpbmilBTTUWk55hrVzdSM0FlbQhtQ"
                page_name="Members"
            }
            members_with_lists_amount
    




