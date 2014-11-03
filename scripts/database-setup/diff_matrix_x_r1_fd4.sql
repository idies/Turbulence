USE [turblib]

--DROP TABLE [turblib].[dbo].[diff_matrix_x_r1_fd4]
--GO
CREATE TABLE [turblib].[dbo].[diff_matrix_x_r1_fd4](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float
)
GO
    
INSERT INTO [turblib].[dbo].[diff_matrix_x_r1_fd4]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4])
     VALUES
           (6.7906109052542005e+00, -5.4324887242033604e+01, 2.6645352591003757e-15, 5.4324887242033604e+01, -6.7906109052542005e+00)
GO

GRANT SELECT ON [diff_matrix_x_r1_fd4] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_x_r1_fd4]