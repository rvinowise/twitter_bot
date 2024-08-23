namespace rvinowise.twitter.test

open System
open System.Collections.Generic
open NUnit.Framework
open Xunit
open rvinowise.twitter

type Scraped_post_check = {
    post: Post_id
    tester: Main_post -> unit
}
    

type Tested_post =
    |Skip of string
    |Scraped_result of Scraped_post_check

module Scraping_twitter_posts_tests =
    
    let prepared_tests_from_tehprom_timeline = 
        [
            {
                post = Post_id 1704659922464444801L
                tester = quotation_test.``parse image-post with a quotation of a 4-video-post``
            };
            {
                post = Post_id 1706184429964513566L
                tester = quotation_test.``parse image-post quoting a reply with a reply-header and images``
            };
            {
                (*repost from JeffBesos*)
                post = Post_id 1735785217305014701L
                tester = quotation_test.``parse a post with a quotation, both having images after the usernames. the only media load is a quoted gif``
            };
            {
                post = Post_id 1704650832828940551L //Sep 21, 2023
                tester = quotation_test.``parse post quoting another post with ShowMore button``
            };
            {
                post = Post_id 1704659543026765991L
                tester = quotation_test.``parse quotation of 4-videos post``
            };
            
            {
                post = Post_id 1704704306895618550L
                tester = external_url.``parse a post with an external website (with a large layout originally, now deprecated)``
            };
            {
                post = Post_id 1704958629932056635L
                tester = external_url.``parse post with external source with small layout``
            };
            {
                post = Post_id 1708421896155398530L
                tester = external_url.``parse post without message, but with only an external url``
            };
            {
                post = Post_id 1727106214990139845L
                tester = poll_test.``parse a finished poll``
            }
            {
                post = Post_id 1729688709425950964L
                tester = poll_test.``parse an ongoing poll``
            }
            {
                post = Post_id 1725955263344029911L
                tester = poll_test.``parse a quoted finished poll``
            }
            {
                post = Post_id 1732776142791184743L
                tester = twitter_event.``parse a post which shares a twitter event``
            }
            {
                post = Post_id 1604486844867133442L
                tester = twitter_event.``parse a post with a twitter-event without a user``
            }
            {
                post = Post_id 1732790626020675814L
                tester = broken_audio_space.``parse a post with a broken audio space``
            }
            {
                post = Post_id 1732790822049903101L
                tester = broken_audio_space.``parse a post which quotes another post with a broken audio space``
            }
            {
                post = Post_id 1733080058988789957L
                tester = broken_audio_space.``parse a post with a broken audio-space and a quoted post which has images``
            }
            {
                post = Post_id 1814375909991883075L
                tester = audio_space.``parse a post with an audio space``
            }
            {
                post = Post_id 1814376074089783568L
                tester = audio_space.``parse a post which quotes another post with an audio space``
            }
            {
                post = Post_id 1814377370654257441L
                tester = audio_space.``parse a post with an audio-space and a quoted post which has images``
            }
        ]

    
    
    let dictionary_post_to_tester
        (prepared_tests: Scraped_post_check list) 
        =
        prepared_tests
        |>List.map(fun prepared_test ->
            prepared_test.post,prepared_test.tester    
        )|>dict|>Dictionary
            
    
    let check_post
        (posts_to_testers: Dictionary<Post_id, Main_post -> unit>)
        (post: Main_post)
        =
        match posts_to_testers.TryGetValue(post.id) with
        |false, _ -> ()
        |true, tester ->
            try
                tester post
            with
            | :? Harvesting_exception
            | :? Bad_post_exception
            | :? AssertionException as exc ->
                Log.error $"""testing a scraped post {Post_id.value post.id} failed: {exc.Message}"""|>ignore
            
            posts_to_testers.Remove(post.id)|>ignore
    
    
       
    [<Fact>] //(Skip="integration")
    let ``run tests of posts scraped from twitter``()=
        let posts_to_testers = dictionary_post_to_tester prepared_tests_from_tehprom_timeline
        
        let browser =
            Local_database.open_connection()
            |>This_worker.this_worker_id
            |>Assigning_browser_profiles.open_browser_with_free_profile
                (Central_database.resiliently_open_connection())
                
        let html_context = AngleSharp.BrowsingContext.New AngleSharp.Configuration.Default

        let no_left_posts_to_test (posts_to_testers:IDictionary<Post_id, Main_post -> unit>) _ =
            if (posts_to_testers.Count = 0) then
                Some Stopping_reason.Enough_posts_scraped
            else None
        
        match
            Scrape_timeline.reveal_and_parse_timeline
                browser
                html_context
                Timeline_tab.Posts_and_replies
                (User_handle "tehprom269887")
                (check_post posts_to_testers)
                (no_left_posts_to_test posts_to_testers)
        with
        |Success amount ->
            Log.info $"{amount} posts have been tested in timeline"
        |problem ->
            $"a problem with revealing and parsing timeline: {problem}"
            |>Harvesting_exception
            |>raise