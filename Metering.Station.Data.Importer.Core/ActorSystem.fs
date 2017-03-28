module Metering.Station.Data.Importer.Core.ActorSystem

open System.Threading.Tasks
open Amazon.DynamoDBv2.DocumentModel
open System.Collections.Generic
open Amazon.DynamoDBv2.Model
open System.Linq
open Akka.FSharp
open Akka.Actor

open Metering.Station.Data.Importer.Aws.AirQualityData
open DeviceActorStore

type DeviceMsg =
    | Start
    | Stop
    | StartDownloading
    | DownloadFinished of seq<AirQualitResult>
    | WorkerFinished
    | WorkerReadyToStop
    | WorkerReady
    | NoMoreWork
    | ToggleWork

type WorkerMsg =
    | DataReady
    | PrepareWorkerToStop
    | WorkToProcess of AirQualitResult


let system = System.create "system" (Configuration.defaultConfig())  

let getActor (mailbox: Actor<'a>) spawnChild prefix id = 
        let actorName = prefix + id.ToString() 
        let actorRef = mailbox.Context.Child(actorName)
        if actorRef.IsNobody() then
          spawnChild actorName id
        else 
          actorRef

let worker (mailbox: Actor<'a>) id = 
    let rec imp () =
       actor {
         let! msg = mailbox.Receive()
         match msg with 
               | DataReady -> mailbox.Context.Parent <! WorkerReady
               | WorkToProcess item ->  printfn "%s %s" mailbox.Context.Self.Path.Name (item.TimeStamp)
                                        mailbox.Context.Parent <! WorkerFinished
                                        |> ignore
               | PrepareWorkerToStop -> mailbox.Context.Parent <! WorkerReadyToStop
         return! imp ()
       }
    imp()

let deviceActor (mailbox: Actor<'a>) = 
    let deviceId = "199404a7-0771-46a2-b4ca-04c93b7ec229"
    let workerPrefix = "Worker_"
    let lastProcessedTimestamp = "1490535976"

    let spawnChild name id = spawn mailbox name <| fun childMailbox -> worker childMailbox id

    let continueWith = fun (resultAsync : Async<seq<AirQualitResult>>) -> 
                async {
                    let! result = resultAsync
                    if  Seq.isEmpty result then
                        return NoMoreWork
                    else
                        return DownloadFinished result
                }    

    let noOfWorkers = 4
    let ready = fun msg -> match msg with
                                       | Start -> mailbox.Self <! StartDownloading
                                       | StartDownloading -> mailbox.Self <! ToggleWork
                                                             match getLastTimeStamp() with
                                                                    | Some ts -> ts
                                                                    | None -> lastProcessedTimestamp 
                                                             |> getMessagesAsync deviceId noOfWorkers
                                                             |> continueWith |!> mailbox.Self |> ignore                   
                                       | WorkerReady ->  match getFromStore() with
                                                                | Many item -> mailbox.Context.Sender <! WorkToProcess item |> ignore
                                                                | Last item -> mailbox.Context.Sender <! WorkToProcess item
                                                                               mailbox.Context.Self <! StartDownloading |> ignore
                                                                | Empty ->  ()
                                                                            
                                       | NoMoreWork -> [1 .. noOfWorkers] |> Seq.iter(fun id -> (getActor mailbox spawnChild workerPrefix id) <! PrepareWorkerToStop)
                                                       mailbox.Self <! Stop
                                       | WorkerReadyToStop -> let noOfChildren = mailbox.Context.GetChildren().Count() - 1
                                                              mailbox.Context.Stop(mailbox.Context.Sender)
                                                              if noOfChildren = 0 then
                                                                mailbox.Self <! Stop
                                       | Stop -> printfn "Stopped"
                                                 //system.Terminate().Wait() |> ignore
                                       | _ -> ()

    let working = fun msg -> match msg with
                                       | DownloadFinished result ->  saveToStore result
                                                                     [1 .. noOfWorkers] |> Seq.iter(fun id -> (getActor mailbox spawnChild workerPrefix id) <! DataReady)
                                                                                        |> ignore
                                                                     mailbox.Self <! ToggleWork
                                       | NoMoreWork ->  mailbox.Self <! ToggleWork
                                       | _ -> ()
    
    let rec runningActor () =
        actor {
            let! message = mailbox.Receive ()
            ready message
            match message with
            | ToggleWork -> return! pausedActor ()
            | _ -> return! runningActor ()
        }
    and pausedActor () =
        actor {
            let! message = mailbox.Receive ()
            working message
            match message with
            | ToggleWork -> 
                mailbox.UnstashAll ()
                return! runningActor ()
            | _ ->  mailbox.Stash ()
                    return! pausedActor ()
        }
    runningActor ()

let dataImporterSystem = spawn system "deviceActor" deviceActor
