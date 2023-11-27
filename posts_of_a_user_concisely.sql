SELECT * FROM public.post_header
join post_quotable_message_body on 
	post_quotable_message_body.main_post_id = post_header.main_post_id
	and
	post_quotable_message_body.is_quotation = post_header.is_quotation
where post_header.is_quotation = false and post_header.author='apaphilosophy'
order by post_header.created_at DESC