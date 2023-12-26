namespace rvinowise.twitter

open System
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
module Parse_timeline_cell =
    
    let cell_looks_like_hidden_replies
        html_cell
        =
        html_cell
        |>Html_node.descendant "span"
        |>Html_node.inner_text = "Show more replies"
        
        
    
    let report_error_when_parsing_post
        (exc: Exception)
        (post_node: Html_node)
        =
        Log.error $"""Exception: {exc.Message}
            stacktrase: {exc.StackTrace}
            when parsing twitter post with html:
            {post_node.OuterHtml}"""
        |>ignore
        
    let try_parse_post
        previous_cell
        article_node
        =
        try
            article_node
            |>Parse_article.parse_twitter_article previous_cell
            |>Parsed_timeline_cell.Adjacent_post
        with
        | :? Bad_post_exception
        | :? ArgumentException as exc ->   
            report_error_when_parsing_post exc article_node
            Parsed_timeline_cell.Error exc.Message
 
    let try_article_node_from_cell_node cell_node =
        cell_node
        |>Html_node.try_descendant "article[data-testid='tweet']"
    
    let parse_timeline_cell
        (previous_cell: Parsed_timeline_cell)
        (html_cell: Html_node)
        =
        
        let article_node =
            try_article_node_from_cell_node html_cell
        
        match article_node with
        |Some article_node ->
            try_parse_post
                previous_cell
                article_node
        |None->
            if
                cell_looks_like_hidden_replies html_cell
            then
                previous_cell
                |>Parsed_timeline_cell.try_post
                |>function
                |Some post ->
                    Parsed_timeline_cell.Distant_connected_post post
                |None ->
                    "this cell contains hidden replies, but there's no post to which they reply"
                    |>Parsed_timeline_cell.Error
            else
                "a timeline cell has neither the article in it, nor hidden replies"
                |>Parsed_timeline_cell.Error
        //returns: Adjacent_post, Distant_connected_post, Error
            
    
    