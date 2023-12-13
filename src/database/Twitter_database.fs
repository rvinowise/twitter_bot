namespace rvinowise.twitter

open System
open System.Data
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
            value: DateTime
        )
        =
        (* this is already done by "set timezone to" *)
        parameter.Value <- value
    
    override this.Parse(value: obj) =
        let retrieved_datetime =
            value :?> DateTime
        if retrieved_datetime.Kind = DateTimeKind.Utc then
            let utc_offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)
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

module Twitter_database =

    let set_timezone_of_this_machine
        (connection:NpgsqlConnection)
        =
        let utc_offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).Negate()
        connection.Query<DateTime>(
            $"""set timezone to '{utc_offset}'"""
        )|>ignore
        
    let open_connection () =
        let connection_string = Settings.db_connection_string
        let data_source = NpgsqlDataSource.Create(connection_string)
        let db_connection = data_source.OpenConnection()
        
        set_timezone_of_this_machine db_connection
        SqlMapper.AddTypeHandler(Timestamp_mapper()) //sometimes it's needed, sometimes not
        SqlMapper.AddTypeHandler(User_handle_mapper())
        SqlMapper.AddTypeHandler(Post_id_mapper())
        SqlMapper.AddTypeHandler(Event_id_mapper())
        SqlMapper.AddTypeHandler(Option_mapper<User_handle>(User_handle_mapper()))
        SqlMapper.AddTypeHandler(Option_mapper<Post_id>(Post_id_mapper()))
        SqlMapper.AddTypeHandler(Option_string_mapper())
        
        db_connection
