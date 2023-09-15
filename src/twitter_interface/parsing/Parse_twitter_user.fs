namespace rvinowise.twitter

open System
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.parallell.functions
open rvinowise.twitter
open FSharp.Data

type Twitter_profile_from_catalog = {
    user: Twitter_user
    bio: string
}

module Twitter_profile_from_catalog =
    let handle profile =
        profile.user.handle


module Parse_twitter_user =
    
    let name_node (catalog_item_cell:HtmlNode) =
        catalog_item_cell
        |>Parsing.descendants "a[role='link'] div[dir='ltr']"
        |>Seq.head//ignore second, useless div
        |>HtmlNode.elements //span with the compound name
        |>Seq.head //there's only one such a span
    
    
    let parse_user_bio_from_textual_user_div
        (node_with_textual_user_info:HtmlNode)
        =
        node_with_textual_user_info
        |>HtmlNode.elements
        |>Seq.tryItem 1 //skip the first div which is the name and handle. take the second div which is the briefing 
        |>function
        |Some bio_node->
            Parsing.readable_text_from_html_segments bio_node
        |None->""
           
    
    let parse_twitter_user_cell user_cell =
        let name =
            user_cell
            |>name_node
            |>Parsing.readable_text_from_html_segments
        
        let node_with_textual_user_info =
            user_cell
            |>Parsing.descend 1
            |>HtmlNode.elements
            |>Seq.item 1 //the second div is the text of the user-info. skip the first div which is the image
        let handle =
            node_with_textual_user_info
            |>Parsing.descendants "div>div>div>div>a[role='link']"
            |>Seq.head
            |>HtmlNode.attributeValue "href"
            |>fun user->user[1..]
            |>User_handle
            
        let user_bio =
            parse_user_bio_from_textual_user_div
                node_with_textual_user_info
            
        {
            user={handle=handle; name=name};
            Twitter_profile_from_catalog.bio=user_bio
        }
        
        
    [<Fact>]
    let ``try parse_user_bio_from_textual_user_div``()=
        let test = 
            """<div dir="auto" class="css-901oao r-18jsvk2 r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0" data-testid="UserDescription"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Researcher </span><div class="css-1dbjc4n r-xoduu5"><span class="r-18u37iz"><a dir="ltr" href="/AgeGlobal" role="link" class="css-4rbku5 css-18t94o4 css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0">@AgeGlobal</a></span></div><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0"> </span><div class="css-1dbjc4n r-xoduu5"><span class="r-18u37iz"><a dir="ltr" href="/HealthyLongeviT" role="link" class="css-4rbku5 css-18t94o4 css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0">@HealthyLongeviT</a></span></div><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0"> formerly </span><div class="css-1dbjc4n r-xoduu5"><span class="r-18u37iz"><a dir="ltr" href="/karolinskainst" role="link" class="css-4rbku5 css-18t94o4 css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0">@karolinskainst</a></span></div><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0"> </span><span class="r-18u37iz"><a dir="ltr" href="/search?q=%23neuroscience&amp;src=hashtag_click" role="link" class="css-4rbku5 css-18t94o4 css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0">#neuroscience</a></span><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0"> </span><span class="r-18u37iz"><a dir="ltr" href="/search?q=%23ageing&amp;src=hashtag_click" role="link" class="css-4rbku5 css-18t94o4 css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0">#ageing</a></span><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0"> </span><span class="r-18u37iz"><a dir="ltr" href="/search?q=%23alzheimerdisease&amp;src=hashtag_click" role="link" class="css-4rbku5 css-18t94o4 css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0">#alzheimerdisease</a></span><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0"> </span><span class="r-18u37iz"><a dir="ltr" href="/search?q=%23longevity&amp;src=hashtag_click" role="link" class="css-4rbku5 css-18t94o4 css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0">#longevity</a></span></div>"""
            |>HtmlNode.Parse |>Seq.head
            |>Parsing.readable_text_from_html_segments
        ()