namespace rvinowise.twitter

open System
open Npgsql
open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.twitter
open rvinowise.twitter.Parse_article
open rvinowise.web_scraping



type Parsing_timeline_result =
    |Success of int
    |Insufficient of int
    |Hidden_timeline of Timeline_not_opened_reason 
    |Exception of Exception 


module Parsing_timeline_result =
    let db_value (result: Parsing_timeline_result) =
        match result with
        |Success amount-> "Success"
        |Insufficient amount-> "Insufficient"
        |Hidden_timeline reason -> Timeline_not_opened_reason.db_value reason
        |Exception exc -> $"Exception: {exc.Message}"

    let articles_amount (result: Parsing_timeline_result) =
        match result with
        |Success amount
        |Insufficient amount ->
            amount
        |Hidden_timeline _
        |Exception _ ->
            0
        
type Scraping_user_status =
    |Free
    |Taken
    |Completed of Parsing_timeline_result
    
    
module Scraping_user_status =
    let db_value (status: Scraping_user_status) =
        match status with
        |Free -> "Free"
        |Taken -> "Taken"
        |Completed result -> Parsing_timeline_result.db_value result

    let from_db_value value =
        match value with
        |"Free" -> Scraping_user_status.Free
        |"Taken" -> Scraping_user_status.Taken
        |unknown_type -> raise (TypeAccessException $"unknown type of Scraping_user_status: {unknown_type}")



module Scrape_timeline =
    
    let parse_timeline_cell
        (parse_post: Main_post -> unit)
        (is_finished: Main_post -> Stopping_reason option) // is it the first ignored post?
        previous_cell
        html_cell
        =
        let thread_context =
            Parse_timeline_cell.parse_timeline_cell
                previous_cell
                html_cell
        
        match thread_context with
        |Thread_context.Post post ->
            match is_finished post with
            |Some stopping_reason ->
                Result_of_timeline_cell_processing.Should_stop stopping_reason
            |None ->
                parse_post post
                Result_of_timeline_cell_processing.Scraped_post thread_context
        |Hidden_thread_replies _ |Empty_context ->
            Result_of_timeline_cell_processing.Scraped_post previous_cell
    
    
    let wait_for_timeline_loading browser =
        //Browser.sleep 1
        "div[role='progressbar']"
        |>Browser.wait_till_disappearance browser 60 |>ignore
    
    let parse_timeline
        (process_item: Thread_context -> Html_node -> Result_of_timeline_cell_processing)
        browser
        html_context
        scrolling_repetitions
        =
        let is_item_needed = fun _ -> true
        
        let scrape_visible_items () =
            Scrape_visible_part_of_list.scrape_items
                browser
                html_context
                is_item_needed
                Twitter_settings.timeline_cell_css
                
        let load_new_item_batch =
            Scrape_dynamic_list.load_new_item_batch
                (fun () -> wait_for_timeline_loading browser)
                scrape_visible_items
                Read_list_updates.cell_id_from_post_id
                (fun () -> Scrape_dynamic_list.load_next_items browser)
        
        let process_item_batch =
            Read_list_updates.process_item_batch_providing_previous_items
                process_item
        
        
        Scrape_dynamic_list.parse_dynamic_list_with_context
            load_new_item_batch
            process_item_batch
            scrolling_repetitions
            
    
    let parse_timeline_with_counting
        browser
        html_parsing_context
        parse_post
        is_finished
        =
        let mutable post_count = 0
        
        let parse_post_with_counting post =
            post_count <- post_count + 1
            parse_post post
        
        let mutable cell_count = 0
        let parse_cell_with_counting item =
            cell_count <- cell_count + 1
            parse_timeline_cell
                parse_post_with_counting
                is_finished
                item
        
        let result =
            parse_timeline
                parse_cell_with_counting
                browser
                html_parsing_context
                Settings.repeat_scrolling_timeline
        
        post_count,cell_count,result
        
        
    
    let all_posts_were_scraped
        browser
        tab
        user
        scraped_posts_amount
        scraped_cells_amount
        =
        
        Log.info $"""
        {scraped_posts_amount} posts from {scraped_cells_amount} cells have been harvested from tab "{Timeline_tab.human_name tab}" of user "{User_handle.value user}".
        """
        
        let minimum_posts_percentage =
            [
                (*70% of scraped likes, relative to the reported amount by twitter, is OK
                possibly because liked Ads are skipped*)
                Timeline_tab.Likes, 70
                
                (*posts_and_replies tab includes the posts to which the reply is made,
                so, it normally has more posts than the targeted user wrote*)
                Timeline_tab.Posts_and_replies, 100
            ]|>Map.ofList
        
        let posts_supposed_amount =
            Scrape_user_social_activity.try_scrape_posts_amount browser
        match posts_supposed_amount with
        |Some supposed_amount ->
            let scraped_percent =
                if (supposed_amount>0) then
                    (float scraped_posts_amount)/(float supposed_amount) * 100.0
                else 100.0
            if
                scraped_percent < minimum_posts_percentage[tab]
            then
                $"""insufficient scraping of timeline {Timeline_tab.human_name tab} of user {User_handle.value user}:
                twitter reports {supposed_amount} posts, but only {scraped_posts_amount} posts were found,
                which is {int scraped_percent}%% and less than needed {minimum_posts_percentage[tab]} %%
                """
                |>Log.error|>ignore
                false
            else true
        |None ->
            $"can't read posts amount from timeline {Timeline_tab.human_name tab} of user {User_handle.value user}"
            |>Log.error|>ignore
            false
            
            
    let reveal_and_parse_timeline
        (browser:Browser)
        html_context
        timeline_tab
        user
        parse_post
        (should_finish_at_post: Main_post -> Stopping_reason option)
        =
        match
            Reveal_user_page.reveal_timeline
                browser
                html_context
                timeline_tab
                user
        with
        |Revealed ->
            
            let
                posts_amount,
                cells_amount,
                result
                    =
                    parse_timeline_with_counting
                        browser
                        html_context
                        parse_post
                        should_finish_at_post
            if
                result = Result_of_timeline_cell_processing.Should_stop Stopping_reason.No_more_posts_appeared
                &&
                all_posts_were_scraped
                    browser
                    timeline_tab
                    user
                    posts_amount
                    cells_amount
                |>not
            then
                Insufficient posts_amount
            else
                Success posts_amount
        |Page_revealing_result.Failed Protected ->
            Log.info $"Timelines of user {User_handle.value user} are protected from strangers."
            Parsing_timeline_result.Hidden_timeline Protected
        |Page_revealing_result.Failed failure_reason ->
            Parsing_timeline_result.Hidden_timeline failure_reason
        
        
    
    