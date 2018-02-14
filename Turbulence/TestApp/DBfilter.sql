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
VALUES (0, 515, 0.0015339807878856412*5, 0.0015339807878856412*5, 0.0015339807878856412*5); 

SELECT reqseq, zindex, x, y, z FROM #temp_zw

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[ExecuteBoxFilterDBWorker]
		@serverName = N'dsp012_4_0',
		@dbname = N'iso4096db101',
		@codedb = N'turblib',
		@dataset = N'vel',
		@workerType = 77,
		@blobDim = 8,
		@time = 0,
		@spatialInterp = 0,
		@temporalInterp = 0,
		@arg = 0.007669904,
		@inputSize = 1,
		@tempTable = N'#temp_zw',
		@minz = 0,
		@maxz = 33554431

SELECT	@return_value as 'Return Value'

GO
