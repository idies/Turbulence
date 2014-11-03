USE [turblib]

DROP TABLE [turblib].[dbo].[barycentric_weights_y_8]
GO
CREATE TABLE [turblib].[dbo].[barycentric_weights_y_8](
	[cell_index] int,
	[offset_index] int,
	[stencil_start_index] int,
	[stencil_end_index] int,
	[w0] float NOT NULL,
	[w1] float NOT NULL,
	[w2] float NOT NULL,
	[w3] float NOT NULL,
	[w4] float NOT NULL,
	[w5] float NOT NULL,
	[w6] float NOT NULL,
	[w7] float NOT NULL
)
GO
    

BULK INSERT [turblib].[dbo].[barycentric_weights_y_8]
FROM N'C:\Users\kalin\Documents\turbulence\channel-baryctrwt\baryctrwt-y-lag8.dat'
WITH
(
FIELDTERMINATOR = ' ',
ROWTERMINATOR = '\n'
)
GO

GRANT SELECT ON [barycentric_weights_y_8] TO [turbquery]
GO

select * from turblib..barycentric_weights_y_8
		 order by cell_index
		 