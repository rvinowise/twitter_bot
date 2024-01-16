namespace rvinowise.twitter.database_schema

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



type User_briefing_table() =
    override _.ToString() = "user_briefing"
    member _.created_at = "created_at"
    member _.handle = "handle"
    member _.name = "name"
    member _.bio = "bio"
    member _.location = "location"
    member _.profession = "profession"
    member _.web_site = "web_site"
    member _.date_joined = "date_joined"

[<CLIMutable>]
type User_briefing_row = {
    created_at: DateTime
    handle: User_handle
    name: string
    bio: string
    location: string
    profession: string
    web_site: string
    date_joined: DateTime
}

type User_name_table() =
    override _.ToString() = "user_name"
    member _.handle = "handle"
    member _.name = "name"


type User_visited_by_following_scraper_table() =
    override _.ToString() = "user_visited_by_following_scraper"
    member _.visited_at = "visited_at"
    member _.handle = "handle"



type User_to_scrape_table() =
    override _.ToString() = "user_to_scrape"
    member _.created_at = "created_at"
    member _.account = "account"
    member _.taken_by = "taken_by"
    member _.status = "status"
    member _.posts_amount = "posts_amount"
    member _.likes_amount = "likes_amount"
    member _.when_taken = "when_taken"
    member _.when_completed = "when_completed"
   
    


type Account_of_matrix_table() =
    override _.ToString() = "account_of_matrix"
    member _.account = "account"
    member _.title = "title"
    

type This_node_table() =
    override _.ToString() = "this_node"
    member _.id = "id"
type Browser_profile_table() =
    override _.ToString() = "browser_profiles"
    member _.email = "email"
    member _.worker = "worker"
    member _.last_used = "last_used"
    

type User_attention_table() =
    override _.ToString() = "public.user_attention"
    member _.attention_type = "attention_type"
    member _.attentive_user = "attentive_user"
    member _.target = "target"
    member _.amount = "amount"
    member _.when_scraped = "when_scraped"
    

type Followers_table() =
    override _.ToString() = "public.followers"
    member _.updated_at = "updated_at"
    member _.id = "id"
    member _.follower = "follower"
    member _.followee = "followee"
    


type Last_visited_post_in_timeline_table() =
    override _.ToString() = "public.last_visited_post_in_timeline"
    member _.account = "account"
    member _.post = "post"
    member _.visited_at = "visited_at"
    member _.timeline = "timeline"
    


type Activity_amount_table() =
    override _.ToString() = "activity_amount"
    member _.account = "account"
    member _.datetime = "datetime"
    member _.activity = "activity"
    member _.amount = "amount"



module tables =

    let post = Post_tables()
    let last_visited_post_in_timeline = Last_visited_post_in_timeline_table()
    let activity_amount = Activity_amount_table()
    let followers = Followers_table()
    let user_attention = User_attention_table()
    let browser_profile = Browser_profile_table()
    let this_node = This_node_table()
    let account_of_matrix = Account_of_matrix_table()
    
    let user_name = User_name_table()
    let user_briefing = User_briefing_table()
    let user_visited_by_following_scraper = User_visited_by_following_scraper_table()
    let user_to_scrape = User_to_scrape_table()
