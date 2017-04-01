module Metering.Station.Data.Importer.Core.Messages

open Metering.Station.Data.Importer.Aws.AirQualityData

type WorkerMsg =
    | DataReady
    | PrepareWorkerToStop
    | WorkToProcess of AirQualityResult

type DeviceMsg =
    | Start 
    | Stop
    | StartDownloading of Option<string>
    | DownloadFinished of seq<AirQualityResult>
    | WorkerFinished
    | WorkerReadyToStop
    | WorkerReady
    | NoMoreWork