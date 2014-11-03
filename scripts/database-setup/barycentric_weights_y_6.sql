USE [turblib]

DROP TABLE [turblib].[dbo].[barycentric_weights_y_6]
GO
CREATE TABLE [turblib].[dbo].[barycentric_weights_y_6](
	[cell_index] int,
	[offset_index] int,
	[stencil_start_index] int,
	[stencil_end_index] int,
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float,
	[w5] float
)
GO

BULK INSERT [turblib].[dbo].[barycentric_weights_y_6]
FROM N'C:\Users\kalin\Documents\turbulence\channel-baryctrwt\baryctrwt-y-lag6.dat'
WITH
(
FIELDTERMINATOR = ' ',
ROWTERMINATOR = '\n'
)
GO

GRANT SELECT ON [barycentric_weights_y_6] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[barycentric_weights_y_6]
		 ORDER BY cell_index