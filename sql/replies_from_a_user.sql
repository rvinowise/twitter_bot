select 
	post_reply.previous_user as account, 
	count(*) as amount 
from post_reply

join post_header as replying_header on
	replying_header.main_post_id = post_reply.next_post
	and replying_header.is_quotation = false
	
where 
	replying_header.author = 'kristenvbrown'

group by 
	post_reply.previous_user
order by amount DESC