USE [turblib]

--DROP TABLE [turblib].[dbo].[diff_matrix_x_r1_fd8]
--GO
CREATE TABLE [turblib].[dbo].[diff_matrix_x_r1_fd8](
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
    
INSERT INTO [turblib].[dbo].[diff_matrix_x_r1_fd8]
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
           (2.9102618165375149e-01, -3.1042792709733500e+00, 1.6297466172610086e+01, -6.5189864690440317e+01, -1.0325074129013956e-14, 6.5189864690440331e+01, -1.6297466172610086e+01, 3.1042792709733495e+00, -2.9102618165375149e-01)
GO

GRANT SELECT ON [diff_matrix_x_r1_fd8] TO [turbquery]
GO

SELECT * FROM [turblib].[dbo].[diff_matrix_x_r1_fd8]