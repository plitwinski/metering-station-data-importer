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

type AirQualitResult(deviceId, timeStamp, payload) = 
    member this.TimeStamp = timeStamp
    member this.DeviceId = deviceId
    member this.Payload : obj =  payload


let getMessagesAsync deviceId itemsToTake lastTimeStamp = async {
                             let tableName = "air-quality-results"
                             let client = getAmazonClient()

                             let deviceAttribute = new AttributeValue()
                             deviceAttribute.S <- deviceId
                             let timeStampAttribute = new AttributeValue ()
                             timeStampAttribute.N <- lastTimeStamp
                             let values = new Dictionary<string, AttributeValue>()
                             values.Add(":deviceId", deviceAttribute)
                             values.Add(":serverTime", timeStampAttribute)
                             let queryRequest = new QueryRequest(tableName)
                             queryRequest.ExpressionAttributeValues <- values
                             queryRequest.KeyConditionExpression <- "serverTime > :serverTime AND deviceId = :deviceId"
                             queryRequest.Limit <- itemsToTake

                             let! respone = awaitTask (client.QueryAsync(queryRequest))
                             return respone.Items |> Seq.map(fun x -> AirQualitResult(x.Item("deviceId").S, x.Item("serverTime").N, x.Item("payload")))
                     }
                     

