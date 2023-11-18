namespace rvinowise.twitter.database

open System

[<CLIMutable>]
type Db_twitter_user = {
    handle: string
    name: string
}

[<CLIMutable>]
type Amount_for_user = {
    datetime: DateTime
    user_handle: string
    amount: int
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
        member _.quotable_core = "post_quotable_core"
        member _.external_url = "post_external_url"
        member _.image = "post_image"
        member _.reply = "post_reply"
        member _.stats = "post_stats"
        member _.video= "post_video"
        
    let post = Post()