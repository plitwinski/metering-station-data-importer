module Metering.Station.Data.Importer.DataAccess.DataModels

open System.ComponentModel.DataAnnotations
open System
open System.ComponentModel.DataAnnotations.Schema

[<CLIMutable>]
[<Table("AirQualityReadings", Schema = "public")>]
 type AirQualityReading = {
    [<Key>]Id:int
    ClientId:string
    DeviceType:string
    MessageId:string
    CreatedDate: DateTimeOffset
    Location: string
    PM10: int
    PM25: int
}