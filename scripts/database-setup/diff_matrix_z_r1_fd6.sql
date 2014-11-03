USE [turblib]

--DROP TABLE [turblib].[dbo].[diff_matrix_z_r1_fd6]
--GO
CREATE TABLE [turblib].[dbo].[diff_matrix_z_r1_fd6](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float,
	[w5] float,
	[w6] float
)
GO
    
INSERT INTO [turblib].[dbo].[diff_matrix_z_r1_fd6]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4]
           ,[w5]
           ,[w6])
     VALUES
           (-2.7162443621016807e+00, 2.4446199258915122e+01, -1.2223099629457565e+02, 2.6645352591003757e-15, 1.2223099629457565e+02, -2.4446199258915122e+01, 2.7162443621016807e+00)
GO

GRANT SELECT ON [diff_matrix_z_r1_fd6] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_z_r1_fd6]