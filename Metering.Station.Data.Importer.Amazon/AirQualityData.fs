module Metering.Station.Data.Importer.Aws.AirQualityData

open System.Threading.Tasks
open Amazon.DynamoDBv2
open Amazon.DynamoDBv2.DocumentModel
open System.Linq
open Amazon.DynamoDBv2.Model
open System.Collections.Generic
open Metering.Station.Data.Importer.Definitions.Models
open System

let private awaitTask (task : Task<'a>) =
    async {
        do! task |> Async.AwaitIAsyncResult |> Async.Ignore
        if task.IsFaulted then raise task.Exception
        return task.Result
    }

let private getAmazonClient = fun() -> new AmazonDynamoDBClient(Amazon.RegionEndpoint.EUWest1)

let private addAttribute (values: Dictionary<string, AttributeValue>) key value applyAttribute =
                            match value with
                                   | Some v -> let attribute = new AttributeValue ()
                                               applyAttribute attribute v
                                               values.Add(key, attribute)
                                   | None -> ()

let private extractReading (payload : AttributeValue) = let clientId = payload.M.Item("clientId").S
                                                        let deviceType = payload.M.Item("deviceType").S
                                                        let messageId = payload.M.Item("id").S
                                                        let localTime = payload.M.Item("localTime").S
                                                        let location =  if payload.M.ContainsKey("location") then  
                                                                           Some(payload.M.Item("location").S)
                                                                        else 
                                                                           None
                                                        let pm10 = if payload.M.Item("PM10").N = null then
                                                                      Convert.ToInt32(payload.M.Item("PM10").S)
                                                                   else
                                                                      Convert.ToInt32(payload.M.Item("PM10").N)
                                                        let pm25 = if payload.M.Item("PM2.5").N = null then
                                                                      Convert.ToInt32(payload.M.Item("PM2.5").S)
                                                                   else
                                                                      Convert.ToInt32(payload.M.Item("PM2.5").N)
                                                        let serverTimeStamp = Convert.ToDouble(payload.M.Item("serverTime").N)
                                                        let calculateCreateDate = let localCreatedDate = DateTime.Parse(localTime.Replace("Z", ""))
                                                                                  let serverCreatedDate = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(serverTimeStamp)
                                                                                  let offsetMinutes = Math.Round((localCreatedDate - serverCreatedDate).TotalMinutes % 1440.0)
                                                                                  new DateTimeOffset(localCreatedDate, TimeSpan.FromMinutes(offsetMinutes))
                                                        let result = AirQualityDeviceReading(clientId, deviceType, messageId, location, pm10, pm25, calculateCreateDate)
                                                        let asdasd = result.CreatedDate
                                                        result

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
                             return respone.Items |> Seq.map(fun x -> AirQualityResult(x.Item("deviceId").S, x.Item("serverTime").N, extractReading(x.Item("payload"))))
                     }
                     

