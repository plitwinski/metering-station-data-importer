﻿module Metering.Station.Data.Importer.Core.ActorHelpers

open Akka.FSharp
open Akka.Actor

let getActor (mailbox: Actor<'a>) spawnChild childName  = 
        let actorRef = mailbox.Context.Child(childName)
        if actorRef.IsNobody() then
          spawnChild childName
        else 
          actorRef

let actorOfState (m: Actor<'a>) (ready: 'a -> unit) (work: 'a -> unit) becomeWorking = fun() ->
        let rec runningActor () =
            actor {
                let! message = m.Receive ()
                ready message
                
                if becomeWorking() then 
                    return! pausedActor ()
                else
                    return! runningActor ()
            }
        and pausedActor () =
            actor {
                let! message = m.Receive ()
                work message

                if becomeWorking() = false then
                    m.UnstashAll ()
                    return! runningActor ()
                else
                    m.Stash ()
                    return! pausedActor ()
            }
        runningActor ()