SELECT post_reply.previous_user, count(*) as amount FROM post_reply

inner join post_header as replying_header on
	replying_header.main_post_id = post_reply.next_post

where replying_header.author = 'MikhailBatin'

group by post_reply.previous_user
ORDER BY amount DESC
