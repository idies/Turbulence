USE [turblib]

DROP TABLE [turblib].[dbo].[barycentric_weights_z_8]
GO
CREATE TABLE [turblib].[dbo].[barycentric_weights_z_8](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float,
	[w5] float,
	[w6] float,
	[w7] float
)
GO
    
INSERT INTO [turblib].[dbo].[barycentric_weights_z_8]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4]
           ,[w5]
           ,[w6]
           ,[w7])
     VALUES
           (-6.0591273637482324e+11, 4.2413891546237627e+12, -1.2724167463871289e+13, 2.1206945773118809e+13, -2.1206945773118812e+13, 1.2724167463871287e+13, -4.2413891546237627e+12, 6.0591273637482324e+11)
GO

GRANT SELECT ON [barycentric_weights_z_8] TO [turbquery]
GO

SELECT * FROM turblib..[barycentric_weights_z_8]
