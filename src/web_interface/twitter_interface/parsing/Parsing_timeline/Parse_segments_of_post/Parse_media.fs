namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter



module Parse_media =
    
    let parse_media_from_stripped_node
        (*without quotation, user-avatar and message (all places with images which are not media-load) *)
        post_node
        =
        post_node
        |>Html_node.descendants "img"
        |>List.map (fun image_node ->
            image_node
            |>Html_node.parent
            |>Html_node.try_attribute_value "aria-label"
            |>function
            |Some "Embedded video" ->
                image_node
                |>Html_node.attribute_value "src"
                |>Media_item.Video_poster
            |_ ->
                Media_item.Image {
                    url=
                        image_node
                        |>Html_node.attribute_value "src"
                    description=
                        image_node
                        |>Html_node.attribute_value "alt"
                }
        )