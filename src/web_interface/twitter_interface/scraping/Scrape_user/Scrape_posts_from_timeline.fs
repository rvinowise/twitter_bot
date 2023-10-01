namespace rvinowise.twitter

open Xunit
open canopy.parallell.functions
open rvinowise.html_parsing
open rvinowise.web_scraping


type Timeline_tab =
    |Posts
    |Replies
    |Media
    |Likes
    with
    override this.ToString() =
        match this with
        |Posts -> ""
        |Replies -> "with_replies"
        |Likes -> "likes"
        |Media -> "media"
        

module Scrape_posts_from_timeline =
    
    
    let parse_post previous_posts html_post =
        html_post
        |>Html_node.from_html_string
        |>Parse_segments_of_post.parse_main_twitter_post
              (List.map snd previous_posts)
      
    let scrape_timeline
        browser
        max_amount
        (tab: Timeline_tab)
        user
        =
        Browser.open_url $"{Twitter_settings.base_url}/{User_handle.value user}/{tab}" browser
        Reveal_user_page.surpass_content_warning browser    
        "article[data-testid='tweet']"
        |>Scrape_dynamic_list.consume_items_of_dynamic_list
            browser
            parse_post
            max_amount
        

  
    [<Fact>]
    let ``try scrape_timeline``()=
        let posts = 
            scrape_timeline
                (
                    Settings.auth_tokens
                    |>Seq.head
                    |>Browser.prepare_authentified_browser
                )
                100
                Timeline_tab.Posts
                (User_handle "RichardDawkins")
        let result = List.map snd posts
        ()



    


