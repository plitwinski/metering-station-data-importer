open Akka.FSharp

open Metering.Station.Data.Importer.Core.Messages
open Metering.Station.Data.Importer.Core.ActorSystem
open Metering.Station.Data.Importer.Definitions.Models
open System
open Microsoft.Extensions.Configuration

let getSystemSettings = 
    let configuration = ConfigurationBuilder()
                                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                                .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
                                .Build()
    let airQualityConnectionString = configuration.GetConnectionString("AirQuality")
    new SystemSettings(airQualityConnectionString)

[<EntryPoint>]
let main _ = 
    let settings = getSystemSettings
    dataImporterSystem settings <! Start
    System.Console.ReadLine() |> ignore
    0
