select 
post_header.author, post_quotable_message_body.message, post_like.post
from post_like

join post_header on post_header.main_post_id = post_like.post
and post_header.is_quotation = false

join post_quotable_message_body on post_header.main_post_id = post_quotable_message_body.main_post_id
and post_quotable_message_body.is_quotation = false

where post_like.liker = 'kaimicahmills'