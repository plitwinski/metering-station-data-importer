module Metering.Station.Data.Importer.Core.Actors.WorkerActor

open Akka.FSharp

open Metering.Station.Data.Importer.Core.ActorHelpers
open Metering.Station.Data.Importer.Core.DeviceActorStore
open Metering.Station.Data.Importer.Core.Messages


let workerActor (mailbox: Actor<'a>) id = 
    let rec imp () =
       actor {
         let! msg = mailbox.Receive()
         match msg with 
               | DataReady -> mailbox.Context.Parent <! WorkerReady
               | WorkToProcess item ->  printfn "%s %s" mailbox.Context.Self.Path.Name (item.TimeStamp)
                                        mailbox.Context.Parent <! WorkerFinished
                                        |> ignore
               | PrepareWorkerToStop -> mailbox.Context.Parent <! WorkerReadyToStop
               | _ -> ()
         return! imp ()
       }
    imp()