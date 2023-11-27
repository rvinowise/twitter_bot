SELECT * FROM public.post_like

inner join post_header on 
	post_like.post = post_header.main_post_id
inner join post_quotable_message_body on 
	post_like.post = post_quotable_message_body.main_post_id and
	post_header.is_quotation = post_quotable_message_body.is_quotation
where post_like.liker = 'Nst_Egorova'
ORDER BY post_like.post DESC 