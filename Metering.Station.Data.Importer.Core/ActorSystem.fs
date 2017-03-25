module Metering.Station.Data.Importer.Core.ActorSystem

open Akka.FSharp
open Akka.Actor

open Metering.Station.Data.Importer.Amazon.AirQualityData
open System.Threading.Tasks


let system = System.create "system" (Configuration.defaultConfig())  
type DownloadResult = { ActorId: int; Result: obj }


type ImporterMsg =  
    | Start
    | Stop
    | WorkerFinished of int
    | DownloadFinished of DownloadResult

type WorkerMsg =
    | ReadyToProcess of obj

let worker (mailbox: Actor<'a>) id = 
    let rec imp () =
       actor {
         let! msg = mailbox.Receive()
         match msg with 
               | ReadyToProcess item -> printfn "Worker %s" mailbox.Context.Self.Path.Name
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
     
    let awaitDownload (dataImporterActor: Actor<'a>) actorId  = fun (task : Task<seq<'b>>)->
        if task.IsFaulted then raise task.Exception
        if task.IsCompleted then
            dataImporterActor.Self <! DownloadFinished { Result = task.Result; ActorId = actorId }

    let lastTimestamp = 1
    let initialItemsToProcess = 4
    
    fun msg -> match msg with
                  | Start _ -> (getMessagesAsync lastTimestamp initialItemsToProcess).ContinueWith(awaitDownload mailbox -1) |> ignore
                  | WorkerFinished actorId -> (getMessagesAsync lastTimestamp initialItemsToProcess).ContinueWith(awaitDownload mailbox actorId) |> ignore
                  | DownloadFinished result -> match result.ActorId with
                                                      | -1 -> match result.Result with
                                                                    | :? seq<obj> -> (result.Result :?> seq<obj>) |> Seq.iteri(fun i x -> (getActor i) <! ReadyToProcess x) |> ignore
                                                                    | _ -> printfn "No mathing results"
                                                      | actorId -> match result.Result with
                                                                    | :? seq<obj> -> (getActor result.ActorId) <! ReadyToProcess (Seq.head (result.Result :?> seq<obj>)) |> ignore
                                                                    | _ -> printfn "No mathing results"
                  | Stop _ -> system.Terminate().Wait() |> ignore

let dataImporterSystem = spawn system "dataActor" (actorOf2 dataActor)
