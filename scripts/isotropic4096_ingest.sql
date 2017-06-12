DECLARE @prod varchar(25)
DECLARE @min BIGINT 
DECLARE @max BIGINT 
DECLARE @slice BIGINT 
DECLARE @iter BIGINT 
DECLARE @prod_iter BIGINT 

SET @iter=1

WHILE @iter<512

BEGIN

SET @slice= @iter
SET @prod_iter = 100 + @slice
SET @prod = 'iso4096db'+STR(@prod_iter,len(@prod_iter))
SET @min = (@iter-1)*134217728 
SET @max = @min + 134217727


INSERT into [turbinfo].[dbo].[DatabaseMap] 
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
		(10,
		'isotropic4096',
		'dsp012',
		@prod,
		'turblib',
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
  FROM [turbinfo].[dbo].[DatabaseMap] WHERE DatasetName = 'isotropic4096' 