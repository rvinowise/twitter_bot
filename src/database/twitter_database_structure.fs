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


    type Post() =
        member _.quotable_message_body = "post_quotable_message_body"
        member _.header = "post_header"
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
        member _.like= "post_like"
        
    let post = Post()