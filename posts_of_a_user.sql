SELECT * FROM public.post_quotable_message_body
join post_header on 
	post_quotable_message_body.main_post_id = post_header.main_post_id and
	post_quotable_message_body.is_quotation = post_header.is_quotation
left join post_image on post_header.main_post_id = post_image.post_id and
	post_header.is_quotation = post_image.is_quotation
left join post_external_url on post_header.main_post_id = post_external_url.post_id
ORDER BY post_header.main_post_id DESC 