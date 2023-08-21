namespace rvinowise.twitter

open System
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open Xunit
open canopy.classic
open FSharp.Configuration
open rvinowise.twitter


type User_handle = User_handle of string 

module User_handle =
    let value (handle:User_handle) =
        let (User_handle value) = handle
        value
    
    let trim_atsign (value:string) =
        if Seq.head value = '@' then
            value[1..]
        else
            value
type Twitter_user = {
    handle: User_handle;
    name: string
}

module Twitter_user =
    let url user =
        $"{Twitter_settings.base_url}/{user.handle|>User_handle.value}"

    let url_from_handle handle =
        $"{Twitter_settings.base_url}/{User_handle.value handle}"