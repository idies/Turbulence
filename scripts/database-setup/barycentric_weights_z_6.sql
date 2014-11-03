USE [turblib]

DROP TABLE [turblib].[dbo].[barycentric_weights_z_6]
GO
CREATE TABLE [turblib].[dbo].[barycentric_weights_z_6](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float,
	[w5] float
)
GO
    
INSERT INTO [turblib].[dbo].[barycentric_weights_z_6]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4]
           ,[w5])
     VALUES
           (-9.5811843262956285e+08, 4.7905921631478148e+09, -9.5811843262956295e+09, 9.5811843262956295e+09, -4.7905921631478148e+09, 9.5811843262956285e+08)
GO

GRANT SELECT ON [barycentric_weights_z_6] TO [turbquery]
GO

SELECT * FROM turblib..[barycentric_weights_z_6]
