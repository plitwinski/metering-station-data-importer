module Metering.Station.Data.Importer.Core.Actors.WorkerActor

open Akka.FSharp

open Metering.Station.Data.Importer.Core.ActorHelpers
open Metering.Station.Data.Importer.Core.Messages

let private fakeDbUpdate = Async.Sleep(2000)

let workerActor = fun (mailbox: Actor<'a>) -> 
    
    let continueWith = fun (resultAsync : Async<unit>) -> 
                async {
                    do! resultAsync
                    return ProcessingFinished
                }  

    let mutable isWorking = false    
    let startWorking = fun() -> isWorking <- true
    let stopWorking = fun() -> isWorking <- false

    let ready =  fun msg -> match msg with
                                    | DataReady -> mailbox.Context.Parent <! WorkerReady
                                    | WorkToProcess item -> startWorking()
                                                            printfn "%s %s" (mailbox.Context.Self.Path.Parent.Name + "/" + mailbox.Context.Self.Path.Name) (item.TimeStamp)
                                                            fakeDbUpdate |> continueWith |!> mailbox.Self |> ignore
                                    | PrepareWorkerToStop -> mailbox.Context.Parent <! WorkerReadyToStop
                                    | _ -> ()

    let working = fun msg -> match msg with
                                     | ProcessingFinished -> stopWorking()
                                                             mailbox.Context.Parent <! WorkerFinished
                                     | _ -> ()

    actorOfState mailbox ready working (fun() -> isWorking)()