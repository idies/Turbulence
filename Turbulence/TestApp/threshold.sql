USE [turbdev_zw]
GO

DECLARE	@return_value nvarchar(max)

EXEC	@return_value = [dbo].[GetThreshold]
		@datasetID = 12, 
		@serverName = N'dsp012_4_0', 
		@dbname = N'bl_zakidb001', 
		@codedb = N'turbdev_zw', 
		@cachedb = N'cachedb', 
		@turbinfodb = N'turbinfo_test',
		@turbinfoserver = N'sciserver02', 
		@tableName = N'vel', 
		@workerType = 88, 
		@blobDim = 8, 
		@timestep = 0, 
		@spatialInterp = 0, 
		@arg = 0, 
		@threshold = -500000, 
		@QueryBox = N'box[0,0,0,1,1,1]'

SELECT	@return_value as 'Return Value'

GO
