namespace rvinowise.twitter

open System
open System.Configuration
open System.Runtime.InteropServices
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.classic
open Dapper
open Npgsql

module Database =

    let open_connection =
        let connection_string = Settings.db_connection_string
        let dataSource = NpgsqlDataSource.Create(connection_string)
        let db_connection = dataSource.OpenConnection()
        db_connection

    