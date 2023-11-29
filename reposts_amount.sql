SELECT post_header.author, count(*) as reposts_amount FROM post_repost

inner join post_header on 
	post_repost.post = post_header.main_post_id

where
	post_repost.reposter = 'MikhailBatin'

group by post_header.author

ORDER BY likes_amount DESC