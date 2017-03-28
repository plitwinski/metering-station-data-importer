module Metering.Station.Data.Importer.Core.ActorSystem

open Akka.FSharp
open Akka.Actor
open Metering.Station.Data.Importer.Aws.AirQualityData
open DeviceActorStore
open ActorHelpers
open Messages


let system = System.create "system" (Configuration.defaultConfig())  

let getActor (mailbox: Actor<'a>) spawnChild prefix id = 
        let actorName = prefix + id.ToString() 
        let actorRef = mailbox.Context.Child(actorName)
        if actorRef.IsNobody() then
          spawnChild actorName id
        else 
          actorRef

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

let deviceActor (mailbox: Actor<'a>) = 
    let deviceId = "199404a7-0771-46a2-b4ca-04c93b7ec229"
    let workerPrefix = "Worker_"
    let lastProcessedTimestamp = "1490535976"

    let spawnChild name id = spawn mailbox name <| fun childMailbox -> workerActor childMailbox id

    let continueWith = fun (resultAsync : Async<seq<AirQualitResult>>) -> 
                async {
                    let! result = resultAsync
                    if  Seq.isEmpty result then
                        return NoMoreWork
                    else
                        return DownloadFinished result
                }    

    let noOfWorkers = 4
    let mutable currentNoOfWorkers = noOfWorkers

    let mutable isWorking = false    
    let startWorking = fun() -> isWorking <- true
    let stopWorking = fun() -> isWorking <- false

    let ready =  fun msg -> match msg with
                                       | Start -> mailbox.Self <! StartDownloading
                                       | StartDownloading -> startWorking()
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
                                       | WorkerReadyToStop -> mailbox.Context.Stop(mailbox.Context.Sender)
                                                              currentNoOfWorkers <- currentNoOfWorkers - 1
                                                              if currentNoOfWorkers = 0 then
                                                                  mailbox.Self <! Stop
                                       | Stop -> printfn "Stopped"
                                                 //system.Terminate().Wait() |> ignore
                                       | _ -> ()

    let working = fun msg -> match msg with
                                       | DownloadFinished result ->  stopWorking()
                                                                     saveToStore result
                                                                     [1 .. noOfWorkers] |> Seq.iter(fun id -> (getActor mailbox spawnChild workerPrefix id) <! DataReady)
                                                                                        |> ignore
                                       | NoMoreWork ->  stopWorking()
                                                        [1 .. noOfWorkers] |> Seq.iter(fun id -> (getActor mailbox spawnChild workerPrefix id) <! PrepareWorkerToStop)
                                       | _ -> ()

    actorOfState mailbox ready working (fun() -> isWorking)()

let dataImporterSystem = spawn system "deviceActor" deviceActor
