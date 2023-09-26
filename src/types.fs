namespace rvinowise.twitter

open System
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open Xunit
open canopy.parallell.functions
open FSharp.Configuration
open rvinowise.twitter


type User_handle = User_handle of string
type Post_id = Post_id of int64

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
    
    
    let url_from_handle handle =
        $"{Twitter_settings.base_url}/{value handle}"        
    let handle_from_url user_url =
        User_handle (Uri(user_url).Segments|>Array.last)
        
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
            
type Twitter_user = {
    handle: User_handle;
    name: string
}

module Twitter_user =
    
    
        
    let handle user =
        user.handle
        
        
    