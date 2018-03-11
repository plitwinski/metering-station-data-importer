module Metering.Station.Data.Importer.Core.Actors.DeviceActor

open Akka.FSharp

open Metering.Station.Data.Importer.Core.ActorHelpers
open Metering.Station.Data.Importer.Core.ActorStore
open Metering.Station.Data.Importer.Aws.AirQualityData
open Metering.Station.Data.Importer.Core.Messages
open Metering.Station.Data.Importer.Core.Actors.WorkerActor
open Metering.Station.Data.Importer.Definitions.Models

[<Literal>]
let private workerPrefix = "Worker_"

[<Literal>]
let private markerCategory = "Devices"

[<Literal>]
let private noOfWorkers = 4

let deviceActor deviceId (settings: SystemSettings) = fun (mailbox: Actor<DeviceMsg>) -> 
    let spawnChild name = spawn mailbox name <| fun childMailbox -> workerActor settings childMailbox

    let continueWithSync = fun (result : seq<AirQualityResult>) -> 
                if  Seq.isEmpty result then
                        NoMoreWorkDevice
                    else
                        DownloadFinishedDevice result  

    let parseToOptionInt (str: option<string>) : option<int> = 
           match str with
                | Some null -> None
                | Some s -> Some (int s)
                | None -> None
    
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
                                                                                     |> parseToOptionInt
                                                                                     |> getMessages getAmazonDynamoDb deviceId noOfWorkers
                                                                                     |> continueWithSync 
                                                                                     |> fun m -> mailbox.Self <! m
                                                                                     |> ignore                   
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