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

module Scrape_posts_from_timeline =
    
    
    
    
    let is_advertisement cell_node =
        cell_node
        |>Html_node.descendants "div[data-testid='placementTracking']"
        <> []
    
    let cell_contains_post cell_node =
        cell_node
        |>is_advertisement|>not
        &&
        cell_node
        |>Html_node.try_descendant "article[data-testid='tweet']"
        |>Option.isSome
    
    let wait_for_timeline_loading browser =
        //Browser.sleep 1
        "div[role='progressbar']"
        |>Browser.wait_till_disappearance browser 60 |>ignore 
            
    



    


