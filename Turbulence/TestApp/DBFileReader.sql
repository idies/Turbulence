USE [turbdev_zw]
GO

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[ExecuteDBFileReader]
		@serverName = N'dsp012_4_0',
		@dbname = N'bl_zakidb001',
		@filePath = N'E:\\filedb\\test_zwdb\bl_zakidb001_vel_0.bin',
		@BlobByteSize = 6144,
		@atomDim = 8,
		@zindexQuery = N'[0,]',
		@zlistCount = 1,
		@dbtype = 2

SELECT	@return_value as 'Return Value'

GO
