USE [iso4096db]
GO

/****** Object: Table [dbo].[DataPath] Script Date: 11/2/2017 1:47:30 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DataPath] (
    [ordinal]                INT            IDENTITY (1, 1) NOT NULL,
    [DatasetID]              INT            NULL,
    [DatasetName]            NVARCHAR (30)  NULL,
    [ProductionMachineName]  [sysname]      NULL,
    [ProductionDatabaseName] [sysname]      NULL,
	[minTime]                INT           NULL,
    [maxTime]                INT           NULL,
    [Path]                   NVARCHAR (MAX) NULL
);


DECLARE @prod varchar(25)
DECLARE @min BIGINT 
DECLARE @max BIGINT 
DECLARE @slice BIGINT 
DECLARE @iter BIGINT 
DECLARE @prod_iter BIGINT 

SET @iter=1

WHILE @iter<=512

BEGIN

SET @slice= @iter
SET @prod_iter = 100 + @slice
SET @prod = 'iso4096db'+STR(@prod_iter,len(@prod_iter))
SET @min = (@iter-1)*134217728 
SET @max = @min + 134217727

INSERT into [dbo].[DataPath] 
		([DatasetID],
		[DatasetName],
		[ProductionMachineName],
		[ProductionDatabaseName],
		[Path],
		[minTime],
		[maxTime]) 
	
	VALUES 
		(10,
		'isotropic4096',
		'dsp012',
		@prod,
		'E:\\filedb\\isotropic4096',
		0,
		0)
SET @iter=@iter+1
END
GO 



/****** Script for SelectTopNRows command from SSMS  ******/
SELECT *
  FROM [iso4096db].[dbo].[DataPath] WHERE DatasetName = 'isotropic4096' 