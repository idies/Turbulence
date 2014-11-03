USE [turblib]

DROP TABLE [turblib].[dbo].[barycentric_weights_x_6]
GO
CREATE TABLE [turblib].[dbo].[barycentric_weights_x_6](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float,
	[w5] float
)
GO
  
INSERT INTO [turblib].[dbo].[barycentric_weights_x_6]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4]
           ,[w5])
     VALUES
           (-2.9941201019673839e+07, 1.4970600509836921e+08, -2.9941201019673842e+08, 2.9941201019673842e+08, -1.4970600509836921e+08, 2.9941201019673839e+07)
GO

GRANT SELECT ON [barycentric_weights_x_6] TO [turbquery]
GO

SELECT * FROM turblib..barycentric_weights_x_6