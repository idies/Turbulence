USE [turbulence]
GO

/****** Object:  Table [dbo].[zindex]    Script Date: 05/02/2011 19:00:35 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[zindex](
	[X] [int] NOT NULL,
	[Y] [int] NOT NULL,
	[Z] [int] NOT NULL,
	[zindex] [bigint] NOT NULL,
 CONSTRAINT [pk_zindex] PRIMARY KEY CLUSTERED 
(
	[zindex] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = ON, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) on [PRIMARY]
) ON [PRIMARY]

GO

INSERT INTO dbo.zindex
    (x,
     Y, 
     Z, 
     zindex)
SELECT 
turblib.dbo.GetMortonX(m.zindex),
turblib.dbo.GetMortonY(m.zindex),
turblib.dbo.GetMortonZ(m.zindex),
m.zindex
FROM velocity as m
where timestep = 0;
GO

CREATE NONCLUSTERED INDEX zindex_x_y_z
ON [dbo].[zindex] ([X],[Y],[Z])
INCLUDE ([zindex])
GO

GRANT SELECT on zindex TO [turbquery]
GO

select * from dbo.zindex