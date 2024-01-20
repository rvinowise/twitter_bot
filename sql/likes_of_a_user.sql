SELECT 
	post_like.liker, post_like.post as main_post, post_like.when_scraped,
	post_header.author, post_header.is_quotation, post_header.when_written,
	post_quotable_message_body.message, post_quotable_message_body.show_more_url, post_quotable_message_body.is_abbreviated
FROM public.post_like

inner join post_header on 
	post_like.post = post_header.main_post_id
inner join post_quotable_message_body on 
	post_like.post = post_quotable_message_body.main_post_id and
	post_header.is_quotation = post_quotable_message_body.is_quotation
	
where post_like.liker = 'MaxEternalLife'
ORDER BY post_like.post DESC 