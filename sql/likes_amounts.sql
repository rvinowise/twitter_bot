SELECT post_header.author, count(*) as likes_amount FROM post_like

inner join post_header on 
	post_like.post = post_header.main_post_id

where
	post_like.liker = 'MikhailBatin'

group by post_header.author

ORDER BY likes_amount DESC
			