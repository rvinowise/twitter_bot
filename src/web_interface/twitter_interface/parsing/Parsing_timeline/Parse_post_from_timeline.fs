namespace rvinowise.twitter

open rvinowise.html_parsing
open rvinowise.twitter
open rvinowise.twitter.Parse_segments_of_post






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
            |>Html_node.to_string
            
        let urls =
            urls_to_external_source
            |>Seq.map Html_node.to_string
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
    
    
    let cell_looks_like_hidden_replies
        html_cell
        =
        html_cell
        |>Html_node.descendant "span"
        |>Html_node.inner_text = "Show more replies"
        
        
    type Parsed_timeline_cell =
    |Post of Main_post*Previous_cell
    |Hidden_post of Previous_cell
    |Error of string
    
    let try_parse_post
        previous_cell
        article_node
        =
        try 
            article_node
            |>Parse_segments_of_post.parse_main_twitter_post previous_cell
            |>Parsed_timeline_cell.Post
        with
        | :? Bad_post_exception as exc ->
            Parsed_timeline_cell.Error exc.Message
           
        
 
    let try_parse_cell
        (previous_cell: Previous_cell)
        (html_cell: Html_node)
        =
        
        let article_node =
            html_cell
            |>Html_node.try_descendant "article[data-testid='tweet']"
        
        match article_node with
        |Some article_node ->
            try_parse_post
                previous_cell
                article_node
        |None->
            if
                cell_looks_like_hidden_replies html_cell
            then
                Parsed_timeline_cell.Hidden_post previous_cell
            else
                Parsed_timeline_cell.Error "a timeline cell has neither the article in it, nor hidden replies"
                
            
    
    