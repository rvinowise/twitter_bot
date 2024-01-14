namespace rvinowise.twitter

open System
open Dapper
open DeviceId.Encoders
open DeviceId.Formatters
open Npgsql
open Xunit
open rvinowise.twitter
open rvinowise.twitter.database

open DeviceId
   
        

module This_worker =
    
    
    let write_this_worker_id
        (database:NpgsqlConnection)
        node_id
        =
        
        database.Query<string>(
            $"insert into 
                {tables.this_node} ({tables.this_node.id})
            values (@node_id)
            
            ",
            {|
                node_id = node_id
            |}
        )|>ignore
    
    

    let ``try write_this_worker_id``() =
        let result =
            write_this_worker_id
                (Twitter_database.open_connection())
                "Main_supermicro"
        ()
        
        
    let read_this_worker_id
        (database:NpgsqlConnection)
        =
        
        database.Query<string>(
            $"select 
                {tables.this_node.id}
                
            from {tables.this_node}
            "
        )|>Seq.tryHead
    
    [<Fact>]
    let ``try read_this_worker_id``() =
        let result =
            read_this_worker_id
                (Twitter_database.open_connection())
        ()
    
    let mutable memoised_id = None
    let this_worker_id
        database
        =
        match memoised_id with
        |Some node_id ->
            node_id
        |None ->
            
            match
                read_this_worker_id
                    database
            with
            |Some node_id->
                Log.info $"the id of this worker is taken from the local database: {node_id}"
                memoised_id <- Some node_id
                node_id
            |None ->
                let generated_node_id =
                    DeviceIdBuilder()
                        .AddMachineName()
                        .UseFormatter(StringDeviceIdFormatter(PlainTextDeviceIdComponentEncoder()))
                        .ToString()+"_"+DateTime.Now.ToString("yyyy-MM-dd_HH:mm")
                
                Log.info $"generating new id of this worker: {generated_node_id} and saving it in the local database"
                
                write_this_worker_id
                    database
                    generated_node_id
                    
                generated_node_id
        
