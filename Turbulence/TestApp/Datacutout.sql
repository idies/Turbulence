USE [turblib]
GO

DECLARE	@return_value Int

EXEC [turblib].[dbo].[GetDataCutout] 
@serverName, 
@database, 
@codedb, 
@dataset, 
@blobDim, 
@timestep, 
@queryBox

SELECT	@return_value as 'Return Value'

GO

