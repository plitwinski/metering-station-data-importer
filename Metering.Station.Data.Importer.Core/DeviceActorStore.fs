module Metering.Station.Data.Importer.Core.DeviceActorStore

open Metering.Station.Data.Importer.Aws.AirQualityData
open System.Collections.Generic

let private internalQueue = new Queue<AirQualitResult>()
let mutable private lastTimeStamp : Option<string> = None

type StoreResult = 
    | Many of AirQualitResult
    | Last of AirQualitResult
    | Empty

let private updateTimeStamp (items : seq<AirQualitResult>)  = items |> Seq.sortBy(fun x -> x.TimeStamp)
                                                                    |> Seq.last
                                                                    |> fun x -> lastTimeStamp <- Some(x.TimeStamp)
                                                                    |> ignore

let getLastTimeStamp = fun() -> lastTimeStamp

let saveToStore = fun items -> updateTimeStamp items
                               items |> Seq.iter(fun x -> internalQueue.Enqueue(x))

let getFromStore = fun() -> if internalQueue.Count > 0 then 
                              let item = internalQueue.Dequeue()
                              if internalQueue.Count = 0 then
                                  Last item
                              else
                                  Many item
                            else Empty


                   