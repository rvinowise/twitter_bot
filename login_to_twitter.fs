namespace rvinowise.twitter

open OpenQA.Selenium
open canopy.classic
open FSharp.Configuration

module Login_to_twitter =
    
    //let user_token = "18de3f465940bbd477eb025a1347819b75b4c2f9" //victor_rybin
    // let user_token = "87ff0490c374881eed1bc94f7882287acf55d3ff" //Announcer693221
    // let username = "Announcer693221"
    // let password = "SayForever"


    let find_login_button_with_text text = 
        let buttons = 
            elements "div[role='button'] div[dir='ltr'] span span"
        highlight "div[role='button'] div[dir='ltr'] span span"
        buttons
        |>List.filter(fun button ->
            button.Text = text
        )|>List.map (fun button_name_span ->
            button_name_span.FindElement(By.XPath("../../.."))
        )|>List.head

    let login_to_twitter =
       url (Twitter_settings.base_url+"/i/flow/login")
       element "input[name='text']" << Settings.Username
       let button_1 = find_login_button_with_text "Next"
       button_1 |> click
       
       let password_field = element "input[name='password']"
       password_field << Settings.Password
       //password_field.SendKeys(Keys.Enter)
//       let button_2 = element "div[role='button'] div[dir='ltr']"
//       
//       let test2 = button_2.TagName
//       let test3 = button_2.GetAttribute("role")
       click """//*[@id="react-root"]/div/div/div/main/div/div/div/div[2]/div[2]/div[2]/div/div/div[1]/div/div/div"""
