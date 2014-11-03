USE [turblib]

--DROP TABLE [turblib].[dbo].[diff_matrix_z_r2_fd4]
--GO
CREATE TABLE [turblib].[dbo].[diff_matrix_z_r2_fd4](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float
)
GO
    
INSERT INTO [turblib].[dbo].[diff_matrix_z_r2_fd4]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4])
     VALUES
           (-2.2133950303947495e+03, 3.5414320486315992e+04, -6.6401850911842484e+04, 3.5414320486315992e+04, -2.2133950303947495e+03)
GO

GRANT SELECT ON [diff_matrix_z_r2_fd4] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_z_r2_fd4]