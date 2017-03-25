open Akka.FSharp

open Metering.Station.Data.Importer.Core.ActorSystem

[<EntryPoint>]
let main argv = 
    dataImporterSystem <! Start
    //dataActor <! Stop
    System.Console.ReadLine() |> ignore
    0
