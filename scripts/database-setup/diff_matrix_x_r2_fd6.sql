USE [turblib]

--DROP TABLE [turblib].[dbo].[diff_matrix_x_r2_fd6]
--GO
CREATE TABLE [turblib].[dbo].[diff_matrix_x_r2_fd6](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float,
	[w5] float,
	[w6] float
)
GO
    
INSERT INTO [turblib].[dbo].[diff_matrix_x_r2_fd6]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4]
           ,[w5]
           ,[w6])
     VALUES
           (7.3779834346491654e+01, -9.9602776367763727e+02, 9.9602776367763745e+03, -1.8076059414890460e+04, 9.9602776367763745e+03, -9.9602776367763727e+02, 7.3779834346491654e+01)
GO

GRANT SELECT ON [diff_matrix_x_r2_fd6] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_x_r2_fd6]