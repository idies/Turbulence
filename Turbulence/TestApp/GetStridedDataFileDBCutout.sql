USE [turblib]
GO

DECLARE	@return_value Int,
		@blob varbinary(max)

EXEC	@return_value = [dbo].[GetStridedFileDBDataCutout]
		@serverName = N'dsp012',
		@dbname = N'channel5200db101',
		@codedb = N'turblib',
		@turbinfodb = N'turbinfo_test',
		@turbinfoserver = N'sciserver02',
		@datasetID = 10,
		@field = N'vel',
		@blobDim = 8,
		@timestep = 0,
		@x_stride = 50,
		@y_stride = 50,
		@z_stride = 50,
		@QueryBox = N'box[0,0,0,511,511,511]'

SELECT	@return_value as 'Return Value'

GO