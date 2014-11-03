DECLARE @RC int
DECLARE @dbname sysname
DECLARE @partitionNum int
DECLARE @sliceNum int
DECLARE @npart int
DECLARE @doExecute bit

SELECT @dbname = 'mixingdb08'
SELECT @sliceNum = 8
SELECT @partitionNum = 1
SELECT @npart = 24
SELECT @doExecute = 1

WHILE (@partitionNum <= @npart)
BEGIN
	EXECUTE [turblib].[dbo].[spCreateSwitchInTableVel08] 
	   @dbname
	  ,@partitionNum
	  ,@sliceNum
	  ,@npart
	  ,@doExecute

	SELECT @partitionNum = @partitionNum + 1	
END