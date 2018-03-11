module Metering.Station.Data.Importer.Aws.AirQualityDevices

open Amazon.S3
open Amazon.S3.Model
open System.Threading.Tasks

let private awaitTask (task : Task<'a>) =
    async {
        do! task |> Async.AwaitIAsyncResult |> Async.Ignore
        if task.IsFaulted then raise task.Exception
        return task.Result
    }

let getAmazonS3Client = fun() -> new AmazonS3Client(Amazon.RegionEndpoint.EUWest1)

let getDevicesListAsync maxDevices marker = 
    async {
        let request = new ListObjectsRequest()
        request.BucketName <- "air-quality-meter-devices"
        request.Marker <- match marker with
                                | Some m -> m
                                | None -> null
        request.MaxKeys <- maxDevices
        use client = getAmazonS3Client()
        let! response = awaitTask (client.ListObjectsAsync(request))
        let result = response.S3Objects |> Seq.map (fun x -> x.Key)
        return result
    }
    

                                            
                                            
