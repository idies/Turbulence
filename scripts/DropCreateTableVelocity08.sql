USE [channeldb01]
GO

--/****** Object:  Table [dbo].[vel]    Script Date: 11/24/2010 17:47:22 ******/
--IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[vel]') AND type in (N'U'))
--DROP TABLE [dbo].[vel]
--GO

/****** Object:  Table [dbo].[vel]    Script Date: 11/24/2010 17:47:22 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[vel](
	[timestep] [int] NOT NULL,
	[zindex] [bigint] NOT NULL,
	[data] [varbinary](6168) NOT NULL,
 CONSTRAINT [pk_vel_time_zindex] PRIMARY KEY CLUSTERED 
(
	[timestep] ASC,
	[zindex] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) on zindexPartScheme(zindex)
)

GO

SET ANSI_PADDING OFF
GO

GRANT SELECT on [vel] TO [turbquery]
GO