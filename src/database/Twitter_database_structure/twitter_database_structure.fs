namespace rvinowise.twitter.database

open System
open rvinowise.twitter



[<CLIMutable>]
type Amount_for_account = {
    datetime: DateTime
    account: User_handle
    amount: int
}

module Amount_for_account =
    let empty account =
        {
            datetime=DateTime.MinValue
            account = account
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
            |Followers -> "followers"
            |Followees -> "followees"
            |Posts -> "posts"

    
    type Post_header() =
        override _.ToString() = "post_header"
        member _.author = "author"
        member _.created_at = "created_at"
        member _.is_quotation = "is_quotation"
        member _.main_post_id = "main_post_id"

    
    type Post_like() =
        override _.ToString() = "post_like"
        member _.liker = "liker"
        member _.post = "post"

    
    type Post_repost() =
        override _.ToString() = "post_repost"
        member _.reposter = "reposter"
        member _.post = "post"

    
    type Post_stats() =
        override _.ToString() = "post_stats"
        member _.post_id = "post_id"
        member _.datetime = "datetime"
        member _.replies = "replies"
        member _.likes = "likes"
        member _.reposts = "reposts"
        member _.views = "views"
        member _.bookmarks = "bookmarks"

    
    type Post_reply() =
        override _.ToString() = "post_reply"
        member _.previous_post = "previous_post"
        member _.next_post = "next_post"
        member _.is_direct = "is_direct"
        member _.previous_user = "previous_user"

    type Post_external_url() =
        override _.ToString() = "post_external_url"
        member _.post_id = "post_id"
        member _.base_url = "base_url"
        member _.page = "page"
        member _.message = "message"
        member _.obfuscated_url = "obfuscated_url"
        
        
    type Post_twitter_event() =
        override _.ToString() = "post_twitter_event"
        member _.id = "id"
        member _.presenter_handle = "presenter_handle"
        member _.presenter_name = "presenter_name"
        member _.title = "title"
    
    type Post_twitter_space() = //aka Post_audio_space
        override _.ToString() = "post_audio_space"
        member _.main_post_id = "main_post_id"
        member _.host = "host"
        member _.title = "title"
        member _.audience_amount = "audience_amount"
        member _.is_quotation = "is_quotation"
    
    type Post_twitter_event_in_post() =
        override _.ToString() = "post_twitter_event_in_post"
        member _.main_post_id = "main_post_id"
        member _.event_id = "event_id"
    
    type Post_image() =
        override _.ToString() = "post_image"
        member _.post_id = "post_id"
        member _.url = "url"
        member _.description = "description"
        member _.sorting_index = "sorting_index"
        member _.is_quotation = "is_quotation"
 
    
    type Post_video() =
        override _.ToString() = "post_video"
        member _.post_id = "post_id"
        member _.url = "url"
        member _.sorting_index = "sorting_index"
        member _.is_quotation = "is_quotation"
        member _.subtitles = "subtitles"
    
    type Poll_choice() =
        override _.ToString() = "poll_choice"
        member _.post_id = "post_id"
        member _.text = "text"
        member _.votes_percent = "votes_percent"
    
    type Poll_summary() =
        override _.ToString() = "poll_summary"
        member _.post_id = "post_id"
        member _.votes_amount = "votes_amount"
    
    type Quotable_part_of_poll() =
        override _.ToString() = "post_quotable_poll"
        member _.post_id = "post_id"
        member _.question = "question"
        member _.is_quotation = "is_quotation"
    
    
    type Post_quotable_message_body() =
        override _.ToString() = "post_quotable_message_body"
        member _.main_post_id = "main_post_id"
        member _.is_quotation = "is_quotation"
        member _.message = "message"
        member _.show_more_url = "show_more_url"
        member _.is_abbreviated = "is_abbreviated"
    
    type Post_tables() =
        member _.quotable_message_body = Post_quotable_message_body()
        member _.header = Post_header()
        member _.external_url = Post_external_url()
        member _.twitter_space = Post_twitter_space()
        member _.twitter_event = Post_twitter_event()
        member _.twitter_event_in_post = Post_twitter_event_in_post()
        member _.image = Post_image()
        member _.reply = Post_reply()
        member _.stats = Post_stats()
        member _.video= Post_video()
        member _.poll_choice= Poll_choice()
        member _.poll_summary= Poll_summary()
        member _.quotable_part_of_poll= Quotable_part_of_poll()
        member _.repost= Post_repost()
        member _.like= Post_like()
        
    let post = Post_tables()
    
    type User_briefing() =
        override _.ToString() = "user_briefing"
        member _.created_at = "created_at"
        member _.handle = "handle"
        member _.name = "name"
        member _.bio = "bio"
        member _.location = "location"
        member _.profession = "profession"
        member _.web_site = "web_site"
        member _.date_joined = "date_joined"
    
    let user_briefing = User_briefing()
    
    type User_name() =
        override _.ToString() = "user_name"
        member _.handle = "handle"
        member _.name = "name"
    
    let user_name = User_name()
    
    type User_visited_by_following_scraper() =
        override _.ToString() = "user_visited_by_following_scraper"
        member _.visited_at = "visited_at"
        member _.handle = "handle"
    
    let user_visited_by_following_scraper = User_visited_by_following_scraper()
    
    
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
    
    type Followers() =
        override _.ToString() = "public.followers"
        member _.updated_at = "updated_at"
        member _.id = "id"
        member _.follower = "follower"
        member _.followee = "followee"
        
    let followers = Followers()
    
    
    type Last_visited_post_in_timeline() =
        override _.ToString() = "public.last_visited_post_in_timeline"
        member _.account = "account"
        member _.post = "post"
        member _.visited_at = "visited_at"
        member _.timeline = "timeline"
        
    let last_visited_post_in_timeline = Last_visited_post_in_timeline()
    
    
    type Activity_amount() =
        override _.ToString() = "activity_amount"
        member _.account = "account"
        member _.datetime = "datetime"
        member _.activity = "activity"
        member _.amount = "amount"

    let activity_amount = Activity_amount()