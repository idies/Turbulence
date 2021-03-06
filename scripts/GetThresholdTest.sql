USE [turblib]
GO

DECLARE	@return_value int

EXEC	@return_value = [dbo].[GetThreshold]
		@datasetID = 4,
		@serverName = N'dsp012',
		@dbname = N'turbdb201',
		@codedb = N'turblib',
		@cachedb = N'cachedb',
		@turbinfodb = N'turbinfo',
		@tableName = N'vel',
		@workerType = 84,
		@blobDim = 8,
		@timestep = 10500,
		@spatialInterp = 0,
		@threshold = .1,
		@arg = '',
		@QueryBox = N'[190,190,190,195,195,195]'


GO


