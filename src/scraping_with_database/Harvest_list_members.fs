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
            "https://twitter.com/i/lists/1451369115613114386"
            "https://twitter.com/i/lists/1465878634519044099"
            "https://twitter.com/i/lists/1316578688205426689"
            "https://twitter.com/i/lists/1518438116943663104"
            "https://twitter.com/i/lists/1460603765262856200"
            "https://twitter.com/i/lists/1020756547213938700"
            "https://twitter.com/i/lists/1332283856720949248"
            "https://twitter.com/i/lists/1090107"
            "https://twitter.com/i/lists/1479004920590311425"
            "https://twitter.com/i/lists/940941894749745152"
            "https://twitter.com/i/lists/1576073400132452352"
            "https://twitter.com/i/lists/1346858133822562304"
            "https://twitter.com/i/lists/1467230292205309958"
            "https://twitter.com/i/lists/1498373473277812743"
            "https://twitter.com/i/lists/1504430180537810948"
            "https://twitter.com/i/lists/1281040400216514560"
            "https://twitter.com/i/lists/1318543386715062272"
            "https://twitter.com/i/lists/1298662177105215491"
            "https://twitter.com/i/lists/1505135154167230468"
            "https://twitter.com/i/lists/1123068084108058624"
            "https://twitter.com/i/lists/1344597663422029824"
            "https://twitter.com/i/lists/1510396523967778818"
            "https://twitter.com/i/lists/1181894239313186816"
            "https://twitter.com/i/lists/1546479016437321732"
            "https://twitter.com/i/lists/807582499283103744"
            "https://twitter.com/i/lists/823592271639695360"
            "https://twitter.com/i/lists/1360780822169681920"
            "https://twitter.com/i/lists/1420072649598795776"
            "https://twitter.com/i/lists/1552795515200454663"
            "https://twitter.com/i/lists/1676915596846596096"
            "https://twitter.com/i/lists/120023753"
            "https://twitter.com/i/lists/117262948"
            "https://twitter.com/i/lists/818235212257755137"
            "https://twitter.com/i/lists/1209211712571879425"
            "https://twitter.com/i/lists/1315146423873175553"
            "https://twitter.com/i/lists/1378398833848197122"
            "https://twitter.com/i/lists/1397909085673099266"
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
        
        // members
        // |>List.map _.handle
        // |>harvest_lacking_social_activity_of_members
        //     browser
        //     html_context
        //     database
    
        export_members_to_spreadsheet
            database
            googlesheet_service
            {
                Google_spreadsheet.doc_id = "1IghY1FjqODJq5QpaDcCDl2GyerqEtRR79-IcP55aOxI"
                page_name="Members"
            }
            members_with_lists_amount
    




