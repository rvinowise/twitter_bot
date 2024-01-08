# Twitter bot
Scrapes twitter pages to collect data about users.
## Use cases

### Collecting big data about users on twitter 

#### Prerequisites

##### 1. Browser for scraping
Install Google Chrome browser for testing, for consistent results (standard google chrome can also work):
https://googlechromelabs.github.io/chrome-for-testing/

Create a dedicated folder with the user profile, and start the browser with it:
**chrome.exe --user-data-dir="C:/Users/my_name/AppData/Local/Google/Chrome for Testing/my_profile_folder"**
Prepare plugins for that profile, if needed 
(e.g.: 
VPN: https://chromewebstore.google.com/detail/%D0%BE%D0%B1%D1%85%D0%BE%D0%B4-%D0%B1%D0%BB%D0%BE%D0%BA%D0%B8%D1%80%D0%BE%D0%B2%D0%BE%D0%BA-%D1%80%D1%83%D0%BD%D0%B5%D1%82%D0%B0/npgcnondjocldhldegnakemclmfkngch?hl=ru&pli=1
adBlock: https://chromewebstore.google.com/detail/adblock-plus-free-ad-bloc/cfhdojbkjhnklbpkdaibdccddilifddb
)
Log in to twitter by using a dedicated account for that profile. After creating a twitter account, you need to use it as a human for a while, because twitter will show pop-up windows, and give captchas for a while. When they stop showing - the account can be used by the scraper.

Specify these paths in the appsetting.json (which should be placed together with the executable):
"browser": {
        "headless": "true",
        "path":"C:/some_path/chrome-win64/chrome.exe",
        "webdriver_version":"119.0.6045.105",
        "profiles_root":"C:/Users/my_name/AppData/Local/Google/Chrome for Testing/",
        "profiles": [
            "my_email@example.com",
            "my_second_email@example.com"
        ]
    },

For the appsettings above, there should be two folders with profiles (they will be rotated when the twitter account of the previous profile reaches its limit):
C:/Users/my_name/AppData/Local/Google/Chrome for Testing/my_email
C:/Users/my_name/AppData/Local/Google/Chrome for Testing/my_second_email

##### 2. Databases
install PostgreSQL:
https://www.postgresql.org/download/

import the schema into the local database in which scraped data will be stored (it can be done in pgAdmin which is shipped with postgres).

In the appsettings, write two connection lines: 
**"central_db"** - for the shared database, which distributes tasks among working nodes
**"db_connection_string"** - your local database



### Competition of influencers
Bot ranks competitors according to their amount of followers and posts, results are shown in a google spreadsheet, 
e.g. here: https://docs.google.com/spreadsheets/d/1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU/edit#gid=1892937278
## Usage
Bot can be scheduled for an automatic launch every hour or so by Task Scheduler on windows.
The file appsettings.json should be edited and put in the folder with bot's executable. 
In that file, the following parameters should be provided:

**auth_token**: a cookie with the same name for www.twitter.com, from a browser which is authorised on twitter under some acount. 
Bot will log in via this account. this cookie can be seen e.g., in the developer panel of the chrome browser (F12) -> Application -> Cookies.

**competitor_list**: a twitter List, from which bot reads the participants of the competition, e.g., for https://twitter.com/i/lists/1692215865779892304 it will be *"1692215865779892304"*

**db_connection_string**: a database with tables for storing the history of scraped twitter data, e.g.: *"Host=db.okfzbpxzalsvelkbfdvu.supabase.co:5432;Username=postgres;Password=my_password;Database=postgres"*

**google_tables**: section points at google spread sheets into which bot will write the results. for this spreadsheet https://docs.google.com/spreadsheets/d/1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU/edit#gid=1892937278 the values are: 

**"doc_id"**: (in the url after docs.google.com/spreadsheets/d/) *"1d39R9T4JUQgMcJBZhCuF49Hm36QB1XA6BUwseIX-UcU"*,

**"page_id"**: (after gid=) *"1892937278"*,

**"page_name"**: (the words on the tab page) *"SF Network 𝕏 Score"*

The file with a google-api-secret for accessing spreadsheets should be obtained by registering the bot in the google console. This file should be put together with the executable.
