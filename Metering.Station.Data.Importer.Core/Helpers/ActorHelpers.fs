module Metering.Station.Data.Importer.Core.ActorHelpers

open Akka.FSharp
open Akka.Actor

let getActor (mailbox: Actor<'a>) spawnChild childName  = 
        let actorRef = mailbox.Context.Child(childName)
        if actorRef.IsNobody() then
          spawnChild childName
        else 
          actorRef

let actorOfState (m: Actor<'a>) (ready: 'a -> bool) (work: 'a -> bool) = fun() ->
        let rec runningActor () =
            actor {
                let! message = m.Receive ()
                let flipMode = ready message
                
                if flipMode then 
                    return! pausedActor ()
                else
                    return! runningActor ()
            }
        and pausedActor () =
            actor {
                let! message = m.Receive ()
                let flipMode = work message

                if flipMode then
                    m.UnstashAll ()
                    return! runningActor ()
                else
                    m.Stash ()
                    return! pausedActor ()
            }
        runningActor ()