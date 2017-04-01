module Metering.Station.Data.Importer.Core.Actors.DeviceActor

open Akka.FSharp

open Metering.Station.Data.Importer.Core.ActorHelpers
open Metering.Station.Data.Importer.Core.DeviceActorStore
open Metering.Station.Data.Importer.Aws.AirQualityData
open Metering.Station.Data.Importer.Core.Messages
open Metering.Station.Data.Importer.Core.Actors.WorkerActor

[<Literal>]
let workerPrefix = "Worker_"

[<Literal>]
let markerCategory = "Devices"

[<Literal>]
let noOfWorkers = 4

let deviceActor deviceId = fun (mailbox: Actor<'a>) -> 
    let spawnChild name id = spawn mailbox name <| fun childMailbox -> workerActor childMailbox id

    let continueWith = fun (resultAsync : Async<seq<AirQualityResult>>) -> 
                async {
                    let! result = resultAsync
                    if  Seq.isEmpty result then
                        return NoMoreWork
                    else
                        return DownloadFinished result
                }    
    
    let lastProcessedTimestamp = getMarker markerCategory deviceId
    let mutable currentNoOfWorkers = noOfWorkers

    let mutable isWorking = false    
    let startWorking = fun() -> isWorking <- true
    let stopWorking = fun() -> isWorking <- false
    let actorStore = new ActorStore<AirQualityResult>(fun item -> item.TimeStamp)

    let ready =  fun msg -> match msg with
                                       | Start -> mailbox.Self <! StartDownloading(lastProcessedTimestamp)
                                                  currentNoOfWorkers <- noOfWorkers
                                       | StartDownloading initialTimestamp -> startWorking()
                                                                              match initialTimestamp with
                                                                                    | Some timeStamp -> Some(timeStamp)
                                                                                    | None -> actorStore.getLastTimeStamp() 
                                                                             |> getMessagesAsync (Some(deviceId)) noOfWorkers
                                                                             |> continueWith |!> mailbox.Self |> ignore                   
                                       | WorkerReady ->  match actorStore.getFromStore() with
                                                                | Many item -> mailbox.Context.Sender <! WorkToProcess item |> ignore
                                                                | Last item -> mailbox.Context.Sender <! WorkToProcess item
                                                                               mailbox.Context.Self <! StartDownloading None |> ignore
                                                                | Empty ->  ()                         
                                       | WorkerReadyToStop -> mailbox.Context.Stop(mailbox.Context.Sender)
                                                              currentNoOfWorkers <- currentNoOfWorkers - 1
                                                              if currentNoOfWorkers = 0 then
                                                                  mailbox.Self <! Stop
                                       | Stop -> match actorStore.getLastTimeStamp() with
                                                       | Some lastMarker -> saveMarker markerCategory deviceId lastMarker
                                                       | None -> printfn "No changes since last run for deviceId: '%s'" deviceId
                                                 printfn "Stopped"
                                       | _ -> ()

    let working = fun msg -> match msg with
                                       | DownloadFinished result ->  stopWorking()
                                                                     actorStore.saveToStore result
                                                                     [1 .. noOfWorkers] |> Seq.iter(fun id -> (getActor mailbox spawnChild workerPrefix id) <! DataReady)
                                                                                        |> ignore
                                       | NoMoreWork ->  stopWorking()
                                                        [1 .. noOfWorkers] |> Seq.iter(fun id -> (getActor mailbox spawnChild workerPrefix id) <! PrepareWorkerToStop)
                                       | _ -> ()

    actorOfState mailbox ready working (fun() -> isWorking)()