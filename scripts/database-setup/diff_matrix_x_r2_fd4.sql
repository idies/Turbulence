USE [turbdev]

--DROP TABLE [turbdev].[dbo].[BL_diffmatrix_x_r2_fd4]
--GO
CREATE TABLE [turbdev].[dbo].[BL_diffmatrix_x_r2_fd4](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float
)
GO
    
INSERT INTO [turbdev].[dbo].[BL_diffmatrix_x_r2_fd4]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4])
     VALUES
           (-9.7594921594373252e-01, 1.5615187455099719e+01, -2.9278476478311973e+01, 1.5615187455099719e+01, -9.7594921594373230e-01)
GO

GRANT SELECT ON [BL_diffmatrix_x_r2_fd4] TO [turbquery]
GO

SELECT * FROM [turbdev].[dbo].[BL_diffmatrix_x_r2_fd4]