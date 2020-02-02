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
VALUES (0, 2147486586, 0.0215, 1.5719, 0.0215);

SELECT reqseq, zindex, x, y, z FROM #temp_zw

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[ExecuteMHDFileDBWorker]
		@serverName = N'dsp012',
		@dbname = N'iso4096db117',
		@codedb = N'turbdev_zw',
		@turbinfodb = N'turbinfo_test',
		@turbinfoserver = N'sciserver02',
		@dataset = N'vel',
		@workerType = 64,
		@blobDim = 8,
		@time = 0,
		@spatialInterp = 104,
		@temporalInterp = 0,
		@arg = 0,
		@inputSize = 1,
		@tempTable = N'#temp_zw',
		@startz = 2147483648,
		@endz = 2281701375
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