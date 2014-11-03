USE [turblib]

--DROP TABLE [turblib].[dbo].[diff_matrix_x_r1_fd6]
--GO
CREATE TABLE [turblib].[dbo].[diff_matrix_x_r1_fd6](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float,
	[w5] float,
	[w6] float
)
GO
    
INSERT INTO [turblib].[dbo].[diff_matrix_x_r1_fd6]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4]
           ,[w5]
           ,[w6])
     VALUES
           (-1.3581221810508404e+00, 1.2223099629457561e+01, -6.1115498147287823e+01, 1.3322676295501878e-15, 6.1115498147287823e+01, -1.2223099629457561e+01, 1.3581221810508404e+00)
GO

GRANT SELECT ON [diff_matrix_x_r1_fd6] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_x_r1_fd6]