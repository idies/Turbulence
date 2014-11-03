USE [turbinfo]
GO

/****** Object:  Table [dbo].[usage2]    Script Date: 05/02/2011 14:48:47 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[usage2](
	[rowid] [bigint] IDENTITY(1,1) NOT NULL,
	[uid] [int] NULL,
	[ip] [binary](4) NOT NULL,
	[date] [datetime] NOT NULL,
	[dataset] [smallint] NOT NULL,
	[op] [smallint] NOT NULL,
	[spatial] [smallint] NOT NULL,
	[temporal] [smallint] NOT NULL,
	[records] [int] NOT NULL,
	[exectime] [real] NULL,
	[timestep] [real] NOT NULL,
	[endTimestep] [real] NULL,
	[nt] [int] NULL,
	[access] [binary](512) NULL,
 CONSTRAINT [pk_usage_rowid] PRIMARY KEY CLUSTERED 
(
	[rowid] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[usage2] ADD  DEFAULT ((0)) FOR [ip]
GO

ALTER TABLE [dbo].[usage2] ADD  DEFAULT (getdate()) FOR [date]
GO

ALTER TABLE [dbo].[usage2] ADD  DEFAULT ((-1)) FOR [dataset]
GO

ALTER TABLE [dbo].[usage2] ADD  DEFAULT ((-1)) FOR [spatial]
GO

ALTER TABLE [dbo].[usage2] ADD  DEFAULT ((-1)) FOR [temporal]
GO


