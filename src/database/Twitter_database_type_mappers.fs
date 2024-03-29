namespace rvinowise.twitter

open System
open System.Data
open System.Data.SqlTypes
open Dapper
open Npgsql
open rvinowise.twitter


      
type Timestamp_mapper() =
    (* by default, Dapper transforms time to UTC when writing to the DB,
    but on some machines it doesn't transform it back when reading,
    it stays as UTC *)
    inherit SqlMapper.TypeHandler<DateTime>()
    override this.SetValue(
            parameter:IDbDataParameter ,
            datetime_value: DateTime
        )
        =
        let utl_value =
            if datetime_value.Kind = DateTimeKind.Utc then
                datetime_value
            else
                let utc_offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow)
                datetime_value.Add(-utc_offset)  
                
        parameter.Value <- utl_value
    
    override this.Parse(value: obj) =
        let retrieved_datetime =
            value :?> DateTime
        if retrieved_datetime.Kind = DateTimeKind.Utc then
            let utc_offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow)
            retrieved_datetime.Add(utc_offset)  
        else
            retrieved_datetime

type User_handle_mapper() =
    inherit SqlMapper.TypeHandler<User_handle>()
    override this.SetValue(
            parameter:IDbDataParameter ,
            value: User_handle
        )
        =
        parameter.Value <- User_handle.value value
    
    override this.Parse(value: obj) =
        User_handle (value :?> string) 

type Email_mapper() =
    inherit SqlMapper.TypeHandler<Email>()
    override this.SetValue(
            parameter:IDbDataParameter ,
            value: Email
        )
        =
        parameter.Value <- Email.value value
    
    override this.Parse(value: obj) =
        Email (value :?> string) 

type Scraping_user_status_mapper() =
    inherit SqlMapper.TypeHandler<Scraping_user_status>()
    override this.SetValue(
            parameter:IDbDataParameter ,
            value: Scraping_user_status
        )
        =
        parameter.Value <- Scraping_user_status.db_value value
    
    override this.Parse(value: obj) =
        Scraping_user_status.from_db_value (value :?> string) 


type Attention_type_mapper() =
    inherit SqlMapper.TypeHandler<Attention_type>()
    override this.SetValue(
            parameter:IDbDataParameter ,
            value: Attention_type
        )
        =
        parameter.Value <- string value
    
    override this.Parse(value: obj) =
        match value :?> string with
        |"Likes" -> Attention_type.Likes
        |"Replies" -> Attention_type.Replies
        |"Reposts" -> Attention_type.Reposts
        | unknown_type ->
            $"unknown attention type: {unknown_type}"
            |>SqlTypeException
            |>raise 

type Post_id_mapper() =
    inherit SqlMapper.TypeHandler<Post_id>()
    override this.SetValue(
            parameter:IDbDataParameter ,
            value: Post_id
        )
        =
        let (Post_id value) = value 
        parameter.Value <- value
    
    override this.Parse(value: obj) =
        Post_id (value :?> int64)

type Event_id_mapper() =
    inherit SqlMapper.TypeHandler<Event_id>()
    override this.SetValue(
            parameter:IDbDataParameter ,
            value: Event_id
        )
        =
        let (Event_id value) = value 
        parameter.Value <- value
    
    override this.Parse(value: obj) =
        Event_id (value :?> int64)

type Option_mapper<'T>(
        type_mapper: SqlMapper.TypeHandler<'T> 
    ) =
    inherit SqlMapper.TypeHandler<option<'T>>()

    override _.SetValue(param, optional_value) = 
        match optional_value with
        | Some value ->
            type_mapper.SetValue(param, value)
        | None ->
            param.Value <- null

    override _.Parse value =
        if isNull value || value = box DBNull.Value 
        then None
        else Some (value :?> 'T)

type Option_string_mapper() =
    inherit SqlMapper.TypeHandler<option<string>>()

    override _.SetValue(param, optional_value) = 
        match optional_value with
        | Some value ->
            param.Value <- value
        | None ->
            param.Value <- ""

    override _.Parse value =
        if isNull value || value = box DBNull.Value || value = ""
        then None
        else Some (value :?> string)



module Twitter_database_type_mappers =
    
    let set_twitter_type_handlers () =
        //SqlMapper.AddTypeHandler(Timestamp_mapper()) //sometimes it's needed, sometimes not
        SqlMapper.AddTypeHandler(User_handle_mapper())
        SqlMapper.AddTypeHandler(Email_mapper())
        SqlMapper.AddTypeHandler(Post_id_mapper())
        SqlMapper.AddTypeHandler(Event_id_mapper())
        SqlMapper.AddTypeHandler(Option_mapper<User_handle>(User_handle_mapper()))
        SqlMapper.AddTypeHandler(Option_mapper<Post_id>(Post_id_mapper()))
        SqlMapper.AddTypeHandler(Option_string_mapper())
        SqlMapper.AddTypeHandler(Scraping_user_status_mapper())
        SqlMapper.AddTypeHandler(Attention_type_mapper())
        
