namespace rvinowise.twitter.database_schema

open System
open rvinowise.twitter


type Post_header_table() =
    override _.ToString() = "post_header"
    member _.main_post_id = "main_post_id"
    member _.is_quotation = "is_quotation"
    member _.author = "author"
    member _.when_written = "when_written"
    member _.when_scraped = "when_scraped"

[<CLIMutable>]
type Post_header_row = {
    main_post_id: Post_id
    is_quotation: bool
    author: User_handle
    created_at: DateTime
}  

type Post_like_table() =
    override _.ToString() = "post_like"
    member _.liker = "liker"
    member _.post = "post"
    member _.when_scraped = "when_scraped"

[<CLIMutable>]
type Post_like_row = {
    liker: User_handle
    post: Post_id
}  

type Post_repost_table() =
    override _.ToString() = "post_repost"
    member _.reposter = "reposter"
    member _.post = "post"
    member _.when_scraped = "when_scraped"

[<CLIMutable>]
type Post_repost_row = {
    reposter: User_handle
    post: Post_id
}  


type Post_stats_table() =
    override _.ToString() = "post_stats"
    member _.post_id = "post_id"
    member _.datetime = "datetime"
    member _.replies = "replies"
    member _.likes = "likes"
    member _.reposts = "reposts"
    member _.views = "views"
    member _.bookmarks = "bookmarks"

[<CLIMutable>]
type Post_stats_row = {
    post_id: Post_id
    datetime: DateTime
    replies: int
    likes: int
    reposts: int
    views: int
    bookmarks: int
}  

type Post_reply_table() =
    override _.ToString() = "post_reply"
    member _.previous_post = "previous_post"
    member _.next_post = "next_post"
    member _.is_direct = "is_direct"
    member _.previous_user = "previous_user"

[<CLIMutable>]
type Post_reply_row = {
    previous_post: Post_id
    next_post: Post_id
    is_direct: bool
    previous_user: User_handle
}  

type Post_external_url_table() =
    override _.ToString() = "post_external_url"
    member _.post_id = "post_id"
    member _.index = "index"
    member _.base_url = "base_url"
    member _.full_url = "full_url"
    member _.page = "page"
    member _.message = "message"
    member _.obfuscated_url = "obfuscated_url"
    
[<CLIMutable>]
type Post_external_url_row = {
    post_id: Post_id
    index: int
    base_url: string
    full_url: string
    page: string
    message: string
    obfuscated_url: string
}    

    
type Post_twitter_event_table() =
    override _.ToString() = "post_twitter_event"
    member _.id = "id"
    member _.presenter_handle = "presenter_handle"
    member _.presenter_name = "presenter_name"
    member _.title = "title"

[<CLIMutable>]
type Post_twitter_event_row = {
    id: int64
    presenter_handle: User_handle
    presenter_name: string
    title: string
}

type Post_twitter_space_table() = //aka Post_audio_space
    override _.ToString() = "post_audio_space"
    member _.main_post_id = "main_post_id"
    member _.host = "host"
    member _.title = "title"
    member _.audience_amount = "audience_amount"
    member _.is_quotation = "is_quotation"

[<CLIMutable>]
type Post_twitter_space_row = {
    main_post_id: Post_id
    is_quotation: bool
    host: string
    title: string
    audience_amount: int
}

type Post_twitter_event_in_post_table() =
    override _.ToString() = "post_twitter_event_in_post"
    member _.main_post_id = "main_post_id"
    member _.event_id = "event_id"

[<CLIMutable>]
type Post_twitter_event_in_post_row = {
    main_post_id: Post_id
    event_id: Event_id
}

type Post_image_table() =
    override _.ToString() = "post_image"
    member _.post_id = "post_id"
    member _.url = "url"
    member _.description = "description"
    member _.sorting_index = "sorting_index"
    member _.is_quotation = "is_quotation"

[<CLIMutable>]
type Post_image_row = {
    post_id: Post_id
    url: string
    description: string
    sorting_index: int
    is_quotation: bool
}



type Post_video_table() =
    override _.ToString() = "post_video"
    member _.post_id = "post_id"
    member _.url = "url"
    member _.sorting_index = "sorting_index"
    member _.is_quotation = "is_quotation"
    member _.subtitles = "subtitles"

[<CLIMutable>]
type Post_video_row = {
    post_id: Post_id
    url: string
    sorting_index: int
    is_quotation: bool
    subtitles: string
}

type Poll_choice_table() =
    override _.ToString() = "poll_choice"
    member _.post_id = "post_id"
    member _.index = "index"
    member _.text = "text"
    member _.votes_percent = "votes_percent"

[<CLIMutable>]
type Poll_choice_row = {
    post_id: Post_id
    index: int
    text: string
    votes_percent: float
} with override _.ToString() = "poll_choice"


type Poll_summary_table() =
    override _.ToString() = "poll_summary"
    member _.post_id = "post_id"
    member _.votes_amount = "votes_amount"

[<CLIMutable>]
type Poll_summary_row = {
    post_id: Post_id
    votes_amount: int
}

type Quotable_part_of_poll_table() =
    override _.ToString() = "post_quotable_poll"
    member _.post_id = "post_id"
    member _.question = "question"
    member _.is_quotation = "is_quotation"

[<CLIMutable>]
type Quotable_part_of_poll_row = {
    post_id: Post_id
    is_quotation: bool
    question: string
}

type Post_quotable_message_body_table() =
    override _.ToString() = "post_quotable_message_body"
    member _.main_post_id = "main_post_id"
    member _.is_quotation = "is_quotation"
    member _.message = "message"
    member _.show_more_url = "show_more_url"
    member _.is_abbreviated = "is_abbreviated"

[<CLIMutable>]
type Post_quotable_message_body_row = {
    main_post_id: Post_id
    is_quotation: bool
    message: string
    show_more_url: string
    is_abbreviated: bool
}

type Post_tables() =
    member _.quotable_message_body = Post_quotable_message_body_table()
    member _.header = Post_header_table()
    member _.external_url = Post_external_url_table()
    member _.twitter_space = Post_twitter_space_table()
    member _.twitter_event = Post_twitter_event_table()
    member _.twitter_event_in_post = Post_twitter_event_in_post_table()
    member _.image = Post_image_table()
    member _.reply = Post_reply_table()
    member _.stats = Post_stats_table()
    member _.video= Post_video_table()
    member _.poll_choice= Poll_choice_table()
    member _.poll_summary= Poll_summary_table()
    member _.quotable_part_of_poll= Quotable_part_of_poll_table()
    member _.repost= Post_repost_table()
    member _.like= Post_like_table()



