module Metering.Station.Data.Importer.DataAccess.AirQualityContextModule

open System.Data.Entity
open DataModels


type NpgsqlConfiguration() =
       inherit DbConfiguration() 
       do 
           base.SetProviderServices("Npgsql", Npgsql.NpgsqlServices.Instance)
           base.SetProviderFactory("Npgsql", Npgsql.NpgsqlFactory.Instance)
           base.SetDefaultConnectionFactory(new Npgsql.NpgsqlConnectionFactory())


[<DbConfigurationType(typeof<NpgsqlConfiguration>)>]
type AirQualityContext() =
     inherit DbContext("AirQuality")
 
        [<DefaultValue>] val mutable readings: DbSet<AirQualityReading>
        member this.Readings with get() = this.readings and set f = this.readings <- f

        override this.OnModelCreating modelBuilder = 
                                      modelBuilder.HasDefaultSchema("public") |> ignore
                                      base.OnModelCreating modelBuilder