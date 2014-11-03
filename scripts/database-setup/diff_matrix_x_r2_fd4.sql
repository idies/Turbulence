USE [turblib]

--DROP TABLE [turblib].[dbo].[diff_matrix_x_r2_fd4]
--GO
CREATE TABLE [turblib].[dbo].[diff_matrix_x_r2_fd4](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float
)
GO
    
INSERT INTO [turblib].[dbo].[diff_matrix_x_r2_fd4]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4])
     VALUES
           (-5.5334875759868737e+02, 8.8535801215789979e+03, -1.6600462727960621e+04, 8.8535801215789979e+03, -5.5334875759868737e+02)
GO

GRANT SELECT ON [diff_matrix_x_r2_fd4] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_x_r2_fd4]