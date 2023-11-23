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
    
    
    let parse_post previous_posts html_cell =
        try 
            html_cell
            |>Html_node.from_html_string
            |>Html_node.descendant "article[data-testid='tweet']"
            |>Parse_segments_of_post.parse_main_twitter_post
                  (List.map snd previous_posts)
            |>Result.Ok
        with
        | :? Bad_post_structure_exception
        | :? Html_parsing_fail as exc ->
            $"""exception {exc.Message} when parsing the post:
            {html_cell}
            """
            |>Log.error
            |>Result.Error
    
    let is_advertisement cell_node =
        cell_node
        |>Html_node.try_descendant "div[data-testid='placementTracking']"
        |>Option.isSome
    
    let is_cell_contains_post cell_node =
        cell_node
        |>is_advertisement|>not
        &&
        cell_node
        |>Html_node.try_descendant "article[data-testid='tweet']"
        |>Option.isSome
    
    let wait_for_timeline_loading browser =
        Browser.sleep 1
        "div[role='progressbar']"
        |>Browser.wait_till_disappearance browser 10 |>ignore 
            
    let scrape_timeline
        browser
        posts_amount
        (tab: Timeline_tab)
        user
        =
        Browser.open_url $"{Twitter_settings.base_url}/{User_handle.value user}/{tab}" browser
        Reveal_user_page.surpass_content_warning browser
        "div[data-testid='cellInnerDiv']"
        |>Scrape_dynamic_list.parse_dynamic_list
            browser
            (fun () -> wait_for_timeline_loading browser)
            (fun item ->
                item
                |>Html_node.from_html_string
                |>is_cell_contains_post)
            parse_post
            posts_amount
        
    
    
    
    [<Fact(Skip="manual")>]
    let ``try scrape_timeline``()=
        let posts = 
            scrape_timeline
                (
                    Settings.auth_tokens
                    |>Seq.head
                    |>Browser.prepare_authentified_browser
                )
                50
                Timeline_tab.Posts
                (User_handle "HilzFuld")
            
        let results = List.map snd posts
        let errors =
            results
            |>List.filter Result.isError
        ()



    


