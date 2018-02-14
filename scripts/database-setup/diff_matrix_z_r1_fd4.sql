USE [turbdev]

--DROP TABLE [turbdev].[dbo].[BL_diff_matrix_z_r1_fd4]
--GO
CREATE TABLE [turbdev].[dbo].[BL_diff_matrix_z_r1_fd4](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float
)
GO
    
INSERT INTO [turbdev].[dbo].[BL_diff_matrix_z_r1_fd4]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4])
     VALUES
           (7.1111111111111114e-01, -5.6888888888888891e+00, -1.1102230246251565e-16, 5.6888888888888891e+00, -7.1111111111111114e-01)
GO

GRANT SELECT ON [BL_diff_matrix_z_r1_fd4] TO [turbquery]
GO

SELECT * FROM [turbdev].[dbo].[BL_diff_matrix_z_r1_fd4]