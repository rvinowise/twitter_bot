namespace rvinowise.twitter

open AngleSharp
open OpenQA.Selenium
open rvinowise.html_parsing
open rvinowise.web_scraping

open FsUnit
open Xunit


module Read_list_updates =
    
    
    let cell_id_from_post_id
        cell_node
        =
        cell_node
        |>Parse_timeline_cell.try_article_node_from_cell_node
        |>Option.map Parse_header.peak_post_id

    
    let cell_id_from_html
        cell_node
        =
        cell_node
        |>Html_node.remove_parts_not_related_to_text
        |>Html_node.to_string
        |>Some
    
    let cell_id_from_list_user
        user_node
        =
        user_node
        |>Parse_twitter_user.peak_handle_from_user_cell
        |>Some
    
    [<Fact>]
    let ``try cell_identity``() =
        """<div data-testid="cellInnerDiv" style="transform: translateY(0px); position: absolute; width: 100%;"><div class="css-175oi2r r-j5o65s r-qklmqi r-1adg3ll r-1ny4l3l"><div class="css-175oi2r"><article aria-labelledby="id__j4l8mtlx3ps id__w7ut21nf4aa id__p1wslli56nl id__os4pdgsbf3h id__17okfocx5dg id__ogjgzks8ahb id__ovxjpcbdld id__0salw4lfgraa id__lrsmbbert3h id__31g19umopxh id__izfwn1paby id__8mf09u7a17 id__8ud4bndzz3x id__7m4uoj27sv2 id__omczup3dxq id__bq1ggujocg id__daub98z6ra7 id__7mnp0ginala" role="article" tabindex="0" class="css-175oi2r r-18u37iz r-1udh08x r-i023vh r-1qhn6m8 r-o7ynqc r-6416eg r-1ny4l3l r-1loqt21" data-testid="tweet"><div class="css-175oi2r r-eqz5dr r-16y2uox r-1wbh5a2"><div class="css-175oi2r r-16y2uox r-1wbh5a2 r-1ny4l3l"><div class="css-175oi2r"><div class="css-175oi2r r-18u37iz"><div class="css-175oi2r r-1iusvr4 r-16y2uox r-ttdzmv"></div></div></div><div class="css-175oi2r r-18u37iz"><div class="css-175oi2r r-18kxxzh r-1b7u577 r-onrtq4 r-1awozwy"><div class="css-175oi2r" data-testid="Tweet-User-Avatar"><div class="css-175oi2r r-18kxxzh r-1wbh5a2 r-13qz1uu"><div class="css-175oi2r r-bztko3 r-1adg3ll" data-testid="UserAvatar-Container-tehprom269887" style="width: 40px; height: 40px;"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-u8s1d r-1d2f490 r-ipm5af r-13qz1uu"><div class="css-175oi2r r-1adg3ll r-1pi2tsx r-13qz1uu r-u8s1d r-1wyvozj r-1v2oles r-desppf r-bztko3"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-u8s1d r-1d2f490 r-ipm5af r-13qz1uu"><div class="css-175oi2r r-sdzlij r-1udh08x r-u8s1d r-ggadg3 r-8jfcpp" style="width: calc(100% + 4px); height: calc(100% + 4px);"><a href="/tehprom269887" aria-hidden="true" role="link" tabindex="-1" class="css-175oi2r r-1pi2tsx r-13qz1uu r-o7ynqc r-6416eg r-1ny4l3l r-1loqt21" style="background-color: rgba(0, 0, 0, 0);"><div class="css-175oi2r r-sdzlij r-1udh08x r-633pao r-u8s1d r-1wyvozj r-1v2oles r-desppf" style="width: calc(100% - 4px); height: calc(100% - 4px);"><div class="css-175oi2r r-1pi2tsx r-13qz1uu" style="background-color: rgba(0, 0, 0, 0);"></div></div><div class="css-175oi2r r-sdzlij r-1udh08x r-633pao r-u8s1d r-1wyvozj r-1v2oles r-desppf" style="width: calc(100% - 4px); height: calc(100% - 4px);"><div class="css-175oi2r r-1pi2tsx r-13qz1uu r-14lw9ot"></div></div><div class="css-175oi2r r-sdzlij r-1udh08x r-633pao r-u8s1d r-1wyvozj r-1v2oles r-desppf" style="background-color: rgb(255, 255, 255); width: calc(100% - 4px); height: calc(100% - 4px);"><div class="css-175oi2r r-1adg3ll r-1udh08x" style=""><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-u8s1d r-1d2f490 r-ipm5af r-13qz1uu"><div aria-label="" class="css-175oi2r r-1mlwlqe r-1udh08x r-417010" style="position: absolute; inset: 0px;"><div class="css-175oi2r r-1niwhzg r-vvn4in r-u6sd8q r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw r-4gszlv" style="background-image: url(&quot;https://abs.twimg.com/sticky/default_profile_images/default_profile_bigger.png&quot;);"></div><img alt="" draggable="true" src="https://abs.twimg.com/sticky/default_profile_images/default_profile_bigger.png" class="css-9pa8cd"></div></div></div></div><div class="css-175oi2r r-sdzlij r-1udh08x r-u8s1d r-1wyvozj r-1v2oles r-desppf" style="width: calc(100% - 4px); height: calc(100% - 4px);"><div class="css-175oi2r r-12181gd r-1pi2tsx r-13qz1uu r-o7ynqc r-6416eg r-1ny4l3l"></div></div></a></div></div></div></div></div></div></div></div><div class="css-175oi2r r-1iusvr4 r-16y2uox r-1777fci r-kzbkwu"><div class="css-175oi2r r-zl2h9q"><div class="css-175oi2r r-k4xj1c r-18u37iz r-1wtj0ep"><div class="css-175oi2r r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-175oi2r r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-175oi2r r-1wbh5a2 r-dnmrzs r-1ny4l3l r-1awozwy r-18u37iz" id="id__17okfocx5dg" data-testid="User-Name"><div class="css-175oi2r r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div class="css-175oi2r r-1wbh5a2 r-dnmrzs"><a href="/tehprom269887" role="link" class="css-175oi2r r-1wbh5a2 r-dnmrzs r-1ny4l3l r-1loqt21"><div class="css-175oi2r r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div dir="ltr" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-b88u0q r-1awozwy r-6koalj r-1udh08x r-3s2u2q" style="color: rgb(15, 20, 25); text-overflow: unset;"><span class="css-1qaijid r-dnmrzs r-1udh08x r-3s2u2q r-bcqeeo r-qvutc0 r-poiln3" style="text-overflow: unset;"><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3" style="text-overflow: unset;">tehprom</span></span></div><div dir="ltr" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-xoduu5 r-18u37iz r-1q142lx" style="color: rgb(15, 20, 25); text-overflow: unset;"><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3 r-1awozwy r-xoduu5" style="text-overflow: unset;"></span></div></div></a></div></div><div class="css-175oi2r r-18u37iz r-1wbh5a2 r-13hce6t"><div class="css-175oi2r r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-175oi2r r-1wbh5a2 r-dnmrzs"><a href="/tehprom269887" role="link" tabindex="-1" class="css-175oi2r r-1wbh5a2 r-dnmrzs r-1ny4l3l r-1loqt21"><div dir="ltr" class="css-1rynq56 r-dnmrzs r-1udh08x r-3s2u2q r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-18u37iz r-1wvb978" style="color: rgb(83, 100, 113); text-overflow: unset;"><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3" style="text-overflow: unset;">@tehprom269887</span></div></a></div><div dir="ltr" aria-hidden="true" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-1q142lx r-s1qlax" style="color: rgb(83, 100, 113); text-overflow: unset;"><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3" style="text-overflow: unset;">·</span></div><div class="css-175oi2r r-18u37iz r-1q142lx"><a href="/tehprom269887/status/1735935432297160849" dir="ltr" aria-label="Dec 16" role="link" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-xoduu5 r-1q142lx r-1w6e6rj r-9aw3ui r-3s2u2q r-1loqt21" style="color: rgb(83, 100, 113); text-overflow: unset;"><time datetime="2023-12-16T08:10:33.000Z">Dec 16</time></a></div></div></div></div></div></div><div class="css-175oi2r r-1jkjb"><div class="css-175oi2r r-1awozwy r-18u37iz r-1cmwbt1 r-1wtj0ep"><div class="css-175oi2r r-1awozwy r-6koalj r-18u37iz"><div class="css-175oi2r"><div class="css-175oi2r r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="More" role="button" tabindex="0" class="css-175oi2r r-1777fci r-bt1l66 r-bztko3 r-lrvibr r-1loqt21 r-1ny4l3l" data-testid="caret"><div dir="ltr" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-1awozwy r-6koalj r-1h0z5md r-o7ynqc r-clp7b1 r-3s2u2q" style="color: rgb(83, 100, 113); text-overflow: unset;"><div class="css-175oi2r r-xoduu5"><div class="css-175oi2r r-xoduu5 r-1p0dtai r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-1niwhzg r-sdzlij r-xf4iuw r-o7ynqc r-6416eg r-1ny4l3l"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1xvli5t r-1hdv0qi"><g><path d="M3 12c0-1.1.9-2 2-2s2 .9 2 2-.9 2-2 2-2-.9-2-2zm9 2c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm7 0c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div><div class="css-175oi2r"><div dir="auto" lang="en" class="css-1rynq56 r-8akbws r-krxsd3 r-dnmrzs r-1udh08x r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-bnwqim" id="id__izfwn1paby" data-testid="tweetText" style="-webkit-line-clamp: 10; color: rgb(15, 20, 25); text-overflow: unset;"><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3" style="text-overflow: unset;">website and broadcast
    </span><a dir="ltr" href="https://t.co/AJwyHBKdmA" rel="noopener noreferrer nofollow" target="_blank" role="link" class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3 r-1loqt21" style="color: rgb(29, 155, 240); text-overflow: unset;"><span aria-hidden="true" class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3 r-hiw28u r-qvk6io" style="text-overflow: unset;">https://</span>lichess.org</a></div></div><div aria-labelledby="id__m5kehp2wld8 id__r5wzjjmrnw" class="css-175oi2r r-1ssbvtb r-1s2bzr4" id="id__8ud4bndzz3x"><div aria-labelledby="id__daczuo7vjtj id__twvt0mp6gom" class="css-175oi2r r-1ets6dv r-1phboty r-rs99b7 r-1udh08x r-1867qdf r-o7ynqc r-6416eg r-1ny4l3l" id="id__r5wzjjmrnw" data-testid="card.wrapper"><div aria-hidden="true" class="css-175oi2r r-1adg3ll r-140t1nj r-rull8r r-qklmqi r-zmljjp r-pm2fo" id="id__daczuo7vjtj" data-testid="card.layoutLarge.media"><div class="css-175oi2r"><div class="css-175oi2r" style=""><div class="css-175oi2r r-1adg3ll r-1udh08x r-pm9dpa"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 56.25%;"></div><div class="r-1p0dtai r-1pi2tsx r-u8s1d r-1d2f490 r-ipm5af r-13qz1uu"><div aria-label="Embedded video" class="css-175oi2r r-1awozwy r-1p0dtai r-1777fci r-1d2f490 r-u8s1d r-zchlnj r-ipm5af" data-testid="previewInterstitial"><div class="css-175oi2r r-1p0dtai r-1d2f490 r-1udh08x r-u8s1d r-zchlnj r-ipm5af"><div class="css-175oi2r r-xigjrr r-1c5lwsr r-1p0dtai r-1d2f490 r-1udh08x r-u8s1d r-zchlnj r-ipm5af"><div aria-label="Embedded video" class="css-175oi2r r-1mlwlqe r-1udh08x r-417010" style="position: absolute; inset: 0px; margin: 0px;"><div class="css-175oi2r r-1niwhzg r-vvn4in r-u6sd8q r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw r-4gszlv" style="background-image: url(&quot;https://pbs.twimg.com/card_img/1738203903177703425/sH0U0zc2?format=webp&amp;name=tiny&quot;);"></div><img alt="Embedded video" draggable="true" src="https://pbs.twimg.com/card_img/1738203903177703425/sH0U0zc2?format=webp&amp;name=tiny" class="css-9pa8cd"></div></div></div></div></div></div></div><div role="button" tabindex="0" class="css-175oi2r r-1awozwy r-1p0dtai r-1777fci r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-1loqt21 r-o7ynqc r-6416eg r-1ny4l3l"><div role="button" tabindex="0" class="css-175oi2r r-sdzlij r-1phboty r-rs99b7 r-lrvibr r-15ysp7h r-4wgw6l r-ymttw5 r-1loqt21 r-o7ynqc r-6416eg r-1ny4l3l" style="border-color: rgba(0, 0, 0, 0); backdrop-filter: blur(4px); background-color: rgba(15, 20, 25, 0.75);"><div dir="ltr" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-q4m81j r-a023e6 r-rjixqe r-b88u0q r-1awozwy r-6koalj r-18u37iz r-16y2uox r-1777fci" style="color: rgb(255, 255, 255); text-overflow: unset;"><span class="css-1qaijid r-dnmrzs r-1udh08x r-3s2u2q r-bcqeeo r-qvutc0 r-poiln3 r-1b43r93 r-1cwl3u0" style="text-overflow: unset;"><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3" style="text-overflow: unset;">Load video</span></span></div></div></div></div></div><div class="css-175oi2r" id="id__twvt0mp6gom"><a href="/i/broadcasts/1OwGWwYEoyDGQ" role="link" class="css-175oi2r r-18u37iz r-16y2uox r-1wtj0ep r-o7ynqc r-6416eg r-1ny4l3l r-1loqt21"><div class="css-175oi2r r-16y2uox r-1wbh5a2 r-z5qs1h r-1777fci r-1e081e0 r-ttdzmv r-kzbkwu" data-testid="card.layoutLarge.detail"><div class="css-175oi2r r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-175oi2r r-1wbh5a2 r-dnmrzs r-1ny4l3l r-1awozwy r-18u37iz"><div class="css-175oi2r r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-175oi2r r-1awozwy r-18u37iz r-dnmrzs"><div class="css-175oi2r r-1adg3ll r-bztko3 r-1q142lx r-1d4mawv" data-testid="UserAvatar-Container-unknown" style="width: 20px; height: 20px;"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-u8s1d r-1d2f490 r-ipm5af r-13qz1uu"><div class="css-175oi2r r-1adg3ll r-1pi2tsx r-13qz1uu r-u8s1d r-1wyvozj r-1v2oles r-desppf r-bztko3"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-u8s1d r-1d2f490 r-ipm5af r-13qz1uu"><div class="css-175oi2r r-sdzlij r-1udh08x r-u8s1d r-ggadg3 r-8jfcpp" style="width: calc(100% + 4px); height: calc(100% + 4px);"><div aria-hidden="true" role="presentation" tabindex="-1" class="css-175oi2r r-1pi2tsx r-13qz1uu r-1ny4l3l" style="background-color: rgba(0, 0, 0, 0);"><div class="css-175oi2r r-sdzlij r-1udh08x r-633pao r-u8s1d r-1wyvozj r-1v2oles r-desppf" style="width: calc(100% - 4px); height: calc(100% - 4px);"><div class="css-175oi2r r-1pi2tsx r-13qz1uu" style="background-color: rgba(0, 0, 0, 0);"></div></div><div class="css-175oi2r r-sdzlij r-1udh08x r-633pao r-u8s1d r-1wyvozj r-1v2oles r-desppf" style="width: calc(100% - 4px); height: calc(100% - 4px);"><div class="css-175oi2r r-1pi2tsx r-13qz1uu r-14lw9ot"></div></div><div class="css-175oi2r r-sdzlij r-1udh08x r-633pao r-u8s1d r-1wyvozj r-1v2oles r-desppf" style="background-color: rgb(255, 255, 255); width: calc(100% - 4px); height: calc(100% - 4px);"><div class="css-175oi2r r-1adg3ll r-1udh08x" style=""><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-u8s1d r-1d2f490 r-ipm5af r-13qz1uu"><div aria-label="" class="css-175oi2r r-1mlwlqe r-1udh08x r-417010" style="position: absolute; inset: 0px;"><div class="css-175oi2r r-1niwhzg r-vvn4in r-u6sd8q r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw r-4gszlv" style="background-image: url(&quot;https://pbs.twimg.com/profile_images/1602390330799685636/XtDFRE20_normal.jpg&quot;);"></div><img alt="" draggable="true" src="https://pbs.twimg.com/profile_images/1602390330799685636/XtDFRE20_normal.jpg" class="css-9pa8cd"></div></div></div></div><div class="css-175oi2r r-sdzlij r-1udh08x r-u8s1d r-1wyvozj r-1v2oles r-desppf" style="width: calc(100% - 4px); height: calc(100% - 4px);"><div class="css-175oi2r r-12181gd r-1pi2tsx r-13qz1uu r-1ny4l3l"></div></div></div></div></div></div></div></div><div dir="ltr" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-b88u0q r-1awozwy r-6koalj r-1udh08x r-3s2u2q" style="color: rgb(15, 20, 25); text-overflow: unset;"><span class="css-1qaijid r-dnmrzs r-1udh08x r-3s2u2q r-bcqeeo r-qvutc0 r-poiln3" style="text-overflow: unset;"><img alt="🇺🇦" draggable="false" src="https://abs-0.twimg.com/emoji/v2/svg/1f1fa-1f1e6.svg" title="Flag of Ukraine" class="r-4qtqp9 r-dflpy8 r-zw8f10 r-sjv1od r-10akycc r-h9hxbl"><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3" style="text-overflow: unset;">Evan Kirstel #B2B #TechFluencer</span><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3 r-1pos5eu" style="text-overflow: unset;"></span></span></div><div dir="ltr" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-xoduu5 r-18u37iz r-1q142lx" style="color: rgb(15, 20, 25); text-overflow: unset;"><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3 r-1awozwy r-xoduu5" style="text-overflow: unset;"><svg viewBox="0 0 22 22" aria-label="Verified account" role="img" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-bnwqim r-1plcrui r-lrvibr r-1cvl2hr r-f9ja8p r-og9te1 r-9cviqr" data-testid="icon-verified"><g><path d="M20.396 11c-.018-.646-.215-1.275-.57-1.816-.354-.54-.852-.972-1.438-1.246.223-.607.27-1.264.14-1.897-.131-.634-.437-1.218-.882-1.687-.47-.445-1.053-.75-1.687-.882-.633-.13-1.29-.083-1.897.14-.273-.587-.704-1.086-1.245-1.44S11.647 1.62 11 1.604c-.646.017-1.273.213-1.813.568s-.969.854-1.24 1.44c-.608-.223-1.267-.272-1.902-.14-.635.13-1.22.436-1.69.882-.445.47-.749 1.055-.878 1.688-.13.633-.08 1.29.144 1.896-.587.274-1.087.705-1.443 1.245-.356.54-.555 1.17-.574 1.817.02.647.218 1.276.574 1.817.356.54.856.972 1.443 1.245-.224.606-.274 1.263-.144 1.896.13.634.433 1.218.877 1.688.47.443 1.054.747 1.687.878.633.132 1.29.084 1.897-.136.274.586.705 1.084 1.246 1.439.54.354 1.17.551 1.816.569.647-.016 1.276-.213 1.817-.567s.972-.854 1.245-1.44c.604.239 1.266.296 1.903.164.636-.132 1.22-.447 1.68-.907.46-.46.776-1.044.908-1.681s.075-1.299-.165-1.903c.586-.274 1.084-.705 1.439-1.246.354-.54.551-1.17.569-1.816zM9.662 14.85l-3.429-3.428 1.293-1.302 2.072 2.072 4.4-4.794 1.347 1.246z"></path></g></svg></span></div></div></div><div class="css-175oi2r r-1awozwy r-18u37iz r-1wbh5a2 r-13hce6t"><div tabindex="-1" class="css-175oi2r r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-175oi2r"><div dir="ltr" class="css-1rynq56 r-dnmrzs r-1udh08x r-3s2u2q r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-18u37iz r-1wvb978" style="color: rgb(83, 100, 113); text-overflow: unset;"><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3" style="text-overflow: unset;">@EvanKirstel</span></div></div></div></div></div></div><div dir="auto" class="css-1rynq56 r-8akbws r-krxsd3 r-dnmrzs r-1udh08x r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41" style="-webkit-line-clamp: 2; color: rgb(15, 20, 25); text-overflow: unset;"><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3" style="text-overflow: unset;"><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3" style="text-overflow: unset;">Microsoft's Journey w/ #ChatGPT, Co-Pilots, and the Ethical Dimensions of AI: Future plans include embedding AI into their services advanced AI in Microsoft Cloud, Office 365, and Dynamics 365, delivering more personalized and streamlined experiences.</span></span></div></div></a></div></div></div><div class="css-175oi2r"><div class="css-175oi2r"><div aria-label="8 views" role="group" class="css-175oi2r r-1kbdv8c r-18u37iz r-1wtj0ep r-1ye8kvj r-1s2bzr4" id="id__f1ddrvqin4d"><div class="css-175oi2r r-18u37iz r-1h0z5md r-13awgt0"><div aria-label="0 Replies. Reply" role="button" tabindex="0" class="css-175oi2r r-1777fci r-bt1l66 r-bztko3 r-lrvibr r-1loqt21 r-1ny4l3l" data-testid="reply"><div dir="ltr" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-1awozwy r-6koalj r-1h0z5md r-o7ynqc r-clp7b1 r-3s2u2q" style="color: rgb(83, 100, 113); text-overflow: unset;"><div class="css-175oi2r r-xoduu5"><div class="css-175oi2r r-xoduu5 r-1p0dtai r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-1niwhzg r-sdzlij r-xf4iuw r-o7ynqc r-6416eg r-1ny4l3l"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1xvli5t r-1hdv0qi"><g><path d="M1.751 10c0-4.42 3.584-8 8.005-8h4.366c4.49 0 8.129 3.64 8.129 8.13 0 2.96-1.607 5.68-4.196 7.11l-8.054 4.46v-3.69h-.067c-4.49.1-8.183-3.51-8.183-8.01zm8.005-6c-3.317 0-6.005 2.69-6.005 6 0 3.37 2.77 6.08 6.138 6.01l.351-.01h1.761v2.3l5.087-2.81c1.951-1.08 3.163-3.13 3.163-5.36 0-3.39-2.744-6.13-6.129-6.13H9.756z"></path></g></svg></div><div class="css-175oi2r r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-1qaijid r-qvutc0 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-s1qlax" style="text-overflow: unset;"></span></span></div></div></div></div><div class="css-175oi2r r-18u37iz r-1h0z5md r-13awgt0"><div aria-expanded="false" aria-haspopup="menu" aria-label="0 reposts. Repost" role="button" tabindex="0" class="css-175oi2r r-1777fci r-bt1l66 r-bztko3 r-lrvibr r-1loqt21 r-1ny4l3l" data-testid="retweet"><div dir="ltr" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-1awozwy r-6koalj r-1h0z5md r-o7ynqc r-clp7b1 r-3s2u2q" style="color: rgb(83, 100, 113); text-overflow: unset;"><div class="css-175oi2r r-xoduu5"><div class="css-175oi2r r-xoduu5 r-1p0dtai r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-1niwhzg r-sdzlij r-xf4iuw r-o7ynqc r-6416eg r-1ny4l3l"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1xvli5t r-1hdv0qi"><g><path d="M4.5 3.88l4.432 4.14-1.364 1.46L5.5 7.55V16c0 1.1.896 2 2 2H13v2H7.5c-2.209 0-4-1.79-4-4V7.55L1.432 9.48.068 8.02 4.5 3.88zM16.5 6H11V4h5.5c2.209 0 4 1.79 4 4v8.45l2.068-1.93 1.364 1.46-4.432 4.14-4.432-4.14 1.364-1.46 2.068 1.93V8c0-1.1-.896-2-2-2z"></path></g></svg></div><div class="css-175oi2r r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-1qaijid r-qvutc0 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-s1qlax" style="text-overflow: unset;"></span></span></div></div></div></div><div class="css-175oi2r r-18u37iz r-1h0z5md r-13awgt0"><div aria-label="0 Likes. Like" role="button" tabindex="0" class="css-175oi2r r-1777fci r-bt1l66 r-bztko3 r-lrvibr r-1loqt21 r-1ny4l3l" data-testid="like"><div dir="ltr" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-1awozwy r-6koalj r-1h0z5md r-o7ynqc r-clp7b1 r-3s2u2q" style="color: rgb(83, 100, 113); text-overflow: unset;"><div class="css-175oi2r r-xoduu5"><div class="css-175oi2r r-xoduu5 r-1p0dtai r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-1niwhzg r-sdzlij r-xf4iuw r-o7ynqc r-6416eg r-1ny4l3l"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1xvli5t r-1hdv0qi"><g><path d="M16.697 5.5c-1.222-.06-2.679.51-3.89 2.16l-.805 1.09-.806-1.09C9.984 6.01 8.526 5.44 7.304 5.5c-1.243.07-2.349.78-2.91 1.91-.552 1.12-.633 2.78.479 4.82 1.074 1.97 3.257 4.27 7.129 6.61 3.87-2.34 6.052-4.64 7.126-6.61 1.111-2.04 1.03-3.7.477-4.82-.561-1.13-1.666-1.84-2.908-1.91zm4.187 7.69c-1.351 2.48-4.001 5.12-8.379 7.67l-.503.3-.504-.3c-4.379-2.55-7.029-5.19-8.382-7.67-1.36-2.5-1.41-4.86-.514-6.67.887-1.79 2.647-2.91 4.601-3.01 1.651-.09 3.368.56 4.798 2.01 1.429-1.45 3.146-2.1 4.796-2.01 1.954.1 3.714 1.22 4.601 3.01.896 1.81.846 4.17-.514 6.67z"></path></g></svg></div><div class="css-175oi2r r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-1qaijid r-qvutc0 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-s1qlax" style="text-overflow: unset;"></span></span></div></div></div></div><div class="css-175oi2r r-18u37iz r-1h0z5md r-13awgt0"><a href="/tehprom269887/status/1735935432297160849/analytics" aria-label="8 views. View post analytics" role="link" class="css-175oi2r r-1777fci r-bt1l66 r-bztko3 r-lrvibr r-1ny4l3l r-1loqt21"><div dir="ltr" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-1awozwy r-6koalj r-1h0z5md r-o7ynqc r-clp7b1 r-3s2u2q" style="color: rgb(83, 100, 113); text-overflow: unset;"><div class="css-175oi2r r-xoduu5"><div class="css-175oi2r r-xoduu5 r-1p0dtai r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-1niwhzg r-sdzlij r-xf4iuw r-o7ynqc r-6416eg r-1ny4l3l"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1xvli5t r-1hdv0qi"><g><path d="M8.75 21V3h2v18h-2zM18 21V8.5h2V21h-2zM4 21l.004-10h2L6 21H4zm9.248 0v-7h2v7h-2z"></path></g></svg></div><div class="css-175oi2r r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-1qaijid r-qvutc0 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-s1qlax" style="text-overflow: unset;"><span class="css-1qaijid r-bcqeeo r-qvutc0 r-poiln3" style="text-overflow: unset;">8</span></span></span></div></div></a></div><div class="css-175oi2r r-18u37iz r-1h0z5md r-1kb76zh"><div aria-label="Bookmark" role="button" tabindex="0" class="css-175oi2r r-1777fci r-bt1l66 r-bztko3 r-lrvibr r-1loqt21 r-1ny4l3l" data-testid="bookmark"><div dir="ltr" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-1awozwy r-6koalj r-1h0z5md r-o7ynqc r-clp7b1 r-3s2u2q" style="color: rgb(83, 100, 113); text-overflow: unset;"><div class="css-175oi2r r-xoduu5"><div class="css-175oi2r r-xoduu5 r-1p0dtai r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-1niwhzg r-sdzlij r-xf4iuw r-o7ynqc r-6416eg r-1ny4l3l"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1xvli5t r-1hdv0qi"><g><path d="M4 4.5C4 3.12 5.119 2 6.5 2h11C18.881 2 20 3.12 20 4.5v18.44l-8-5.71-8 5.71V4.5zM6.5 4c-.276 0-.5.22-.5.5v14.56l6-4.29 6 4.29V4.5c0-.28-.224-.5-.5-.5h-11z"></path></g></svg></div></div></div></div><div class="css-175oi2r" style="transform: rotate(0deg) scale(1) translate3d(0px, 0px, 0px); justify-content: inherit; display: inline-grid;"><div class="css-175oi2r r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="Share post" role="button" tabindex="0" class="css-175oi2r r-1777fci r-bt1l66 r-bztko3 r-lrvibr r-1loqt21 r-1ny4l3l"><div dir="ltr" class="css-1rynq56 r-bcqeeo r-qvutc0 r-37j5jr r-a023e6 r-rjixqe r-16dba41 r-1awozwy r-6koalj r-1h0z5md r-o7ynqc r-clp7b1 r-3s2u2q" style="color: rgb(83, 100, 113); text-overflow: unset;"><div class="css-175oi2r r-xoduu5"><div class="css-175oi2r r-xoduu5 r-1p0dtai r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-1niwhzg r-sdzlij r-xf4iuw r-o7ynqc r-6416eg r-1ny4l3l"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1xvli5t r-1hdv0qi"><g><path d="M12 2.59l5.7 5.7-1.41 1.42L13 6.41V16h-2V6.41l-3.3 3.3-1.41-1.42L12 2.59zM21 15l-.02 3.51c0 1.38-1.12 2.49-2.5 2.49H5.5C4.11 21 3 19.88 3 18.5V15h2v3.5c0 .28.22.5.5.5h12.98c.28 0 .5-.22.5-.5L19 15h2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div></div></div></article></div></div></div>"""
        |>Html_node.from_text
        |>cell_id_from_post_id
        |>should equal (1735935432297160849L|>Post_id|>Some)


    
    let last_previous_item_with_id reversed_previous_items_ids =
        reversed_previous_items_ids
        |>List.tryPick (fun (item_id, index) ->
            match item_id with
            |Some item_id ->
                Some (Some item_id,index)
            |None -> None
        )
    
    (*
    empty cells may appear on the first page, and then disappear on the second page after scrolling
    *)
    let check_that_items_without_id_are_such
        cell_identity
        new_items_after_last_previous_item_with_id
        skipped_amount
        =
        let items_without_id =
            new_items_after_last_previous_item_with_id
            |>Seq.take skipped_amount
        items_without_id
        |>Seq.iter(fun node ->
            match
                node
                |>cell_identity
            with
            |Some cell_id ->
                $"this cell was without an ID in previous_items, but it has ID {cell_id} in new_items"
                |>Bad_post_exception
                |>raise
            |None -> ()
        )
    let new_items_from_visible_items
        cell_identity
        previous_items
        visible_items
        =
        let previous_items_ids =
            previous_items
            |>List.rev
            |>List.mapi (fun index cell_node ->
                cell_identity cell_node,
                index
            )
        let visible_items_ids =    
            visible_items
            |>List.map (fun cell_node ->
                cell_identity cell_node,
                cell_node
            )
        
        match
            last_previous_item_with_id previous_items_ids
        with
        |Some (last_known_id,skipped_amount) ->
            
            visible_items_ids
            |>List.tryFindIndex (fun (new_item_id,_) ->
                new_item_id = last_known_id
            )|>function
            |Some i_first_previously_known_item ->
                let new_items_after_last_previous_item_with_id =
                    visible_items
                    |>List.splitAt i_first_previously_known_item
                    |>snd
                    |>List.tail
                
                new_items_after_last_previous_item_with_id
                |>List.skip skipped_amount 
            |None ->
                "lists are not overlapping, so all visible cells will be considered new"
                |>Log.error|>ignore
                visible_items
        |None ->
            let visible_items_have_ids visible_items_ids =
                visible_items_ids
                |>List.exists (fun (item_id, _) -> Option.isSome item_id)
            
            if (not previous_items.IsEmpty) then
                if (visible_items_have_ids visible_items_ids) then
                    "previous items don't have any ids, and visible items have ids, so all visible cells will be considered new"
                    |>Log.info
                    visible_items
                else
                    "neither previous nor visible items have any ids, so no items are returned"
                    |>Log.info
                    []
            else
                visible_items
     
        
    let process_item_batch_providing_previous_items
        process_item
        thread_context
        items
        =
        let rec iteration_of_batch_processing
            thread_context
            items
            =
            match items with
            |next_item::rest_items ->
                let new_context =
                    process_item thread_context next_item
                match new_context with
                |None->None
                |Some thread_context ->
                    iteration_of_batch_processing
                        thread_context
                        rest_items
            |[]->Some thread_context
            
        
        iteration_of_batch_processing
            thread_context
            items    