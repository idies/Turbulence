USE [turblib]

--DROP TABLE [turblib].[dbo].[diff_matrix_z_r1_fd4]
--GO
CREATE TABLE [turblib].[dbo].[diff_matrix_z_r1_fd4](
	[w0] float,
	[w1] float,
	[w2] float,
	[w3] float,
	[w4] float
)
GO
    
INSERT INTO [turblib].[dbo].[diff_matrix_z_r1_fd4]
           ([w0]
           ,[w1]
           ,[w2]
           ,[w3]
           ,[w4])
     VALUES
           (1.3581221810508401e+01, -1.0864977448406721e+02, 5.3290705182007514e-15, 1.0864977448406721e+02, -1.3581221810508401e+01)
GO

GRANT SELECT ON [diff_matrix_z_r1_fd4] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_z_r1_fd4]