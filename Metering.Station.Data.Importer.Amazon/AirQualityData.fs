module Metering.Station.Data.Importer.Aws.AirQualityData

open System.Threading.Tasks
open Amazon.DynamoDBv2
open Amazon.DynamoDBv2.DocumentModel
open System.Linq
open Amazon.DynamoDBv2.Model
open System.Collections.Generic

let awaitTask (task : Task<'a>) =
    async {
        do! task |> Async.AwaitIAsyncResult |> Async.Ignore
        if task.IsFaulted then raise task.Exception
        return task.Result
    }

let getAmazonClient = fun() -> new AmazonDynamoDBClient(Amazon.RegionEndpoint.EUWest1)

type AirQualityResult(deviceId, timeStamp, payload) = 
    member this.TimeStamp = timeStamp
    member this.DeviceId = deviceId
    member this.Payload : obj =  payload

let private addAttribute (values: Dictionary<string, AttributeValue>) key value applyAttribute =
                            match value with
                                   | Some v -> let attribute = new AttributeValue ()
                                               applyAttribute attribute v
                                               values.Add(key, attribute)
                                   | None -> ()

let getMessagesAsync deviceId itemsToTake lastTimeStamp = async {
                             let tableName = "air-quality-results"
                             let client = getAmazonClient()
                             let values = new Dictionary<string, AttributeValue>()
                             addAttribute values ":deviceId" deviceId (fun a -> fun v -> a.S <- v)
                             addAttribute values ":serverTime" lastTimeStamp (fun a -> fun v -> a.N <- v)
                             let queryRequest = new QueryRequest(tableName)
                             queryRequest.ExpressionAttributeValues <- values
                             queryRequest.KeyConditionExpression <- "deviceId = :deviceId"
                             match lastTimeStamp with
                                    | Some _ -> queryRequest.KeyConditionExpression <- "serverTime > :serverTime AND " + queryRequest.KeyConditionExpression
                                    | None -> ()

                             queryRequest.Limit <- itemsToTake
                             let! respone = awaitTask (client.QueryAsync(queryRequest))
                             client.Dispose()
                             return respone.Items |> Seq.map(fun x -> AirQualityResult(x.Item("deviceId").S, x.Item("serverTime").N, x.Item("payload")))
                     }
                     

