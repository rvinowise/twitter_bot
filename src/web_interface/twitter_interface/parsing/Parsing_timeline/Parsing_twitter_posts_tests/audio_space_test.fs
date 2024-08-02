namespace rvinowise.twitter.test

open System
open Xunit
open FsUnit
open rvinowise.html_parsing
open rvinowise.twitter



module audio_space =
    
    
    let ``parse a post with an audio space``
        post //https://x.com/tehprom269887/status/1814375909991883075
        =
        post
        |>Main_post.quotable_message
        |>_.audio_space
        |>function
        |Some space ->
            space.title
            |>should equal "🚨@XSPACES Q & A FEEDBACK W/ 𝕏 LEAD MEDIA ENGINEER @marmars"
            space.host
            |>should equal "Diligent Denizen"
            space.audience_amount
            |>should equal 20100
        |wrong_source -> raise (Bad_post_exception($"this post should contain a twitter audio space, but here's {wrong_source}"))
        
        post
        |>Main_post.media_load
        |>should be TopLevelOperators.Empty
    
    
    let ``locally parse a post which shares an audio space``() =
        //https://x.com/tehprom269887/status/1814375909991883075
        let post =
            """"""
            |>Html_node.from_text
            |>general_posts.parse_single_main_twitter_post   
        
        ``parse a post with an audio space`` post
    
    
    
    let ``parse a post which quotes another post with an audio space``
        post //https://x.com/tehprom269887/status/1814376074089783568
        =
        post
        |>Main_post.external_source
        |>function
        |Some (Quoted_message quotation) ->
            
            quotation.audio_space
            |>function
            |Some space ->
                space.title
                |>should equal "🚨@XSPACES Q & A FEEDBACK W/ 𝕏 LEAD MEDIA ENGINEER @marmars"
                space.host
                |>should equal "Diligent Denizen"
                space.audience_amount
                |>should equal 2700
            |None->raise (Bad_post_exception("no twitter audio space in quotation"))
        
            quotation.media_load
            |>should be Empty
            
        |wrong_source -> raise (Bad_post_exception($"this post should contain a quotation but here's {wrong_source}"))
        
        
    let ``locally parse a post which quotes another post with an audio space``() =
        //https://x.com/tehprom269887/status/1814376074089783568
        let post =
            """"""
            |>Html_node.from_text
            |>general_posts.parse_single_main_twitter_post   
        
        ``parse a post which quotes another post with an audio space`` post
        
        
    let ``parse a post with an audio-space and a quoted post which has images``
        post //https://x.com/tehprom269887/status/1814377370654257441
        =    
        post
        |>Main_post.audio_space
        |>function
        |Some space ->
            space.title
            |>should equal "🚨@XSPACES Q & A FEEDBACK W/ 𝕏 LEAD MEDIA ENGINEER @marmars"
            space.host
            |>should equal "Diligent Denizen"
            space.audience_amount
            |>should equal 2700
        |None->raise (Bad_post_exception("no audio space in the main post"))
        
        post
        |>Main_post.external_source
        |>function
        |Some (Quoted_message quotation) ->
            
            quotation.audio_space
            |>should equal None
            
            quotation.message
            |>Post_message.text
            |>should equal "an image-reply to my image-post"
            
            quotation.media_load
            |>should equal [
                Media_item.Image {
                    url="https://pbs.twimg.com/media/F61myJuWkAArDgv"
                    description = "Image" 
                }
                Media_item.Image {
                    url="https://pbs.twimg.com/media/F61mz2MWsAAN5CY"
                    description = "Image" 
                }
            ]
            
        |wrong_source -> raise (Bad_post_exception($"this post should contain a quotation but here's {wrong_source}"))  
        
        
    let ``locally parse a post with an audio-space and a quoted post which has images``() =
        //[audio-space][quotation[images]]
        //https://x.com/tehprom269887/status/1814377370654257441
        let post =
            """"""
            |>Html_node.from_text
            |>general_posts.parse_single_main_twitter_post   
        
        ``parse a post with an audio-space and a quoted post which has images`` post