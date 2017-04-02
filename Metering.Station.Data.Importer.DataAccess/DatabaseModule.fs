module Metering.Station.Data.Importer.DataAccess.DatabaseModule

open AirQualityContextModule


let validateDatabase = fun () -> let dbContextValidator (context:AirQualityContext) = context.Database.Log <- fun log -> printfn "Database  '%s'" log
                                                                                      context.Database.CreateIfNotExists() 
                                 using(new AirQualityContext())(dbContextValidator) |> printfn "database exists '%b'"
