namespace rvinowise.twitter

open OpenQA.Selenium
open canopy.classic
open canopy.parallell.functions

module Login_to_twitter =
    

    let find_login_button_with_text browser text = 
        let buttons = 
            elements "div[role='button'] div[dir='ltr'] span span" browser
        highlight "div[role='button'] div[dir='ltr'] span span" browser
        buttons
        |>List.filter(fun button ->
            button.Text = text
        )|>List.map (fun button_name_span ->
            button_name_span.FindElement(By.XPath("../../.."))
        )|>List.head

    let login_to_twitter (browser: IWebDriver) =
       url (Twitter_settings.base_url+"/i/flow/login") browser
       (element "input[name='text']" browser) << Settings.login
       let button_1 = find_login_button_with_text browser "Next" 
       click button_1 browser
       
       let password_field = element "input[name='password']" browser
       password_field << Settings.password
       //password_field.SendKeys(Keys.Enter)
//       let button_2 = element "div[role='button'] div[dir='ltr']"
//       
//       let test2 = button_2.TagName
//       let test3 = button_2.GetAttribute("role")
       click """//*[@id="react-root"]/div/div/div/main/div/div/div/div[2]/div[2]/div[2]/div/div/div[1]/div/div/div"""
