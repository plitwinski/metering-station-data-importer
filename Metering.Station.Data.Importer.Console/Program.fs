open Akka.FSharp

open Metering.Station.Data.Importer.Core.Messages
open Metering.Station.Data.Importer.Core.ActorSystem

[<EntryPoint>]
let main argv = 
    dataImporterSystem <! Start
    System.Console.ReadLine() |> ignore
    0
