USE [turblib]

--DROP TABLE [turblib].[dbo].[diff_matrix_z_r1_fd8]
--GO
CREATE TABLE [turblib].[dbo].[diff_matrix_z_r1_fd8](
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
    
INSERT INTO [turblib].[dbo].[diff_matrix_z_r1_fd8]
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
           (5.8205236330750298e-01, -6.2085585419467000e+00, 3.2594932345220172e+01, -1.3037972938088063e+02, -2.0650148258027912e-14, 1.3037972938088066e+02, -3.2594932345220172e+01, 6.2085585419466991e+00, -5.8205236330750298e-01)
GO

GRANT SELECT ON [diff_matrix_z_r1_fd8] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_z_r1_fd8]