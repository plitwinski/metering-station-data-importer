module Metering.Station.Data.Importer.Definitions.Models

open System

type AirQualityDeviceReading(clientId, deviceType, messageId, location, pm10, pm25, createdDate) =
     member this.ClientId: string = clientId
     member this.DeviceType: string = deviceType
     member this.MessageId: string = messageId
     member this.Location: Option<string> = location
     member this.CreatedDate: DateTimeOffset = createdDate
     member this.PM10: int = pm10
     member this.PM25: int = pm25

type AirQualityResult(deviceId, timeStamp, payload) = 
    member this.TimeStamp: string = timeStamp
    member this.DeviceId: string = deviceId
    member this.Payload : AirQualityDeviceReading =  payload