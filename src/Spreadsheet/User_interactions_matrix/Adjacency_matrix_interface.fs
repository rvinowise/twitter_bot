namespace rvinowise.twitter

open rvinowise.twitter
open rvinowise.web_scraping
open Xunit



module Adjacency_matrix_interface =
        

    let all_sorted_handles =
        [
            "aubreydegrey"
            "realNathanCheng"
            "RokoMijic"
            "USTranshumanist"
            "tomchapin"
            "kaimicahmills"
            "01Core_Ben"
            "Scinquisitor"
            "fedichev"
            "GeroMaxim"
            "GStolyarovII"
            "SF_Longevity"
            "irat1onal"
            "yangranat"
            "schw90"
            "vadbars"
            "DanilaImmortal"
            "OpenLongevity"
            "victorforissier"
            "SsJankauskas"
            "MikhailBatin"
            "ActivistCher"
            "DrPatrickLinden"
            "ValleeRl"
            "Cancel_Aging"
            "Nst_Egorova"
            "petrenko_ai"
            "strygah"
            "LidskyPeter"
            "FedorovIdeas"
            "turchin"
            "IsmanAnar"
            "EricSiebert9"
            "Timrael"
            "FirstApproval"
            "SayForeverOrg"
            "SciFi_by_Allen"
            "GriffinBrumley"
            "Pavel853997"
            "FeelixNoel"
            "v_govorov"
            "kevinperrott"
            "dantegrity"
            "nikitiusivanov"
            "_sviridov_"
            "productmg"
            "enz0benz0"
            "VampArtyom"
            "Closethesky1"
            "remonechev"
            "rvinowise"
            "CRafikov"
            "EvanMorgun"
            "100orbits"
            "TSGlinin"
            "OttoMller12"
            "OlBorisova"
            "allan_darcy"
            "NastyaKostylev7"
            "felixwerth2"
            "his_slowlife"
            "1rchv"
            "rejuicey"
            "neobiosis"
            "EnriqueSegarra_"
            "Darviridis"
            "SeanFST"
            "IuriiPotes84621"
            "grigonCH"
            "Santanu91744049"
            "kristian_rados"
            "ConcretAbstrakt"
            "madiyar_123"
            "DaniWotton"
            "InSociEUP"
            "am_adeliya"
            "AlexLarionov"
            "dimka_prokofev"
            "notjasonhowarth"
            "CuringAgingConf"
            "EndrigoGlauco"
        ]
        |>List.map User_handle
    
    let likes_color = {r=1;g=0;b=0}
    let reposts_color = {r=0;g=1;b=0}
    let replies_color = {r=0.2;g=0.2;b=1}
    
    
    let sorted_users_from_handles
        database
        all_sorted_handles
        =
        let user_names =
            Social_user_database.read_user_names_from_handles
                database
        
        all_sorted_handles
        |>List.map (fun handle ->
            {
                handle= handle
                name =
                    (user_names
                    |>Map.tryFind handle
                    |>Option.defaultValue (User_handle.value handle))
            }    
        )
    
    let interaction_type_from_db
        all_user_handles
        read_interactions
        color
        =
        all_user_handles
        |>Adjacency_matrix.maps_of_user_interactions
            read_interactions    
        |>Adjacency_matrix.interaction_type_for_colored_interactions
            color
    
    
    [<Fact(Skip="manual")>]//
    let ``fill table with the test of color blending ``() =
        let googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=398318420
            page_name="Color blending example"
        }
        let googlesheet2 = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=1806921415
            page_name="Color blending example2"
        }
        
        
        let all_sorted_users =
            List.init 30 (fun index ->
                {
                    handle= index|>string|>User_handle
                    name= index|>string
                }    
            )
            
        let all_sorted_handles =
            all_sorted_users
            |>List.map (fun user -> user.handle)
        
        let likes_interactions =
            all_sorted_handles
            |>List.mapi (fun row user ->
                user,
                all_sorted_handles
                |>List.mapi(fun column other_user ->
                    other_user,
                    row%10
                )|>Map.ofList
            )|>Map.ofList
            |>Adjacency_matrix.interaction_type_for_colored_interactions
                likes_color
                
        let reposts_interactions =
            all_sorted_handles
            |>List.mapi (fun row user ->
                user,
                all_sorted_handles
                |>List.mapi(fun column other_user ->
                    other_user,
                    column%10
                )|>Map.ofList
            )|>Map.ofList
            |>Adjacency_matrix.interaction_type_for_colored_interactions
                reposts_color
        
        let square_side = 10
        let replies_interactions =
            all_sorted_handles
            |>List.mapi (fun row user ->
                user,
                all_sorted_handles
                |>List.mapi(fun column other_user ->
                    other_user,
                    (column/square_side) +
                    (row/square_side)*
                    (all_sorted_handles.Length/square_side)
                )|>Map.ofList
            )|>Map.ofList
            |>Adjacency_matrix.interaction_type_for_colored_interactions
                replies_color
                
        Adjacency_matrix_compound.update_googlesheet_with_total_interactions
            googlesheet
            3
            0.4
            all_sorted_users
            [
                likes_interactions;
                reposts_interactions;
                replies_interactions;
            ]
        // Adjacency_matrix_compound.update_googlesheet_with_total_interactions
        //     googlesheet
        //     3
        //     0.4
        //     all_sorted_users
        //     [
        //         reposts_interactions;
        //         replies_interactions;
        //         likes_interactions;
        //     ]
    
    [<Fact(Skip="manual")>]//
    let ``try update_googlesheet``() =
        //https://docs.google.com/spreadsheets/d/1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY/edit#gid=0
        let likes_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=0
            page_name="Likes"
        }
        
        let reposts_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=2108706810
            page_name="Reposts"
        }
   
        let replies_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=2007335692
            page_name="Replies"
        }
        
        let everything_googlesheet = {
            Google_spreadsheet.doc_id = "1HqO4nKW7Jt4i4T3Rir9xtkSwI0l9uVVsqHTOPje-pAY"
            page_id=1019851571
            page_name="Everything"
        }
            
        let database = Twitter_database.open_connection()
        
        let all_sorted_users =
            sorted_users_from_handles
                database
                all_sorted_handles
        
        let all_user_handles =
            Set.ofList all_sorted_handles
        
        let likes_interactions =
            interaction_type_from_db
                all_user_handles
                (User_interaction.read_likes_by_user database)
                likes_color
        
        let reposts_interactions =
            interaction_type_from_db
                all_user_handles
                (User_interaction.read_reposts_by_user database)
                reposts_color
        
        let replies_interactions =
            interaction_type_from_db
                all_user_handles
                (User_interaction.read_replies_by_user database)
                replies_color
        
        
        let update_googlesheet_with_interaction_type =
            Adjacency_matrix_single.update_googlesheet
                all_sorted_users
        
    
        update_googlesheet_with_interaction_type
            likes_googlesheet
            3
            0.4
            likes_interactions

        update_googlesheet_with_interaction_type
            reposts_googlesheet
            3
            0.4
            reposts_interactions

        update_googlesheet_with_interaction_type
            replies_googlesheet
            3
            0.4
            replies_interactions
            
            
        Adjacency_matrix_compound.update_googlesheet_with_total_interactions
            everything_googlesheet
            3
            0.4
            all_sorted_users
            [
                likes_interactions;
                reposts_interactions;
                replies_interactions
            ]