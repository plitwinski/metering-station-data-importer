module Metering.Station.Data.Importer.Core.Messages

open Metering.Station.Data.Importer.Aws.AirQualityData

type WorkerMsg =
    | DataReady
    | PrepareWorkerToStop
    | WorkToProcess of AirQualityResult

type DeviceMsg =
    | StartDevice 
    | StopDevice
    | DeviceDataReady
    | StartDownloadingDevice of Option<string>
    | DownloadFinishedDevice of seq<AirQualityResult>
    | WorkerFinished
    | WorkerReadyToStop
    | WorkerReady
    | NoMoreWorkDevice

type CommanderMsg =
    | Start 
    | Stop
    | StartDownloading of Option<string>
    | DownloadFinished of seq<string>
    | DeviceFinished
    | DeviceRequestsWork
    //| WorkerFinished
    //| WorkerReadyToStop
    //| WorkerReady
    | NoMoreWork