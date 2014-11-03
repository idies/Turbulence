USE [cachedb]
GO

--/****** Object:  Table [dbo].[cache_data]    Script Date: 11/24/2010 17:47:22 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[cache_data]') AND type in (N'U'))
DROP TABLE [dbo].[cache_data]
GO

/****** Object:  Table [dbo].[cache_data]    Script Date: 11/24/2010 17:47:22 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[cache_data](
	[cache_info_ordinal] int NOT NULL,
	[zindex] [bigint] NOT NULL,
	[data] [float] NOT NULL,
 FOREIGN KEY (cache_info_ordinal) REFERENCES [dbo].[cache_info]
      (ordinal) ON DELETE CASCADE,
 CONSTRAINT [pk_cache_data_cache_info_ordinal] PRIMARY KEY CLUSTERED 
(
	[cache_info_ordinal] ASC,
	[zindex] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) on CACHEDATA
)

GO

SET ANSI_PADDING OFF
GO

GRANT SELECT, INSERT, UPDATE, DELETE on [cache_data] TO [turbquery]
GO