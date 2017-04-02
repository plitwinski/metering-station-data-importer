module Metering.Station.Data.Importer.Core.Actors.Commander

open Akka.FSharp

open DeviceActor
open Metering.Station.Data.Importer.Core.ActorStore
open Metering.Station.Data.Importer.Core.Messages
open Metering.Station.Data.Importer.Aws.AirQualityDevices
open Metering.Station.Data.Importer.Core.ActorHelpers
open Metering.Station.Data.Importer.DataAccess.DatabaseModule


[<Literal>]
let private maxNoOfParallelDevices = 2

[<Literal>]
let private devicePrefix = "Device_"

let private actorStore = new ActorStore<string>(fun item -> item)
let mutable private currentNoOfDevices = 0

let commanderActor (mailbox: Actor<'a>) =
        let spawnChild deviceId = spawn mailbox deviceId <| fun childMailbox -> deviceActor deviceId childMailbox
        let continueWith = fun (resultAsync : Async<seq<string>>) -> 
                    async {
                        let! result = resultAsync
                        if  Seq.isEmpty result then
                            return NoMoreWork
                        else
                            return DownloadFinished result
                    }    

        fun msg -> match msg with
                          | Start -> validateDatabase()
                                     mailbox.Self <! StartDownloading None
                          | StartDownloading lastDeviceId -> getDevicesListAsync maxNoOfParallelDevices lastDeviceId
                                                             |> continueWith |!> mailbox.Self |> ignore  
                          | DownloadFinished devices -> actorStore.saveToStore devices
                                                        currentNoOfDevices <- currentNoOfDevices + Seq.length(devices)
                                                        [1 .. maxNoOfParallelDevices] |> Seq.iter(fun id -> (getActor mailbox spawnChild (devicePrefix + id.ToString())) <! DeviceDataReady)              
                          | DeviceFinished -> mailbox.Context.Stop(mailbox.Context.Sender)
                                              currentNoOfDevices <- currentNoOfDevices - 1
                                              if currentNoOfDevices = 0 then
                                                  mailbox.Self <! Stop  
                          | DeviceRequestsWork -> match actorStore.getFromStore() with
                                                  | Many deviceId -> (getActor mailbox spawnChild deviceId) <! StartDevice
                                                  | Last deviceId -> (getActor mailbox spawnChild deviceId) <! StartDevice
                                                                     mailbox.Context.Self <! StartDownloading(actorStore.getLastTimeStamp())
                                                  | Empty -> ()
                                                             
                          | Stop -> printfn "Commander Stopped"
                          | _ -> ()