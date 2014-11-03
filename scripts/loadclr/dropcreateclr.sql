DROP FUNCTION [dbo].[CreateMortonIndex]
DROP FUNCTION [dbo].[GetMortonX]
DROP FUNCTION [dbo].[GetMortonY]
DROP FUNCTION [dbo].[GetMortonZ]
DROP FUNCTION [dbo].[MHDCover]
DROP PROCEDURE [dbo].[ExecuteTurbulenceWorker]
DROP PROCEDURE [dbo].[ExecuteGetPositionWorker]
DROP PROCEDURE [dbo].[ExecuteMHDWorker]
DROP PROCEDURE [dbo].[ExecuteBoxFilterWorker]
DROP PROCEDURE [dbo].[GetDataCutout]
DROP PROCEDURE [dbo].[GetThreshold]
DROP ASSEMBLY Turbulence 
CREATE ASSEMBLY Turbulence FROM @DLL_Turbulence WITH PERMISSION_SET = UNSAFE 
GO
CREATE PROCEDURE [dbo].[ExecuteGetPositionWorker] (
	@serverName nvarchar(4000),
	@database nvarchar(4000),
	@codedb nvarchar(4000),
	@dataset nvarchar(4000),
	@workerType int,
	@blobDim int,
	@time real,
	@spatialInterp int,
	@temporalInterp int,
	@correcting_pos int,
	@dt real,
	@inputSize int,
	@tempTable nvarchar(4000)
) AS EXTERNAL NAME Turbulence.StoredProcedures.ExecuteGetPositionWorker
GO
GRANT EXECUTE ON [dbo].[ExecuteGetPositionWorker] TO [turbquery]
GO
CREATE PROCEDURE [dbo].[ExecuteTurbulenceWorker] (
	@database nvarchar(4000),
	@dataset nvarchar(4000),
	@workerType int,
	@time real,
	@spatialInterp int,
	@temporalInterp int,
	@arg int,
	@tempTable nvarchar(4000)
) AS EXTERNAL NAME Turbulence.StoredProcedures.ExecuteTurbulenceWorker
GO
GRANT EXECUTE ON [dbo].[ExecuteTurbulenceWorker] TO [turbquery]
GO
CREATE PROCEDURE [dbo].[ExecuteMHDWorker] (
	@serverName nvarchar(4000),
	@dbname nvarchar(4000),
	@codedb nvarchar(4000),
	@dataset nvarchar(4000),
	@workerType int,
	@blobDim int,
	@time real,
	@spatialInterp int,
	@temporalInterp int,
	@arg real,
	@intpuSize int,
	@tempTable nvarchar(4000)
) AS EXTERNAL NAME Turbulence.StoredProcedures.ExecuteMHDWorker
GO
GRANT EXECUTE ON [dbo].[ExecuteMHDWorker] TO [turbquery]
GO
CREATE PROCEDURE [dbo].[ExecuteBoxFilterWorker] (
	@serverName nvarchar(4000),
	@dbname nvarchar(4000),
	@codedb nvarchar(4000),
	@dataset nvarchar(4000),
	@workerType int,
	@blobDim int,
	@time real,
	@spatialInterp int,
	@temporalInterp int,
	@arg real,
	@intpuSize int,
	@tempTable nvarchar(4000)
) AS EXTERNAL NAME Turbulence.StoredProcedures.ExecuteBoxFilterWorker
GO
GRANT EXECUTE ON [dbo].[ExecuteBoxFilterWorker] TO [turbquery]
GO
CREATE PROCEDURE [dbo].[GetDataCutout] (
	@serverName nvarchar(4000),
	@database nvarchar(4000),
	@codedb nvarchar(4000),
	@dataset nvarchar(4000),
	@blobDim int,
	@timestep int,
	@queryBox nvarchar(4000)
) AS EXTERNAL NAME Turbulence.StoredProcedures.GetDataCutout
GO
GRANT EXECUTE ON [dbo].[GetDataCutout] TO [turbquery]
GO
CREATE PROCEDURE [dbo].[GetThreshold] (
	@datasetID [smallint],
	@serverName [nvarchar](4000),
	@dbname [nvarchar](4000),
	@codedb [nvarchar](4000),
	@cachedb [nvarchar](4000),
	@turbinfodb [nvarchar](4000),
	@tableName [nvarchar](4000),
	@workerType [int],
	@blobDim [int],
	@timestep [int],
	@spatialInterp [int],
	@arg [real],
	@threshold [float],
	@QueryBox [nvarchar](4000)
) AS EXTERNAL NAME Turbulence.StoredProcedures.GetThreshold
GO
GRANT EXECUTE ON [dbo].[GetThreshold] TO [turbquery]
GO
CREATE FUNCTION dbo.CreateMortonIndex (
	@z int,
	@y int,
	@x int
)
RETURNS bigint
AS EXTERNAL NAME Turbulence.UserDefinedFunctions.CreateMortonIndex
GO
GRANT EXECUTE ON [dbo].[CreateMortonIndex] TO [turbquery]
GO
CREATE FUNCTION dbo.GetMortonX (
	@key bigint
)
RETURNS int
AS EXTERNAL NAME Turbulence.UserDefinedFunctions.GetMortonX
GO
GRANT EXECUTE ON [dbo].[GetMortonX] TO [turbquery]
GO
CREATE FUNCTION dbo.GetMortonY (
	@key bigint
)
RETURNS int
AS EXTERNAL NAME Turbulence.UserDefinedFunctions.GetMortonY
GO
GRANT EXECUTE ON [dbo].[GetMortonY] TO [turbquery]
GO
CREATE FUNCTION dbo.GetMortonZ (
	@key bigint
)
RETURNS int
AS EXTERNAL NAME Turbulence.UserDefinedFunctions.GetMortonZ
GO
GRANT EXECUTE ON [dbo].[GetMortonZ] TO [turbquery]
GO
--GRANT SELECT ON [dbo].[fCover] TO turbquery
--GO
--GRANT EXECUTE ON TYPE::Shape TO turbquery
--GO
--CREATE FUNCTION dbo.MHDCover (@s Shape)
--RETURNS TABLE AS RETURN 
--(
--SELECT 512*KeyMin as KeyMin, 512*KeyMax as KeyMax, ShiftX, ShiftY, ShiftZ 
--FROM [dbo].[fCover] (
--  'Z'
--  ,7
--  ,'Box [0,0,0,1024,1024,1024]'
--  ,1
--  ,@s) 
--)
--GO
--GRANT SELECT ON [dbo].[MHDCover] TO [turbquery]
--GO