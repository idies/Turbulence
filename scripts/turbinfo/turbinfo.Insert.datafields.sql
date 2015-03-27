use [turbinfo]
GO


SET IDENTITY_INSERT [dbo].[datafields] ON 

GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (1, N'vel', 3, N'u', 3, N'velocity', N'velocity08')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (2, N'mag', 3, N'b', 3, NULL, N'magnetic08')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (3, N'vec', 3, N'a', 3, NULL, N'potential08')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (4, N'pr', 3, N'p', 1, NULL, N'pressure08')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (5, N'density', 7, N'd', 1, NULL, NULL)
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (6, N'vel', 8, N'u', 3, N'velocity', NULL)
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (7, N'vel', 4, N'u', 3, N'velocity', N'vel')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (8, N'pr', 4, N'p', 1, NULL, N'pr')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (9, N'vel', 5, N'u', 3, N'velocity', N'isotropic1024fine_vel')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (10, N'pr', 5, N'p', 1, NULL, N'isotropic1024fine_pr')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (11, N'vel', 6, N'u', 3, N'velocity', N'vel')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (12, N'pr', 6, N'p', 1, NULL, N'pr')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (13, N'vel', 7, N'u', 3, N'velocity', N'vel')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (14, N'pr', 7, N'p', 1, NULL, N'pr')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (15, N'vorticity', 4, N'w', 3, N'vorticity', N'vel')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (16, N'vorticity', 3, N'w', 3, N'vorticity', N'velocity08')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (17, N'vorticity', 5, N'w', 3, N'vorticity', N'isotropic1024fine_vel')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (18, N'vorticity', 6, N'w', 3, N'vorticity', N'vel')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (19, N'vorticity', 7, N'w', 3, N'vorticity', N'vel')
GO
INSERT [dbo].[datafields] ([DatafieldID], [name], [DatasetID], [charname], [components], [longname], [tablename]) VALUES (20, N'vel', 9, N'u', 3, N'velocity', N'velocity_9')
GO
SET IDENTITY_INSERT [dbo].[datafields] OFF