INSERT INTO [gw01].[turbinfo].[dbo].[DatabaseMap]
           ([DatasetID]
           ,[DatasetName]
           ,[ProductionMachineName]
           ,[ProductionDatabaseName]
           ,[HotSpareMachineName]
           ,[HotSpareDatabaseName]
           ,[CodeDatabaseName]
           ,[BackupLocation]
           ,[HotSpareActive]
           ,[SliceNum]
           ,[PartitionNum]
           ,[minLim]
           ,[maxLim])
     SELECT
           7
           ,'mixing'
           ,'dsp048'
           ,'mixingdb08'
           ,NULL
           ,NULL
           ,'turblib'
           ,NULL
           ,NULL
           ,8
           ,PartitionNum
           ,minLim
           ,maxLim
     FROM turblib..PartLimits08
     WHERE sliceNum = 8
GO


