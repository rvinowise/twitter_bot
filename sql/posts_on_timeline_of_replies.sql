select * 

from public.post_header

inner join post_quotable_message_body on 
	post_quotable_message_body.main_post_id = post_header.main_post_id and
	post_quotable_message_body.is_quotation = post_header.is_quotation

join post_reply on
	post_reply.next_post = post_header.main_post_id
	
join post_header as addressed_header on
	addressed_header.main_post_id = post_reply.previous_post


where post_header.author = 'rejuicey'
ORDER BY post_header.main_post_id DESC 