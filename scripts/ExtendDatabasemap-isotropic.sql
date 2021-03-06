
use turbinfo
/****** Script for SelectTopNRows command from SSMS  ******/
insert into databasemap(datasetid, datasetname, productionmachinename, ProductionDatabaseName, [HotSpareMachineName]
      ,[HotSpareDatabaseName]
      ,[CodeDatabaseName]
      ,[BackupLocation]
      ,[HotSpareActive], SliceNum, PartitionNum, minLim, maxLim, minTime, maxtime)
      
SELECT TOP 1000
      [DatasetID]
      ,[DatasetName]
      ,'dsp012'
      ,'turbdb201'
      ,[HotSpareMachineName]
      ,[HotSpareDatabaseName]
      ,[CodeDatabaseName]
      ,[BackupLocation]
      ,[HotSpareActive]
      ,201
      ,[PartitionNum]
      ,[minLim]
      ,[maxLim]
      ,10270
      ,50280
      

  FROM [turbinfo].[dbo].[DatabaseMap]
  where datasetname='isotropic1024coarse' and ProductionDatabaseName='turbdb101'
  
  