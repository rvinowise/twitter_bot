namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit
open System

(* user interactions from local databases are combined in the central database.
this module exports them into the google sheet  *)

module Stitched_interactions_to_sheet =
    
    
    let export_local_interactions
        central_db
        local_db
        =
        ()
        
    [<Fact>]    
    let ``try stitched_interactions_to_sheet``()=
        let result =
            export_local_interactions
                (Central_task_database.open_connection())
                (Twitter_database.open_connection())
            
        ()
   