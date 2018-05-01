USE [turbdev_zw]
GO

CREATE TABLE [dbo].[#temp_zw] (
    [reqseq] INT    NULL,
    [zindex] BIGINT NULL,
    [x]      REAL   NULL,
    [y]      REAL   NULL,
    [z]      REAL   NULL
);
INSERT INTO [dbo].[#temp_zw]  
VALUES (0, 1065216, 0, 0, 9.84855886663);

SELECT reqseq, zindex, x, y, z FROM #temp_zw

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[ExecuteParticleTrackingChannelDBWorkerTaskParallel]
		@codedb = N'turbdev_zw',
		@turbinfoDB = N'turbinfo_test',
		@turbinfoServer = N'sciserver02',
		@localServer = N'dsp012',
		@localDatabase = N'bl_zakidb001',
		@datasetID = 12,
		@tableName = N'vel',
		@atomDim = 8,
		@workerType = 22,
		@spatialInterp = 4,
		@temporalInterp = 1,
		@inputSize = 1,
		@tempTable = N'#temp_zw',
		@time = 0,
		@endTime = 1,
		@dt = 0.25
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