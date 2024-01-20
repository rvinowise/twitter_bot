namespace rvinowise.twitter

open System
open System.Collections.Generic
open Npgsql
open Dapper
open Xunit

open rvinowise.twitter.database_schema.tables
open rvinowise.twitter.database_schema


type Attention_type =
    |Likes
    |Replies
    |Reposts
    with
    override this.ToString() =
        match this with
        |Likes -> "Likes"
        |Replies -> "Replies"
        |Reposts -> "Reposts"
        
module User_attention_from_posts =
    
    
    
    [<CLIMutable>]
    type Amount_for_account = {
        account: User_handle
        amount: int
    }
    
    let amounts_for_user_as_tuples
        (amount_for_users: Amount_for_account seq)
        =
        amount_for_users
        |>Seq.map (fun amount ->
            amount.account,amount.amount    
        )
    
    
    let read_likes_by_user
        (database: NpgsqlConnection)
        (before_datetime: DateTime)
        liker
        =
        database.Query<Amount_for_account>($"""
            select 
                {post.header.author} as account, 
                count(*) as amount 
            from {post.like}

            join {post.header} on 
	            {post.like.post} = {post.header.main_post_id}
                and {post.header.is_quotation} = false

            where
	            {post.like.liker} = @liker
                and {post.like.when_scraped} < @before_datetime 

            group by {post.header.author}

            ORDER BY amount DESC
            """,
            {|
                liker=liker
                before_datetime=before_datetime
            |}
        )|>amounts_for_user_as_tuples
        
    let ``try read_likes_by_user``()=
        let result =
            read_likes_by_user
                (Local_database.open_connection())
                DateTime.Now
                (User_handle "kristenvbrown")
        ()
        
    let read_reposts_by_user
        (database: NpgsqlConnection)
        (before_datetime: DateTime)
        reposter
        =
        database.Query<Amount_for_account>($"""
            select 
                {post.header.author} as account,
                count(*) as amount 
            from {post.repost}

            join {post.header} on 
	            {post.repost.post} = {post.header.main_post_id}
                and {post.header.is_quotation} = false

            where
	            {post.repost.reposter} = @reposter
                and {post.repost.when_scraped} < @before_datetime
            
            group by {post.header.author}

            order by amount DESC
            """,
            {|
                reposter=reposter
                before_datetime=before_datetime
            |}
        )|>amounts_for_user_as_tuples
    
    let ``try read_reposts_by_user``()=
        let result =
            read_reposts_by_user
                (Local_database.open_connection())
                DateTime.Now
                (User_handle "kristenvbrown")
        ()
    
    let read_replies_by_user
        (database: NpgsqlConnection)
        (before_datetime: DateTime)
        replier
        =
        database.Query<Amount_for_account>($"""
            select 
                {post.reply.previous_user} as account, 
                count(*) as amount 
            from {post.reply}

            join {post.header} as replying_header on
	            replying_header.{post.header.main_post_id} = {post.reply.next_post}
                and replying_header.{post.header.is_quotation} = false
            
            where 
                replying_header.{post.header.author} = @replier
                and replying_header.{post.header.when_written} < @before_datetime
            
            group by 
                {post.reply.previous_user}
            
            order by amount DESC
            """,
            {|
                replier=replier
                before_datetime=before_datetime
            |}
        )|>amounts_for_user_as_tuples
        
        
    let ``try read_replies_by_user``()=
        let result = 
            read_replies_by_user
                (Local_database.open_connection())
                DateTime.Now
                "kristenvbrown"
        ()
        
    let read_all_users
        (database: NpgsqlConnection)
        =
        database.Query<User_handle>($"""
            select 
                {post.header.author} 
            from {post.header}

            group by {post.header.author}
            """
        )
    
    
    let attention_types =
        [
            Attention_type.Likes, read_likes_by_user
            Attention_type.Reposts, read_reposts_by_user
            Attention_type.Replies, read_replies_by_user
        ]
      
    let read_all_attention_from_account
        (database: NpgsqlConnection)
        (before_datetime: DateTime)
        account
        =
        attention_types
        |>List.map(fun (attention_type, read) ->
            attention_type,
            read database before_datetime account
            |>List.ofSeq
        )
        |>Map.ofList
        
        
    