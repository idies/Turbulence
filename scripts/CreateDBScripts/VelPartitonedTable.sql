--Creates vel partitioned table
--zindexPartScheme must already exist!


CREATE TABLE [dbo].[vel](
	[timestep] [int] NOT NULL,
	[zindex] [bigint] NOT NULL,
	[data] [varbinary](6168) NOT NULL,
 CONSTRAINT [pk_vel] PRIMARY KEY CLUSTERED 
(
	[timestep] ASC,
	[zindex] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [zindexPartScheme]([zindex])
) ON [zindexPartScheme]([zindex])