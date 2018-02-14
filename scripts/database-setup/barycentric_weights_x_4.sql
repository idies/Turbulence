USE [turbdev]

DROP TABLE [dbo].[BL_barycentric_weights_x_4]
GO
CREATE TABLE [dbo].[BL_barycentric_weights_x_4](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float
)
GO
    
INSERT INTO [dbo].[BL_barycentric_weights_x_4]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3])
     VALUES
           (-6.6797690613913581e+00, 2.0039307184174074e+01, -2.0039307184174074e+01, 6.6797690613913581e+00)
GO

GRANT SELECT ON [BL_barycentric_weights_x_4] TO [turbquery]
GO

SELECT * FROM turbdev..BL_barycentric_weights_x_4