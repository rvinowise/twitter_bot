namespace rvinowise.twitter

open System
open AngleSharp
open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.twitter.Parse_segments_of_post
open rvinowise.web_scraping

open FsUnit
open Xunit


module Scrape_dynamic_list =
    
    
    let skim_displayed_items
        (is_item_needed: Html_string -> bool) 
        browser
        item_css
        =
        //use parameters = Scraping_parameters.wait_seconds 60 browser
        let items = Browser.elements item_css browser
        items
        |>List.map (fun web_element ->
            try
                web_element
                |>Web_element.attribute_value "outerHTML"
                |>Html_string
                |>Some
            with
            | :? StaleElementReferenceException as exc ->
                Log.error $"skim_displayed_items error: {exc.Message}"|>ignore
                None
        )|>List.choose id
        |>List.filter is_item_needed
        
        

    let add_new_items_to_map
        added_items
        map
        =
        added_items
        |>Seq.fold (fun all_items (new_item,parsed_new_item) -> 
            all_items
            |>Map.add new_item parsed_new_item
        )
            map
    
    let new_items_from_visible_items
        (items_are_equal: 'a -> 'a -> bool)
        (visible_items: 'a array)
        old_items
        =
        let last_known_item =
            old_items
            |>List.tryLast
        match last_known_item with
        |Some last_known_item ->
            visible_items
            |>Array.tryFindIndex (items_are_equal last_known_item)
            |>function
            |Some i_first_previously_known_item ->
                visible_items
                |>Array.splitAt i_first_previously_known_item
                |>snd
                |>Array.tail
            |None->
                Log.error $"""lists are not overlapping, some elements might be missed.
                full_list={old_items};
                new_list={visible_items}"""|>ignore
                visible_items
            
        |None->
            visible_items
        
   
    [<Fact>]
    let ``try unique_items_of_new_collection``() =
        let new_items = [|3;4;5;6;7|]
        let all_items = [1;2;3;4;5]
        new_items_from_visible_items
            (=)
            new_items
            all_items
        |>should equal [|
            6;7
        |]
        
        let new_items = [|3;4;5|]
        let all_items = [0;1;2;3;4;5]
        new_items_from_visible_items
            (=) 
            new_items
            all_items
        |>should equal [||]
    
    
    let html_has_same_text
        (node1: Html_node)
        (node2: Html_node)
        =
        node1
        |>Html_node.remove_parts_not_related_to_text
        |>Html_node.to_string
        |>(=) (
            node2
            |>Html_node.remove_parts_not_related_to_text
            |>Html_node.to_string
        )
        
    [<Fact>]
    let ``try compare equivalent posts with differences in IDs``()=
        let context = BrowsingContext.New AngleSharp.Configuration.Default
        let post1 =
            """<div data-testid="cellInnerDiv"><div><div><article aria-labelledby="id__4co35hjyi6s id__0m879k6ikgmj id__urg5b5kbv8q id__lyuovvthqik id__9ecr51ofk6w id__8trhmoa5m1a id__7aryo9w99pf id__ezu7pr420w id__cyr88kffgl6 id__11y035ysvyk id__vxyj2njjyok id__ru6d3gya9v id__rrx1rhshylq id__wwnsihtmou id__aef9lmw27q id__qccwr4hgaes id__htf85xyufme id__dtmrjywp70o id__n9tj8jefcaa" role="article" tabindex="0" data-testid="tweet"><div><div><div><div><div></div></div></div><div><div><div data-testid="Tweet-User-Avatar"><div><div><div data-testid="UserAvatar-Container-realNathanCheng"><div></div><div><div><div></div><div><div><a href="/realNathanCheng" aria-hidden="true" role="link" tabindex="-1"><div><div></div></div><div><div></div></div><div><div><div></div><div><div aria-label=""><div></div><img alt="" draggable="true" src="https://pbs.twimg.com/profile_images/1295110787019743234/s8BrGSf2_bigger.jpg"></div></div></div></div><div><div></div></div></a></div></div></div></div></div></div></div></div><div></div></div><div><div><div><div><div><div id="id__9ecr51ofk6w" data-testid="User-Name"><div><div><a href="/realNathanCheng" role="link"><div><div dir="ltr"><span><span>Nathan S. Cheng thinks you should work on aging.</span></span></div><div dir="ltr"><span><svg viewBox="0 0 22 22" aria-label="Verified account" role="img" data-testid="icon-verified"><g><path d="M20.396 11c-.018-.646-.215-1.275-.57-1.816-.354-.54-.852-.972-1.438-1.246.223-.607.27-1.264.14-1.897-.131-.634-.437-1.218-.882-1.687-.47-.445-1.053-.75-1.687-.882-.633-.13-1.29-.083-1.897.14-.273-.587-.704-1.086-1.245-1.44S11.647 1.62 11 1.604c-.646.017-1.273.213-1.813.568s-.969.854-1.24 1.44c-.608-.223-1.267-.272-1.902-.14-.635.13-1.22.436-1.69.882-.445.47-.749 1.055-.878 1.688-.13.633-.08 1.29.144 1.896-.587.274-1.087.705-1.443 1.245-.356.54-.555 1.17-.574 1.817.02.647.218 1.276.574 1.817.356.54.856.972 1.443 1.245-.224.606-.274 1.263-.144 1.896.13.634.433 1.218.877 1.688.47.443 1.054.747 1.687.878.633.132 1.29.084 1.897-.136.274.586.705 1.084 1.246 1.439.54.354 1.17.551 1.816.569.647-.016 1.276-.213 1.817-.567s.972-.854 1.245-1.44c.604.239 1.266.296 1.903.164.636-.132 1.22-.447 1.68-.907.46-.46.776-1.044.908-1.681s.075-1.299-.165-1.903c.586-.274 1.084-.705 1.439-1.246.354-.54.551-1.17.569-1.816zM9.662 14.85l-3.429-3.428 1.293-1.302 2.072 2.072 4.4-4.794 1.347 1.246z"></path></g></svg></span></div></div></a></div></div><div><div><div><a href="/realNathanCheng" role="link" tabindex="-1"><div dir="ltr"><span>@realNathanCheng</span></div></a></div><div dir="ltr" aria-hidden="true"><span>·</span></div><div><a href="/realNathanCheng/status/1694793444009337123" dir="ltr" aria-label="Aug 24" role="link"><time datetime="2023-08-24T19:26:59.000Z">Aug 24</time></a></div></div></div></div></div></div><div><div><div><div><div><div aria-expanded="false" aria-haspopup="menu" aria-label="More" role="button" tabindex="0" data-testid="caret"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M3 12c0-1.1.9-2 2-2s2 .9 2 2-.9 2-2 2-2-.9-2-2zm9 2c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm7 0c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div><div><div dir="auto" lang="en" id="id__vxyj2njjyok" data-testid="tweetText"><span>There are 8B other people with you in this escape room. But most don’t even know it’s an escape room.</span></div></div><div aria-labelledby="id__qm9gtpiyen id__h5aiiq877ji" id="id__rrx1rhshylq"><div id="id__qm9gtpiyen"><div dir="ltr"><span>Quote</span></div><div tabindex="0" role="link"><div><div><div><div><div><div data-testid="Tweet-User-Avatar"><div><div data-testid="UserAvatar-Container-realNathanCheng"><div></div><div><div><div></div><div><div><div aria-hidden="true" role="presentation" tabindex="-1"><div><div></div></div><div><div></div></div><div><div><div></div><div><div aria-label=""><div></div><img alt="" draggable="true" src="https://pbs.twimg.com/profile_images/1295110787019743234/s8BrGSf2_normal.jpg"></div></div></div></div><div><div></div></div></div></div></div></div></div></div></div></div><div><div data-testid="User-Name"><div><div><div><div><div dir="ltr"><span><span>Nathan S. Cheng thinks you should work on aging.</span></span></div><div dir="ltr"><span><svg viewBox="0 0 22 22" aria-label="Verified account" role="img" data-testid="icon-verified"><g><path d="M20.396 11c-.018-.646-.215-1.275-.57-1.816-.354-.54-.852-.972-1.438-1.246.223-.607.27-1.264.14-1.897-.131-.634-.437-1.218-.882-1.687-.47-.445-1.053-.75-1.687-.882-.633-.13-1.29-.083-1.897.14-.273-.587-.704-1.086-1.245-1.44S11.647 1.62 11 1.604c-.646.017-1.273.213-1.813.568s-.969.854-1.24 1.44c-.608-.223-1.267-.272-1.902-.14-.635.13-1.22.436-1.69.882-.445.47-.749 1.055-.878 1.688-.13.633-.08 1.29.144 1.896-.587.274-1.087.705-1.443 1.245-.356.54-.555 1.17-.574 1.817.02.647.218 1.276.574 1.817.356.54.856.972 1.443 1.245-.224.606-.274 1.263-.144 1.896.13.634.433 1.218.877 1.688.47.443 1.054.747 1.687.878.633.132 1.29.084 1.897-.136.274.586.705 1.084 1.246 1.439.54.354 1.17.551 1.816.569.647-.016 1.276-.213 1.817-.567s.972-.854 1.245-1.44c.604.239 1.266.296 1.903.164.636-.132 1.22-.447 1.68-.907.46-.46.776-1.044.908-1.681s.075-1.299-.165-1.903c.586-.274 1.084-.705 1.439-1.246.354-.54.551-1.17.569-1.816zM9.662 14.85l-3.429-3.428 1.293-1.302 2.072 2.072 4.4-4.794 1.347 1.246z"></path></g></svg></span></div></div></div></div></div><div><div><div><div tabindex="-1"><div dir="ltr"><span>@realNathanCheng</span></div></div></div><div dir="ltr" aria-hidden="true"><span>·</span></div><div><div><div dir="ltr" aria-label="Aug 24"><time datetime="2023-08-24T18:34:27.000Z">Aug 24</time></div><div dir="ltr" aria-hidden="true"><span>·</span></div><div dir="ltr"><svg viewBox="0 0 24 24" aria-label="This is the latest version of this post." role="img"><g><path d="M21.15 6.232c.97.977.97 2.559 0 3.536L9.91 21H3v-6.914L14.23 2.854c.98-.977 2.56-.977 3.54 0l3.38 3.378zM14.75 19l-2 2H21v-2h-6.25z"></path></g></svg></div></div></div></div></div></div></div></div></div></div></div><div><div dir="auto" lang="en" data-testid="tweetText"><span>Human biology is the ultimate escape room: You get 80 years to figure out the puzzle or you die.</span></div></div></div></div></div></div><div><div><div aria-label="4 replies, 9 reposts, 83 likes, 1 bookmark, 5360 views" role="group" id="id__n9tj8jefcaa"><div><div aria-label="4 Replies. Reply" role="button" tabindex="0" data-testid="reply"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M1.751 10c0-4.42 3.584-8 8.005-8h4.366c4.49 0 8.129 3.64 8.129 8.13 0 2.96-1.607 5.68-4.196 7.11l-8.054 4.46v-3.69h-.067c-4.49.1-8.183-3.51-8.183-8.01zm8.005-6c-3.317 0-6.005 2.69-6.005 6 0 3.37 2.77 6.08 6.138 6.01l.351-.01h1.761v2.3l5.087-2.81c1.951-1.08 3.163-3.13 3.163-5.36 0-3.39-2.744-6.13-6.129-6.13H9.756z"></path></g></svg></div><div><span data-testid="app-text-transition-container"><span><span>4</span></span></span></div></div></div></div><div><div aria-expanded="false" aria-haspopup="menu" aria-label="9 reposts. Repost" role="button" tabindex="0" data-testid="retweet"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M4.5 3.88l4.432 4.14-1.364 1.46L5.5 7.55V16c0 1.1.896 2 2 2H13v2H7.5c-2.209 0-4-1.79-4-4V7.55L1.432 9.48.068 8.02 4.5 3.88zM16.5 6H11V4h5.5c2.209 0 4 1.79 4 4v8.45l2.068-1.93 1.364 1.46-4.432 4.14-4.432-4.14 1.364-1.46 2.068 1.93V8c0-1.1-.896-2-2-2z"></path></g></svg></div><div><span data-testid="app-text-transition-container"><span><span>9</span></span></span></div></div></div></div><div><div aria-label="83 Likes. Like" role="button" tabindex="0" data-testid="like"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M16.697 5.5c-1.222-.06-2.679.51-3.89 2.16l-.805 1.09-.806-1.09C9.984 6.01 8.526 5.44 7.304 5.5c-1.243.07-2.349.78-2.91 1.91-.552 1.12-.633 2.78.479 4.82 1.074 1.97 3.257 4.27 7.129 6.61 3.87-2.34 6.052-4.64 7.126-6.61 1.111-2.04 1.03-3.7.477-4.82-.561-1.13-1.666-1.84-2.908-1.91zm4.187 7.69c-1.351 2.48-4.001 5.12-8.379 7.67l-.503.3-.504-.3c-4.379-2.55-7.029-5.19-8.382-7.67-1.36-2.5-1.41-4.86-.514-6.67.887-1.79 2.647-2.91 4.601-3.01 1.651-.09 3.368.56 4.798 2.01 1.429-1.45 3.146-2.1 4.796-2.01 1.954.1 3.714 1.22 4.601 3.01.896 1.81.846 4.17-.514 6.67z"></path></g></svg></div><div><span data-testid="app-text-transition-container"><span><span>83</span></span></span></div></div></div></div><div><a href="/realNathanCheng/status/1694793444009337123/analytics" aria-label="5360 views. View post analytics" role="link"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M8.75 21V3h2v18h-2zM18 21V8.5h2V21h-2zM4 21l.004-10h2L6 21H4zm9.248 0v-7h2v7h-2z"></path></g></svg></div><div><span data-testid="app-text-transition-container"><span><span>5.3K</span></span></span></div></div></a></div><div><div aria-label="Bookmark" role="button" tabindex="0" data-testid="bookmark"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M4 4.5C4 3.12 5.119 2 6.5 2h11C18.881 2 20 3.12 20 4.5v18.44l-8-5.71-8 5.71V4.5zM6.5 4c-.276 0-.5.22-.5.5v14.56l6-4.29 6 4.29V4.5c0-.28-.224-.5-.5-.5h-11z"></path></g></svg></div></div></div></div><div><div><div aria-expanded="false" aria-haspopup="menu" aria-label="Share post" role="button" tabindex="0"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M12 2.59l5.7 5.7-1.41 1.42L13 6.41V16h-2V6.41l-3.3 3.3-1.41-1.42L12 2.59zM21 15l-.02 3.51c0 1.38-1.12 2.49-2.5 2.49H5.5C4.11 21 3 19.88 3 18.5V15h2v3.5c0 .28.22.5.5.5h12.98c.28 0 .5-.22.5-.5L19 15h2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div></div></div></article></div></div></div>"""
            |>Html_node.from_text_and_context context
        let post2 =
            """<div data-testid="cellInnerDiv"><div><div><article aria-labelledby="id__92vhokvcprv id__37mgc40yhoi id__bn0ooztzf id__io1ihdhcq7b id__4m5tz8kznsk id__2irzctgd2vt id__kdr4o76rlpr id__tgpdlji9fvs id__rzli0pi4h88 id__hyke4xyzh3 id__yzvbmo7gcj7 id__ysukzfhzq1t id__adhq64amauc id__qvamggi2ka id__fe9uhx97l7n id__scktkz5nega id__9943iy6rf7i id__td8x7lvnr5o id__c150jy5zi7v" role="article" tabindex="0" data-testid="tweet"><div><div><div><div><div></div></div></div><div><div><div data-testid="Tweet-User-Avatar"><div><div><div data-testid="UserAvatar-Container-realNathanCheng"><div></div><div><div><div></div><div><div><a href="/realNathanCheng" aria-hidden="true" role="link" tabindex="-1"><div><div></div></div><div><div></div></div><div><div><div></div><div><div aria-label=""><div></div><img alt="" draggable="true" src="https://pbs.twimg.com/profile_images/1295110787019743234/s8BrGSf2_bigger.jpg"></div></div></div></div><div><div></div></div></a></div></div></div></div></div></div></div></div><div></div></div><div><div><div><div><div><div id="id__4m5tz8kznsk" data-testid="User-Name"><div><div><a href="/realNathanCheng" role="link"><div><div dir="ltr"><span><span>Nathan S. Cheng thinks you should work on aging.</span></span></div><div dir="ltr"><span><svg viewBox="0 0 22 22" aria-label="Verified account" role="img" data-testid="icon-verified"><g><path d="M20.396 11c-.018-.646-.215-1.275-.57-1.816-.354-.54-.852-.972-1.438-1.246.223-.607.27-1.264.14-1.897-.131-.634-.437-1.218-.882-1.687-.47-.445-1.053-.75-1.687-.882-.633-.13-1.29-.083-1.897.14-.273-.587-.704-1.086-1.245-1.44S11.647 1.62 11 1.604c-.646.017-1.273.213-1.813.568s-.969.854-1.24 1.44c-.608-.223-1.267-.272-1.902-.14-.635.13-1.22.436-1.69.882-.445.47-.749 1.055-.878 1.688-.13.633-.08 1.29.144 1.896-.587.274-1.087.705-1.443 1.245-.356.54-.555 1.17-.574 1.817.02.647.218 1.276.574 1.817.356.54.856.972 1.443 1.245-.224.606-.274 1.263-.144 1.896.13.634.433 1.218.877 1.688.47.443 1.054.747 1.687.878.633.132 1.29.084 1.897-.136.274.586.705 1.084 1.246 1.439.54.354 1.17.551 1.816.569.647-.016 1.276-.213 1.817-.567s.972-.854 1.245-1.44c.604.239 1.266.296 1.903.164.636-.132 1.22-.447 1.68-.907.46-.46.776-1.044.908-1.681s.075-1.299-.165-1.903c.586-.274 1.084-.705 1.439-1.246.354-.54.551-1.17.569-1.816zM9.662 14.85l-3.429-3.428 1.293-1.302 2.072 2.072 4.4-4.794 1.347 1.246z"></path></g></svg></span></div></div></a></div></div><div><div><div><a href="/realNathanCheng" role="link" tabindex="-1"><div dir="ltr"><span>@realNathanCheng</span></div></a></div><div dir="ltr" aria-hidden="true"><span>·</span></div><div><a href="/realNathanCheng/status/1694793444009337123" dir="ltr" aria-label="Aug 24" role="link"><time datetime="2023-08-24T19:26:59.000Z">Aug 24</time></a></div></div></div></div></div></div><div><div><div><div><div><div aria-expanded="false" aria-haspopup="menu" aria-label="More" role="button" tabindex="0" data-testid="caret"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M3 12c0-1.1.9-2 2-2s2 .9 2 2-.9 2-2 2-2-.9-2-2zm9 2c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm7 0c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div><div><div dir="auto" lang="en" id="id__yzvbmo7gcj7" data-testid="tweetText"><span>There are 8B other people with you in this escape room. But most don’t even know it’s an escape room.</span></div></div><div aria-labelledby="id__5f0kypbzvg5 id__ic8u4zy4rv" id="id__adhq64amauc"><div id="id__5f0kypbzvg5"><div dir="ltr"><span>Quote</span></div><div tabindex="0" role="link"><div><div><div><div><div><div data-testid="Tweet-User-Avatar"><div><div data-testid="UserAvatar-Container-realNathanCheng"><div></div><div><div><div></div><div><div><div aria-hidden="true" role="presentation" tabindex="-1"><div><div></div></div><div><div></div></div><div><div><div></div><div><div aria-label=""><div></div><img alt="" draggable="true" src="https://pbs.twimg.com/profile_images/1295110787019743234/s8BrGSf2_normal.jpg"></div></div></div></div><div><div></div></div></div></div></div></div></div></div></div></div><div><div data-testid="User-Name"><div><div><div><div><div dir="ltr"><span><span>Nathan S. Cheng thinks you should work on aging.</span></span></div><div dir="ltr"><span><svg viewBox="0 0 22 22" aria-label="Verified account" role="img" data-testid="icon-verified"><g><path d="M20.396 11c-.018-.646-.215-1.275-.57-1.816-.354-.54-.852-.972-1.438-1.246.223-.607.27-1.264.14-1.897-.131-.634-.437-1.218-.882-1.687-.47-.445-1.053-.75-1.687-.882-.633-.13-1.29-.083-1.897.14-.273-.587-.704-1.086-1.245-1.44S11.647 1.62 11 1.604c-.646.017-1.273.213-1.813.568s-.969.854-1.24 1.44c-.608-.223-1.267-.272-1.902-.14-.635.13-1.22.436-1.69.882-.445.47-.749 1.055-.878 1.688-.13.633-.08 1.29.144 1.896-.587.274-1.087.705-1.443 1.245-.356.54-.555 1.17-.574 1.817.02.647.218 1.276.574 1.817.356.54.856.972 1.443 1.245-.224.606-.274 1.263-.144 1.896.13.634.433 1.218.877 1.688.47.443 1.054.747 1.687.878.633.132 1.29.084 1.897-.136.274.586.705 1.084 1.246 1.439.54.354 1.17.551 1.816.569.647-.016 1.276-.213 1.817-.567s.972-.854 1.245-1.44c.604.239 1.266.296 1.903.164.636-.132 1.22-.447 1.68-.907.46-.46.776-1.044.908-1.681s.075-1.299-.165-1.903c.586-.274 1.084-.705 1.439-1.246.354-.54.551-1.17.569-1.816zM9.662 14.85l-3.429-3.428 1.293-1.302 2.072 2.072 4.4-4.794 1.347 1.246z"></path></g></svg></span></div></div></div></div></div><div><div><div><div tabindex="-1"><div dir="ltr"><span>@realNathanCheng</span></div></div></div><div dir="ltr" aria-hidden="true"><span>·</span></div><div><div><div dir="ltr" aria-label="Aug 24"><time datetime="2023-08-24T18:34:27.000Z">Aug 24</time></div><div dir="ltr" aria-hidden="true"><span>·</span></div><div dir="ltr"><svg viewBox="0 0 24 24" aria-label="This is the latest version of this post." role="img"><g><path d="M21.15 6.232c.97.977.97 2.559 0 3.536L9.91 21H3v-6.914L14.23 2.854c.98-.977 2.56-.977 3.54 0l3.38 3.378zM14.75 19l-2 2H21v-2h-6.25z"></path></g></svg></div></div></div></div></div></div></div></div></div></div></div><div><div dir="auto" lang="en" data-testid="tweetText"><span>Human biology is the ultimate escape room: You get 80 years to figure out the puzzle or you die.</span></div></div></div></div></div></div><div><div><div aria-label="4 replies, 9 reposts, 83 likes, 1 bookmark, 5360 views" role="group" id="id__c150jy5zi7v"><div><div aria-label="4 Replies. Reply" role="button" tabindex="0" data-testid="reply"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M1.751 10c0-4.42 3.584-8 8.005-8h4.366c4.49 0 8.129 3.64 8.129 8.13 0 2.96-1.607 5.68-4.196 7.11l-8.054 4.46v-3.69h-.067c-4.49.1-8.183-3.51-8.183-8.01zm8.005-6c-3.317 0-6.005 2.69-6.005 6 0 3.37 2.77 6.08 6.138 6.01l.351-.01h1.761v2.3l5.087-2.81c1.951-1.08 3.163-3.13 3.163-5.36 0-3.39-2.744-6.13-6.129-6.13H9.756z"></path></g></svg></div><div><span data-testid="app-text-transition-container"><span><span>4</span></span></span></div></div></div></div><div><div aria-expanded="false" aria-haspopup="menu" aria-label="9 reposts. Repost" role="button" tabindex="0" data-testid="retweet"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M4.5 3.88l4.432 4.14-1.364 1.46L5.5 7.55V16c0 1.1.896 2 2 2H13v2H7.5c-2.209 0-4-1.79-4-4V7.55L1.432 9.48.068 8.02 4.5 3.88zM16.5 6H11V4h5.5c2.209 0 4 1.79 4 4v8.45l2.068-1.93 1.364 1.46-4.432 4.14-4.432-4.14 1.364-1.46 2.068 1.93V8c0-1.1-.896-2-2-2z"></path></g></svg></div><div><span data-testid="app-text-transition-container"><span><span>9</span></span></span></div></div></div></div><div><div aria-label="83 Likes. Like" role="button" tabindex="0" data-testid="like"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M16.697 5.5c-1.222-.06-2.679.51-3.89 2.16l-.805 1.09-.806-1.09C9.984 6.01 8.526 5.44 7.304 5.5c-1.243.07-2.349.78-2.91 1.91-.552 1.12-.633 2.78.479 4.82 1.074 1.97 3.257 4.27 7.129 6.61 3.87-2.34 6.052-4.64 7.126-6.61 1.111-2.04 1.03-3.7.477-4.82-.561-1.13-1.666-1.84-2.908-1.91zm4.187 7.69c-1.351 2.48-4.001 5.12-8.379 7.67l-.503.3-.504-.3c-4.379-2.55-7.029-5.19-8.382-7.67-1.36-2.5-1.41-4.86-.514-6.67.887-1.79 2.647-2.91 4.601-3.01 1.651-.09 3.368.56 4.798 2.01 1.429-1.45 3.146-2.1 4.796-2.01 1.954.1 3.714 1.22 4.601 3.01.896 1.81.846 4.17-.514 6.67z"></path></g></svg></div><div><span data-testid="app-text-transition-container"><span><span>83</span></span></span></div></div></div></div><div><a href="/realNathanCheng/status/1694793444009337123/analytics" aria-label="5360 views. View post analytics" role="link"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M8.75 21V3h2v18h-2zM18 21V8.5h2V21h-2zM4 21l.004-10h2L6 21H4zm9.248 0v-7h2v7h-2z"></path></g></svg></div><div><span data-testid="app-text-transition-container"><span><span>5.3K</span></span></span></div></div></a></div><div><div aria-label="Bookmark" role="button" tabindex="0" data-testid="bookmark"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M4 4.5C4 3.12 5.119 2 6.5 2h11C18.881 2 20 3.12 20 4.5v18.44l-8-5.71-8 5.71V4.5zM6.5 4c-.276 0-.5.22-.5.5v14.56l6-4.29 6 4.29V4.5c0-.28-.224-.5-.5-.5h-11z"></path></g></svg></div></div></div></div><div><div><div aria-expanded="false" aria-haspopup="menu" aria-label="Share post" role="button" tabindex="0"><div dir="ltr"><div><div></div><svg viewBox="0 0 24 24" aria-hidden="true"><g><path d="M12 2.59l5.7 5.7-1.41 1.42L13 6.41V16h-2V6.41l-3.3 3.3-1.41-1.42L12 2.59zM21 15l-.02 3.51c0 1.38-1.12 2.49-2.5 2.49H5.5C4.11 21 3 19.88 3 18.5V15h2v3.5c0 .28.22.5.5.5h12.98c.28 0 .5-.22.5-.5L19 15h2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div></div></div></article></div></div></div>"""
            |>Html_node.from_text_and_context context
        
        html_has_same_text post1 post2
        |>should equal true
    
    let new_nodes_from_visible_nodes
        new_nodes
        previous_nodes
        =
        new_items_from_visible_items
            html_has_same_text
            new_nodes
            previous_nodes
     
    
    let skim_new_visible_items
        browser
        html_parsing_context
        is_item_needed
        item_selector
        previous_items
        =
        let visible_skimmed_items =
            skim_displayed_items
                is_item_needed
                browser
                item_selector
            |>List.map (Html_node.from_html_string_and_context html_parsing_context)
            |>Array.ofList
            
        previous_items
        |>new_nodes_from_visible_nodes
              visible_skimmed_items
        |>List.ofArray
    
    let process_item_batch_providing_previous_items
        (context: Previous_cell)
        items
        (process_item: Previous_cell -> Html_node -> Previous_cell option)
        =
        let rec iteration_of_batch_processing
            context
            items
            =
            match items with
            |next_item::rest_items ->
                let new_context =
                    process_item context next_item
                match new_context with
                |None->None
                |Some context ->
                    iteration_of_batch_processing
                        context
                        rest_items
            |[]->Some context
            
        
        iteration_of_batch_processing
            context
            items
            
    
    let rec skim_and_scroll_iteration
        wait_for_loading
        (skim_new_visible_items: list<Html_node> -> list<Html_node>)
        (process_item: Previous_cell -> Html_node -> Previous_cell option)
        load_next_items
        (previous_items: list<Html_node>) // sorted 0=top
        (previous_context: Previous_cell)
        scrolling_repetitions
        repetitions_left
        =
        
        wait_for_loading()
        
        let new_skimmed_items =
            skim_new_visible_items previous_items
        
        load_next_items()
                
        match new_skimmed_items with
        |[]->
            if repetitions_left = 0 then
                None
            else
                Log.debug $"skim_and_scroll_iteration didn't find new items, attempts left={repetitions_left}; trying again "
                skim_and_scroll_iteration
                    wait_for_loading
                    skim_new_visible_items
                    process_item
                    load_next_items
                    previous_items
                    previous_context
                    scrolling_repetitions
                    (repetitions_left-1)
        | new_skimmed_items ->
            
            let next_context =
                process_item_batch_providing_previous_items
                    previous_context
                    new_skimmed_items
                    process_item
                    
            match next_context with
            |Some next_context ->
                skim_and_scroll_iteration
                    wait_for_loading
                    skim_new_visible_items
                    process_item
                    load_next_items
                    new_skimmed_items
                    next_context
                    scrolling_repetitions
                    scrolling_repetitions
            |None->
                None
            
    
    let parse_dynamic_list_with_previous_item
        wait_for_loading
        is_item_needed
        (process_item: Previous_cell -> Html_node -> Previous_cell option)
        browser
        html_parsing_context
        scrolling_repetitions
        item_selector
        =
        let skim_new_visible_items =
            skim_new_visible_items
                browser
                html_parsing_context
                is_item_needed
                item_selector
                   
        let load_next_items =
            fun () -> Browser.send_keys [Keys.PageDown;Keys.PageDown;Keys.PageDown] browser
        
        skim_and_scroll_iteration
            wait_for_loading
            skim_new_visible_items
            process_item
            load_next_items
            []
            Previous_cell.No_cell
            scrolling_repetitions
            scrolling_repetitions
        |>ignore
    
    let collect_items_of_dynamic_list
        browser
        wait_for_loading
        is_item_needed
        item_selector
        =
        let html_parsing_context = BrowsingContext.New AngleSharp.Configuration.Default
            
        let rec skim_and_scroll_iteration
            (all_items: list<Html_node>)
            =
            
            wait_for_loading()
            
            let new_skimmed_items =
                skim_new_visible_items
                    browser
                    html_parsing_context
                    is_item_needed
                    item_selector
                    all_items
            
            if
                not new_skimmed_items.IsEmpty
            then
                Browser.send_keys [Keys.PageDown;Keys.PageDown;Keys.PageDown] browser
                
                all_items
                @
                new_skimmed_items
                |>skim_and_scroll_iteration
            else
                all_items
        
        skim_and_scroll_iteration []     
    
    let dont_parse_html_item _ _ = ()
    
    let all_items_are_needed _ = true
    
    let collect_all_html_items_of_dynamic_list
        browser
        wait_for_loading
        item_selector
        =
        item_selector
        |>collect_items_of_dynamic_list
            browser
            wait_for_loading
            all_items_are_needed
        
    let collect_some_html_items_of_dynamic_list
        browser
        wait_for_loading
        item_selector
        =
        item_selector
        |>collect_items_of_dynamic_list
            browser
            wait_for_loading
            all_items_are_needed
        
    

    
    



    


