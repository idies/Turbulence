USE [turbdev]

DROP TABLE [turbdev].[dbo].[BL_barycentric_weights_z_4]
GO
CREATE TABLE [turbdev].[dbo].[BL_barycentric_weights_z_4](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float
)
GO
    
INSERT INTO [turbdev].[dbo].[BL_barycentric_weights_z_4]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3])
     VALUES
           (-1.0356306172839506e+02, 3.1068918518518518e+02, -3.1068918518518518e+02, 1.0356306172839506e+02)
GO

GRANT SELECT ON [BL_barycentric_weights_z_4] TO [turbquery]
GO

SELECT * FROM turbdev..BL_barycentric_weights_z_4