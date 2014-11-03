USE [turblib]

DROP TABLE [dbo].[barycentric_weights_x_4]
GO
CREATE TABLE [dbo].[barycentric_weights_x_4](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float
)
GO
    
INSERT INTO [dbo].[barycentric_weights_x_4]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3])
     VALUES
           (-9.0181826586204246e+04, 2.7054547975861275e+05, -2.7054547975861275e+05, 9.0181826586204246e+04)
GO

GRANT SELECT ON [barycentric_weights_x_4] TO [turbquery]
GO

SELECT * FROM turblib..barycentric_weights_x_4