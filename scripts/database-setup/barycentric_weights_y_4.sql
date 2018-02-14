USE [turbdev]

DROP TABLE [turbdev].[dbo].[BL_barycentric_weights_y_4]
GO
CREATE TABLE [turbdev].[dbo].[BL_barycentric_weights_y_4](
	[cell_index] int,
	[offset_index] int,
	[stencil_start_index] int,
	[stencil_end_index] int,
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float
)
GO
    

BULK INSERT [turbdev].[dbo].[BL_barycentric_weights_y_4]
FROM N'C:\Users\zwu27\Documents\Turbulence-for-publish\BL-baryctrwt\Output\BL-baryctrwt-y-lag4.dat'
WITH
(
FIELDTERMINATOR = ' ',
ROWTERMINATOR = '\n'
)
GO

GRANT SELECT ON [BL_barycentric_weights_y_4] TO [turbquery]
GO

SELECT * FROM [turbdev].[dbo].[BL_barycentric_weights_y_4]
		 ORDER BY cell_index
		 

