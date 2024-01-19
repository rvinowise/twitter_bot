namespace rvinowise.twitter

open System
open rvinowise.html_parsing
open rvinowise.twitter


(*
posted urls sometimes create a beautiful rectangle with details of that url:
    Events, Spaces, External_websites, Broadcasts

Media-items (images, videos and Gifs): 
    scramble Events, Spaces, External_websites, Broadcasts (the link will be represented as plain text)
    (in both, Main posts and Quoted posts) 
    but:
        both, Main post and its Quoted post, can have 4 media-items each;
        Main post can have a Space, even if its quoted post has media-items
        

External_websites:
    don't exist in quotations

Events:
    don't exist in quotations
    
Spaces: 
    exist in quotations, if the main post doesn't have Media-items

Broadcasts:
    kind of exist in quotations, but look like a video (a media-item) without a poster

External_websites, Events, Spaces and Broadcasts can't coexist with each other within one Article
    
Quoted_post
    scramble External_websites and Events in the Main post, and can't have either themselves
    can have Spaces, unless its Main post has media-items
    
*)

type Post_stats = {
    replies: int
    likes: int
    reposts: int
    views: int
    bookmarks: int
}

module Post_stats =
    
    let all_zero =
        {
            Post_stats.replies=0
            likes=0
            reposts=0
            views=0
            bookmarks=0
        }

type Abbreviated_message = {
    message: string
    show_more_url: string option
}
type Post_message =
    | Abbreviated of Abbreviated_message
    | Full of string

module Post_message =
    let from_html_node (node:Html_node) = //"tweetText"
        let show_more_css = "[data-testid='tweet-text-show-more-link']"
        let show_more_node =
            node
            |>Html_node.try_descendant show_more_css
        
        let message =
            node
            |>Html_node.direct_children
            |>List.filter (Html_node.matches show_more_css >> not)
            |>List.map Html_parsing.segment_of_composed_text_as_text
            |>String.concat ""
            |>Html_parsing.standartize_linebreaks
            
        match show_more_node with
        |Some show_more_node ->
            Post_message.Abbreviated {
                message = message
                show_more_url =
                    show_more_node
                    |>Html_node.try_attribute_value "href"
                    |>Option.map Twitter_settings.absolute_url
            }
        |None ->
            Post_message.Full message
    
    let text message =
        match message with
        |Abbreviated abbreviated_message ->
            abbreviated_message.message
        |Full text -> text
    
    let empty =
        Post_message.Full ""
            
type Posted_image = {
    url: string
    description: string
}

module Posted_image =
    let from_html_image_node (node: Html_node) = //img[]
        {
            Posted_image.url=
                node
                |>Html_node.attribute_value "src"
            description=
                node
                |>Html_node.attribute_value "alt"
        }
        

module Posted_video =
    let from_html_video_node
        (
            ``node of video[]``
                : Html_node
        )
        =
        ``node of video[]``
        |>Html_node.attribute_value "poster"
    
    let from_div_node_with_image
        (
            ``last node of aria-label="Embedded video"``
                : Html_node
        )
        =
        ``last node of aria-label="Embedded video"``
        |>Html_node.try_descendant "img"
        |>Option.map (Html_node.attribute_value "src")
        |>Option.defaultValue ""
    
    let from_poster_node
        (
            ``node with single aria-label="Embedded video" data-testid="previewInterstitial"``
                : Html_node
        )
        = 
        ``node with single aria-label="Embedded video" data-testid="previewInterstitial"``
        |>Html_node.descendant "img"
        |>Html_node.attribute_value "src"
        
type Media_item =
    |Image of Posted_image
    |Video_poster of string


type Reply = {
    to_user: User_handle
    to_post: Post_id option
    is_direct: bool
}
    
    
type Post_header = {
    author: Twitter_user
    created_at: DateTime
    reply: Reply option
}

type Twitter_audio_space = {
    host: string
    title: string
    audience_amount: int
}

type Quotable_message = {
    header: Post_header
    message: Post_message
    media_load: Media_item list
    audio_space: Twitter_audio_space option
}

type Quotable_poll = {
    header: Post_header
    question: string
}

type Poll_choice = {
    text: string
    votes_percent: float
}

(* a Poll can be a continuation of a Thread, but it can't be any other Reply *)
type Poll = {
    quotable_part: Quotable_poll
    choices: Poll_choice list
    votes_amount: int
}

type External_website = {
    base_url: string option
    full_url: string option
    page: string option
    message: string option
    obfuscated_url: string option
}

type Twitter_event_presenter =
    |User of Twitter_user
    |Company of string

type Twitter_event = {
    id: Event_id
    presenter: Twitter_event_presenter
    title: string
}


type External_source_node =
    |Quoted_message of Html_node
    |Quoted_poll of Html_node
    |External_website of Html_node*Html_node//card.wrapper and card.layoutSmall nodes
    |Carousel of Html_node * (Html_node list) //carousel root and card.wrappers
    |Twitter_event of Html_node * Event_id
    
module External_source_node =
    let root_html_node (node:External_source_node) =
        match node with
        |Quoted_message html
        |Quoted_poll html
        |Carousel (html, _)
        |External_website (html,_)
        |Twitter_event (html,_) ->
            html

type External_source =
    |External_websites of External_website list
    (*sometimes the quoted message ID can be determined from the timeline, e.g.:
    if the replying message has images of showMore button;*)
    |Quoted_message of Quotable_message
    |Quoted_poll of Quotable_poll
    |Twitter_event of Twitter_event

type Main_post_body =
    |Message of Quotable_message * External_source option
    |Poll of Poll

type Main_post = {
    id: Post_id
    body: Main_post_body
    stats: Post_stats
    reposter: User_handle option
    is_pinned: bool
}


module Post_header =
    let author header =
        header.author

    let created_at header =
        header.created_at

    let reply_status header =
        header.reply

module Main_post =
    
    let header post =
        match post.body with
        |Message (quotable_message, _) ->
            quotable_message.header
        |Poll poll ->
            poll.quotable_part.header
    
    let author_handle post =
        post|>header|>Post_header.author|>Twitter_user.handle
            
    let quotable_message post =
        match post.body with
        |Message (quotable_message, _) ->
            quotable_message
        |Poll _ ->
            raise <| Bad_post_exception "trying to obtain the Message from a Post which is Poll"
    
    let message post =
        match post.body with
        |Message (quotable_message, _) ->
            quotable_message.message
        |Poll _ ->
            raise <| Bad_post_exception "trying to obtain the Message from a Post which is Poll"
            
    let external_source post =
        match post.body with
        |Message (_, external_source) ->
            external_source
        |Poll _ ->
            None
    
    let audio_space post =
        match post.body with
        |Message (quotable_core, _) ->
            quotable_core.audio_space
        |Poll _ ->
            None
            
    let main_text post =
        match post.body with
        |Message (quotable_message, _) ->
            quotable_message.message
            |>Post_message.text
        |Poll poll ->
            poll.quotable_part.question
            
    let media_load post =
        match post.body with
        |Message (quotable_message,_) ->
            quotable_message.media_load
        |_ ->
            []
            
    let poll post =
        match post.body with
        |Poll poll ->
            poll
        |_ ->
            raise <| Bad_post_exception "trying to obtain the Poll from a Post which doesn't have a Poll"



type Thread_context =
    |Post of Main_post
    |Hidden_thread_replies of Main_post
    |Empty_context

module Thread_context =
    
    let try_post cell =
        match cell with
        |Post post
        |Hidden_thread_replies post ->
            Some post
        |Empty_context ->
            None
    let post cell =
        cell
        |>try_post
        |>function
        |Some post -> post
        |None -> raise (Bad_post_exception("timeline cell doesn't have a post"))
    
    let human_name (cell:Thread_context) =
        match cell with
        |Post post ->
            $"""Post {post.id} from "{Main_post.author_handle post}" """
        |Hidden_thread_replies post ->
            $"""Hidden_thread_replies with last visible post {post.id} from "{Main_post.author_handle post}" """
        |Empty_context ->
            "Empty thread context"