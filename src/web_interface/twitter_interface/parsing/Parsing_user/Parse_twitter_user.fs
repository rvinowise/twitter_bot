namespace rvinowise.twitter

open Xunit

open rvinowise.html_parsing

type Twitter_profile_from_catalog = {
    user: Twitter_user
    bio: string
}

module Twitter_profile_from_catalog =
    let handle profile =
        profile.user.handle
    let user profile =
        profile.user

module Parse_twitter_user =
    
    let name_node catalog_item_cell =
        catalog_item_cell
        |>Html_node.descendants "a[role='link'] div[dir='ltr']"
        |>Seq.head//ignore second, useless div
        |>Html_node.direct_children //span with the compound name
        |>Seq.head //there's only one such a span
    
    
    let parse_user_bio_from_textual_user_div
        node_with_textual_user_info
        =
        node_with_textual_user_info
        |>Html_node.direct_children
        |>Seq.tryItem 1 //skip the first div which is the name and handle. take the second div which is the briefing 
        |>function
        |Some bio_node->
            Html_parsing.readable_text_from_html_segments bio_node
        |None->""
           
    let node_with_textual_user_info
        user_cell
        =
        user_cell
        |>Html_node.descend 1
        |>Html_node.direct_children
        |>Seq.item 1 //the second div is the text of the user-info. skip the first div which is the image
    
    let handle_from_textual_user_info_node
        textual_user_info_node
        =
        textual_user_info_node
        |>Html_node.descendants "div>div>div>div>a[role='link']"
        |>Seq.head
        |>Html_node.attribute_value "href"
        |>fun user->user[1..]
        |>User_handle
    
    let peak_handle_from_user_cell
        user_cell
        =
        user_cell
        |>node_with_textual_user_info
        |>handle_from_textual_user_info_node
    
(*works with both catalogues: "Lists" and "Followers/Following", e.g.:
    https://twitter.com/i/lists/1692215865779892304/members
    https://twitter.com/productmg/followers
    *)    
    let parse_twitter_user_cell user_cell =
        let name =
            user_cell
            |>name_node
            |>Html_parsing.readable_text_from_html_segments
        
        let node_with_textual_user_info =
            node_with_textual_user_info user_cell
            
        let handle =
            handle_from_textual_user_info_node
                node_with_textual_user_info
            
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
            |>Html_node.from_text
            |>Html_parsing.readable_text_from_html_segments
        ()
        
    [<Fact>]
    let ``parse user cell from a twitter list``()=
        let user = 
            """<div role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1ny4l3l r-ymttw5 r-1f1sjgu r-o7ynqc r-6416eg" data-testid="UserCell"><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-onrtq4 r-18kxxzh r-1h0z5md r-1b7u577"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1adg3ll r-bztko3 r-13qz1uu" data-testid="UserAvatar-Container-madiyar_123" style="height: 40px;"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-1adg3ll r-1pi2tsx r-1wyvozj r-bztko3 r-u8s1d r-1v2oles r-desppf r-13qz1uu"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-ggadg3 r-1udh08x r-u8s1d r-8jfcpp" style="height: calc(100% - -4px); width: calc(100% - -4px);"><a href="/madiyar_123" aria-hidden="true" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1niwhzg r-1loqt21 r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1niwhzg r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-14lw9ot r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-14lw9ot r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1adg3ll r-1udh08x" style=""><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div aria-label="" class="css-1dbjc4n r-1p0dtai r-1mlwlqe r-1d2f490 r-1udh08x r-u8s1d r-zchlnj r-ipm5af r-417010"><div class="css-1dbjc4n r-1niwhzg r-vvn4in r-u6sd8q r-4gszlv r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw" style="background-image: url(&quot;https://pbs.twimg.com/profile_images/1692631554017660928/oUvMuzbZ_bigger.jpg&quot;);"></div><img alt="" draggable="true" src="https://pbs.twimg.com/profile_images/1692631554017660928/oUvMuzbZ_bigger.jpg" class="css-9pa8cd"></div></div></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-12181gd r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"></div></div></a></div></div></div></div></div></div></div><div class="css-1dbjc4n r-1iusvr4 r-16y2uox"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wtj0ep"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/madiyar_123" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-dnmrzs"><div dir="ltr" class="css-901oao r-1awozwy r-18jsvk2 r-6koalj r-37j5jr r-a023e6 r-b88u0q r-rjixqe r-bcqeeo r-1udh08x r-3s2u2q r-qvutc0"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Madiyar Nurakhmetov</span><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-1pos5eu r-qvutc0"></span></span></div><div dir="ltr" class="css-901oao r-18jsvk2 r-xoduu5 r-18u37iz r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-1awozwy r-xoduu5 r-poiln3 r-bcqeeo r-qvutc0"></span></div></div></a></div><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/madiyar_123" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n"><div dir="ltr" class="css-901oao css-1hf3ou5 r-14j79pv r-18u37iz r-37j5jr r-1wvb978 r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">@madiyar_123</span></div></div></a></div></div></div></div><div class="css-1dbjc4n r-19u6a5r r-bcqeeo"><div aria-describedby="id__2lqy30vu8q" aria-label="Follow @madiyar_123" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-sdzlij r-1phboty r-rs99b7 r-15ysp7h r-4wgw6l r-1ny4l3l r-ymttw5 r-o7ynqc r-6416eg r-lrvibr" data-testid="1692631214761324544-follow" style="background-color: rgb(15, 20, 25); border-color: rgba(0, 0, 0, 0);"><div dir="ltr" class="css-901oao r-1awozwy r-jwli3a r-6koalj r-18u37iz r-16y2uox r-37j5jr r-a023e6 r-b88u0q r-1777fci r-rjixqe r-bcqeeo r-q4m81j r-qvutc0"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-1b43r93 r-1cwl3u0 r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Follow</span></span></div></div></div><div dir="auto" class="css-901oao r-hvic4v" id="id__2lqy30vu8q">Click to Follow madiyar_123</div></div><div dir="auto" class="css-901oao r-18jsvk2 r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-1h8ys4a r-1jeg54m r-qvutc0"><img alt="🌍" draggable="false" src="https://abs-0.twimg.com/emoji/v2/svg/1f30d.svg" title="Earth globe europe-africa" class="r-4qtqp9 r-dflpy8 r-sjv1od r-zw8f10 r-10akycc r-h9hxbl"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0"> Rational optimist
</span><img alt="👍" draggable="false" src="https://abs-0.twimg.com/emoji/v2/svg/1f44d.svg" title="Thumbs up" class="r-4qtqp9 r-dflpy8 r-sjv1od r-zw8f10 r-10akycc r-h9hxbl"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0"> Good policymaking, long life, and walkable cities!
</span><img alt="🧬" draggable="false" src="https://abs-0.twimg.com/emoji/v2/svg/1f9ec.svg" title="DNA" class="r-4qtqp9 r-dflpy8 r-sjv1od r-zw8f10 r-10akycc r-h9hxbl"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Big believer in longevity
</span><img alt="🏛️" draggable="false" src="https://abs-0.twimg.com/emoji/v2/svg/1f3db.svg" title="Classical building" class="r-4qtqp9 r-dflpy8 r-sjv1od r-zw8f10 r-10akycc r-h9hxbl"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0"> MPA'24 at </span><div class="css-1dbjc4n r-xoduu5"><span class="r-18u37iz"><a dir="ltr" href="/columbia" role="link" class="css-4rbku5 css-18t94o4 css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0">@columbia</a></span></div><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">
</span><img alt="🇰🇿" draggable="false" src="https://abs-0.twimg.com/emoji/v2/svg/1f1f0-1f1ff.svg" title="Flag of Kazakhstan" class="r-4qtqp9 r-dflpy8 r-sjv1od r-zw8f10 r-10akycc r-h9hxbl"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0"> From Kazakhstan with love</span></div></div></div></div>"""
            |>Html_node.from_text
            |>parse_twitter_user_cell
        ()
        
    [<Fact>]
    let ``parse user cell from followers catalog``()=
        let user = 
            """<div role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1ny4l3l r-ymttw5 r-1f1sjgu r-o7ynqc r-6416eg" data-testid="UserCell"><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-onrtq4 r-18kxxzh r-1h0z5md r-1b7u577"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1adg3ll r-bztko3 r-13qz1uu" data-testid="UserAvatar-Container-TroyHeibein" style="height: 40px;"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-1adg3ll r-1pi2tsx r-1wyvozj r-bztko3 r-u8s1d r-1v2oles r-desppf r-13qz1uu"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-ggadg3 r-1udh08x r-u8s1d r-8jfcpp" style="height: calc(100% - -4px); width: calc(100% - -4px);"><a href="/TroyHeibein" aria-hidden="true" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1niwhzg r-1loqt21 r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1niwhzg r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-14lw9ot r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-14lw9ot r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1adg3ll r-1udh08x" style=""><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div aria-label="" class="css-1dbjc4n r-1p0dtai r-1mlwlqe r-1d2f490 r-1udh08x r-u8s1d r-zchlnj r-ipm5af r-417010"><div class="css-1dbjc4n r-1niwhzg r-vvn4in r-u6sd8q r-4gszlv r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw" style="background-image: url(&quot;https://pbs.twimg.com/profile_images/1683186445203259392/RkSypShv_bigger.jpg&quot;);"></div><img alt="" draggable="true" src="https://pbs.twimg.com/profile_images/1683186445203259392/RkSypShv_bigger.jpg" class="css-9pa8cd"></div></div></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-12181gd r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"></div></div></a></div></div></div></div></div></div></div><div class="css-1dbjc4n r-1iusvr4 r-16y2uox"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wtj0ep"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/TroyHeibein" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-dnmrzs"><div dir="ltr" class="css-901oao r-1awozwy r-18jsvk2 r-6koalj r-37j5jr r-a023e6 r-b88u0q r-rjixqe r-bcqeeo r-1udh08x r-3s2u2q r-qvutc0"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Troy Heibein</span><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-1pos5eu r-qvutc0"></span></span></div><div dir="ltr" class="css-901oao r-18jsvk2 r-xoduu5 r-18u37iz r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-1awozwy r-xoduu5 r-poiln3 r-bcqeeo r-qvutc0"><svg viewBox="0 0 22 22" aria-label="Verified account" role="img" class="r-1cvl2hr r-4qtqp9 r-yyyyoo r-1xvli5t r-9cviqr r-f9ja8p r-og9te1 r-bnwqim r-1plcrui r-lrvibr" data-testid="icon-verified"><g><path d="M20.396 11c-.018-.646-.215-1.275-.57-1.816-.354-.54-.852-.972-1.438-1.246.223-.607.27-1.264.14-1.897-.131-.634-.437-1.218-.882-1.687-.47-.445-1.053-.75-1.687-.882-.633-.13-1.29-.083-1.897.14-.273-.587-.704-1.086-1.245-1.44S11.647 1.62 11 1.604c-.646.017-1.273.213-1.813.568s-.969.854-1.24 1.44c-.608-.223-1.267-.272-1.902-.14-.635.13-1.22.436-1.69.882-.445.47-.749 1.055-.878 1.688-.13.633-.08 1.29.144 1.896-.587.274-1.087.705-1.443 1.245-.356.54-.555 1.17-.574 1.817.02.647.218 1.276.574 1.817.356.54.856.972 1.443 1.245-.224.606-.274 1.263-.144 1.896.13.634.433 1.218.877 1.688.47.443 1.054.747 1.687.878.633.132 1.29.084 1.897-.136.274.586.705 1.084 1.246 1.439.54.354 1.17.551 1.816.569.647-.016 1.276-.213 1.817-.567s.972-.854 1.245-1.44c.604.239 1.266.296 1.903.164.636-.132 1.22-.447 1.68-.907.46-.46.776-1.044.908-1.681s.075-1.299-.165-1.903c.586-.274 1.084-.705 1.439-1.246.354-.54.551-1.17.569-1.816zM9.662 14.85l-3.429-3.428 1.293-1.302 2.072 2.072 4.4-4.794 1.347 1.246z"></path></g></svg></span></div></div></a></div><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/TroyHeibein" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n"><div dir="ltr" class="css-901oao css-1hf3ou5 r-14j79pv r-18u37iz r-37j5jr r-1wvb978 r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">@TroyHeibein</span></div></div></a></div></div></div></div><div class="css-1dbjc4n r-19u6a5r r-bcqeeo"><div aria-describedby="id__napebnkvga" aria-label="Follow @TroyHeibein" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-sdzlij r-1phboty r-rs99b7 r-15ysp7h r-4wgw6l r-1ny4l3l r-ymttw5 r-o7ynqc r-6416eg r-lrvibr" data-testid="1683184588317683712-follow" style="background-color: rgb(15, 20, 25); border-color: rgba(0, 0, 0, 0);"><div dir="ltr" class="css-901oao r-1awozwy r-jwli3a r-6koalj r-18u37iz r-16y2uox r-37j5jr r-a023e6 r-b88u0q r-1777fci r-rjixqe r-bcqeeo r-q4m81j r-qvutc0"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-1b43r93 r-1cwl3u0 r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Follow</span></span></div></div></div><div dir="auto" class="css-901oao r-hvic4v" id="id__napebnkvga">Click to Follow TroyHeibein</div></div><div dir="auto" class="css-901oao r-18jsvk2 r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-1h8ys4a r-1jeg54m r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">CRO/COO. Operational &amp; Revenue Excellence. Family Man. Golden Retriever Lover. Libertarian. Canada Born. 2x China Resident. Now Florida, USA |</span><img alt="💙" draggable="false" src="https://abs-0.twimg.com/emoji/v2/svg/1f499.svg" title="Blue heart" class="r-4qtqp9 r-dflpy8 r-sjv1od r-zw8f10 r-10akycc r-h9hxbl"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">: </span><img alt="🎾" draggable="false" src="https://abs-0.twimg.com/emoji/v2/svg/1f3be.svg" title="Tennis ball" class="r-4qtqp9 r-dflpy8 r-sjv1od r-zw8f10 r-10akycc r-h9hxbl"><img alt="🧬" draggable="false" src="https://abs-0.twimg.com/emoji/v2/svg/1f9ec.svg" title="DNA" class="r-4qtqp9 r-dflpy8 r-sjv1od r-zw8f10 r-10akycc r-h9hxbl"><img alt="🛰️" draggable="false" src="https://abs-0.twimg.com/emoji/v2/svg/1f6f0.svg" title="Satellite" class="r-4qtqp9 r-dflpy8 r-sjv1od r-zw8f10 r-10akycc r-h9hxbl"><img alt="📚" draggable="false" src="https://abs-0.twimg.com/emoji/v2/svg/1f4da.svg" title="Books" class="r-4qtqp9 r-dflpy8 r-sjv1od r-zw8f10 r-10akycc r-h9hxbl"><img alt="🏖️" draggable="false" src="https://abs-0.twimg.com/emoji/v2/svg/1f3d6.svg" title="Beach with umbrella" class="r-4qtqp9 r-dflpy8 r-sjv1od r-zw8f10 r-10akycc r-h9hxbl"></div></div></div></div>"""
            |>Html_node.from_text
            |>parse_twitter_user_cell
        ()