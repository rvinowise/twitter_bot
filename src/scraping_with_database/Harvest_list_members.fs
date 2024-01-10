namespace rvinowise.twitter

open System
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open canopy.types
open rvinowise.html_parsing
open rvinowise.twitter.database.tables
open rvinowise.web_scraping

open FsUnit
open Xunit

module Harvest_list_members =
   
    let lists =
        [
            "https://twitter.com/i/lists/878862836042153984"
            "https://twitter.com/i/lists/71920806"
            "https://twitter.com/i/lists/1378099512359591941"
            "https://twitter.com/i/lists/1391914804953194499"
            "https://twitter.com/i/lists/1319382252208459780"
            "https://twitter.com/i/lists/1330801495860793348"
            "https://twitter.com/i/lists/1397353557838401537"
            "https://twitter.com/i/lists/1460797977769885700"
            "https://twitter.com/i/lists/1409896835276234754"
            "https://twitter.com/i/lists/1369058532792930308"
            "https://twitter.com/i/lists/181072039"
            "https://twitter.com/i/lists/1428694689251794948"
            "https://twitter.com/i/lists/1619444175807647744"
            "https://twitter.com/i/lists/1516165329239613450"
            "https://twitter.com/i/lists/1510658778848534533"
            "https://twitter.com/i/lists/1507436796673728512"
            "https://twitter.com/i/lists/1500402405632094209"
            "https://twitter.com/i/lists/1482445806347112450"
            "https://twitter.com/i/lists/1466418926057840641"
            "https://twitter.com/i/lists/1434244379519029249"
            "https://twitter.com/i/lists/1432484199513141250"
            "https://twitter.com/i/lists/1403019461171449858"
            "https://twitter.com/i/lists/1304087470158614531"
            "https://twitter.com/i/lists/1272186364365242369"
            "https://twitter.com/i/lists/205082422"
            "https://twitter.com/i/lists/1149360369762263040"
            "https://twitter.com/i/lists/1432436305980321792"
            "https://twitter.com/i/lists/1429884547127136258"
            "https://twitter.com/i/lists/1568377523666587649"
            "https://twitter.com/i/lists/200195165"
        ]
        |>List.map(fun list_url ->
            list_url.Split "/"
            |>Array.last
        )
    
    
    let members_sheet = {
        Google_spreadsheet.doc_id = "1rm2ZzuUWDA2ZSSfv2CWFkOIfaRebSffN7JyuSqBvuJ0"
        page_name="Members"
    }
    
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
        members
        |>Seq.iter(fun user_bio ->
            Harvest_user.harvest_top_of_user_page
                browser
                parsing_context
                database
                user_bio.user.handle
        )
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
                Social_activity_amounts.Followers
                member_profile.handle
            |>_.amount
            |>Cell.from_plain_integer
            
            Social_activity_database.read_last_amount_for_user
                database
                Social_activity_amounts.Followees
                member_profile.handle
            |>_.amount
            |>Cell.from_plain_integer
            
            Social_activity_database.read_last_amount_for_user
                database
                Social_activity_amounts.Posts
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
    
    let import_users_from_spreadsheet
        googlesheet_service
        googlesheet
        =
        ()
    
    [<Fact(Skip="manual")>]
    let ``try import_users_from_spreadsheet``()=
        let googlesheet_service = Googlesheet.create_googlesheet_service()
        let users =
            import_users_from_spreadsheet
                googlesheet_service
                members_sheet
        ()
        
    [<Fact(Skip="manual")>]//
    let ``try export members``()=
        let database = Twitter_database.open_connection() 
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
            members_sheet
    
    [<Fact(Skip="manual")>] //
    let ``harvest lists``() =
        let browser = Browser.open_browser()
        let html_context = AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default
        let database = Twitter_database.open_connection() 
        let googlesheet_service = Googlesheet.create_googlesheet_service()
        
        let members_with_amount = 
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
        
        let members = members_with_amount|>List.map fst
            
        Log.important $"found {List.length members} distinct members in given lists"
        
        members
        |>List.iter (fun user ->
            Harvest_user.harvest_top_of_user_page
                browser
                html_context
                database
                user.handle
        )
    
        export_members_to_spreadsheet
            database
            googlesheet_service
            members_sheet
            members_with_amount
    




