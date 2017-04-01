module Metering.Station.Data.Importer.Core.Actors.DeviceActor

open Akka.FSharp

open Metering.Station.Data.Importer.Core.ActorHelpers
open Metering.Station.Data.Importer.Core.ActorStore
open Metering.Station.Data.Importer.Aws.AirQualityData
open Metering.Station.Data.Importer.Core.Messages
open Metering.Station.Data.Importer.Core.Actors.WorkerActor

[<Literal>]
let private workerPrefix = "Worker_"

[<Literal>]
let private markerCategory = "Devices"

[<Literal>]
let private noOfWorkers = 4

let deviceActor deviceId = fun (mailbox: Actor<'a>) -> 
    let spawnChild name = spawn mailbox name <| fun childMailbox -> workerActor childMailbox

    let continueWith = fun (resultAsync : Async<seq<AirQualityResult>>) -> 
                async {
                    let! result = resultAsync
                    if  Seq.isEmpty result then
                        return NoMoreWorkDevice
                    else
                        return DownloadFinishedDevice result
                }    
    
    let lastProcessedTimestamp = getMarker markerCategory deviceId
    let mutable currentNoOfWorkers = noOfWorkers

    let mutable isWorking = false    
    let startWorking = fun() -> isWorking <- true
    let stopWorking = fun() -> isWorking <- false
    let actorStore = new ActorStore<AirQualityResult>(fun item -> item.TimeStamp)

    let ready =  fun msg -> match msg with
                                       | StartDevice -> mailbox.Self <! StartDownloadingDevice(lastProcessedTimestamp)
                                                        currentNoOfWorkers <- noOfWorkers
                                       | StartDownloadingDevice initialTimestamp -> startWorking()
                                                                                    match initialTimestamp with
                                                                                            | Some timeStamp -> Some(timeStamp)
                                                                                            | None -> actorStore.getLastTimeStamp() 
                                                                                     |> getMessagesAsync (Some(deviceId)) noOfWorkers
                                                                                     |> continueWith |!> mailbox.Self |> ignore                   
                                       | WorkerReady ->  match actorStore.getFromStore() with
                                                                | Many item -> mailbox.Context.Sender <! WorkToProcess item |> ignore
                                                                | Last item -> mailbox.Context.Sender <! WorkToProcess item
                                                                               mailbox.Context.Self <! StartDownloadingDevice None |> ignore
                                                                | Empty ->  ()                         
                                       | WorkerReadyToStop -> mailbox.Context.Stop(mailbox.Context.Sender)
                                                              currentNoOfWorkers <- currentNoOfWorkers - 1
                                                              if currentNoOfWorkers = 0 then
                                                                  mailbox.Self <! StopDevice
                                       | DeviceDataReady -> mailbox.Context.Parent <! DeviceRequestsWork
                                       | StopDevice -> match actorStore.getLastTimeStamp() with
                                                               | Some lastMarker -> saveMarker markerCategory deviceId lastMarker
                                                               | None -> printfn "No changes since last run for deviceId: '%s'" deviceId
                                                       mailbox.Context.Parent <! DeviceFinished
                                                       printfn "Device actor '%s' finished" deviceId
                                       | _ -> ()

    let working = fun msg -> match msg with
                                       | DownloadFinishedDevice result ->  stopWorking()
                                                                           actorStore.saveToStore result
                                                                           [1 .. noOfWorkers] |> Seq.iter(fun id -> (getActor mailbox spawnChild (workerPrefix + id.ToString())) <! DataReady) |> ignore
                                       | NoMoreWorkDevice ->  stopWorking()
                                                              [1 .. noOfWorkers] |> Seq.iter(fun id -> (getActor mailbox spawnChild (workerPrefix + id.ToString())) <! PrepareWorkerToStop)
                                       | _ -> ()

    actorOfState mailbox ready working (fun() -> isWorking)()