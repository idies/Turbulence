USE [mhddev_hamilton]
GO

DECLARE	@return_value int

EXEC	@return_value = [dbo].[StoreFilteredCutout]
		@serverName = N'gwwn1',
		@dbname = N'turbdb101',
		@codedb = N'mhddev_hamilton',
		@turbinfodb = N'turbinfo',
		@outputdb = N'mhddev_hamilton',
		@datasetID = 5,
		@field = N'vel',
		@blobDim = 8,
		@timestep = 1,
		@filter_width = 3,
		@x_stride = 2,
		@y_stride = 2,
		@z_stride = 2,
		@components = 3,
		@QueryBox = N'[0,0,0,256,256,256]'

SELECT	'Return Value' = @return_value

GO
