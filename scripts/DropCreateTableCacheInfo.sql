USE [cachedb]
GO

--/****** Object:  Table [dbo].[cache_info]    Script Date: 11/24/2010 17:47:22 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[cache_info]') AND type in (N'U'))
DROP TABLE [dbo].[cache_info]
GO

/****** Object:  Table [dbo].[cache_info]    Script Date: 11/24/2010 17:47:22 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[cache_info](
	[ordinal] [int] IDENTITY(1,1) NOT NULL,
	[DatasetID] [smallint] NOT NULL,
	[serverName] [nvarchar](30) NOT NULL,
	[dbName] [nvarchar](30) NOT NULL,
	[timestep] [int] NOT NULL,
	[worker] [int] NOT NULL,
	[spatialOption] [int] NOT NULL,
	[start_index] [bigint] NOT NULL,
	[end_index] [bigint] NOT NULL,
	[threshold] [float] NOT NULL,
	[date_used] [datetime] NOT NULL,
	[rows] [int] NOT NULL,
 CONSTRAINT [cache_info_ordinal] UNIQUE(ordinal),
 CONSTRAINT [pk_cache_info_dataset_server_worker_time] PRIMARY KEY CLUSTERED 
(
	[DatasetID] ASC,
	[serverName] ASC,
	[dbName] ASC,
	[timestep] ASC,
	[worker] ASC,
	[spatialOption] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) on CACHEDATA
)

GO

SET ANSI_PADDING OFF
GO

GRANT SELECT, INSERT, UPDATE, DELETE on [cache_info] TO [turbquery]
GO