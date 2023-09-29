namespace rvinowise.twitter





module Twitter_settings =

    let base_url = "https://twitter.com"

    let max_post_length = 280

    let absolute_url (relative_url:string) =
        if relative_url[0]='/' then
            base_url+relative_url
        else
            base_url+"/"+relative_url