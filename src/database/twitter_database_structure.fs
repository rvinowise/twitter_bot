namespace rvinowise.twitter.database

open System
open rvinowise.twitter

[<CLIMutable>]
type Db_twitter_user = {
    handle: string
    name: string
}

[<CLIMutable>]
type Amount_for_user = {
    datetime: DateTime
    user_handle: User_handle
    amount: int
}

module Amount_for_user =
    let empty user =
        {
            datetime=DateTime.MinValue
            user_handle = user
            amount=0
        }

module tables =

    type Social_activity_amounts =
        |Followers
        |Followees
        |Posts
        with
        override this.ToString() =
            match this with
            |Followers -> "followers_amount"
            |Followees -> "followees_amount"
            |Posts -> "posts_amount"

    
    type Post_header() =
        
        override _.ToString() = "post_header"
        member _.author = "author"
        member _.created_at = "created_at"
        member _.is_quotation = "is_quotation"

    let post_header = Post_header()
    
    type Post_like() =
        
        override _.ToString() = "post_like"
        member _.liker = "liker"
        member _.post = "post"

    let post_like = Post_like()
    
    type Post_tables() =
        member _.quotable_message_body = "post_quotable_message_body"
        member _.header = post_header
        member _.external_url = "post_external_url"
        member _.twitter_space = "post_twitter_space"
        member _.twitter_event = "post_twitter_event"
        member _.twitter_event_in_post = "post_twitter_event_in_post"
        member _.image = "post_image"
        member _.reply = "post_reply"
        member _.stats = "post_stats"
        member _.video= "post_video"
        member _.poll_choice= "poll_choice"
        member _.poll_summary= "poll_summary"
        member _.quotable_part_of_poll= "post_quotable_poll"
        member _.repost= "post_repost"
        member _.like= post_like
        
    let post = Post_tables()
    
    type User_to_scrape() =
        override _.ToString() = "user_to_scrape"
        member _.created_at = "created_at"
        member _.handle = "handle"
        member _.taken_by = "taken_by"
        member _.status = "status"
        member _.posts_amount = "posts_amount"
        member _.likes_amount = "likes_amount"
        member _.when_taken = "when_taken"
        member _.when_completed = "when_completed"
       
        
    let user_to_scrape = User_to_scrape()
    
    
    type Account_of_matrix() =
        override _.ToString() = "account_of_matrix"
        
        member _.account = "account"
        member _.title = "title"
        
    let account_of_matrix = Account_of_matrix()
    
    type This_node() =
        override _.ToString() = "this_node"
        member _.id = "id"
    let this_node = This_node()
    
    type Browser_profile() =
        override _.ToString() = "browser_profiles"
        member _.email = "email"
        member _.worker = "worker"
        member _.last_used = "last_used"
        
    let browser_profile = Browser_profile()
    
    type User_attention() =
        override _.ToString() = "public.user_attention"
        member _.attention_type = "attention_type"
        member _.attentive_user = "attentive_user"
        member _.target = "target"
        member _.amount = "amount"
        member _.when_scraped = "when_scraped"
        
    let user_attention = User_attention()