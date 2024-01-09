namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit
open System



module Export_local_interactinos =
    
    
    let export_local_interactions
        central_db
        local_db
        =
        ()
        
    [<Fact>]    
    let ``try export_local_interactions``()=
        let result =
            export_local_interactions
                (Central_task_database.open_connection())
                (Twitter_database.open_connection())
            
        ()
   
    [<Fact>]    
    let ``try export interactions of one user ``()=
        let central_db = Central_task_database.open_connection()
        let local_db= Twitter_database.open_connection()
        let this_worker = This_worker.this_worker_id local_db
        
        let all_users =
            Central_task_database.read_jobs_completed_by_worker
                central_db
                this_worker 
        
        let result =
            Combining_user_interactions.upload_interactions
                central_db
                local_db
                "User_interactions"
                all_users
                (Set.ofSeq all_users)
        ()         