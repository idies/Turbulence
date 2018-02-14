USE [turblib]
GO

CREATE TABLE [dbo].[#temp_zw] (
    [reqseq] INT    NULL,
    [zindex] BIGINT NULL,
    [x]      REAL   NULL,
    [y]      REAL   NULL,
    [z]      REAL   NULL
);
INSERT INTO [dbo].[#temp_zw]  
VALUES (0, 1, 0.0015339807878856412*9, -0.9999835, 0); 

SELECT reqseq, zindex, x, y, z FROM #temp_zw

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[ExecuteMHDWorker]
		@serverName = N'dsp085',
		@dbname = N'channeldb01',
		@codedb = N'turblib',
		@dataset = N'vel',
		@workerType = 122,
		@blobDim = 8,
		@time = 0,
		@spatialInterp = 40,
		@temporalInterp = 0,
		@arg = 1,
		@inputSize = 1,
		@tempTable = N'#temp_zw'
		/*@startz = 0,
		@endz = 134217727*/

SELECT	@return_value as 'Return Value'

GO
