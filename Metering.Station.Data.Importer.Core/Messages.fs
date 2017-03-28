module Metering.Station.Data.Importer.Core.Messages

open Metering.Station.Data.Importer.Aws.AirQualityData

type WorkerMsg =
    | DataReady
    | PrepareWorkerToStop
    | WorkToProcess of AirQualitResult

type DeviceMsg =
    | Start
    | Stop
    | StartDownloading
    | DownloadFinished of seq<AirQualitResult>
    | WorkerFinished
    | WorkerReadyToStop
    | WorkerReady
    | NoMoreWork