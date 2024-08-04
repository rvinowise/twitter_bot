namespace rvinowise.html_parsing

open System
open System.Globalization
open AngleSharp.Dom
open AngleSharp.Html.Parser
open FSharp.Data
open AngleSharp
open OpenQA.Selenium

open Xunit
open FsUnit
open rvinowise.twitter


(*this module encapsulates an external library for parsing HTML *)

type Html_node = AngleSharp.Dom.IElement
type Comparable_html_hode = FSharp.Data.HtmlNode
type Html_string = Html_string of string
type Web_node = OpenQA.Selenium.IWebElement

module Html_node =
    
    
    
    
    let attribute_value attribute (node:Html_node) =
        match node.GetAttribute(attribute) with
        |null->raise (NullReferenceException $"node {node} doesn't have an attribute {attribute}") 
        |value -> value
    
    let remove_url_parameters (input:string) =
        let index = input.IndexOf("?");
        if (index >= 0) then
           input.Substring(0, index)
        else input
    
    let pure_url_from_attribute attribute (node:Html_node) =
        node
        |>attribute_value attribute
        |>remove_url_parameters
        
    let try_attribute_value attribute (node:Html_node) =
        match node.GetAttribute(attribute) with
        |null->None
        |value -> Some value
    
    let inner_text (node:Html_node) =
        node.TextContent

    let to_string (node:Html_node) =
        node.OuterHtml
    
    let to_html_string (node:Html_node) =
        node.OuterHtml
        |>Html_string
    
    let matches css (node:Html_node) =
        node.Matches css
    
    let descendants css (node:Html_node) =
        node.QuerySelectorAll css
        |>List.ofSeq
    
    let descendants_with_this css (node:Html_node) =
        let children =
            node
            |>descendants css
        if
            node|>matches css
        then
            node::children
        else
            children
    
    
    let direct_text (node:Html_node) =
        node.ChildNodes
        |>Seq.filter (fun child->child.NodeType = NodeType.Text)
        |>Seq.map (_.TextContent)
        |>String.concat ""      
    
    (* a button inside a button will be considered a sibling (not a child), because it's invalid HTML, but it exists on twitter *)
    let direct_children (node:Html_node) =
        List.ofSeq node.Children
        // node.QuerySelectorAll(":scope > *")
        // |>List.ofSeq
    
    let direct_children_with_css css (node:Html_node) =
        node.Children
        |>Seq.filter (fun child ->
            child.Matches css
        )|>List.ofSeq
    
    let direct_children_except css (node:Html_node) =
        node.Children
        |>Seq.filter (fun child ->
            child.Matches css |>not
        )|>List.ofSeq
        
    
    
    let should_be_single seq =
        if Seq.length seq = 1 then
            Seq.head seq
        else if Seq.length seq = 0 then
            "expected one element, but there's none"
            |>ArgumentException
            |>raise
        else
            $"expected one element, but there's {Seq.length seq}"
            |>ArgumentException
            |>raise
    
    let descendant css (node:Html_node) =
        node
        |>descendants css
        |>should_be_single
    
    
    
    
    let ancestors css (node:Html_node) =
        
        let rec step_up_hierarchy
            (node: Html_node)
            hierarchy
            =
            if isNull node then
                hierarchy
            else
                if node.Matches css then
                    step_up_hierarchy
                        node.ParentElement
                        (node::hierarchy)
                else
                    step_up_hierarchy
                        node.ParentElement
                        hierarchy
        
        step_up_hierarchy
            node.ParentElement
            []
        |>List.rev
        
    
    let first_ancestor_with_css css (node:Html_node) =
        node
        |>ancestors css
        |>List.head
    
    let try_first_ancestor_with_css css (node:Html_node) =
        node
        |>ancestors css
        |>List.tryHead
        
    let parent (node:Html_node) =
        let parent = node.ParentElement
        if isNull parent then
            raise (NullReferenceException $"Html_node {node} doesn't have a parent which is an Element")
        parent
    
            
    
    let try_descendant css (node:Html_node) =
        node
        |>descendants css
        |>function
        |[]->None
        |descendants ->
            descendants
            |>should_be_single
            |>Some
    
    
    let descendants_from_highest_level css (node:Html_node) =
        //takes nodes with the needed css from one level of depth, the first encountered level
        let rec iteration_of_finding_children
            css
            (nodes: Html_node list)
            =
            if nodes.IsEmpty then
                []
            else
                let needed_children =
                    nodes
                    //|>List.collect (descendants (":scope>"+css))
                    |>List.collect (direct_children_with_css css)
                match needed_children with
                |[] ->
                    nodes
                    |>List.collect direct_children
                    |>iteration_of_finding_children
                        css
                |found_children -> found_children
    
        iteration_of_finding_children
            css
            [node]
            
    let descendents_without_deepening css (node:Html_node) =
        //takes first descendents matching the css, without going deeper into those found descendents
        let rec iteration_of_finding_children
            css
            (needed_nodes: Html_node list)
            (nodes: Html_node list)
            =
            if nodes.IsEmpty then
                needed_nodes
            else
                
                let not_taken_children,needed_children =
                    nodes
                    |>List.collect direct_children
                    |>List.groupBy(fun child ->
                        child.Matches css
                    )|>Map.ofList
                    |>fun children ->
                        Map.tryFind false children
                        |>Option.defaultValue []
                        ,
                        Map.tryFind true children
                        |>Option.defaultValue []
                    
                
                match not_taken_children with
                |[] ->
                     needed_nodes @ needed_children
                |children_for_deepening ->
                    children_for_deepening
                    |>iteration_of_finding_children
                        css
                        (needed_nodes @ needed_children)
    
        iteration_of_finding_children
            css
            []
            [node]
            
    
    
    let which_have_direct_child
        (child:Html_node)
        (parents: Html_node seq)
        =
        parents
        |>Seq.filter(fun parent->
            parent.Children
            |>Seq.contains child
        )
    

    let travel_down
        (child_positions: int list)
        (node:Html_node)
        =
        [0..(Seq.length child_positions)-1]
        |>List.fold (fun node child_level ->
            let child_index_on_this_level =
                (List.item child_level child_positions) - 1
            node
            |>direct_children
            |>List.item child_index_on_this_level
        )
            node
    
    let descend levels_amount (node:Html_node) =
        node
        |>travel_down (List.init levels_amount (fun _ -> 1))
    
    // let descend levels_amount (node:Html_node) =
    //     [1..levels_amount]
    //     |>List.fold (fun node _ ->
    //         node
    //         |>direct_children
    //         |>should_be_single
    //     )
    //         node
    
    let from_text_and_context
        (context: IBrowsingContext)
        (html_text:string)
        =
        let htmlParser = context.GetService<IHtmlParser>()
        let document = htmlParser.ParseDocument(String.Empty)
        let nodes = htmlParser.ParseFragment(html_text.Trim(), document.Body)
        nodes|>should_be_single :?> Html_node
        
    let from_scraped_node_and_context
        (context: IBrowsingContext)
        (web_node:IWebElement)
        =
        from_text_and_context
            context
            (web_node.GetAttribute("outerHTML"))
            
    let from_text (html_text:string) =
        let context = BrowsingContext.New AngleSharp.Configuration.Default
        from_text_and_context context html_text
    
    let from_html_string_and_context context html_string =
        let (Html_string text) = html_string
        from_text_and_context context text
    let from_html_string html_text =
        let (Html_string text) = html_text
        from_text text
    
    let from_scraped_node (web_node:IWebElement) =
        let html_text = web_node.GetAttribute("outerHTML")
        from_text html_text

    
    let non_textual_attributes=
        ["class";"style";"aria-labelledby";"id"]
        |>Set.ofList
    let rec remove_parts_not_related_to_text (node:Html_node)=
        
        node.Attributes
        |>List.ofSeq
        |>List.iter (fun attribute->
            if non_textual_attributes|>Set.contains attribute.Name then
                node.RemoveAttribute(attribute.NamespaceUri, attribute.Name)|>ignore
        )
        
        node
        |>direct_children
        |>List.iter (remove_parts_not_related_to_text>>ignore)
        
        node
        
    let detach_from_parent
        (removed_node: Html_node) 
        =
        removed_node.Remove()
        removed_node
    
                
    [<Fact>]
    let ``try first_descendants_with_css when there's no such descendants``()=
        """<article aria-labelledby="id__pvh95th8bp id__fuzoepp7cxs id__3ihacnfzgwt id__hcd6uy5rle8 id__4t5svg3sqgy id__bwqsupziqa8 id__852yjbvelp2 id__qevzug51xo9 id__b71zamtz3s id__w6kzrfhofw id__9rqcgslvvvh id__3i885rmu4hk id__lgtvh6jm6j id__05ofwuddbgf2 id__hyare5ude0t id__tc7t4px04r id__3b0z7eb9ss id__cr5q6zgt75g id__cqc8sn2ju3" role="article" tabindex="0" class="css-1dbjc4n r-1loqt21 r-18u37iz r-1ny4l3l r-1udh08x r-1qhn6m8 r-i023vh r-o7ynqc r-6416eg" data-testid="tweet"><div class="css-1dbjc4n r-eqz5dr r-16y2uox r-1wbh5a2"><div class="css-1dbjc4n r-16y2uox r-1wbh5a2 r-1ny4l3l"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-1iusvr4 r-16y2uox r-ttdzmv"><div class="css-1dbjc4n r-15zivkp r-q3we1"><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-obd0qt r-onrtq4 r-18kxxzh r-1777fci r-1b7u577"><svg viewBox="0 0 24 24" aria-hidden="true" class="r-14j79pv r-4qtqp9 r-yyyyoo r-10ptun7 r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1janqcz"><g><path d="M7 4.5C7 3.12 8.12 2 9.5 2h5C15.88 2 17 3.12 17 4.5v5.26L20.12 16H13v5l-1 2-1-2v-5H3.88L7 9.76V4.5z"></path></g></svg></div><div class="css-1dbjc4n r-1iusvr4 r-16y2uox"><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-1habvwh r-1wbh5a2 r-1777fci"><div dir="ltr" class="css-901oao css-cens5h r-14j79pv r-37j5jr r-n6v787 r-b88u0q r-1cwl3u0 r-bcqeeo r-qvutc0" id="id__hcd6uy5rle8" data-testid="socialContext" style="-webkit-line-clamp: 2; text-overflow: unset;"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;">Pinned</span></div></div></div></div></div></div></div></div></div><div class="css-1dbjc4n r-18u37iz"><div class="css-1dbjc4n r-1awozwy r-onrtq4 r-18kxxzh r-1b7u577"><div class="css-1dbjc4n" data-testid="Tweet-User-Avatar"><div class="css-1dbjc4n r-18kxxzh r-1wbh5a2 r-13qz1uu"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1adg3ll r-bztko3" data-testid="UserAvatar-Container-HilzFuld" style="height: 40px; width: 40px;"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-1adg3ll r-1pi2tsx r-1wyvozj r-bztko3 r-u8s1d r-1v2oles r-desppf r-13qz1uu"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-ggadg3 r-1udh08x r-u8s1d r-8jfcpp" style="height: calc(100% - -4px); width: calc(100% - -4px);"><a href="/HilzFuld" aria-hidden="true" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1niwhzg r-1loqt21 r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1niwhzg r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-14lw9ot r-1pi2tsx r-13qz1uu"></div></div><div class="css-1dbjc4n r-14lw9ot r-sdzlij r-1wyvozj r-1udh08x r-633pao r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-1adg3ll r-1udh08x" style=""><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 100%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div aria-label="" class="css-1dbjc4n r-1p0dtai r-1mlwlqe r-1d2f490 r-1udh08x r-u8s1d r-zchlnj r-ipm5af r-417010"><div class="css-1dbjc4n r-1niwhzg r-vvn4in r-u6sd8q r-4gszlv r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw" style="background-image: url(&quot;https://pbs.twimg.com/profile_images/1679121273421541376/gOt7Lgmy_bigger.jpg&quot;);"></div><img alt="" draggable="true" src="https://pbs.twimg.com/profile_images/1679121273421541376/gOt7Lgmy_bigger.jpg" class="css-9pa8cd"></div></div></div></div><div class="css-1dbjc4n r-sdzlij r-1wyvozj r-1udh08x r-u8s1d r-1v2oles r-desppf" style="height: calc(100% - 4px); width: calc(100% - 4px);"><div class="css-1dbjc4n r-12181gd r-1pi2tsx r-1ny4l3l r-o7ynqc r-6416eg r-13qz1uu"></div></div></a></div></div></div></div></div></div></div></div></div><div class="css-1dbjc4n r-1iusvr4 r-16y2uox r-1777fci r-kzbkwu"><div class="css-1dbjc4n r-zl2h9q"><div class="css-1dbjc4n r-k4xj1c r-18u37iz r-1wtj0ep"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs r-1ny4l3l" id="id__4t5svg3sqgy" data-testid="User-Name"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/HilzFuld" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1wbh5a2 r-dnmrzs"><div dir="ltr" class="css-901oao r-1awozwy r-18jsvk2 r-6koalj r-37j5jr r-a023e6 r-b88u0q r-rjixqe r-bcqeeo r-1udh08x r-3s2u2q r-qvutc0" style="text-overflow: unset;"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;">Hillel Fuld</span></span></div><div dir="ltr" class="css-901oao r-18jsvk2 r-xoduu5 r-18u37iz r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0" style="text-overflow: unset;"><span class="css-901oao css-16my406 r-1awozwy r-xoduu5 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;"><svg viewBox="0 0 22 22" aria-label="Verified account" role="img" class="r-1cvl2hr r-4qtqp9 r-yyyyoo r-1xvli5t r-9cviqr r-f9ja8p r-og9te1 r-bnwqim r-1plcrui r-lrvibr" data-testid="icon-verified"><g><path d="M20.396 11c-.018-.646-.215-1.275-.57-1.816-.354-.54-.852-.972-1.438-1.246.223-.607.27-1.264.14-1.897-.131-.634-.437-1.218-.882-1.687-.47-.445-1.053-.75-1.687-.882-.633-.13-1.29-.083-1.897.14-.273-.587-.704-1.086-1.245-1.44S11.647 1.62 11 1.604c-.646.017-1.273.213-1.813.568s-.969.854-1.24 1.44c-.608-.223-1.267-.272-1.902-.14-.635.13-1.22.436-1.69.882-.445.47-.749 1.055-.878 1.688-.13.633-.08 1.29.144 1.896-.587.274-1.087.705-1.443 1.245-.356.54-.555 1.17-.574 1.817.02.647.218 1.276.574 1.817.356.54.856.972 1.443 1.245-.224.606-.274 1.263-.144 1.896.13.634.433 1.218.877 1.688.47.443 1.054.747 1.687.878.633.132 1.29.084 1.897-.136.274.586.705 1.084 1.246 1.439.54.354 1.17.551 1.816.569.647-.016 1.276-.213 1.817-.567s.972-.854 1.245-1.44c.604.239 1.266.296 1.903.164.636-.132 1.22-.447 1.68-.907.46-.46.776-1.044.908-1.681s.075-1.299-.165-1.903c.586-.274 1.084-.705 1.439-1.246.354-.54.551-1.17.569-1.816zM9.662 14.85l-3.429-3.428 1.293-1.302 2.072 2.072 4.4-4.794 1.347 1.246z"></path></g></svg></span></div></div></a></div></div><div class="css-1dbjc4n r-18u37iz r-1wbh5a2 r-13hce6t"><div class="css-1dbjc4n r-1d09ksm r-18u37iz r-1wbh5a2"><div class="css-1dbjc4n r-1wbh5a2 r-dnmrzs"><a href="/HilzFuld" role="link" tabindex="-1" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1wbh5a2 r-dnmrzs r-1ny4l3l"><div dir="ltr" class="css-901oao css-1hf3ou5 r-14j79pv r-18u37iz r-37j5jr r-1wvb978 r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-qvutc0" style="text-overflow: unset;"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;">@HilzFuld</span></div></a></div><div dir="ltr" aria-hidden="true" class="css-901oao r-14j79pv r-1q142lx r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-s1qlax r-qvutc0" style="text-overflow: unset;"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;">·</span></div><div class="css-1dbjc4n r-18u37iz r-1q142lx"><a href="/HilzFuld/status/1715358246918103214" dir="ltr" aria-label="Oct 20" role="link" class="css-4rbku5 css-18t94o4 css-901oao r-14j79pv r-1loqt21 r-xoduu5 r-1q142lx r-1w6e6rj r-37j5jr r-a023e6 r-16dba41 r-9aw3ui r-rjixqe r-bcqeeo r-3s2u2q r-qvutc0" style="text-overflow: unset;"><time datetime="2023-10-20T13:24:10.000Z">Oct 20</time></a></div></div></div></div></div></div><div class="css-1dbjc4n r-1jkjb"><div class="css-1dbjc4n r-1awozwy r-18u37iz r-1cmwbt1 r-1wtj0ep"><div class="css-1dbjc4n r-1awozwy r-6koalj r-18u37iz"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="More" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="caret"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0" style="text-overflow: unset;"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M3 12c0-1.1.9-2 2-2s2 .9 2 2-.9 2-2 2-2-.9-2-2zm9 2c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm7 0c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div><div class="css-1dbjc4n"><div dir="auto" lang="en" class="css-901oao css-cens5h r-18jsvk2 r-37j5jr r-a023e6 r-16dba41 r-rjixqe r-bcqeeo r-bnwqim r-qvutc0" id="id__9rqcgslvvvh" data-testid="tweetText" style="-webkit-line-clamp: 10; text-overflow: unset;"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;">I had to address some of the confusion. The world is lost, let's make sure we don't join them!</span></div></div><div aria-labelledby="id__7c5yzrcn6zg id__bs7cizoeqvc" class="css-1dbjc4n r-1ssbvtb r-1s2bzr4" id="id__lgtvh6jm6j"><div class="css-1dbjc4n r-9aw3ui"><div class="css-1dbjc4n"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-1ets6dv r-1867qdf r-1phboty r-rs99b7 r-1ny4l3l r-1udh08x r-o7ynqc r-6416eg"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-1pi2tsx" data-testid="tweetPhoto"><div class="css-1dbjc4n"><div class="css-1dbjc4n"><div class="css-1dbjc4n r-1adg3ll r-pm9dpa r-1udh08x"><div class="r-1adg3ll r-13qz1uu" style="padding-bottom: 56.1404%;"></div><div class="r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-ipm5af r-13qz1uu"><div aria-label="Embedded video" class="css-1dbjc4n r-1awozwy r-1p0dtai r-1777fci r-1d2f490 r-u8s1d r-zchlnj r-ipm5af" data-testid="previewInterstitial"><div class="css-1dbjc4n r-1p0dtai r-1d2f490 r-1udh08x r-u8s1d r-zchlnj r-ipm5af"><div class="css-1dbjc4n r-1p0dtai r-xigjrr r-1d2f490 r-1udh08x r-u8s1d r-zchlnj r-ipm5af r-1c5lwsr"><div aria-label="Embedded video" class="css-1dbjc4n r-1p0dtai r-1mlwlqe r-1d2f490 r-11wrixw r-61z16t r-1udh08x r-u8s1d r-zchlnj r-ipm5af r-417010" style="margin-bottom: 0px; margin-top: 0px;"><div class="css-1dbjc4n r-1niwhzg r-vvn4in r-u6sd8q r-4gszlv r-1p0dtai r-1pi2tsx r-1d2f490 r-u8s1d r-zchlnj r-ipm5af r-13qz1uu r-1wyyakw" style="background-image: url(&quot;https://pbs.twimg.com/amplify_video_thumb/1715357838225092608/img/8zHsDJBeE47ADKLt?format=webp&amp;name=tiny&quot;);"></div><img alt="Embedded video" draggable="true" src="https://pbs.twimg.com/amplify_video_thumb/1715357838225092608/img/8zHsDJBeE47ADKLt?format=webp&amp;name=tiny" class="css-9pa8cd"></div></div></div><div class="css-1dbjc4n r-rki7wi r-18u37iz r-161ttwi r-633pao r-u8s1d"><div class="css-1dbjc4n r-1awozwy r-k200y r-13w96dm r-6t5ypu r-zmljjp r-z2wwpe r-kicko2 r-t12b5v r-z80fyv r-1777fci r-a5pmau r-s1qlax r-633pao"><div dir="ltr" class="css-901oao r-jwli3a r-37j5jr r-n6v787 r-16dba41 r-1cwl3u0 r-bcqeeo r-q4m81j r-lrvibr r-qvutc0" style="text-overflow: unset;"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;">5:23</span></div></div><div class="css-1dbjc4n r-1awozwy r-k200y r-13w96dm r-pm2fo r-1dpl46z r-z2wwpe r-ou6ah9 r-notknq r-z80fyv r-1777fci r-s1qlax r-633pao"><div dir="ltr" class="css-901oao r-jwli3a r-37j5jr r-n6v787 r-16dba41 r-1cwl3u0 r-bcqeeo r-q4m81j r-lrvibr r-qvutc0" style="text-overflow: unset;">18 MB</div></div></div></div></div></div></div><div role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1awozwy r-1p0dtai r-1777fci r-1d2f490 r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"><div role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-42olwf r-sdzlij r-1phboty r-rs99b7 r-15ysp7h r-4wgw6l r-1ny4l3l r-ymttw5 r-o7ynqc r-6416eg r-lrvibr" style="backdrop-filter: blur(4px); background-color: rgba(15, 20, 25, 0.75);"><div dir="ltr" class="css-901oao r-1awozwy r-jwli3a r-6koalj r-18u37iz r-16y2uox r-37j5jr r-a023e6 r-b88u0q r-1777fci r-rjixqe r-bcqeeo r-q4m81j r-qvutc0" style="text-overflow: unset;"><span class="css-901oao css-16my406 css-1hf3ou5 r-poiln3 r-1b43r93 r-1cwl3u0 r-bcqeeo r-qvutc0" style="text-overflow: unset;"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;">Load video</span></span></div></div></div></div></div></div></div></div></div></div></div><div class="css-1dbjc4n"><div class="css-1dbjc4n"><div aria-label="1349 replies, 1776 reposts, 3360 likes, 509 bookmarks, 315026 views" role="group" class="css-1dbjc4n r-1kbdv8c r-18u37iz r-1wtj0ep r-1s2bzr4 r-1ye8kvj" id="id__cqc8sn2ju3"><div class="css-1dbjc4n r-13awgt0 r-18u37iz r-1h0z5md"><div aria-label="1349 Replies. Reply" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="reply"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0" style="text-overflow: unset;"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M1.751 10c0-4.42 3.584-8 8.005-8h4.366c4.49 0 8.129 3.64 8.129 8.13 0 2.96-1.607 5.68-4.196 7.11l-8.054 4.46v-3.69h-.067c-4.49.1-8.183-3.51-8.183-8.01zm8.005-6c-3.317 0-6.005 2.69-6.005 6 0 3.37 2.77 6.08 6.138 6.01l.351-.01h1.761v2.3l5.087-2.81c1.951-1.08 3.163-3.13 3.163-5.36 0-3.39-2.744-6.13-6.129-6.13H9.756z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1pn2ns4 r-qvutc0" style="text-overflow: unset;"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;">1.3K</span></span></span></div></div></div></div><div class="css-1dbjc4n r-13awgt0 r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="1776 reposts. Repost" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="retweet"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0" style="text-overflow: unset;"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M4.5 3.88l4.432 4.14-1.364 1.46L5.5 7.55V16c0 1.1.896 2 2 2H13v2H7.5c-2.209 0-4-1.79-4-4V7.55L1.432 9.48.068 8.02 4.5 3.88zM16.5 6H11V4h5.5c2.209 0 4 1.79 4 4v8.45l2.068-1.93 1.364 1.46-4.432 4.14-4.432-4.14 1.364-1.46 2.068 1.93V8c0-1.1-.896-2-2-2z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1pn2ns4 r-qvutc0" style="text-overflow: unset;"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;">1.7K</span></span></span></div></div></div></div><div class="css-1dbjc4n r-13awgt0 r-18u37iz r-1h0z5md"><div aria-label="3360 Likes. Like" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="like"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0" style="text-overflow: unset;"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M16.697 5.5c-1.222-.06-2.679.51-3.89 2.16l-.805 1.09-.806-1.09C9.984 6.01 8.526 5.44 7.304 5.5c-1.243.07-2.349.78-2.91 1.91-.552 1.12-.633 2.78.479 4.82 1.074 1.97 3.257 4.27 7.129 6.61 3.87-2.34 6.052-4.64 7.126-6.61 1.111-2.04 1.03-3.7.477-4.82-.561-1.13-1.666-1.84-2.908-1.91zm4.187 7.69c-1.351 2.48-4.001 5.12-8.379 7.67l-.503.3-.504-.3c-4.379-2.55-7.029-5.19-8.382-7.67-1.36-2.5-1.41-4.86-.514-6.67.887-1.79 2.647-2.91 4.601-3.01 1.651-.09 3.368.56 4.798 2.01 1.429-1.45 3.146-2.1 4.796-2.01 1.954.1 3.714 1.22 4.601 3.01.896 1.81.846 4.17-.514 6.67z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1pn2ns4 r-qvutc0" style="text-overflow: unset;"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;">3.3K</span></span></span></div></div></div></div><div class="css-1dbjc4n r-13awgt0 r-18u37iz r-1h0z5md"><a href="/HilzFuld/status/1715358246918103214/analytics" aria-label="315026 views. View post analytics" role="link" class="css-4rbku5 css-18t94o4 css-1dbjc4n r-1loqt21 r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0" style="text-overflow: unset;"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M8.75 21V3h2v18h-2zM18 21V8.5h2V21h-2zM4 21l.004-10h2L6 21H4zm9.248 0v-7h2v7h-2z"></path></g></svg></div><div class="css-1dbjc4n r-xoduu5 r-1udh08x"><span data-testid="app-text-transition-container" style="transition-property: transform; transition-duration: 0.3s; transform: translate3d(0px, 0px, 0px);"><span class="css-901oao css-16my406 r-poiln3 r-n6v787 r-1cwl3u0 r-1k6nrdp r-1pn2ns4 r-qvutc0" style="text-overflow: unset;"><span class="css-901oao css-16my406 r-poiln3 r-bcqeeo r-qvutc0" style="text-overflow: unset;">315K</span></span></span></div></div></a></div><div class="css-1dbjc4n r-18u37iz r-1h0z5md r-1b7u577"><div aria-label="Bookmark" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr" data-testid="bookmark"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0" style="text-overflow: unset;"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M4 4.5C4 3.12 5.119 2 6.5 2h11C18.881 2 20 3.12 20 4.5v18.44l-8-5.71-8 5.71V4.5zM6.5 4c-.276 0-.5.22-.5.5v14.56l6-4.29 6 4.29V4.5c0-.28-.224-.5-.5-.5h-11z"></path></g></svg></div></div></div></div><div class="css-1dbjc4n" style="display: inline-grid; justify-content: inherit; transform: rotate(0deg) scale(1) translate3d(0px, 0px, 0px); -webkit-box-pack: inherit;"><div class="css-1dbjc4n r-18u37iz r-1h0z5md"><div aria-expanded="false" aria-haspopup="menu" aria-label="Share post" role="button" tabindex="0" class="css-18t94o4 css-1dbjc4n r-1777fci r-bt1l66 r-1ny4l3l r-bztko3 r-lrvibr"><div dir="ltr" class="css-901oao r-1awozwy r-14j79pv r-6koalj r-37j5jr r-a023e6 r-16dba41 r-1h0z5md r-rjixqe r-bcqeeo r-o7ynqc r-clp7b1 r-3s2u2q r-qvutc0" style="text-overflow: unset;"><div class="css-1dbjc4n r-xoduu5"><div class="css-1dbjc4n r-1niwhzg r-sdzlij r-1p0dtai r-xoduu5 r-1d2f490 r-xf4iuw r-1ny4l3l r-u8s1d r-zchlnj r-ipm5af r-o7ynqc r-6416eg"></div><svg viewBox="0 0 24 24" aria-hidden="true" class="r-4qtqp9 r-yyyyoo r-1xvli5t r-dnmrzs r-bnwqim r-1plcrui r-lrvibr r-1hdv0qi"><g><path d="M12 2.59l5.7 5.7-1.41 1.42L13 6.41V16h-2V6.41l-3.3 3.3-1.41-1.42L12 2.59zM21 15l-.02 3.51c0 1.38-1.12 2.49-2.5 2.49H5.5C4.11 21 3 19.88 3 18.5V15h2v3.5c0 .28.22.5.5.5h12.98c.28 0 .5-.22.5-.5L19 15h2z"></path></g></svg></div></div></div></div></div></div></div></div></div></div></div></div></article>"""
        |>from_text
        |>descendants_from_highest_level Twitter_settings.timeline_cell_css   
        |>should be Empty
    
module Html_parsing =
    
    let parsing_context () =
        BrowsingContext.New(AngleSharp.Configuration.Default)
    
    let last_url_segment (text:string) =
        text.Substring(text.LastIndexOf(@"/")+1)
    
  
    
    let parseable_node_from_string
        (context:IBrowsingContext )
        (html_text: string)
        =
        let htmlParser = context.GetService<IHtmlParser>()
        let document = htmlParser.ParseDocument html_text
        document.Body
        
    let parseable_node_from_scraped_node
        (context:IBrowsingContext )
        (web_node: Web_node): Html_node
        =
        let html_text = web_node.GetAttribute("outerHTML")
        parseable_node_from_string context html_text
                
    let comparable_node_from_scraped_node
        (web_node: Web_node): Comparable_html_hode
        =
        web_node.GetAttribute("outerHTML")
        |>Comparable_html_hode.Parse
        |>Seq.head

    
    
    let segment_of_composed_text_as_text (segment:Html_node) =
        match segment.LocalName with
        |"img" -> //an emoji
            Html_node.attribute_value "alt" segment
        |_->Html_node.inner_text segment
    
    let readable_text_from_html_segments (root:Html_node) =
        root
        |>Html_node.direct_children
        |>Seq.map segment_of_composed_text_as_text
        |>String.concat ""
    
    
        
    
        
    let standartize_linebreaks (text:string) =
        text.ReplaceLineEndings()