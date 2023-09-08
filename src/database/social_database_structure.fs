namespace rvinowise.twitter.social_database

open System

[<CLIMutable>]
type Db_twitter_user = {
    handle: string
    name: string
}

type Table_with_amounts =
    |Followers
    |Posts
    with
    override this.ToString() =
        match this with
        |Followers -> "followers_amount"
        |Posts -> "posts_amount"

[<CLIMutable>]
type Amount_for_user = {
    datetime: DateTime
    user_handle: string
    amount: int
}