USE [turbdev]

--DROP TABLE [turbdev].[dbo].[spline_coeff_y_m1q4_d0]
--GO
CREATE TABLE [turbdev].[dbo].[spline_coeff_y_m1q4_d0](
	[cell_index] int NOT NULL,
	[neighbor_index] int NOT NULL,
	[w0] float NOT NULL,
	[w1] float NOT NULL,
	[w2] float NOT NULL,
	[w3] float NOT NULL
)
GO
CREATE CLUSTERED INDEX [pk_spline_coeff_y_m1q4_d0_cell_index] ON [dbo].[spline_coeff_y_m1q4_d0] 
(
	[cell_index] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

BULK INSERT [turbdev].[dbo].[spline_coeff_y_m1q4_d0]
FROM N'\\tenacious\kalin\channel-splines\channel_yspline_m1q04_d0_coeff.csv'
WITH
(
FIELDTERMINATOR = ',',
ROWTERMINATOR = '\n'
)
GO

GRANT SELECT ON [spline_coeff_y_m1q4_d0] TO [turbquery]
GO

SELECT * FROM [turbdev].[dbo].[spline_coeff_y_m1q4_d0]