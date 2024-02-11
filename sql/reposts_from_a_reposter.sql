select * 
from post_repost

left join post_quotable_message_body on post_quotable_message_body.main_post_id = post_repost.post
where reposter = 'PhilosophyFrAll'