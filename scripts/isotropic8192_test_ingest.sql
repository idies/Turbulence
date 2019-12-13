DECLARE @prod varchar(25)
DECLARE @min BIGINT 
DECLARE @max BIGINT 
DECLARE @slice BIGINT 
DECLARE @iter BIGINT 
DECLARE @prod_iter BIGINT 

SET @iter=1

WHILE @iter<4097

BEGIN

SET @slice= @iter
SET @prod_iter = 100 + @slice
SET @prod = 'iso8192db' + right('0000'+ cast(@iter as varchar(4)),4) 
SET @min = (@iter-1)*134217728 
SET @max = @min + 134217727


INSERT into [turbinfo_test].[dbo].[DatabaseMap] 
		([DatasetID],
		[DatasetName],
		[ProductionMachineName],
		[ProductionDatabaseName],
		[CodeDatabaseName],
		[HotSpareActive],
		[SliceNum],
		[PartitionNum],
		[minlim],
		[maxlim],
		[minTime],
		[maxTime],
		[dbtype]) 
	
	VALUES 
		(14,
		'isotropic8192',
		'dsp012',
		@prod,
		'turbdev_zw',
		0,
		@slice,
		1,
		@min,
		@max,
		0,
		1,
		1)
SET @iter=@iter+1
END
GO 



/****** Script for SelectTopNRows command from SSMS  ******/
SELECT *
  FROM [turbinfo_test].[dbo].[DatabaseMap] WHERE DatasetName = 'isotropic8192' 