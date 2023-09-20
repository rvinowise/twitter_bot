namespace rvinowise.twitter

open System
open Xunit
open FsUnit
open canopy.parallell.functions
open canopy.types
open rvinowise.html_parsing
open rvinowise.twitter
open FParsec


type Timeline_tab =
    |Posts
    |Replies
    |Media
    |Likes
    with
    override this.ToString() =
        match this with
        |Posts -> "posts"
        |Replies -> "with_replies"
        |Likes -> "likes"
        |Media -> "media"
        

module Scrape_post_from_timeline =


     
            
    let scrape_timeline
        browser
        (tab: Timeline_tab)
        user
        =
        url $"{Twitter_settings.base_url}/{User_handle.value user}/{tab}" browser
        Reveal_user_page.surpass_content_warning browser    
        "article[data-testid='tweet']"
        |>Scrape_dynamic_list.collect_all_items_of_dynamic_list
            browser
        |>Seq.map (Html_node.from_html_string>>Parse_post_from_timeline.parse_main_twitter_post)
        

  
    [<Fact>]
    let ``try scrape_timeline``()=
        scrape_timeline
            (
                Settings.auth_tokens
                |>Seq.head
                |>Scraping.prepare_authentified_browser
                |>Browser.browser
            )
            Timeline_tab.Posts
            (User_handle "InfidelNoodle")
    



    


