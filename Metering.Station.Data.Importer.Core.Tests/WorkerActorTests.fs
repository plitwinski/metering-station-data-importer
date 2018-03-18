module Metering.Station.Data.Importer.Core.Tests.WorkerActorTests

open Xunit
open AkkaHelpers
open Metering.Station.Data.Importer.Core.Messages
open Metering.Station.Data.Importer.Core.Actors.WorkerActor
open Metering.Station.Data.Importer.Definitions.Models
open System

let private airQualityPayload = new AirQualityDeviceReading("clientId", "deviceId","messageId", None, 1, 1, new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero))
let private testWorkItem = new AirQualityResult("deviceName", "00:00:01", airQualityPayload)

let private self = new TestCanTell()
let private parent = TestCanTell()
let private saveReading = fun _ -> async { return () }
let private getPath = fun() -> "test"

[<Fact>]
let ``when ready receives DataReady message`` = 
    let becomeReady = ready (self, parent, getPath) saveReading             
    let result = becomeReady DataReady
    Assert.False(result)

[<Fact>]
let ``when ready receives PrepareWorkerToStop message`` = 
    let becomeReady = ready (self, parent, getPath) saveReading             
    let result = becomeReady PrepareWorkerToStop
    Assert.False(result)

[<Fact>]
let ``when ready receives WorkToProcess message`` = 
    let becomeReady = ready (self, parent, getPath) saveReading             
    let result = becomeReady (WorkToProcess testWorkItem)
    Assert.True(result)





