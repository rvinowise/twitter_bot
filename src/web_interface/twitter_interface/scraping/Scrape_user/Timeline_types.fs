namespace rvinowise.twitter

open Xunit
open canopy.parallell.functions
open rvinowise.html_parsing
open rvinowise.web_scraping


type Timeline_tab =
    |Posts
    |Posts_and_replies
    |Media
    |Likes
    with
    override this.ToString() =
        match this with
        |Posts -> ""
        |Posts_and_replies -> "with_replies"
        |Likes -> "likes"
        |Media -> "media"

module Timeline_tab =
    let human_name (tab: Timeline_tab) =
        match tab with
        |Posts -> "Posts"
        |Posts_and_replies -> "Posts_and_replies"
        |Likes -> "Likes"
        |Media -> "Media"


    



    


