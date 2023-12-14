namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit



module Adjacency_matrix_interface =
        


    
    
    
    [<Fact>]//(Skip="manual")
    let ``try update_googlesheet``() =
        //https://docs.google.com/spreadsheets/d/1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY/edit#gid=0
        let likes_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=0
            page_name="Likes"
        }
        let likes_colorscheme = {
            min_color={r=1; g=1;b=1}
            max_color={r=1; g=0;b=0}
        }
        
        let reposts_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=2108706810
            page_name="Reposts"
        }
        let reposts_colorscheme = {
            min_color={r=1; g=1;b=1}
            max_color={r=0; g=1;b=0}
        }
        
        let replies_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=2007335692
            page_name="Replies"
        }
        let replies_colorscheme = {
            min_color={r=1; g=1;b=1}
            max_color={r=0; g=0;b=1}
        }
        
        let everything_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=1019851571
            page_name="Everything"
        }
            
        let database = Twitter_database.open_connection()
        
        let all_sorted_users =
            Settings.Competitors.list
            |>Scrape_list_members.scrape_twitter_list_members
                (Browser.open_browser())
            |>List.map (Twitter_profile_from_catalog.user >> Twitter_user.handle)
        
        let all_users = Set.ofList all_sorted_users
        let user_names =
            Social_activity_database.read_user_names_from_handles
                database
        
        
        let likes_interactions =
            all_users
            |>Adjacency_matrix.maps_of_user_interactions
                (User_interaction.read_likes_by_user database)    
        
        let reposts_interactions =
            all_users
            |>Adjacency_matrix.maps_of_user_interactions
                (User_interaction.read_reposts_by_user database)
                
        let replies_interactions =
            all_users
            |>Adjacency_matrix.maps_of_user_interactions
                (User_interaction.read_replies_by_user database)  
        
        let update_googlesheet_with_interaction_type =
            Adjacency_matrix_single.update_googlesheet
                all_sorted_users
                user_names
        
        update_googlesheet_with_interaction_type
            likes_googlesheet
            likes_colorscheme
            likes_interactions
        update_googlesheet_with_interaction_type
            reposts_googlesheet
            reposts_colorscheme
            reposts_interactions
        update_googlesheet_with_interaction_type
            replies_googlesheet
            replies_colorscheme
            replies_interactions
            
        Adjacency_matrix_compound.update_googlesheet_with_total_interactions
            everything_googlesheet
            likes_interactions
            reposts_interactions
            replies_interactions