module Metering.Station.Data.Importer.Core.ActorSystem

open Akka.FSharp
open Metering.Station.Data.Importer.Core.Actors.Commander

let system = System.create "AirQualityMessageProcessor" (Configuration.defaultConfig()) 
let dataImporterSystem settings = spawn system "CommanderActor" (actorOf2 (commanderActor settings))
