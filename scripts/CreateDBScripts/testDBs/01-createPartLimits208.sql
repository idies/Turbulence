



/****** Object:  Table [dbo].[PartLimits208]    Script Date: 10/15/2018 3:26:50 PM ******/


CREATE TABLE [dbo].[PartLimits208](
	[sliceNum] [int] NOT NULL,
	[partitionNum] [int] NOT NULL,
	[minLim] [bigint] NOT NULL,
	[maxLim] [bigint] NOT NULL,
	[ordinal] [int] NOT NULL,
 CONSTRAINT [pk_partLimits208] PRIMARY KEY CLUSTERED 
(
	[sliceNum] ASC,
	[partitionNum] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


--
insert PartLimits208
select * from turbdb203.dbo.PartLimits208  --change this to the name of any turbdb2xx that exists on the current server



