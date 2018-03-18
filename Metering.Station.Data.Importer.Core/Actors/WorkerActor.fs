module Metering.Station.Data.Importer.Core.Actors.WorkerActor

open Akka.FSharp

open Metering.Station.Data.Importer.Core.ActorHelpers
open Metering.Station.Data.Importer.Core.Messages
open Metering.Station.Data.Importer.DataAccess.DatabaseModule
open Metering.Station.Data.Importer.Definitions.Models

let private continueWith = fun (resultAsync : Async<unit>) -> 
                async {
                    do! resultAsync
                    return ProcessingFinished
                }  

let ready (mailbox: Actor<WorkerMsg>) =  fun saveReading -> 
                                         fun msg ->
                                                match msg with
                                                    | DataReady -> mailbox.Context.Parent <! WorkerReady 
                                                                   false
                                                    | WorkToProcess item -> printfn "%s %s" (mailbox.Context.Self.Path.Parent.Name + "/" + mailbox.Context.Self.Path.Name) (item.TimeStamp)
                                                                            saveReading item.Payload |> continueWith |!> mailbox.Self |> ignore
                                                                            true
                                                    | PrepareWorkerToStop -> mailbox.Context.Parent <! WorkerReadyToStop 
                                                                             false
                                                    | _ -> false

let working (mailbox: Actor<WorkerMsg>) = fun msg -> match msg with
                                                             | ProcessingFinished -> mailbox.Context.Parent <! WorkerFinished
                                                                                     true
                                                             | _ -> false

let workerActor (settings: SystemSettings) = fun (mailbox: Actor<'a>) -> 
    let saveReading = upsertAirQualityReading settings.ConnectionString
    let becomeReady = (ready mailbox) saveReading
    let becomeWorking = working mailbox
    actorOfState mailbox becomeReady becomeWorking ()