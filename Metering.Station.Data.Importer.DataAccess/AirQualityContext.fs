module Metering.Station.Data.Importer.DataAccess.AirQualityContextModule

open Microsoft.EntityFrameworkCore
open DataModels

type AirQualityContext (connectionString: string) =
        inherit DbContext()
 
        [<DefaultValue>]
        val mutable readings: DbSet<AirQualityReading>
        member this.Readings with get() = this.readings and set f = this.readings <- f

        //override this.OnModelCreating modelBuilder = 
        //                              modelBuilder.HasDefaultSchema("public") |> ignore
        //                              base.OnModelCreating modelBuilder
        
        override this.OnConfiguring  optionsBuilder =
            optionsBuilder.UseNpgsql(connectionString) |> ignore