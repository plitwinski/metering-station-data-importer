module Metering.Station.Data.Importer.Core.ActorSystem

open Akka.FSharp
open Akka.Actor
open Metering.Station.Data.Importer.Aws.AirQualityData
open DeviceActorStore
open ActorHelpers
open Messages
open Metering.Station.Data.Importer.Core.Actors.DeviceActor


let system = System.create "system" (Configuration.defaultConfig())  


let dataImporterSystem = spawn system "deviceActor" (deviceActor "199404a7-0771-46a2-b4ca-04c93b7ec229")
