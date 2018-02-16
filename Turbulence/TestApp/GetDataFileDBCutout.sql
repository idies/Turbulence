USE [turbdev_zw]
GO

DECLARE	@return_value Int,
		@blob varbinary(max)

EXEC	@return_value = [dbo].[GetDataFileDBCutout2]
		@serverName = N'dsp012',
		@dbname = N'bl_zakidb001',
		@codedb = N'turbdev_zw',
		@turbinfodb = N'turbinfo_test',
		@turbinfoserver = N'sciserver02',
		@field = N'vel',
		@blobDim = 8,
		@timestep = 0,
		@QueryBox = N'box[0,0,0,510,220,1]',
		@blob = @blob OUTPUT

SELECT	@blob as N'@blob'

SELECT	@return_value as 'Return Value'

GO