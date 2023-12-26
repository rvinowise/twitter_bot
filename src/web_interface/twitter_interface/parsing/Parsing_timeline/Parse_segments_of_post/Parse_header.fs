namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter


type Parsed_post_header = {
    author:Twitter_user
    written_at: DateTime
    post_url: string option //quotations don't have their URL, only main posts do
}

module Parse_header =
    
    let valuable_header_node node =
        node
        |>Html_node.descendants "div[data-testid='User-Name']"
        |>List.head
        
        
    let detach_post_header
        node //role=link for the quotation, or article for the main post
        =
        let user_avatar_node =
            node
            |>Html_node.descendants "div[data-testid='Tweet-User-Avatar']"
            |>List.head
        
        user_avatar_node
        |>Html_node.detach_from_parent
        |>ignore
            
        node
        |>valuable_header_node 
        |>Html_node.detach_from_parent
    
    let datetime_node_from_valuable_header_node
        header_node //div[data-testid='User-Name']
        =
        header_node
        |>Html_node.direct_children
        |>List.item 1
        |>Html_node.descendant "time"

    let url_from_datetime_node datetime_node =
        datetime_node
        |>Html_node.parent
        |>Html_node.try_attribute_value "href"
    
    let parse_post_header
        header_node //div[data-testid='User-Name']
        =
        let author_name =
            header_node
            |>Html_node.direct_children
            |>List.head
            |>Html_node.descendants "span"
            |>List.head
            |>Html_parsing.readable_text_from_html_segments
            
        let author_handle =
            header_node
            |>Html_node.direct_children
            |>List.item 1
            |>Html_node.descendants "span"
            |>List.head
            |>Html_node.inner_text
            |>fun url_with_atsign->url_with_atsign[1..]
            |>User_handle
        
        let datetime_node =
            datetime_node_from_valuable_header_node header_node
        
        let datetime =
            datetime_node
            |>Html_node.attribute_value "datetime"
            |>Parsing_twitter_datatypes.parse_twitter_datetime
        
        let url =
            url_from_datetime_node datetime_node
        
        {
            Parsed_post_header.author = {name=author_name;handle=author_handle}
            written_at = datetime
            post_url=url
        }
        
    let detach_and_parse_header
        node //role=link for the quotation, or article for the main post
        =
        node
        |>detach_post_header
        |>parse_post_header
    
    let post_id_from_post_url post_url =
        post_url
        |>Html_parsing.last_url_segment
        |>int64|>Post_id
    
    let peak_post_id
        article_node
        =
        article_node
        |>valuable_header_node
        |>datetime_node_from_valuable_header_node
        |>url_from_datetime_node
        |>function
        |Some url->
            post_id_from_post_url url
        |None ->
            raise (Bad_post_exception("main post should have its url in its header"))