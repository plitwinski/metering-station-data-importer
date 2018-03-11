module Metering.Station.Data.Importer.Aws.AirQualityData

open Amazon.DynamoDBv2
open Metering.Station.Data.Importer.Definitions.Models
open System
open FSharp.AWS.DynamoDB

type internal AirQualityPayload = {
    clientId: string
    deviceType: string
    id: string
    localTime: DateTimeOffset
    location: string
    PM10: int
    ``PM2.5``: int
    serverTime: int
}

type internal AirQualityReading = 
    {
       [<FSharp.AWS.DynamoDB.HashKey>]
       deviceId : string
       [<FSharp.AWS.DynamoDB.RangeKey>]
       serverTime : int
       payload: AirQualityPayload
    }

let private stringToOption (n : string) = 
                               if String.IsNullOrWhiteSpace(n)
                               then None 
                               else Some n

let private extractPayload (r: AirQualityPayload) = 
    let getLocalTime localTime (serverTimeStamp: int)  = 
        let serverCreatedDate = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddSeconds((float serverTimeStamp))
        let offsetMinutes = Math.Round((localTime - serverCreatedDate).TotalMinutes % 1440.0)
        new DateTimeOffset(localTime, TimeSpan.FromMinutes(offsetMinutes))

    AirQualityDeviceReading(r.clientId, r.deviceType, r.id, stringToOption r.location, 
                            r.PM10, r.``PM2.5``, getLocalTime r.localTime.DateTime r.serverTime)

let getAmazonDynamoDb = fun() -> new AmazonDynamoDBClient(Amazon.RegionEndpoint.EUWest1)

let getMessages getAmazonDynamoDb deviceId (itemsToTake:int) lastTimeStamp = 
    let tableName = "air-quality-results"
    use client = getAmazonDynamoDb()
    let table = TableContext.Create<AirQualityReading>(client, tableName = tableName, createIfNotExists = false)
    let mutable keyQuery = <@ fun r -> r.deviceId = deviceId @>
    if lastTimeStamp = None 
    then keyQuery <- <@ fun r -> r.deviceId = deviceId @>
    else keyQuery <- <@ fun r -> r.deviceId = deviceId && r.serverTime > lastTimeStamp.Value @>
    let result = table.Query(keyCondition = keyQuery, limit = itemsToTake)
    result |> Seq.map(fun x -> AirQualityResult(x.deviceId, x.serverTime.ToString(), extractPayload(x.payload)))
                     

