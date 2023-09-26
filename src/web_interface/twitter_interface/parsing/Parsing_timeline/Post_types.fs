namespace rvinowise.twitter

open System
open AngleSharp.Dom
open OpenQA.Selenium
open OpenQA.Selenium.Interactions
open OpenQA.Selenium.Support.UI
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.parallell.functions
open rvinowise.html_parsing
open FSharp.Data
open rvinowise.twitter



type Post_header = {
    author:Twitter_user
    written_at: DateTime
    post_url: string option //quotations don't have their URL, only main posts do
}
type Post_stats = {
    replies_amount: int
    likes_amount: int
    reposts_amount: int
    views_amount: int
}


type Abbreviated_message = {
    message: string
    show_more_url: string
}
type Post_message =
    | Abbreviated of Abbreviated_message
    | Full of string

module Post_message =
    let from_html_node (node:Html_node) = //"tweetText"
        let show_more_css = "a[data-testid='tweet-text-show-more-link']"
        let show_mode_node =
            node
            |>Html_node.try_descendant show_more_css
        
        let message =
            node
            |>Html_node.direct_children
            |>List.filter (Html_node.matches show_more_css >> not)
            |>List.map Html_parsing.segment_of_composed_text_as_text
            |>String.concat ""
            |>Html_parsing.standartize_linebreaks
            
        match show_mode_node with
        |Some show_mode_node ->
            Post_message.Abbreviated {
                message = message
                show_more_url =
                    show_mode_node
                    |>Html_node.attribute_value "href"
                    |>Twitter_settings.absolute_url
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


type Reply_target = User_handle * Post_id option

type Quotable_post = {
    author: Twitter_user
    created_at: DateTime
    reply_target: Reply_target option
    message: Post_message
    media_load: Media_item list
}

type External_url = {
    base_url: string
    page: string
    message: string
    (* the actual referenced URL is hidden, need to click in order to see it *)
    obfuscated_url: string 
}

type External_source =
    |External_url of External_url
    |Quotation of Quotable_post

type Main_post = {
    id: Post_id
    quotable_core: Quotable_post
    external_source: External_source option
    stats: Post_stats
}

type Post =
    |Main_post of Main_post
    |Quoted_post of Quotable_post * Media_item list
