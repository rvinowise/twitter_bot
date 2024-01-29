namespace rvinowise.twitter.database_schema

open System
open rvinowise.twitter


type User_attention_table() =
    override _.ToString() = "cached_user_attention"
    member _.attentive_user = "attentive_user"
    member _.target = "target"
    member _.before_datetime = "before_datetime"
    member _.attention_type = "attention_type"
    member _.absolute_amount = "absolute_amount"

[<CLIMutable>]
type Cached_user_attention_row = {
    attentive_user: User_handle
    target: User_handle
    before_datetime: DateTime
    attention_type: Attention_type
    absolute_amount: int
}

type Cached_total_user_attention_table() =
    override _.ToString() = "cached_total_user_attention"
    member _.attentive_user = "attentive_user"
    member _.before_datetime = "before_datetime"
    member _.attention_type = "attention_type"
    member _.amount = "amount"

[<CLIMutable>]
type Cached_total_user_attention_row = {
    attentive_user: User_handle
    before_datetime: DateTime
    attention_type: Attention_type
    amount: int
} 