-------------------------------------------
-- create table statements for turbinfo DB
--
------------------------------------------



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

GO



/****** Object:  Table [dbo].[messages]    Script Date: 3/27/2015 3:43:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[messages](
	[id] [nvarchar](50) NOT NULL,
	[message] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[particlecount]    Script Date: 3/27/2015 3:43:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[particlecount](
	[uid] [int] NULL,
	[records] [bigint] NOT NULL DEFAULT ((0)),
UNIQUE NONCLUSTERED 
(
	[uid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[usage]    Script Date: 3/27/2015 3:43:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[usage](
	[rowid] [bigint] IDENTITY(1,1) NOT NULL,
	[uid] [int] NULL,
	[ip] [binary](4) NOT NULL DEFAULT ((0)),
	[date] [datetime] NOT NULL DEFAULT (getdate()),
	[dataset] [smallint] NOT NULL DEFAULT ((-1)),
	[op] [smallint] NOT NULL,
	[spatial] [smallint] NOT NULL DEFAULT ((-1)),
	[temporal] [smallint] NOT NULL DEFAULT ((-1)),
	[records] [int] NOT NULL,
	[exectime] [real] NULL,
	[timestep] [real] NOT NULL,
	[endTimestep] [real] NULL,
	[dt] [real] NULL,
	[access] [binary](512) NULL,
 CONSTRAINT [pk_usage_rowid] PRIMARY KEY CLUSTERED 
(
	[rowid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[userinfo]    Script Date: 3/27/2015 3:43:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[userinfo](
	[uid] [int] NULL,
	[contact] [varchar](255) NULL,
	[url] [varchar](255) NULL,
	[description] [varchar](255) NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[users]    Script Date: 3/27/2015 3:43:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[users](
	[uid] [int] IDENTITY(10000,1) NOT NULL,
	[authkey] [varchar](255) NOT NULL,
	[limit] [int] NOT NULL DEFAULT ((-1)),
PRIMARY KEY CLUSTERED 
(
	[uid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[authkey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Index [usage_date]    Script Date: 3/27/2015 3:43:53 PM ******/
CREATE NONCLUSTERED INDEX [usage_date] ON [dbo].[usage]
(
	[date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[particlecount]  WITH CHECK ADD FOREIGN KEY([uid])
REFERENCES [dbo].[users] ([uid])
GO
ALTER TABLE [dbo].[userinfo]  WITH CHECK ADD FOREIGN KEY([uid])
REFERENCES [dbo].[users] ([uid])
GO
