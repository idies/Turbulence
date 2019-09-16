USE [turblib]
GO

DECLARE	@return_value Int,
		@blob varbinary(max)

EXEC	@return_value = [dbo].[GetDataCutout]
		@serverName = N'dsp038',
		@dbname = N'channeldb01',
		@codedb = N'turblib',
		@turbinfodb = N'turbinfo',
		@turbinfoserver = N'lumberjack',
		@dataset = N'vel',
		@blobDim = 8,
		@timestep = 132015,
		@QueryBox = N'box[0,0,0,1,1,1]',
		@blob = @blob OUTPUT

SELECT	@blob as N'@blob'

SELECT	@return_value as 'Return Value'


