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
    
    
    let report_error_when_parsing_post
        (exc: Exception)
        (post_node: Html_node)
        =
        Log.error $"""Exception: {exc.GetType()} {exc.Message}
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
            |>Thread_context.Post
        with
        | :? Bad_post_exception
        | :? ArgumentException
        | :? FormatException as exc ->   
            report_error_when_parsing_post exc article_node
            Thread_context.Empty_context
 
    
    let try_article_node_from_cell_node cell_node =
        cell_node
        |>Html_node.try_descendant "article[data-testid='tweet']"
        
        
    let try_parse_cell_with_post previous_cell cell_node =
        match
            try_article_node_from_cell_node cell_node
        with
        |Some article_node ->
            try_parse_post previous_cell article_node
            |>Some
        |None -> None
    
    let is_cell_hidden_replies
        cell_node
        =
        cell_node
        |>Html_node.descendants "span"
        |>List.tryHead
        |>function
        |Some span_node ->
            Html_node.inner_text span_node = "Show more replies"
        |None -> false
    
    let try_parse_cell_with_hidden_thread_replies previous_cell cell_node =
        if is_cell_hidden_replies cell_node then
            previous_cell
            |>Thread_context.try_post
            |>function
            |Some previous_post ->
                previous_post
                |>Thread_context.Hidden_thread_replies
                |>Some
            |None ->
                "this cell contains hidden replies, but there's no post to which they reply"
                |>Harvesting_exception
                |>raise
        else
            None
        
    
        
    let parse_timeline_cell
        (previous_cell: Thread_context)
        (cell_node: Html_node)
        =
        let parsed_cell = 
            [
                (try_parse_cell_with_post previous_cell)
                (try_parse_cell_with_hidden_thread_replies previous_cell)
            ]|>List.tryPick (fun parser -> parser cell_node)
        
        match parsed_cell with
        |Some timeline_cell ->
            timeline_cell
        |None->
            Thread_context.Empty_context
            
    
    