module Metering.Station.Data.Importer.Amazon.AirQualityData

open System.Threading.Tasks
open Amazon.DynamoDBv2
open Amazon.DynamoDBv2.DocumentModel
open System.Linq
open Amazon.DynamoDBv2.Model
open System.Collections.Generic

let getAmazonClient = fun() -> new AmazonDynamoDBClient(Amazon.RegionEndpoint.EUWest1)

let getMessagesAsync lastTimeStamp itemsToTake : Task<List<Dictionary<string, AttributeValue>>> =
                     let tableName = "air-quality-results"
                     let client = getAmazonClient()
                     let table = Table.LoadTable(client, tableName)
                     let scanFilter = ScanFilter()
                     let timeStampAttribute = new AttributeValue ()
                     timeStampAttribute.N <- lastTimeStamp
                     let values = [| timeStampAttribute |].ToList()
                     scanFilter.AddCondition("serverTime", ScanOperator.GreaterThan, values)                  
                     let scanRequest = new ScanRequest(tableName)
                     scanRequest.Limit <- itemsToTake
                     scanRequest.ScanFilter <- scanFilter.ToConditions()
                     let responseTask = client.ScanAsync(scanRequest)
                     responseTask.Wait()
                     Task.FromResult(responseTask.Result.Items)

