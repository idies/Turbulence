USE [iso8192db]
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
DECLARE @minTIme BIGINT
DECLARE @maxTime BIGINT

SET @iter=1

WHILE @iter<=4096

BEGIN

SET @slice= @iter
SET @prod_iter = @slice
SET @prod = 'iso8192db'+format(@prod_iter,'0000')
SET @min = (@iter-1)*134217728 
SET @max = @min + 134217727
SET @minTime = 0
SET @maxTime = 4

INSERT into [dbo].[DataPath] 
		([DatasetID],
		[DatasetName],
		[ProductionMachineName],
		[ProductionDatabaseName],
		[Path],
		[minTime],
		[maxTime]) 
	
	VALUES 
		(14,
		'isotropic8192',
		'dsp012',
		@prod,
		'E:\\filedb\\isotropic8192',
		@minTime,
		@maxTime)
SET @iter=@iter+1
END
GO 



/****** Script for SelectTopNRows command from SSMS  ******/
SELECT *
  FROM [iso8192db].[dbo].[DataPath] WHERE DatasetName = 'isotropic8192' 