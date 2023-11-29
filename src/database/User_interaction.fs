namespace rvinowise.twitter

open System
open System.Collections.Generic
open Npgsql
open Dapper
open Xunit

open rvinowise.twitter.database.tables



module User_interaction =
        
    [<CLIMutable>]
    type Amount_for_user = {
        author: User_handle
        amount: int64
    }
        
    let read_likes_by_user
        (database: NpgsqlConnection)
        liker
        =
        database.Query<Amount_for_user>($"""
            SELECT post_header.author, count(*) as amount FROM post_like

            inner join post_header on 
	            post_like.post = post_header.main_post_id

            where
	            post_like.liker = @liker

            group by post_header.author

            ORDER BY amount DESC
            """,
            {|liker=liker|}
        )
        
    
    let read_reposts_by_user
        (database: NpgsqlConnection)
        reposter
        =
        database.Query<Amount_for_user>($"""
            SELECT post_header.author, count(*) as amount FROM post_repost

            inner join post_header on 
	            post_repost.post = post_header.main_post_id

            where
	            post_repost.reposter = @reposter

            group by post_header.author

            ORDER BY amount DESC
            """,
            {|reposter=reposter|}
        )
    
    let read_replies_by_user
        (database: NpgsqlConnection)
        replier
        =
        database.Query<Amount_for_user>($"""
            SELECT post_reply.previous_user, count(*) as amount FROM post_reply

            inner join post_header as replying_header on
	            replying_header.main_post_id = post_reply.next_post

            where replying_header.author = @replier

            group by post_reply.previous_user
            ORDER BY amount DESC
            """,
            {|replier=replier|}
        )
        
        
    [<Fact>]
    let ``try read_likes_from_user``()=
        let result = 
            read_replies_by_user
                (Twitter_database.open_connection())
                "MikhailBatin"
        ()