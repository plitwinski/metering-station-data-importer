module Metering.Station.Data.Importer.Core.Actors.WorkerActor

open Akka.FSharp

open Metering.Station.Data.Importer.Core.ActorHelpers
open Metering.Station.Data.Importer.Core.Messages
open Metering.Station.Data.Importer.DataAccess.DatabaseModule
open Metering.Station.Data.Importer.Definitions.Models
open Akka.Actor

let private continueWith = fun (resultAsync : Async<unit>) -> 
                async {
                    do! resultAsync
                    return ProcessingFinished
                }  

let ready (self, parent, getPath) =  fun saveReading -> 
                                         fun msg ->
                                                match msg with
                                                    | DataReady -> parent <! WorkerReady 
                                                                   false
                                                    | WorkToProcess item -> printfn "%s %s" (getPath()) (item.TimeStamp)
                                                                            saveReading item.Payload |> continueWith |!> self |> ignore
                                                                            true
                                                    | PrepareWorkerToStop -> parent <! WorkerReadyToStop 
                                                                             false
                                                    | _ -> false

let working (mailbox: Actor<WorkerMsg>) = fun msg -> match msg with
                                                             | ProcessingFinished -> mailbox.Context.Parent <! WorkerFinished
                                                                                     true
                                                             | _ -> false

let workerActor (settings: SystemSettings) = fun (mailbox: Actor<'a>) -> 
    let saveReading = upsertAirQualityReading settings.ConnectionString
    let getPath = fun () -> (mailbox.Context.Self.Path.Parent.Name + "/" + mailbox.Context.Self.Path.Name)
    let becomeReady = (ready (mailbox.Self, mailbox.Context.Parent, getPath)) saveReading
    let becomeWorking = working mailbox
    actorOfState mailbox becomeReady becomeWorking ()