namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.parallell.functions
open canopy.types
open rvinowise.twitter
open FParsec



module Scrape_timeline =


   
    let scrape_timeline browser user =
        
     
            
    let scrape_likes_given_by_user browser user =
        url $"{Twitter_settings.base_url}/{User_handle.value user}/likes" browser
        Reveal_user_page.surpass_content_warning browser    
        
    
    

  


    
    



    


