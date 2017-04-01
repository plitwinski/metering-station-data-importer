module Metering.Station.Data.Importer.Core.ActorSystem

open Akka.FSharp
open Akka.Actor
open Metering.Station.Data.Importer.Core.Actors.Commander

let system = System.create "AirQualityMessageProcessor" (Configuration.defaultConfig()) 
let dataImporterSystem = spawn system "CommanderActor" (actorOf2 commanderActor)
