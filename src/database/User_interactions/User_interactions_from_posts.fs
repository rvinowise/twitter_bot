namespace rvinowise.twitter

open System
open System.Collections.Generic
open Npgsql
open Dapper
open Xunit

open rvinowise.twitter.database.tables



module User_interactions_from_posts =
        
    [<CLIMutable>]
    type Amount_for_user = {
        user: User_handle
        amount: int
    }
    
    let amounts_for_user_as_tuples
        (amount_for_users: Amount_for_user seq)
        =
        amount_for_users
        |>Seq.map (fun amount ->
            amount.user,amount.amount    
        )
    
    let read_likes_by_user
        (database: NpgsqlConnection)
        liker
        =
        database.Query<Amount_for_user>($"""
            SELECT post_header.author as user, count(*) as amount FROM post_like

            inner join post_header on 
	            post_like.post = post_header.main_post_id

            where
	            post_like.liker = @liker

            group by post_header.author

            ORDER BY amount DESC
            """,
            {|liker=liker|}
        )|>amounts_for_user_as_tuples
        
    
    let read_reposts_by_user
        (database: NpgsqlConnection)
        reposter
        =
        database.Query<Amount_for_user>($"""
            SELECT post_header.author as user, count(*) as amount FROM post_repost

            inner join post_header on 
	            post_repost.post = post_header.main_post_id

            where
	            post_repost.reposter = @reposter

            group by post_header.author

            ORDER BY amount DESC
            """,
            {|reposter=reposter|}
        )|>amounts_for_user_as_tuples
    
    let read_replies_by_user
        (database: NpgsqlConnection)
        replier
        =
        database.Query<Amount_for_user>($"""
            SELECT post_reply.previous_user as user, count(*) as amount FROM post_reply

            inner join post_header as replying_header on
	            replying_header.main_post_id = post_reply.next_post

            where replying_header.author = @replier

            group by post_reply.previous_user
            ORDER BY amount DESC
            """,
            {|replier=replier|}
        )|>amounts_for_user_as_tuples
        
        
    [<Fact(Skip="manual")>]
    let ``try read_likes_from_user``()=
        let result = 
            read_replies_by_user
                (Twitter_database.open_connection())
                "yangranat"
        ()
        
    let read_all_users
        (database: NpgsqlConnection)
        =
        database.Query<User_handle>($"""
            SELECT post_header.author FROM post_header

            group by post_header.author

            """
        )
   
    [<Fact(Skip="manual")>]
    let ``try read_all_users``()=
        let result = 
            read_all_users
                (Twitter_database.open_connection())
        ()