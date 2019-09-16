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
VALUES (0, 699842561, 12.1895, -1, 4.6142);

SELECT reqseq, zindex, x, y, z FROM #temp_zw

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[ExecuteMHDWorker]
		@serverName = N'dsp038',
		@dbname = N'channeldb04',
		@codedb = N'turblib',
		@turbinfodb = N'turbinfo',
		@turbinfoserver = N'lumberjack',
		@dataset = N'vel',
		@workerType = 120,
		@blobDim = 8,
		@time = 16,
		@spatialInterp = 0,
		@temporalInterp = 0,
		@arg = 0,
		@inputSize = 1,
		@tempTable = N'#temp_zw'
		--@startz = 0,
		--@endz = 536870911
SELECT	@return_value as 'Return Value'

GO

DROP TABLE #temp_zw
--56: //velocity
--64: //velocity gradient
--71: //velocity hessian
--68: //velocity laplacian
--30: //GetQThreshold
--84: //GetVelocityThreshold
--82: //GetCurlThreshold
--57: //pressure
--67: //pressure gradient
--74: //pressure hessian
--87: //GetPressureThreshold

--None = 0,
--None_Fd4 = 40,
--None_Fd6 = 60,
--None_Fd8 = 80,
--Fd4Lag4 = 44,
--Lag4 = 4,
--Lag6 = 6,
--Lag8 = 8,