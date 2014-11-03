USE [turblib]

--DROP TABLE [turblib].[dbo].[diff_matrix_x_r2_fd8]
--GO
CREATE TABLE [turblib].[dbo].[diff_matrix_x_r2_fd8](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float,
	[w5] float,
	[w6] float,
	[w7] float,
	[w8] float
)
GO
    
INSERT INTO [turblib].[dbo].[diff_matrix_x_r2_fd8]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4]
           ,[w5]
           ,[w6]
           ,[w7]
           ,[w8])
     VALUES
           (-1.1857473377114736e+01, 1.6863962136340962e+02, -1.3280370182368504e+03, 1.0624296145894798e+04, -1.8906082551288484e+04, 1.0624296145894796e+04, -1.3280370182368497e+03, 1.6863962136340945e+02, -1.1857473377114724e+01)
GO

GRANT SELECT ON [diff_matrix_x_r2_fd8] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_x_r2_fd8]