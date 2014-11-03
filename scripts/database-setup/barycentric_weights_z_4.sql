USE [turblib]

DROP TABLE [turblib].[dbo].[barycentric_weights_z_4]
GO
CREATE TABLE [turblib].[dbo].[barycentric_weights_z_4](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float
)
GO
    
INSERT INTO [turblib].[dbo].[barycentric_weights_z_4]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3])
     VALUES
           (-7.2145461268963397e+05, 2.1643638380689020e+06, -2.1643638380689020e+06, 7.2145461268963397e+05)
GO

GRANT SELECT ON [barycentric_weights_z_4] TO [turbquery]
GO

SELECT * FROM turblib..barycentric_weights_z_4