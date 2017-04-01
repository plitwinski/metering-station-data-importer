module Metering.Station.Data.Importer.Core.ActorStore

open Metering.Station.Data.Importer.Aws.AirQualityData
open System.Collections.Generic
open Microsoft.FSharp.Core
open System.IO
open System


let saveMarker category markerId markerContent = let markerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, category)
                                                 if Directory.Exists(markerPath) = false  then
                                                    Directory.CreateDirectory(markerPath) |> ignore
                                                 let markerFile = Path.Combine(markerPath, markerId + ".txt")
                                                 File.WriteAllText(markerFile, markerContent)

let getMarker category markerId = let markerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, category)
                                  let markerFile = Path.Combine(markerPath, markerId + ".txt")
                                  if File.Exists(markerFile) then
                                      let marker = File.ReadAllText(markerFile)
                                      Some(marker)
                                  else
                                      None


type StoreResult<'a> = 
        | Many of 'a
        | Last of 'a
        | Empty

type ActorStore<'a>(getKey: 'a -> string) = 
    let mutable lastTimeStamp: Option<string> = None
    let internalQueue = new Queue<'a>()
                                             //based on assumption that results are always sorted 
                                             //which is true with current S3 and DynamoDB configuration
                                             //if changed sorting needs to be added
    let updateTimeStamp (items : seq<'a>)  = items |> Seq.last
                                                   |> fun x -> lastTimeStamp <- Some(getKey(x))
                                                   |> ignore

    member this.getLastTimeStamp = fun() -> lastTimeStamp

    member this.saveToStore = fun items -> updateTimeStamp items
                                           items |> Seq.iter(fun x -> internalQueue.Enqueue(x))

    member this.getFromStore = fun() -> if internalQueue.Count > 0 then 
                                          let item = internalQueue.Dequeue()
                                          if internalQueue.Count = 0 then
                                              Last item
                                          else
                                              Many item
                                        else Empty


                   