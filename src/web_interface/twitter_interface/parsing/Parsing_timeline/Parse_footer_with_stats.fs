namespace rvinowise.twitter

open rvinowise.html_parsing
open rvinowise.twitter
open FParsec
open FsUnit
open Xunit

module Parse_footer_with_stats =


    //let number_from_explanation text =
        
    
    
    (* 4308 replies, 9439 reposts, 63913 likes, 452 bookmarks, 5006309 views *)
    let stats_from_explanation (text:string) =
        text.Split ","
        |>Array.map (fun stat_element -> stat_element.Trim())
        |>Array.map (fun stat -> stat.Split " ")
        |>Array.fold (fun stats stat_element ->
            match List.ofArray stat_element with
            |amount::["replies"] | amount::["reply"] ->
                {stats with replies=int amount}
            |amount::["reposts"] | amount::["repost"] ->
                {stats with reposts=int amount}
            |amount::["likes"] | amount::["like"] ->
                {stats with likes=int amount}
            |amount::["bookmarks"] | amount::["bookmark"] ->
                {stats with bookmarks=int amount}
            |amount::["views"] | amount::["view"] ->
                {stats with views=int amount}
            |["Liked"]|["Reposted"] -> stats //likes and reposts are scraped separately from timelines of that user
            |[""]->stats 
            |unexpected ->
                Log.error
                    $"unexpected explanation {unexpected} of post statistics: {text}"|>ignore
                stats
        )
            Post_stats.all_zero
        
    
    
    [<Fact>]
    let ``try stats_from_explanation``()=
        "4308 replies, 9439 reposts, 63913 likes, 452 bookmarks, 5006309 views"
        |>stats_from_explanation
        |>should equal {
            Post_stats.replies = 4308
            reposts = 9439
            likes = 63913
            views = 5006309
            bookmarks = 452
        }
        "4 replies, 4 reposts, 35 likes, Liked, 6 bookmarks, 2479 views"
        |>stats_from_explanation
        |>should equal {
            Post_stats.replies = 4
            reposts = 4
            likes = 35
            views = 2479
            bookmarks = 6
        }
        "1 reply, 1 like, Liked, 11 views"
        |>stats_from_explanation
        |>should equal {
            Post_stats.replies = 1
            reposts = 0
            likes = 1
            views = 11
            bookmarks = 0
        }
        
    let stats_from_hidden_explanation_of_footer
        footer_node //role=group
        =
        footer_node
        |>Html_node.attribute_value "aria-label"
        |>stats_from_explanation
    
    
    let number_inside_footer_element stat_segment =
        stat_segment
        |>Html_node.try_descendant ":scope>span>span"
        |>function
        |None -> 0
        |Some node ->
            node
            |>Html_node.inner_text
            |>Parsing_twitter_datatypes.parse_abbreviated_number
            
    let stats_from_html_children_of_footer
        footer //node with all app-text-transition-container of a post (and role="group")
        =
        //it's not precise, because shown numbers are abbreviated
        let stat_segments =
            footer
            |>Html_node.descendants "span[data-testid='app-text-transition-container']"
        {
            Post_stats.replies=
                stat_segments
                |>Seq.head
                |>number_inside_footer_element
            reposts=
                stat_segments
                |>Seq.item 1
                |>number_inside_footer_element
            likes=
                stat_segments
                |>Seq.item 2
                |>number_inside_footer_element
            views= //may not exist!
                stat_segments
                |>Seq.item 3
                |>number_inside_footer_element
            bookmarks=0
        }
    
    let parse_post_footer
        footer //role="group"
        =
        stats_from_hidden_explanation_of_footer footer