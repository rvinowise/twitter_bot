﻿namespace rvinowise.twitter

open System
open System.Collections.Generic
open Npgsql
open Dapper
open Dapper
open Xunit

open rvinowise.twitter.database_schema.tables
open rvinowise.twitter.database_schema



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
    
    let total_known_amounts_as_tuples
        (users_attention: Cached_total_user_attention_row seq)
        =
        users_attention
        |>Seq.map (fun total_attention ->
            total_attention.attentive_user,total_attention.amount    
        )
    
    let attention_as_maps
        (users_attention: Cached_user_attention_row seq)
        =
        users_attention
        |>Seq.groupBy _.attentive_user
        |>Seq.map (fun (attentive_user, amount_for_targets) ->
            attentive_user,
            amount_for_targets
            |>Seq.map(fun attention_of_user -> attention_of_user.target, attention_of_user.absolute_amount)
            |>Map.ofSeq
        )|>Map.ofSeq
    
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
                and {post.like}.{post.like.when_scraped} < @before_datetime 

            group by {post.header.author}

            ORDER BY amount DESC
            """,
            {|
                liker=liker
                before_datetime=before_datetime
            |}
        )|>amounts_for_user_as_tuples
    
    
    let read_total_likes_by_user
        (database: NpgsqlConnection)
        (before_datetime: DateTime)
        liker
        =
        database.QuerySingleOrDefault<int>($"""
            select 
                count(*) as amount 
            from {post.like}

            where
	            {post.like.liker} = @liker
                and {post.like.when_scraped} < @before_datetime 
            """,
            {|
                liker=liker
                before_datetime=before_datetime
            |}
        )
    
    let sql_condition_filtering_only_matrix_members
        attentive_user
        target_of_attention
        =
        $"""
        --the attentive user should be part of the desired matrix
        {Twitter_user_database.sql_account_should_be_inside_matrix attentive_user}
        
        --the target of attention should be part of the desired matrix
        and {Twitter_user_database.sql_account_should_be_inside_matrix target_of_attention}
        """
    
    let read_likes_inside_matrix
        (database: NpgsqlConnection)
        matrix_title
        (before_datetime: DateTime)
        =
        database.Query<Cached_user_attention_row>($"""
            select 
                {post.like.liker} as {user_attention.attentive_user},
                {post.header}.{post.header.author} as {user_attention.target}, 
                count(*) as {user_attention.absolute_amount} 
            from {post.like} as main_attention

            join {post.header} on 
	            main_attention.{post.like.post} = {post.header}.{post.header.main_post_id}
                and {post.header.is_quotation} = false

            where
                {sql_condition_filtering_only_matrix_members ("main_attention."+post.like.liker) (string post.header+"."+post.header.author) }
                and main_attention.{post.like.when_scraped} < @before_datetime 

            group by 
                {user_attention.attentive_user}, {user_attention.target}

            ORDER BY amount DESC
            """,
            {|
                matrix_title=string matrix_title
                before_datetime=before_datetime
            |}
        )|>attention_as_maps
    
    let read_total_known_likes_of_matrix_members
        (database: NpgsqlConnection)
        matrix_title
        (before_datetime: DateTime)
        =
        database.Query<Cached_total_user_attention_row>($"""
            select 
                {post.like.liker} as {total_user_attention.attentive_user},
                count(*) as {total_user_attention.amount} 
            from {post.like} as main_attention

            join {post.header} on 
	            main_attention.{post.like.post} = {post.header}.{post.header.main_post_id}
                and {post.header.is_quotation} = false

            where
                {Twitter_user_database.sql_account_should_be_inside_matrix
                     ("main_attention."+post.like.liker)}
                and main_attention.{post.like.when_scraped} < @before_datetime 

            group by 
                {total_user_attention.attentive_user}

            ORDER BY amount DESC
            """,
            {|
                matrix_title=string matrix_title
                before_datetime=before_datetime
            |}
        )|>total_known_amounts_as_tuples
        
    let ``try read_likes``()=
        let result =
            read_total_known_likes_of_matrix_members
                (Local_database.open_connection())
                Longevity_members
                DateTime.UtcNow
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
                and {post.repost}.{post.repost.when_scraped} < @before_datetime
            
            group by {post.header.author}

            order by amount DESC
            """,
            {|
                reposter=reposter
                before_datetime=before_datetime
            |}
        )|>amounts_for_user_as_tuples
    
    let read_total_reposts_by_user
        (database: NpgsqlConnection)
        (before_datetime: DateTime)
        reposter
        =
        database.QuerySingleOrDefault<int>($"""
            select 
                count(*) as amount 
            from {post.repost}

            where
	            {post.repost.reposter} = @reposter
                and {post.repost.when_scraped} < @before_datetime
            """,
            {|
                reposter=reposter
                before_datetime=before_datetime
            |}
        )
    
    let read_reposts_inside_matrix
        (database: NpgsqlConnection)
        matrix_title
        (before_datetime: DateTime)
        =
        database.Query<Cached_user_attention_row>($"""
            select 
                {post.repost.reposter} as {user_attention.attentive_user},
                {post.header.author} as {user_attention.target},
                count(*) as {user_attention.absolute_amount} 
            from {post.repost} as main_attention

            join {post.header} on 
	            main_attention.{post.repost.post} = {post.header}.{post.header.main_post_id}
                and {post.header.is_quotation} = false

            where
                {sql_condition_filtering_only_matrix_members ("main_attention."+post.repost.reposter) (string post.header+"."+post.header.author) }
                and main_attention.{post.repost.when_scraped} < @before_datetime
            
            group by 
                {user_attention.attentive_user}, {user_attention.target}

            order by amount DESC
            """,
            {|
                matrix_title=string matrix_title
                before_datetime=before_datetime
            |}
        )|>attention_as_maps
   
    let read_total_known_reposts_of_matrix_members
        (database: NpgsqlConnection)
        matrix_title
        (before_datetime: DateTime)
        =
        database.Query<Cached_total_user_attention_row>($"""
            select 
                {post.repost.reposter} as {total_user_attention.attentive_user},
                count(*) as {total_user_attention.amount} 
            from {post.repost} as main_attention

            join {post.header} on 
	            main_attention.{post.repost.post} = {post.header}.{post.header.main_post_id}
                and {post.header.is_quotation} = false

            where
                {Twitter_user_database.sql_account_should_be_inside_matrix
                     ("main_attention."+post.repost.reposter)}
                and main_attention.{post.repost.when_scraped} < @before_datetime
            
            group by 
                {total_user_attention.attentive_user}

            order by amount DESC
            """,
            {|
                matrix_title=string matrix_title
                before_datetime=before_datetime
            |}
        )|>total_known_amounts_as_tuples
     
    let ``try read_reposts``()=
        let result =
            read_total_known_reposts_of_matrix_members
                (Local_database.open_connection())
                Adjacency_matrix.Longevity_members
                DateTime.UtcNow
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
    
    let read_total_replies_by_user
        (database: NpgsqlConnection)
        (before_datetime: DateTime)
        replier
        =
        database.QuerySingleOrDefault<int>($"""
            select 
                count(*) as amount 
            from {post.reply}

            join {post.header} as replying_header on
	            replying_header.{post.header.main_post_id} = {post.reply.next_post}
                and replying_header.{post.header.is_quotation} = false
            
            where 
                replying_header.{post.header.author} = @replier
                and replying_header.{post.header.when_written} < @before_datetime
            
            """,
            {|
                replier=replier
                before_datetime=before_datetime
            |}
        )
    
    let read_replies_inside_matrix
        (database: NpgsqlConnection)
        matrix_title
        (before_datetime: DateTime)
        =
        database.Query<Cached_user_attention_row>($"""
            select 
                replying_header.{post.header.author} as {user_attention.attentive_user},
                {post.reply.previous_user} as {user_attention.target}, 
                count(*) as {user_attention.absolute_amount} 
            from {post.reply} as main_attention

            join {post.header} as replying_header on
	            replying_header.{post.header.main_post_id} = {post.reply.next_post}
                and replying_header.{post.header.is_quotation} = false
            
            where 
                {sql_condition_filtering_only_matrix_members ("replying_header."+post.header.author) ("main_attention."+post.reply.previous_user) }
                and replying_header.{post.header.when_written} < @before_datetime
            
            group by 
                {user_attention.attentive_user}, {user_attention.target}
            
            order by amount DESC
            """,
            {|
                matrix_title=string matrix_title
                before_datetime=before_datetime
            |}
        )|>attention_as_maps  
    
    let read_total_known_replies_of_matrix_members
        (database: NpgsqlConnection)
        matrix_title
        (before_datetime: DateTime)
        =
        database.Query<Cached_total_user_attention_row>($"""
            select 
                replying_header.{post.header.author} as {total_user_attention.attentive_user},
                count(*) as {total_user_attention.amount} 
            from {post.reply} as main_attention

            join {post.header} as replying_header on
	            replying_header.{post.header.main_post_id} = {post.reply.next_post}
                and replying_header.{post.header.is_quotation} = false
            
            where 
                {Twitter_user_database.sql_account_should_be_inside_matrix
                     ("replying_header."+post.header.author)}
                and replying_header.{post.header.when_written} < @before_datetime
            
            group by 
                {total_user_attention.attentive_user}
            
            order by amount DESC
            """,
            {|
                matrix_title=string matrix_title
                before_datetime=before_datetime
            |}
        )|>total_known_amounts_as_tuples
    
    let ``try read_attention_inside_matrix``()=
        let result = 
            read_total_known_replies_of_matrix_members
                (Local_database.open_connection())
                Adjacency_matrix.Longevity_members
                DateTime.UtcNow
        ()    
    let ``try read_replies_by_user``()=
        let result = 
            read_replies_by_user
                (Local_database.open_connection())
                DateTime.UtcNow
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
        account
        (before_datetime: DateTime)
        =
        attention_types
        |>List.map(fun (attention_type, read) ->
            attention_type,
            read database before_datetime account
            |>List.ofSeq
        )
        |>Map.ofList
        
        
    