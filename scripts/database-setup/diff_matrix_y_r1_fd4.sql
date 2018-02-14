USE [turbdev]

--DROP TABLE [turbdev].[dbo].[BL_diff_matrix_y_r1_fd4]
--GO
CREATE TABLE [turbdev].[dbo].[BL_diff_matrix_y_r1_fd4](
	[cell_index] int,
	[offset_index] int,
	[stencil_start_index] int,
	[stencil_end_index] int,
	[d0] float,
	[d1] float,
	[d2] float,
	[d3] float,
	[d4] float
)
GO
    

BULK INSERT [turbdev].[dbo].[BL_diff_matrix_y_r1_fd4]
FROM N'C:\Users\zwu27\Documents\Turbulence-for-publish\BL-baryctrwt\Output\BL-baryctrwt-diffmat-y-r-1-fd4.dat'
WITH
(
FIELDTERMINATOR = ' ',
ROWTERMINATOR = '\n'
)
GO

GRANT SELECT ON [BL_diff_matrix_y_r1_fd4] TO [turbquery]
GO

SELECT * FROM [turbdev].[dbo].[BL_diff_matrix_y_r1_fd4]
		 ORDER BY cell_index, stencil_start_index, offset_index
		 

