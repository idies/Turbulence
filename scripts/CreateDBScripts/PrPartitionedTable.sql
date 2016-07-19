--Creates pr partitioned table
--zindexPartScheme must already exist!


CREATE TABLE [dbo].[pr](
	[timestep] [int] NOT NULL,
	[zindex] [bigint] NOT NULL,
	[data] [varbinary](2072) NOT NULL,
 CONSTRAINT [pk_pr] PRIMARY KEY CLUSTERED 
(
	[timestep] ASC,
	[zindex] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [zindexPartScheme]([zindex])
) ON [zindexPartScheme]([zindex])