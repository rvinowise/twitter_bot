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
    
    let parse_post_header
        node
        =
        let header_node =
            node
            |>Html_node.descendant "div[data-testid='User-Name']"
        
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
            header_node
            |>Html_node.direct_children
            |>List.item 1
            |>Html_node.descendant "time"
        
        let datetime =
            datetime_node
            |>Html_node.attribute_value "datetime"
            |>Parsing_twitter_datatypes.parse_twitter_datetime
        
        let url =
            datetime_node
            |>Html_node.parent
            |>Html_node.try_attribute_value "href"
        
        {
            Parsed_post_header.author = {name=author_name;handle=author_handle}
            written_at = datetime
            post_url=url
        }
        
        
    