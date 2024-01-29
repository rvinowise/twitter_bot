namespace rvinowise.twitter.test

open Xunit
open FsUnit
open rvinowise.twitter

module Attention_test =
    
    let lists_to_attention_maps
        (lists: List<string * List<string*float> > ) 
        =
        lists
        |>List.map (fun (attentive_user,targets) ->
            attentive_user,
            targets
            |>List.map (fun (target,attention) -> User_handle target, attention)
            |>Map.ofList
        )
        |>List.map (fun (attentor, attention) -> User_handle attentor, attention)
        |>Map.ofList
    
    [<Fact>]
    let ``try combined_attention``() =
        
        let likes =
            [
                "user1",
                [
                    "user10",10.
                    "user11",11.
                ]
                "user2",
                [
                    "user20",20.
                    "user21",21.
                ]
            ]|>lists_to_attention_maps
            
        let reposts =
            [
                "user1",
                [
                    "user10",100.
                ]
                
                "user2",
                [
                    "user20",200.
                    "user21",210.
                ]
                
                "user3",
                [
                    "user20",200.
                    "user21",210.
                ]
                
            ]|>lists_to_attention_maps
            
        let replies =
            [
                "user1",
                [
                    "user10",1000.
                ]
                
            ]|>lists_to_attention_maps
             
        let combined_attention =
            [
                "user1",
                [
                    "user10",1110.
                    "user11",11.
                ]
                "user2",
                [
                    "user20",220.
                    "user21",231.
                ]
                "user3",
                [
                    "user20",200.
                    "user21",210.
                ]
            ]|>lists_to_attention_maps
            
            
        Dendrogram.combined_attention
            [likes;reposts;replies]
        |>should equal combined_attention