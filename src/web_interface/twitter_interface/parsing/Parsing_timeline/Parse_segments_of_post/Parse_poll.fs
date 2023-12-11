namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.html_parsing
open rvinowise.twitter



module Parse_poll =
    
    
    
    let quotation_is_a_poll
        quotation_node // without message node
        =
        quotation_node
        |>Html_node.descendants "span"
        |>List.exists(fun node ->
            node
            |>Html_node.inner_text
            |>(=) "Show this poll"
        )
            
    
    let parse_ongoing_poll_choice
        button_node //div[role="radio"]
        =
        let text =
            button_node
            |>Html_node.descendants "span"
            |>List.head
            |>Html_node.inner_text
        {
            Poll_choice.text=text
            votes_percent=0
        } 
    
    let parse_finished_poll_choice li_node =
        let choice_nodes =
            li_node
            |>Html_node.descend 1
            |>Html_node.direct_children
            |>List.tail
        
        let text =
            choice_nodes
            |>List.head
            |>Html_node.descendants "span"
            |>List.head
            |>Html_node.inner_text
            
        let votes_percent =
            choice_nodes
            |>List.last
            |>Html_node.descendant "span"
            |>Html_node.inner_text
            |>fun text -> //68.5%
                Double.Parse(
                    text.TrimEnd('%'),
                    CultureInfo.InvariantCulture    
                )
       
        {
            Poll_choice.text=text
            votes_percent=votes_percent
        }            
        
    let votes_amount_text_to_int (text:String) =
        //1 vote // 10 votes
        text.Split(" ")
        |>Array.head
        |>Parsing_twitter_datatypes.parse_abbreviated_number
    
    let parse_ongoing_poll_details cardPoll_node choices_node =
        let choices =
            choices_node
            |>Html_node.descendants "div[role='radio']"
            |>List.map parse_ongoing_poll_choice
    
        let votes_amount =
            cardPoll_node
            |>Html_node.direct_children
            |>List.item 2
            |>Html_node.descendants "span>span"
            |>List.head
            |>Html_node.inner_text
            |>votes_amount_text_to_int
        
        choices,votes_amount
    
    let parse_finished_poll_details cardPoll_node choices_node =
        let choices =    
            choices_node
            |>Html_node.descendants "li[role='listitem']"
            |>List.map parse_finished_poll_choice
        
        let votes_amount =
            cardPoll_node
            |>Html_node.direct_children
            |>List.item 1
            |>Html_node.descendants "span>span"
            |>List.head
            |>Html_node.inner_text
            |>votes_amount_text_to_int
        
        choices,votes_amount
     
    let parse_poll_detail cardPoll_node =
        let ongoing_choices_node =
            cardPoll_node
            |>Html_node.try_descendant "div[aria-label='Poll options']"
        match ongoing_choices_node with
        |Some finished_choices_node ->
            parse_ongoing_poll_details cardPoll_node finished_choices_node
        |None ->
            cardPoll_node
            |>Html_node.descendant "ul[role='list']"
            |>parse_finished_poll_details cardPoll_node 
        
   