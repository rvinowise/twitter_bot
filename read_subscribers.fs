namespace rvinowise.twitter

open System
open OpenQA.Selenium
open Xunit
open canopy.classic


module Read_subscribers =

    let read_community_members community_id = 
        let communiy_members_url = 
            $"{Twitter_settings.base_url}/i/communities/{community_id}/members"
        url communiy_members_url
        
        elements "li[role='listitem'] a[role='link']"
        |>List.map (fun element ->
            element.GetAttribute("href")
        )
        |>List.distinct
        |>List.take 3 //test
        |>Set.ofList
    
    let read_followers group_members =
        group_members
        |>Set.map (fun user_url ->
            url user_url
            let user_id= Uri(user_url).Segments|>Array.last;
            let followers_qty =
                element $"a[href='/{user_id}/followers'] span span"
                |>read|>int
            
            user_id, followers_qty
        )    

    
    //let check_unlock_more_popup =
    //    try element "[aria-label='Tweet text']" << text

    
    let get_previous_score = 
        let unroll_all_last_posts = 
            element "[data-testid='cellInnerDiv'] div[role='button']"
        click unroll_all_last_posts 


    

    let test_score() = 
        [
        "Batin r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51",353453535
        "Rybin r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51",434535454
        "Batin2 r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51",353453535
        "Rybin2 r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51",434535454
        "Batin3 r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51",353453535
        "Rybin4 r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51",434535454
        "Batin5 r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51",353453535
        "Rybin5 r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51",434535454

        ]
    
    

    let read_previous_score =
        url (Twitter_settings.base_url+"/"+Login_to_twitter.username)
        element "article[data-testid='tweet']" |>click


    
    [<Fact>]
    let ``scrape members of a community``()=
        let owner_community_id = "1687958543591219334" //immortalxnetwork
        
        
        canopy.configuration.chromeDir <- System.AppContext.BaseDirectory
        

        let authorisation_cookie:Cookie =
            Cookie(
                "auth_token",
                Login_to_twitter.user_token,
                ".twitter.com",
                "/",
                DateTime.Now.AddYears(1);
            )
        
        start canopy.types.BrowserStartMode.Chrome
        //url base_url
        //browser.Manage().Cookies.AddCookie(authorisation_cookie)
        
        Login_to_twitter.login_to_twitter
        
        owner_community_id
        |>read_community_members
        |>read_followers
        
        //test_score()
        |>Post_on_twitter.post_subscribers_score
        

        ()



    


