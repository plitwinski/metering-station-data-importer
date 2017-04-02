CREATE TABLE "AirQualityReadings"
(
  "Id" serial NOT NULL,
  "ClientId" text,
  "DeviceType" text, 
  "MessageId" text, 
  "CreatedDate" timestamp with time zone, 
  "Location" text, 
  "PM10" integer, 
  "PM25" integer
);

CREATE UNIQUE INDEX "AirQualityReadings_IX_MessageIdConstraint"
  ON "AirQualityReadings"
  USING btree
  ("MessageId" COLLATE pg_catalog."default");