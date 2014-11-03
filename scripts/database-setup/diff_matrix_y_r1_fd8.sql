USE [turblib]

DROP TABLE [turblib].[dbo].[diff_matrix_y_r1_fd8]
GO
CREATE TABLE [turblib].[dbo].[diff_matrix_y_r1_fd8](
	[cell_index] int,
	[offset_index] int,
	[stencil_start_index] int,
	[stencil_end_index] int,
	[d0] float,
	[d1] float,
	[d2] float,
	[d3] float,
	[d4] float,
	[d5] float,
	[d6] float,
	[d7] float,
	[d8] float
)
GO
    

BULK INSERT [turblib].[dbo].[diff_matrix_y_r1_fd8]
FROM N'C:\Users\kalin\Documents\turbulence\channel-baryctrwt\baryctrwt-diffmat-y-r-1-fd8.dat'
WITH
(
FIELDTERMINATOR = ' ',
ROWTERMINATOR = '\n'
)
GO

GRANT SELECT ON [diff_matrix_y_r1_fd8] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_y_r1_fd8]
		 ORDER BY cell_index, stencil_start_index, offset_index


