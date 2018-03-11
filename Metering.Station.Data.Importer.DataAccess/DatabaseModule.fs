module Metering.Station.Data.Importer.DataAccess.DatabaseModule

open AirQualityContextModule
open Metering.Station.Data.Importer.Definitions.Models
open DataModels
open System.Threading.Tasks
open System.Data.Entity
open System.Data.Entity.Infrastructure


let private awaitTask (task : Task) =
    async {
         do! task |> Async.AwaitIAsyncResult |> Async.Ignore
         if task.IsFaulted then raise task.Exception
    }

let private awaitTaskResult (task : Task<'a>) = 
    async {
         do! task |> Async.AwaitIAsyncResult |> Async.Ignore
         if task.IsFaulted then raise task.Exception
         return task.Result
    }

let airQualityContextFactory = fun() -> new AirQualityContext()

let private mapReadingToEntity = fun (reading: AirQualityDeviceReading) -> { 
   Id = 0; 
   ClientId = reading.ClientId; 
   DeviceType = reading.DeviceType; 
   MessageId = reading.MessageId;
   CreatedDate = reading.CreatedDate;
   Location = match reading.Location with
                       | Some loc -> loc
                       | None -> null;
   PM10 = reading.PM10;
   PM25 = reading.PM25 
}

let upsertAirQualityReading (contextFactory: unit -> AirQualityContext) (reading: AirQualityDeviceReading) = 
    async {
        let upsert (context:AirQualityContext) = async {
                try
                    let! exists = context.Readings.AnyAsync(fun p -> p.MessageId = reading.MessageId) |> awaitTaskResult
                    if  exists = false then
                        let itemToInsert = mapReadingToEntity reading
                        context.Readings.Add(itemToInsert) |> ignore
                        do! context.SaveChangesAsync() |> awaitTask
                with 
                    | :? DbUpdateException as ex  -> printfn "Duplicated message id: %s" reading.MessageId
                    | _ -> printfn "Unexpected message exception"
        }
        use context = contextFactory()
        do! upsert(context)
    }