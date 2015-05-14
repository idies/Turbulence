USE [turbinfo]
GO

/****** Object:  Table [dbo].[datasets]    Script Date: 3/26/2015 1:04:45 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

drop table datasets2

CREATE TABLE [dbo].[datasets2](
	[DatasetID] [int] NOT NULL,
	[name] [nvarchar](200) NULL,
	[isUserCreated] bit null,
	[ScratchID] int null,
	[schemaname] sysname null,
	[minLim] [bigint] NULL,
	[maxLim] [bigint] NULL,
	[maxTime] [float] NULL,
	[dt] [float] NULL,
	[timeinc] [int] NULL,
	[timeoff] [int] NULL,
	[thigh] [int] NULL,
	[xhigh] [int] NULL,
	[yhigh] [int] NULL,
	[zhigh] [int] NULL
) ON [PRIMARY]

GO


insert datasets2(DatasetID, name, isUserCreated, ScratchID, schemaname, minLim, maxLim, maxTime, dt, timeinc, timeoff, thigh, xhigh, yhigh, zhigh)
select DatasetId, name, null, null, null, minLim, maxLim, maxTime, dt, timeinc, timeoff, thigh, xhigh, yhigh, zhigh
from datasets

alter table datasets
add constraint pk_datasets_id
primary key clustered (DatasetID)


--naming convention for user created datasets
-- sourcedataset_schemaname_friendlyname

--mhd1024_swerner_testvelocity for example
-- one row gets inserted into datasets table 

/*
/****** Script for SelectTopNRows command from SSMS  ******/
SELECT TOP 1000 [datafieldsID]
      ,[name]
      ,[datasetID]
      ,[charname]
      ,[components]
      ,[longname]
      ,[tablename] ------dynamic, this is the one that gets read.  tablename_datasetID from datasets table
  FROM [turbinfo].[dbo].[datafields]
  */

  --so when a user creates a new dataset, one row gets added to datasets, and one row gets added to datafields


  create table ScratchInfo (
	ScratchID int not null,
	ServerName sysname not null,
	DatabaseName sysname not null
)

alter database TurbScratch
set trustworthy on


set identity_insert dbo.datafields on
go
CREATE TABLE [dbo].[datafields](
	[DatafieldID] [int] identity(1,1) not NULL,
	[name] [nvarchar](30) NULL,
	[DatasetID] [int] NULL,
	[charname] [nchar](1) NULL,
	[components] [int] NULL,
	[longname] [nvarchar](50) NULL,
	[tablename] [nvarchar](30) NULL
) ON [PRIMARY]
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (1, N'vel', 3, N'u', 3, N'velocity', N'velocity08')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (2, N'mag', 3, N'b', 3, NULL, N'magnetic08')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (3, N'vec', 3, N'a', 3, NULL, N'potential08')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (4, N'pr', 3, N'p', 1, NULL, N'pressure08')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (5, N'density', 7, N'd', 1, NULL, NULL)
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (6, N'vel', 8, N'u', 3, N'velocity', NULL)
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (7, N'vel', 4, N'u', 3, N'velocity', N'vel')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (8, N'pr', 4, N'p', 1, NULL, N'pr')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (9, N'vel', 5, N'u', 3, N'velocity', N'isotropic1024fine_vel')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (10, N'pr', 5, N'p', 1, NULL, N'isotropic1024fine_pr')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (11, N'vel', 6, N'u', 3, N'velocity', N'vel')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (12, N'pr', 6, N'p', 1, NULL, N'pr')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (13, N'vel', 7, N'u', 3, N'velocity', N'vel')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (14, N'pr', 7, N'p', 1, NULL, N'pr')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (15, N'vorticity', 4, N'w', 3, N'vorticity', N'vel')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (16, N'vorticity', 3, N'w', 3, N'vorticity', N'velocity08')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (17, N'vorticity', 5, N'w', 3, N'vorticity', N'isotropic1024fine_vel')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (18, N'vorticity', 6, N'w', 3, N'vorticity', N'vel')
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (19, N'vorticity', 7, N'w', 3, N'vorticity', N'vel')


INSERT [dbo].[datafields] ([name], [DatasetID], [charname], [components], [longname], [tablename])
'isotropic1024coarse_suetest_test1',	True	1	suetest_test1	0	134217727	0.02	0.0002	10	0	100	511	511	511



INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES


select ServerName, DatabaseName 
from ScratchInfo
where ScratchID=1


select * from datasets where datasetID = 9

insert datasets (name, isUserCreated, ScratchID, schemaname, SourceDatasetID, minLim, maxLim, maxTime, dt, timeinc, timeoff, thigh, xhigh, yhigh, zhigh)
VALUES ('isotropic1024coarse_wsid_1539980082_myvelocity1', 1, 1, 'wsid_1539980082', 4, 0, 134217727, 0.02, 0.0002, 10, 0, 100, 511, 511, 511)

select * from datasets where datasetID = 10


select * from datafields


insert datafields(name, datasetID, charname, components, longname, tablename)
VALUES ('vel', 9, 'u', 3, 'velocity', 'myVelocity_9')
