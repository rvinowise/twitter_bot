namespace rvinowise.twitter

open System
open System.Collections.Generic
open Npgsql
open Dapper
open Xunit

open rvinowise.twitter.database.tables



module User_interaction =
        
        
    let read_likes_from_user
        (database: NpgsqlConnection)
        date_from
        date_to
        liker
        =
        database.Query<User_handle>($"
            SELECT {post.like}.liker, {post.header}.author FROM {post.like}

            inner join {post.header} on 
	            {post.like}.post = {post.header}.main_post_id
	            
            where
                {post.like}.liker = '@liker'
                and
                {post.header}.created_at > date_from
                and
                {post.header}.created_at < date_to
            ORDER BY post_like.post DESC
            ",
            {|liker=liker|}
        )
        
        
        
    [<Fact>]
    let ``try read_likes_from_user()``=
        let result = 
            read_likes_from_user
                Twitter_database.open_connection()
                DateTime.MinValue
                DateTime.Now