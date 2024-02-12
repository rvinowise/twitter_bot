namespace rvinowise.twitter

open System
open System.IO
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open canopy.types
open rvinowise.html_parsing
open rvinowise.twitter.database_schema
open rvinowise.twitter.database_schema.tables
open rvinowise.web_scraping

open FsUnit
open Xunit

module Find_users_in_text =
    
    
    let find_users_in_noisy_text
        (noisy_text: string)
        =
        noisy_text
        |>fun text -> text.Split " "
        |>Array.filter (fun word -> Seq.tryHead word = Some '@')
        |>Array.map User_handle.try_handle_from_text
        |>Array.choose id
        
    let strip_noise_from_file_with_userhandles
        src_file
        =
        File.ReadAllLines src_file
        |>String.concat " "
        |>find_users_in_noisy_text
        
        
    let ``get users from noisy file``()=
        let src_file = """C:\Users\rvi\Desktop\Transhumanist_people.txt"""
        let users =
            strip_noise_from_file_with_userhandles src_file
        let dst_file = """C:\Users\rvi\Desktop\Transhumanist_people_handles.txt"""
        
        let user_handles=
            Array.map (fun handle ->
                $"""=HYPERLINK("https://twitter.com/{User_handle.value handle}", "{User_handle.value handle}")"""
            ) users
        File.WriteAllLines (dst_file, user_handles)