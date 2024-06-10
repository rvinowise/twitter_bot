﻿namespace rvinowise.twitter.test

open System
open Xunit
open FsUnit
open rvinowise.html_parsing
open rvinowise.twitter



module general_posts =
    
    let parse_single_main_twitter_post article_html =
        Parse_article.parse_twitter_article
            Thread_context.Empty_context
            article_html
        
    
    let ``parse post with a url which is part of the message, not decorated as external source`` post =
        post
        |>Main_post.message
        |>should equal (
            Post_message.Full
                "Link to the article: https://richarddawkins.substack.com/p/on-being-accused-of-islamophobia…"
        )
        
        post
        |>Main_post.external_source
        |>should equal None
        
        
    [<Fact>]
    let ``locally parse post with a url which is part of the message, not decorated as external source``()=
        //https://twitter.com/tehprom269887/status/1708518870846202279
        let post =
            """<article aria-labelledby="id__kknzgm13whm id__i6elxcntvr id__h83moa5khft id__nzkscgjwfdl id__dpwy3ossup9 id__41omwtivu0k id__d6eza1a6oh7 id__8gag01yx4yv id__rvq796rzbp id__qc47mct1vq id__lmqwx5k3vb id__rhezi6pr9ig id__7r3ul73xqld id__glrs7rofufv id__n39c08t2b3m id__l1fsnnkob5p id__4qnijdju93 id__cwslie3k0mh" role="article" tabindex="0" class="css-1dbjc4n r-1loqt21 r-18u37iz r-1ny4l3l r-1udh08x r-1qhn6m8 r-i023vh r-o7ynqc r-6416eg" data-testid="tweet"><div class="css-1dbjc4n r-eqz5dr r-16y2uox r-1wbh5a2"><div class="css-1dbjc4n r-16y2uox r-1wbh5a2 r-1ny4l3l"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-1iusvr4 r-16y2uox r-ttdzmv"></div></div></div><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-1awozwy r-onrtq4 r-18kxxzh r-1b7u577"><div class="css-1dbjc4n" data-testid="Tweet-User-Avatar"><div class="css-1dbjc4n r-18kxxzh r-1wbh5a2 r-13qz1uu"><div class="css-1dbjc4n r-1adg3ll r-bztko3" data-testid="UserAvatar-Container-tehprom269887" style="height: 40px; width: 40px;"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-1adg3ll r-1pi2tsx r-1wyvozj r-bztko3 r-u8s1d r-1v2oles r-desppf r-13qz1uu"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-ggadg3 r-1udh08x r-u8s1d r-8jfcpp" style="height: calc(100% - -4px); width: calc(100% - -4px);"><a href="/tehprom269887" aria-hidden="true" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1niwhzg r-1loqt21 r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1niwhzg r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-14lw9ot r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-14lw9ot r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1adg3ll r-1udh08x"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div aria-label="" class="css-1dbjc4n r-1p0dtai r-1mlwlqe r-1d2f490 r-1udh08x r-u8s1d r-zchlnj r-ipm5af r-417010"><div class="css-1dbjc4n r-1niwhzg r-vvn4in r-u6sd8q r-4gszlv r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw" style="background-image: url(&quot;https://abs.twimg.com/sticky/default_profile_images/default_profile_bigger.png&quot;);"></div><img alt="" draggable="true" src="https://abs.twimg.com/sticky/default_profile_images/default_profile_bigger.png" class="css-9pa8cd"></div></div></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-12181gd r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"></div></div></a></div></div></div></div></div></div></div></div><div class="css-1dbjc4n r-1iusvr4 r-16y2uox r-1777fci r-kzbkwu"><div class="css-1dbjc4n r-zl2h9q"><div class="css-1dbjc4n r-k4xj1c r-18u37iz r-1wtj0ep"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs r-1ny4l3l" id="id__dpwy3ossup9" data-testid="User-Name"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/tehprom269887" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div dir="ltr" class="css-901oao r-1awozwy r-18jsvk2 r-6koalj r-37j5jr r-a023e6 r-b88u0q r-rjixqe r-bcqeeo r-1udh08x r-3s2u2q r-qvutc0"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">tehprom</span></span></div><div dir="ltr" class="css-901oao r-18jsvk2 r-xoduu5 r-18u37iz r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-1awozwy r-xoduu5 r-poiln3 r-bcqeeo r-qvutc0"></span></div></div></a></div></div><div class="css-1dbjc4n r-18u37iz r-1wbh5a2 r-13hce6t"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/tehprom269887" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div dir="ltr" class="css-901oao css-1hf3ou5 r-14j79pv r-18u37iz r-37j5jr r-1wvb978 r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">@tehprom269887</span></div></a></div><div dir="ltr" aria-hidden="true" class="css-901oao r-14j79pv r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-s1qlax r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">·</span></div><div class="css-1dbjc4n r-18u37iz r-1q142lx"><a href="/tehprom269887/status/1708518870846202279" dir="ltr" aria-label="10 seconds ago" role="link" class="css-4rbku5 css-18t94o4 css-901oao r-14j79pv r-1loqt21 r-xoduu5 r-1q142lx r-1w6e6rj r-37j5jr r-a023e6 r-16dba41 r-9aw3ui r-rjixqe r-bcqeeo r-3s2u2q r-qvutc0"><time datetime="2023-10-01T16:26:56.000Z">10s</time></a></div></div></div></div></div></div><div class="css-1dbjc4n r-1jkjb"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1cmwbt1 r-1wtj0ep"><div class="css-1dbjc4n r-1awozwy r-6koalj r-18u37iz"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="More" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="caret"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M3 12c0-1.1.9-2 2-2s2 .9 2 2-.9 2-2 2-2-.9-2-2zm9 2c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm7 0c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div><div class="css-1dbjc4n"><div dir="auto" lang="en" class="css-901oao r-18jsvk2 r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-bnwqim r-qvutc0" id="id__lmqwx5k3vb" data-testid="tweetText"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Link to the article: </span><a dir="ltr" href="https://t.co/pCDBaSDOU0" rel="noopener noreferrer nofollow" target="_blank" role="link" class="css-4rbku5 css-18t94o4 css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0"><span aria-hidden="true" class="css-901oao css-16my406 r-poiln3 r-hiw28u r-qvk6io r-bcqeeo r-qvutc0">https://</span>richarddawkins.substack.com/p/on-being-acc<span aria-hidden="true" class="css-901oao css-16my406 r-poiln3 r-hiw28u r-qvk6io r-bcqeeo r-qvutc0">used-of-islamophobia</span><span aria-hidden="true" class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-lrvibr r-qvutc0">…</span></a></div></div><div aria-labelledby="id__p6frcjdhidl id__8b56vfn2tnb" class="css-1dbjc4n r-1ssbvtb r-1s2bzr4" id="id__7r3ul73xqld"></div><div class="css-1dbjc4n"><div class="css-1dbjc4n"><div aria-label="" role="group" class="css-1dbjc4n r-1kbdv8c r-18u37iz r-1wtj0ep r-1s2bzr4 r-1ye8kvj" id="id__b7us5klg32e"><div class="css-1dbjc4n r-13awgt0 r-18u37iz r-1h0z5md"><div aria-label="0 Replies. Reply" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="reply"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M1.751 10c0-4.42 3.584-8 8.005-8h4.366c4.49 0 8.129 3.64 8.129 8.13 0 2.96-1.607 5.68-4.196 7.11l-8.054 4.46v-3.69h-.067c-4.49.1-8.183-3.51-8.183-8.01zm8.005-6c-3.317 0-6.005 2.69-6.005 6 0 3.37 2.77 6.08 6.138 6.01l.351-.01h1.761v2.3l5.087-2.81c1.951-1.08 3.163-3.13 3.163-5.36 0-3.39-2.744-6.13-6.129-6.13H9.756z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1pn2ns4 r-qvutc0"></span></span></div></div></div></div><div class="css-1dbjc4n r-13awgt0 r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="0 reposts. Repost" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="retweet"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M4.5 3.88l4.432 4.14-1.364 1.46L5.5 7.55V16c0 1.1.896 2 2 2H13v2H7.5c-2.209 0-4-1.79-4-4V7.55L1.432 9.48.068 8.02 4.5 3.88zM16.5 6H11V4h5.5c2.209 0 4 1.79 4 4v8.45l2.068-1.93 1.364 1.46-4.432 4.14-4.432-4.14 1.364-1.46 2.068 1.93V8c0-1.1-.896-2-2-2z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1pn2ns4 r-qvutc0"></span></span></div></div></div></div><div class="css-1dbjc4n r-13awgt0 r-18u37iz r-1h0z5md"><div aria-label="0 Likes. Like" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="like"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M16.697 5.5c-1.222-.06-2.679.51-3.89 2.16l-.805 1.09-.806-1.09C9.984 6.01 8.526 5.44 7.304 5.5c-1.243.07-2.349.78-2.91 1.91-.552 1.12-.633 2.78.479 4.82 1.074 1.97 3.257 4.27 7.129 6.61 3.87-2.34 6.052-4.64 7.126-6.61 1.111-2.04 1.03-3.7.477-4.82-.561-1.13-1.666-1.84-2.908-1.91zm4.187 7.69c-1.351 2.48-4.001 5.12-8.379 7.67l-.503.3-.504-.3c-4.379-2.55-7.029-5.19-8.382-7.67-1.36-2.5-1.41-4.86-.514-6.67.887-1.79 2.647-2.91 4.601-3.01 1.651-.09 3.368.56 4.798 2.01 1.429-1.45 3.146-2.1 4.796-2.01 1.954.1 3.714 1.22 4.601 3.01.896 1.81.846 4.17-.514 6.67z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1pn2ns4 r-qvutc0"></span></span></div></div></div></div><div class="css-1dbjc4n r-13awgt0 r-18u37iz r-1h0z5md"><a href="/tehprom269887/status/1708518870846202279/analytics" aria-label="View post analytics" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M8.75 21V3h2v18h-2zM18 21V8.5h2V21h-2zM4 21l.004-10h2L6 21H4zm9.248 0v-7h2v7h-2z"></path></g></svg></div></div></a></div><div class="css-1dbjc4n" style="display: inline-grid; justify-content: inherit; transform: rotate(0deg) scale(1) translate3d(0px, 0px, 0px); -webkit-box-pack: inherit;"><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="Share post" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M12 2.59l5.7 5.7-1.41 1.42L13 6.41V16h-2V6.41l-3.3 3.3-1.41-1.42L12 2.59zM21 15l-.02 3.51c0 1.38-1.12 2.49-2.5 2.49H5.5C4.11 21 3 19.88 3 18.5V15h2v3.5c0 .28.22.5.5.5h12.98c.28 0 .5-.22.5-.5L19 15h2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div></div></div></article>"""
            |>Html_node.from_text
            |>parse_single_main_twitter_post
        
        ``parse post with a url which is part of the message, not decorated as external source`` post
        
    
    [<Fact>]
    let ``parse post with a ShowMore button in the main post (without url)``()=
        //https://twitter.com/RichardDawkins/status/1694469132152975786
        let post =
            """<article aria-labelledby="id__8rsidtxcjlj id__cuy94the5n id__wyr5hgzdevj id__6m1nrwbrct id__cirufkv8aw id__ot2hhehv56 id__ri1ka6qbnzp id__b7bu2qz2yd id__arb9544c4nc id__35ac32djfi2 id__yvmwe7tfmr id__h7uv8mt3zcm id__6g0nf87zy6c id__7985ikswv7t id__7ilhiuy1iao id__0spkddip0u id__j1gvc9wdn1s id__oc3jr36hcr id__x2e7k7l8y2" role="article" tabindex="0" class="css-1dbjc4n r-1loqt21 r-18u37iz r-1ny4l3l r-1udh08x r-1qhn6m8 r-i023vh r-o7ynqc r-6416eg" data-testid="tweet"><div class="css-1dbjc4n r-eqz5dr r-16y2uox r-1wbh5a2"><div class="css-1dbjc4n r-16y2uox r-1wbh5a2 r-1ny4l3l"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-1iusvr4 r-16y2uox r-ttdzmv"><div class="css-1dbjc4n r-15zivkp r-q3we1"><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-obd0qt r-onrtq4 r-18kxxzh r-1777fci r-1b7u577"><svg viewBox="0 0 24 24" aria-hidden="true" class="r-14j79pv r-4qtqp9 r-yyyyoo r-10ptun7 r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1janqcz"><g><path d="M4.75 3.79l4.603 4.3-1.706 1.82L6 8.38v7.37c0 .97.784 1.75 1.75 1.75H13V20H7.75c-2.347 0-4.25-1.9-4.25-4.25V8.38L1.853 9.91.147 8.09l4.603-4.3zm11.5 2.71H11V4h5.25c2.347 0 4.25 1.9 4.25 4.25v7.37l1.647-1.53 1.706 1.82-4.603 4.3-4.603-4.3 1.706-1.82L18 15.62V8.25c0-.97-.784-1.75-1.75-1.75z"></path></g></svg></div><div class="css-1dbjc4n r-1iusvr4 r-16y2uox"><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-1habvwh r-1wbh5a2 r-1777fci"><div class="css-1dbjc4n"><a href="/tehprom269887" dir="ltr" role="link" class="css-4rbku5 css-18t94o4 css-901oao r-14j79pv r-1loqt21 r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0" id="id__6m1nrwbrct"><span class="css-901oao css-16my406 css-cens5h r-14j79pv r-poiln3 r-n6v787 r-b88u0q r-1cwl3u0 r-bcqeeo r-qvutc0" id="id__6m1nrwbrct" data-testid="socialContext" style="-webkit-line-clamp: 2;"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">You reposted</span></span></a></div></div></div></div></div></div></div></div></div><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-1awozwy r-onrtq4 r-18kxxzh r-1b7u577"><div class="css-1dbjc4n" data-testid="Tweet-User-Avatar"><div class="css-1dbjc4n r-18kxxzh r-1wbh5a2 r-13qz1uu"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1adg3ll r-bztko3" data-testid="UserAvatar-Container-RichardDawkins" style="height: 40px; width: 40px;"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-1adg3ll r-1pi2tsx r-1wyvozj r-bztko3 r-u8s1d r-1v2oles r-desppf r-13qz1uu"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-ggadg3 r-1udh08x r-u8s1d r-8jfcpp" style="height: calc(100% - -4px); width: calc(100% - -4px);"><a href="/RichardDawkins" aria-hidden="true" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1niwhzg r-1loqt21 r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1niwhzg r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-14lw9ot r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-14lw9ot r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1adg3ll r-1udh08x" style=""><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div aria-label="" class="css-1dbjc4n r-1p0dtai r-1mlwlqe r-1d2f490 r-1udh08x r-u8s1d r-zchlnj r-ipm5af r-417010"><div class="css-1dbjc4n r-1niwhzg r-vvn4in r-u6sd8q r-4gszlv r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw" style="background-image: url(&quot;https://pbs.twimg.com/profile_images/1094327574031945729/tU8wz8DL_bigger.jpg&quot;);"></div><img alt="" draggable="true" src="https://pbs.twimg.com/profile_images/1094327574031945729/tU8wz8DL_bigger.jpg" class="css-9pa8cd"></div></div></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-12181gd r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"></div></div></a></div></div></div></div></div></div></div></div></div><div class="css-1dbjc4n r-1iusvr4 r-16y2uox r-1777fci r-kzbkwu"><div class="css-1dbjc4n r-zl2h9q"><div class="css-1dbjc4n r-k4xj1c r-18u37iz r-1wtj0ep"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs r-1ny4l3l" id="id__cirufkv8aw" data-testid="User-Name"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/RichardDawkins" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div dir="ltr" class="css-901oao r-1awozwy r-18jsvk2 r-6koalj r-37j5jr r-a023e6 r-b88u0q r-rjixqe r-bcqeeo r-1udh08x r-3s2u2q r-qvutc0"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Richard Dawkins</span></span></div><div dir="ltr" class="css-901oao r-18jsvk2 r-xoduu5 r-18u37iz r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-1awozwy r-xoduu5 r-poiln3 r-bcqeeo r-qvutc0"><svg viewBox="0 0 22 22" aria-label="Verified account" role="img" class="r-1cvl2hr r-4qtqp9 r-yyyyoo r-1xvli5t r-9cviqr r-f9ja8p r-og9te1 r-bnwqim r-1plcrui r-lrvibr" data-testid="icon-verified"><g><path d="M20.396 11c-.018-.646-.215-1.275-.57-1.816-.354-.54-.852-.972-1.438-1.246.223-.607.27-1.264.14-1.897-.131-.634-.437-1.218-.882-1.687-.47-.445-1.053-.75-1.687-.882-.633-.13-1.29-.083-1.897.14-.273-.587-.704-1.086-1.245-1.44S11.647 1.62 11 1.604c-.646.017-1.273.213-1.813.568s-.969.854-1.24 1.44c-.608-.223-1.267-.272-1.902-.14-.635.13-1.22.436-1.69.882-.445.47-.749 1.055-.878 1.688-.13.633-.08 1.29.144 1.896-.587.274-1.087.705-1.443 1.245-.356.54-.555 1.17-.574 1.817.02.647.218 1.276.574 1.817.356.54.856.972 1.443 1.245-.224.606-.274 1.263-.144 1.896.13.634.433 1.218.877 1.688.47.443 1.054.747 1.687.878.633.132 1.29.084 1.897-.136.274.586.705 1.084 1.246 1.439.54.354 1.17.551 1.816.569.647-.016 1.276-.213 1.817-.567s.972-.854 1.245-1.44c.604.239 1.266.296 1.903.164.636-.132 1.22-.447 1.68-.907.46-.46.776-1.044.908-1.681s.075-1.299-.165-1.903c.586-.274 1.084-.705 1.439-1.246.354-.54.551-1.17.569-1.816zM9.662 14.85l-3.429-3.428 1.293-1.302 2.072 2.072 4.4-4.794 1.347 1.246z"></path></g></svg></span></div></div></a></div></div><div class="css-1dbjc4n r-18u37iz r-1wbh5a2 r-13hce6t"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/RichardDawkins" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div dir="ltr" class="css-901oao css-1hf3ou5 r-14j79pv r-18u37iz r-37j5jr r-1wvb978 r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">@RichardDawkins</span></div></a></div><div dir="ltr" aria-hidden="true" class="css-901oao r-14j79pv r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-s1qlax r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">·</span></div><div class="css-1dbjc4n r-18u37iz r-1q142lx"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><a href="/RichardDawkins/status/1694469132152975786" dir="ltr" aria-label="Aug 24" role="link" class="css-4rbku5 css-18t94o4 css-901oao r-14j79pv r-1loqt21 r-xoduu5 r-1q142lx r-1w6e6rj r-37j5jr r-a023e6 r-16dba41 r-9aw3ui r-rjixqe r-bcqeeo r-3s2u2q r-qvutc0"><time datetime="2023-08-23T21:58:17.000Z">Aug 24</time></a><div dir="ltr" aria-hidden="true" class="css-901oao r-14j79pv r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-s1qlax r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">·</span></div><div class="css-1dbjc4n"><div dir="ltr" class="css-901oao r-k200y r-14j79pv r-18u37iz r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0"><svg viewBox="0 0 24 24" aria-label="This is the latest version of this post." role="img" class="r-4qtqp9 r-yyyyoo r-1b43r93 r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr"><g><path d="M21.15 6.232c.97.977.97 2.559 0 3.536L9.91 21H3v-6.914L14.23 2.854c.98-.977 2.56-.977 3.54 0l3.38 3.378zM14.75 19l-2 2H21v-2h-6.25z"></path></g></svg></div></div></div></div></div></div></div></div></div><div class="css-1dbjc4n r-1jkjb"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1cmwbt1 r-1wtj0ep"><div class="css-1dbjc4n r-1awozwy r-6koalj r-18u37iz"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="More" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="caret"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M3 12c0-1.1.9-2 2-2s2 .9 2 2-.9 2-2 2-2-.9-2-2zm9 2c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm7 0c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div><div class="css-1dbjc4n"><div dir="auto" lang="en" class="css-901oao r-18jsvk2 r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-bnwqim r-qvutc0" id="id__yvmwe7tfmr" data-testid="tweetText"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Do animals feel pain more than we do? What is pain for? What is consciousness and whom might we consider a conscious being? My friend, one of my favourite moral philosophers, </span><div class="css-1dbjc4n r-xoduu5"><span class="r-18u37iz"><a dir="ltr" href="/PeterSinger" role="link" class="css-4rbku5 css-18t94o4 css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0">@PeterSinger</a></span></div><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0"> and I take a dive into the deep end. To get early access to episodes, join our membership.…</span><span tabindex="0" class="css-901oao css-16my406 r-1cvl2hr r-1loqt21 r-poiln3 r-bcqeeo r-qvutc0" data-testid="tweet-text-show-more-link" role="link"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">&nbsp;</span><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">Show more</span></span></div></div><div class="css-1dbjc4n"><div class="css-1dbjc4n"><div aria-label="102 replies, 99 reposts, Reposted, 894 likes, 85 bookmarks, 116524 views" role="group" class="css-1dbjc4n r-1kbdv8c r-18u37iz r-1wtj0ep r-1s2bzr4 r-1ye8kvj" id="id__x2e7k7l8y2"><div class="css-1dbjc4n r-13awgt0 r-18u37iz r-1h0z5md"><div aria-label="102 Replies. Reply" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="reply"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M1.751 10c0-4.42 3.584-8 8.005-8h4.366c4.49 0 8.129 3.64 8.129 8.13 0 2.96-1.607 5.68-4.196 7.11l-8.054 4.46v-3.69h-.067c-4.49.1-8.183-3.51-8.183-8.01zm8.005-6c-3.317 0-6.005 2.69-6.005 6 0 3.37 2.77 6.08 6.138 6.01l.351-.01h1.761v2.3l5.087-2.81c1.951-1.08 3.163-3.13 3.163-5.36 0-3.39-2.744-6.13-6.129-6.13H9.756z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1pn2ns4 r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">102</span></span></span></div></div></div></div><div class="css-1dbjc4n r-13awgt0 r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="99 reposts. Reposted" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="unretweet"><div dir="ltr" class="css-901oao r-1awozwy r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0" style="color: rgb(0, 186, 124);"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M4.75 3.79l4.603 4.3-1.706 1.82L6 8.38v7.37c0 .97.784 1.75 1.75 1.75H13V20H7.75c-2.347 0-4.25-1.9-4.25-4.25V8.38L1.853 9.91.147 8.09l4.603-4.3zm11.5 2.71H11V4h5.25c2.347 0 4.25 1.9 4.25 4.25v7.37l1.647-1.53 1.706 1.82-4.603 4.3-4.603-4.3 1.706-1.82L18 15.62V8.25c0-.97-.784-1.75-1.75-1.75z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1pn2ns4 r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">99</span></span></span></div></div></div></div><div class="css-1dbjc4n r-13awgt0 r-18u37iz r-1h0z5md"><div aria-label="894 Likes. Like" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="like"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M16.697 5.5c-1.222-.06-2.679.51-3.89 2.16l-.805 1.09-.806-1.09C9.984 6.01 8.526 5.44 7.304 5.5c-1.243.07-2.349.78-2.91 1.91-.552 1.12-.633 2.78.479 4.82 1.074 1.97 3.257 4.27 7.129 6.61 3.87-2.34 6.052-4.64 7.126-6.61 1.111-2.04 1.03-3.7.477-4.82-.561-1.13-1.666-1.84-2.908-1.91zm4.187 7.69c-1.351 2.48-4.001 5.12-8.379 7.67l-.503.3-.504-.3c-4.379-2.55-7.029-5.19-8.382-7.67-1.36-2.5-1.41-4.86-.514-6.67.887-1.79 2.647-2.91 4.601-3.01 1.651-.09 3.368.56 4.798 2.01 1.429-1.45 3.146-2.1 4.796-2.01 1.954.1 3.714 1.22 4.601 3.01.896 1.81.846 4.17-.514 6.67z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1pn2ns4 r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">894</span></span></span></div></div></div></div><div class="css-1dbjc4n r-13awgt0 r-18u37iz r-1h0z5md"><a href="/RichardDawkins/status/1694469132152975786/analytics" aria-label="116524 views. View post analytics" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M8.75 21V3h2v18h-2zM18 21V8.5h2V21h-2zM4 21l.004-10h2L6 21H4zm9.248 0v-7h2v7h-2z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1pn2ns4 r-qvutc0"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0">116.5K</span></span></span></div></div></a></div><div class="css-1dbjc4n" style="display: inline-grid; justify-content: inherit; transform: rotate(0deg) scale(1) translate3d(0px, 0px, 0px); -webkit-box-pack: inherit;"><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="Share post" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M12 2.59l5.7 5.7-1.41 1.42L13 6.41V16h-2V6.41l-3.3 3.3-1.41-1.42L12 2.59zM21 15l-.02 3.51c0 1.38-1.12 2.49-2.5 2.49H5.5C4.11 21 3 19.88 3 18.5V15h2v3.5c0 .28.22.5.5.5h12.98c.28 0 .5-.22.5-.5L19 15h2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div></div></div></article>"""
            |>Html_node.from_text
            |>parse_single_main_twitter_post
        
        post
        |>Main_post.message
        |>should equal (
            {
                Abbreviated_message.message="""Do animals feel pain more than we do? What is pain for? What is consciousness and whom might we consider a conscious being? My friend, one of my favourite moral philosophers, @PeterSinger and I take a dive into the deep end. To get early access to episodes, join our membership.…"""
                show_more_url = None
            }
            |>Post_message.Abbreviated
        )
        
        
    
        
    
        
    
            
            
    
    
        
        
    