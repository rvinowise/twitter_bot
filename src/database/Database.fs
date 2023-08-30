namespace rvinowise.twitter

open System
open System.Configuration
open System.Data
open System.Runtime.InteropServices
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.classic
open Dapper
open Npgsql



type Timestamp_mapper() =
    (* by default, Dapper transforms time to UTC when writing to the DB,
    but it doesn't transform it back when reading, it stays as UTC *)
    inherit SqlMapper.TypeHandler<DateTime>()
    override this.SetValue(
            parameter:IDbDataParameter ,
            value: DateTime
        )
        =
        (* this is already done by "set timezone to" *)
        parameter.Value <- value
    
    override this.Parse(value: obj) =
        let utc_offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)
        (value :?> DateTime).AddMinutes(utc_offset.TotalMinutes)  

module Database =

    let set_timezone_of_this_machine
        (connection:NpgsqlConnection)
        =
        let utc_offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).Negate()
        
//        connection.Query<DateTime>(
//            $"""set timezone to '{utc_offset.ToString()}'"""
//        )|>ignore
        //let timezone_name = "Europe/Moscow"
        connection.Query<DateTime>(
            $"""set timezone to '{utc_offset}'"""
        )|>ignore
        
    let open_connection =
        let connection_string = Settings.db_connection_string
        let dataSource = NpgsqlDataSource.Create(connection_string)
        let db_connection = dataSource.OpenConnection()
        
        set_timezone_of_this_machine db_connection
        SqlMapper.AddTypeHandler(Timestamp_mapper());
        
        db_connection

    