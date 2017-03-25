module Metering.Station.Data.Importer.Core.ActorSystem

open System.Threading.Tasks
open Amazon.DynamoDBv2.DocumentModel
open System.Collections.Generic
open Amazon.DynamoDBv2.Model
open System.Linq
open Akka.FSharp
open Akka.Actor

open Metering.Station.Data.Importer.Amazon.AirQualityData


type DynamoDocument = Dictionary<string,AttributeValue>
type DownloadResult = { ActorId: int; Result: seq<DynamoDocument> }
type ImporterMsg =  
    | Start
    | Stop
    | WorkerFinished of int
    | DownloadFinished of DownloadResult
type WorkerMsg =
    | ReadyToProcess of DynamoDocument


let system = System.create "system" (Configuration.defaultConfig())  

let worker (mailbox: Actor<'a>) id = 
    let rec imp () =
       actor {
         let! msg = mailbox.Receive()
         match msg with 
               | ReadyToProcess item -> printfn "Worker %s %s" mailbox.Context.Self.Path.Name (item.Item("serverTime").N)
                                        mailbox.Context.Parent <! WorkerFinished id
         return! imp ()
       }
    imp()

let dataActor (mailbox: Actor<'a>) = 
    let spawnChild name id = spawn mailbox name <| fun childMailbox -> worker childMailbox id

    let getActor id = 
        let actorName = "worker_" + id.ToString() 
        let actorRef = mailbox.Context.Child(actorName)
        if actorRef.IsNobody() then
          spawnChild actorName id
        else 
          actorRef
     
    let awaitDownload (dataImporterActor: Actor<'a>) actorId  = fun (task : Task<List<DynamoDocument>>) ->
        if task.IsFaulted then raise task.Exception
        if task.IsCompleted then
            dataImporterActor.Self <! DownloadFinished { Result = task.Result; ActorId = actorId }

    let mutable lastTimestamp = "1490440921"
    let initialItemsToProcess = 4

    let updateLastTimestamp = fun (documents: seq<DynamoDocument>) ->
        let maxValue = documents |> Seq.map(fun p -> p.Item("serverTime").N) 
                                 |> Seq.sortBy(fun p -> p)
                                 |> Seq.last
        lastTimestamp <- maxValue
        documents
    
    fun msg -> match msg with
                  | Start _ -> (getMessagesAsync lastTimestamp initialItemsToProcess).ContinueWith(awaitDownload mailbox -1) |> ignore
                  | WorkerFinished actorId -> (getMessagesAsync lastTimestamp initialItemsToProcess).ContinueWith(awaitDownload mailbox actorId) |> ignore
                  | DownloadFinished result -> match result.ActorId with
                                                      | -1 -> result.Result |> updateLastTimestamp 
                                                                            |> Seq.iteri(fun i x -> (getActor i) <! ReadyToProcess x) |> ignore
                                                      | actorId -> (getActor result.ActorId) <! ReadyToProcess (updateLastTimestamp result.Result |> Seq.head ) |> ignore
                  | Stop _ -> system.Terminate().Wait() |> ignore

let dataImporterSystem = spawn system "dataActor" (actorOf2 dataActor)
