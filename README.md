# Twitter bot
Scrapes twitter pages to collect data about users.
## Use cases
### Competition of influencers
Bot ranks competitors according to their amount of followers and posts, results are shown in a google spreadsheet, 
e.g. here: https://docs.google.com/spreadsheets/d/1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU/edit#gid=1892937278
## Usage
Bot can be scheduled for an automatic launch every hour or so by Task Scheduler on windows.
Edited file appsettings.json should exist in the folder with bot's executable. 
In that file, these parameters should be provided:

**auth_token**: a cookie with the same name for www.twitter.com, from a browser which is authorised on twitter under some acount. 
Bot will log in via this account. this cookie can be seen e.g., on chrome browser in the developer panel (F12) -> Application -> Cookies.

**competitor_list**: a twitter List, from which bot reads the participants of the competition, e.g.: https://twitter.com/i/lists/1692215865779892304

**db_connection_string**: a database with tables for storing the history of scraped twitter data, e.g.: *Host=db.okfzbpxzalsvelkbfdvu.supabase.co:5432;Username=postgres;Password=my_password;Database=postgres*

**google_tables**: section points at google spread sheets into which bot will write the results. for this spreadsheet https://docs.google.com/spreadsheets/d/1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU/edit#gid=1892937278 the values are: 

**"doc_id"**: (in the url after docs.google.com/spreadsheets/d/) *"1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU"*,

**"page_id"**: (after gid=) *"1892937278"*,

**"page_name"**: (the words on the tab page) *"SF Network 𝕏 Score"*
