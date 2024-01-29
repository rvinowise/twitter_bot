namespace rvinowise.twitter

open System
open System.Globalization
open rvinowise.twitter


type User_handle = User_handle of string
type Post_id = Post_id of int64
type Event_id = Event_id of int64

module Post_id =
    let value (post:Post_id) =
        let (Post_id value) = post
        value

module Event_id =
    let value (value:Event_id) =
        let (Event_id value) = value
        value

module User_handle =
    let value (handle:User_handle) =
        let (User_handle value) = handle
        value
    let db_value (handle:User_handle) =
        (value handle).ToCharArray()
        
    let trim_potential_atsign (value:string) =
        if Seq.head value = '@' then
            value[1..]
        else
            value
    
    let trim_potential_slash (value:string) =
        if Seq.head value = '/' then
            value[1..]
        else
            value
    
    let url_from_handle handle =
        $"{Twitter_settings.base_url}/{value handle}"        
    let handle_from_url user_url =
        User_handle (Uri(user_url).Segments|>Array.last)
    
    let try_handle_from_text (text:string) =
        if Seq.tryHead text = Some '@' then
            text[1..]
            |>User_handle
            |>Some
        else
            None
        
    let try_handle_from_url user_url =
        try
            (Uri(user_url).Segments)
            |>Array.last
            |>User_handle
            |>Some
        with
        | :? FormatException as exc ->
            Log.error $"can't get user handle from url {user_url}: {exc}"|>ignore
            None

[<CLIMutable>]            
type Twitter_user = {
    handle: User_handle
    name: string
}

module Twitter_user =
    
    
        
    let handle user =
        user.handle
        
        

module Datetime =
    let parse_datetime format text =
        DateTime.ParseExact(
            text,
            format,
            CultureInfo.InvariantCulture
        )