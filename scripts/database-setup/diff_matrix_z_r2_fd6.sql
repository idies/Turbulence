USE [turblib]

--DROP TABLE [turblib].[dbo].[diff_matrix_z_r2_fd6]
--GO
CREATE TABLE [turblib].[dbo].[diff_matrix_z_r2_fd6](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float,
	[w5] float,
	[w6] float
)
GO
    
INSERT INTO [turblib].[dbo].[diff_matrix_z_r2_fd6]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4]
           ,[w5]
           ,[w6])
     VALUES
           (2.9511933738596662e+02, -3.9841110547105491e+03, 3.9841110547105498e+04, -7.2304237659561841e+04, 3.9841110547105498e+04, -3.9841110547105491e+03, 2.9511933738596662e+02)
GO

GRANT SELECT ON [diff_matrix_z_r2_fd6] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_z_r2_fd6]