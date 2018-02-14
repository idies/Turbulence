USE [turbdev]

--DROP TABLE [turbdev].[dbo].[BL_diff_matrix_z_r2_fd4]
--GO
CREATE TABLE [turbdev].[dbo].[BL_diff_matrix_z_r2_fd4](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float
)
GO
    
INSERT INTO [turbdev].[dbo].[BL_diff_matrix_z_r2_fd4]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4])
     VALUES
           (-6.0681481481481487e+00, 9.7090370370370380e+01, -1.8204444444444445e+02, 9.7090370370370380e+01, -6.0681481481481487e+00)
GO

GRANT SELECT ON [BL_diff_matrix_z_r2_fd4] TO [turbquery]
GO

SELECT * FROM [turbdev].[dbo].[BL_diff_matrix_z_r2_fd4]