USE [turbinfo]
GO
/****** Object:  Table [dbo].[DatabaseMap]    Script Date: 3/27/2015 3:40:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DatabaseMap](
	[ordinal] [int] IDENTITY(1,1) NOT NULL,
	[DatasetID] [int] NULL,
	[DatasetName] [nvarchar](30) NULL,
	[ProductionMachineName] [sysname] NULL,
	[ProductionDatabaseName] [sysname] NULL,
	[HotSpareMachineName] [sysname] NULL,
	[HotSpareDatabaseName] [sysname] NULL,
	[CodeDatabaseName] [sysname] NULL,
	[BackupLocation] [sysname] NULL,
	[HotSpareActive] [bit] NULL,
	[SliceNum] [int] NULL,
	[PartitionNum] [int] NULL,
	[minLim] [bigint] NULL,
	[maxLim] [bigint] NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[datafields]    Script Date: 3/27/2015 3:40:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[datafields](
	[DatafieldID] [int] IDENTITY(1,1) NOT NULL,
	[name] [nvarchar](30) NULL,
	[DatasetID] [int] NULL,
	[charname] [nchar](1) NULL,
	[components] [int] NULL,
	[longname] [nvarchar](50) NULL,
	[tablename] [nvarchar](30) NULL,
 CONSTRAINT [pk_datafields] PRIMARY KEY CLUSTERED 
(
	[DatafieldID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[datasets]    Script Date: 3/27/2015 3:40:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[datasets](
	[DatasetID] [int] IDENTITY(100,1) NOT NULL,
	[name] [nvarchar](200) NULL,
	[isUserCreated] [bit] NULL,
	[ScratchID] [int] NULL,
	[schemaname] [sysname] NULL,
	[SourceDatasetID] [int] NULL,
	[minLim] [bigint] NULL,
	[maxLim] [bigint] NULL,
	[maxTime] [float] NULL,
	[dt] [float] NULL,
	[timeinc] [int] NULL,
	[timeoff] [int] NULL,
	[thigh] [int] NULL,
	[xhigh] [int] NULL,
	[yhigh] [int] NULL,
	[zhigh] [int] NULL,
 CONSTRAINT [pk_datasets_id] PRIMARY KEY CLUSTERED 
(
	[DatasetID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[ScratchInfo]    Script Date: 3/27/2015 3:40:25 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ScratchInfo](
	[ScratchID] [int] NOT NULL,
	[ServerName] [sysname] NOT NULL,
	[DatabaseName] [sysname] NOT NULL,
 CONSTRAINT [pk_ScratchInfo] PRIMARY KEY CLUSTERED 
(
	[ScratchID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

use [turbinfo]
GO

INSERT [dbo].[ScratchInfo] ([ScratchID], [ServerName], [DatabaseName]) VALUES (1, N'sciserver02', N'TurbScratch')
GO
