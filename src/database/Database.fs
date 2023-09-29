namespace rvinowise.twitter

open System
open System.Data
open Dapper
open Npgsql



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


module Database =

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
        SqlMapper.AddTypeHandler(Timestamp_mapper()); //sometimes it's needed, sometimes not
        SqlMapper.AddTypeHandler(User_handle_mapper())
        
        db_connection
