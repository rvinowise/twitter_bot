select 
	count(*) as amount 
from post_like

where
	post_like.liker = 'rvinowise'
	and post_like.when_scraped < now()
