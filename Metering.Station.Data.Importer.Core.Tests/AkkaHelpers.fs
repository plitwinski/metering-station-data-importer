module AkkaHelpers

open Akka.FSharp
open Akka.Actor

type TestCanTell() =
    interface ICanTell with
        member this.Tell(message: obj, sender: IActorRef): unit = ()