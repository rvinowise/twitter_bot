namespace rvinowise.twitter

open rvinowise.html_parsing
open rvinowise.twitter






(*
a problem: it's not clear, what html-node should be sent to a parsing function, 
which parses a certain part of the html-layout.

1: this html-node should not have other children with similar tags/attributes to those which are needed from the parsed part,
so that the needed information could be found unambiguously.

2: it makes sense to find children by unique css-identifiers, rather than in absolute terms relative to their parents, 
so that different parts of the html-hierarchy could be sent as parameters 

 *)
module Parse_post_from_timeline =
    
    
    
    let has_different_items items =
        if Seq.length items < 2 then
            false
        else
            items
            |>Seq.forall((=) (Seq.head items))
            |>not
    
    let report_external_source_having_different_links 
        additional_load_node
        urls_to_external_source
        =
        let html =
            additional_load_node
            |>Html_node.as_html_string
            
        let urls =
            urls_to_external_source
            |>Seq.map Html_node.as_html_string
            |>String.concat "; "
            
        $"additional load of tweet has different links. post:\n{html}\nlinks: {urls}"    
        |>Log.error
        |>ignore
    
    

    
    let long_post_has_show_more_button post_body =
           post_body
           |>Html_node.try_descendant "data-testid='tweet-text-show-more-link'"
           |>function
               |Some _ ->true
               |None->false
    
    
    let url_from_show_more_button button_show_more =
        button_show_more
        |>Html_node.attribute_value "href"
    
       
    let repost_mark article_html =
        article_html
        |>Html_node.try_descendant "span[data-testid='socialContext']"
    
    
    
        
        

    
    