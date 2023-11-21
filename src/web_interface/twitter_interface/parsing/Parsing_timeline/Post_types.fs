﻿namespace rvinowise.twitter

open System
open rvinowise.html_parsing
open rvinowise.twitter




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


type Reply_status =
    
    (*the author will be the same as in this message; text "show this thread" after the message *)
    |External_thread of User_handle
    (* this type of reply may be identical to the next one (External_message) (is it discontinued?) *)
    
    (*text "replying to..." below header *)     
    |External_message of User_handle * Post_id option
    
    (*there will be a replying message right after this message; vertical line in the timeline.
    can not exist inside a quotation *)
    
    |Starting_local_thread 
    |Continuing_local_thread of Post_id //replying to the previous message in the timeline
    |Ending_local_thread of Post_id
    
    
type Post_header = {
    author: Twitter_user
    created_at: DateTime
    reply_status: Reply_status option
}

type Quotable_message = {
    header: Post_header
    message: Post_message
    media_load: Media_item list
}

type Quotable_poll = {
    header: Post_header
    question: string
}

type Poll_choice = {
    text: string
    votes_percent: float
}

type Poll = {
    quotable_part: Quotable_poll
    choices: Poll_choice list
    votes_amount: int
}

type External_url = {
    base_url: string option
    page: string option
    message: string option
    obfuscated_url: string option
}

type External_source =
    |External_url of External_url
    (*sometimes the quoted message ID can be determined from the timeline, e.g.:
    if the replying message has images of showMore button;*)
    |Quotation of Quotable_message
    |Poll of Quotable_poll

type Main_post_body =
    |Message of Quotable_message * External_source option
    |Poll of Poll

type Main_post = {
    id: Post_id
    body: Main_post_body
    stats: Post_stats
    repost: User_handle option
    is_pinned: bool
}

