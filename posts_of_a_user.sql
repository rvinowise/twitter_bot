SELECT * FROM public.post_quotable_message_body
join post_header on post_quotable_message_body.main_post_id = post_header.main_post_id
left join post_image on post_header.main_post_id = post_image.post_id
left join post_external_url on post_header.main_post_id = post_external_url.post_id
ORDER BY post_header.created_at DESC 