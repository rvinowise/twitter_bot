select 
reposter ,author, post_quotable_message_body.message, post_repost.post 
from post_repost

join post_header on post_header.main_post_id = post_repost.post
join post_quotable_message_body on post_quotable_message_body.main_post_id = post_repost.post