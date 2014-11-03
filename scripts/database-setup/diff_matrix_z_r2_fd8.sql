USE [turblib]

--DROP TABLE [turblib].[dbo].[diff_matrix_z_r2_fd8]
--GO
CREATE TABLE [turblib].[dbo].[diff_matrix_z_r2_fd8](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float,
	[w5] float,
	[w6] float,
	[w7] float,
	[w8] float
)
GO
    
INSERT INTO [turblib].[dbo].[diff_matrix_z_r2_fd8]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4]
           ,[w5]
           ,[w6]
           ,[w7]
           ,[w8])
     VALUES
           (-4.7429893508458946e+01, 6.7455848545363847e+02, -5.3121480729474015e+03, 4.2497184583579190e+04, -7.5624330205153936e+04, 4.2497184583579183e+04, -5.3121480729473988e+03, 6.7455848545363779e+02, -4.7429893508458896e+01)
GO

GRANT SELECT ON [diff_matrix_z_r2_fd8] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_z_r2_fd8]