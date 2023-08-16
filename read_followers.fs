namespace rvinowise.twitter

open System
open System.Configuration
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open Xunit
open canopy.classic
open rvinowise.twitter


module Read_followers =

    
    let read_community_members community_id = 
        let community_members_url = 
            $"{Twitter_settings.base_url}/i/communities/{community_id}/members"
        url community_members_url
        
        let user_web_entries = elements "li[role='listitem']"
        let users = 
            user_web_entries
            |>List.map (fun user_entry ->
                let name_field = user_entry.FindElement(By.CssSelector("a[role='link'] div > div > span > span"))
                let handle_field = user_entry.FindElement(By.CssSelector("a[role='link']"))
                let user_name = name_field.Text
                let user_url = Uri(handle_field.GetAttribute("href"))
                let user_handle = user_url.Segments|>Array.last;
                {Twitter_user.name=user_name; handle=user_handle; url=user_url}
            )
        users
        //|>List.take 4
        
    
    let read_followers_of_users 
        (users: Twitter_user list) 
        =
        users
        |>Seq.map (fun twitter_user ->
            url (twitter_user.url.ToString())
            let followers_qty_field = $"a[href='/{twitter_user.handle}/followers'] span span"
            waitForElement followers_qty_field
            let followers_qty =
                element followers_qty_field
                |>read|>int
            
            twitter_user, followers_qty
        )    

    
    let get_previous_score = 
        let unroll_all_last_posts = 
            element "[data-testid='cellInnerDiv'] div[role='button']"
        click unroll_all_last_posts 


    let test_score() = 
        [
        {Twitter_user.name="Batin";handle="r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51"; url=Uri("http://no.no")},353453535
        {Twitter_user.name="Batin1";handle="r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51"; url=Uri("http://no.no")},35345353
        {Twitter_user.name="Batin2";handle="r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51"; url=Uri("http://no.no")},353453
        {Twitter_user.name="Batin3";handle="r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51"; url=Uri("http://no.no")},35345
        {Twitter_user.name="Batin4";handle="r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51"; url=Uri("http://no.no")},3534
        {Twitter_user.name="Batin5";handle="r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51"; url=Uri("http://no.no")},3534
        {Twitter_user.name="Batin6";handle="r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51"; url=Uri("http://no.no")},3534
        {Twitter_user.name="Batin7";handle="r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51"; url=Uri("http://no.no")},353
        {Twitter_user.name="Batin8";handle="r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51"; url=Uri("http://no.no")},35
        {Twitter_user.name="Batin8";handle="r-1p0dtai r-13gxpu9 r-4qtqp9 r-yyyyoo r-wy61xf r-1d2f490 r-pd4s r-ywje51"; url=Uri("http://no.no")},3

        ]
    
    

    let read_previous_score =
        url (Twitter_settings.base_url+"/"+Settings.Username)
        element "article[data-testid='tweet']" |>click


    
    [<Fact>]
    let ``scrape members of a community``()=
        canopy.configuration.chromeDir <- System.AppContext.BaseDirectory
        
        let authorisation_cookie:Cookie =
            Cookie(
                "auth_token",
                Settings.auth_token,
                ".twitter.com",
                "/",
                DateTime.Now.AddYears(1);
            )
        
        start canopy.types.BrowserStartMode.ChromeHeadless
        url Twitter_settings.base_url
        browser.Manage().Cookies.AddCookie(authorisation_cookie)
        //Login_to_twitter.login_to_twitter
        
        Settings.transhumanist_community
        |>read_community_members
        |>read_followers_of_users
        
        //test_score()
        |>Post_on_twitter.post_followers_score_of_users

        ()



    


