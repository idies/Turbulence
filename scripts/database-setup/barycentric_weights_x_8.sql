USE [turblib]

DROP TABLE [turblib].[dbo].[barycentric_weights_x_8]
GO
CREATE TABLE [turblib].[dbo].[barycentric_weights_x_8](
	[w0] float NOT NULL,
	[w1] float NOT NULL,
	[w2] float NOT NULL,
	[w3] float NOT NULL,
	[w4] float NOT NULL,
	[w5] float NOT NULL,
	[w6] float NOT NULL,
	[w7] float NOT NULL
)
GO
    
INSERT INTO [turblib].[dbo].[barycentric_weights_x_8]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4]
           ,[w5]
           ,[w6]
           ,[w7])
     VALUES
           (-4.7336932529283066e+09, 3.3135852770498146e+10, -9.9407558311494446e+10, 1.6567926385249069e+11, -1.6567926385249072e+11, 9.9407558311494431e+10, -3.3135852770498146e+10, 4.7336932529283066e+09)
GO

GRANT SELECT ON [barycentric_weights_x_8] TO [turbquery]
GO

SELECT * FROM turblib..barycentric_weights_x_8
