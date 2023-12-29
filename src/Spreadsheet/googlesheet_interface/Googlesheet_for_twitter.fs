namespace rvinowise.twitter

open System.Collections.Generic
open System.IO
open System.Threading.Tasks
open Google.Apis.Auth.OAuth2
open Google.Apis.Services

open Google.Apis.Sheets.v4
open Google.Apis.Sheets.v4.Data
open Xunit



module Googlesheet_for_twitter =
    
    let hyperlink_to_twitter_user_handle handle =
        sprintf
            """=HYPERLINK("%s", "@%s")"""
            (User_handle.url_from_handle handle)
            (handle|>User_handle.value)
            
    let hyperlink_to_twitter_user (user:Twitter_user) =
        sprintf
            """=HYPERLINK("%s", "%s")"""
            (User_handle.url_from_handle user.handle)
            user.name
            
    let cell_for_twitter_user (user:Twitter_user) =
        Cell.from_url
            user.name
            (User_handle.url_from_handle user.handle)
            
    let hyperlink_of_user
        usernames
        handle
        =
        {
            handle=handle
            name=
                usernames
                |>Map.tryFind handle
                |>Option.defaultValue (User_handle.value handle)
        }
        |>hyperlink_to_twitter_user