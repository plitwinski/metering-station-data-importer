module Metering.Station.Data.Importer.Amazon.AirQualityData

open System.Threading.Tasks


let getMessagesAsync lastTimeStamp itemsToTake =
                                                let result = Seq.take itemsToTake [| "Message1"; "Message2"; "Message3"; "Message4" |]
                                                Task.FromResult(result)
