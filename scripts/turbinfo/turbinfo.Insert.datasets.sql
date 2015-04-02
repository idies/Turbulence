use [turbinfo]
SET IDENTITY_INSERT [dbo].[datasets] ON 

GO
INSERT [dbo].[datasets] ([DatasetID], [name], [isUserCreated], [ScratchID], [schemaname], [SourceDatasetID], [minLim], [maxLim], [maxTime], [dt], [timeinc], [timeoff], [thigh], [xhigh], [yhigh], [zhigh]) VALUES (3, N'mhd1024', NULL, NULL, NULL, NULL, 0, 1073741823, 2.501, 0.00025, 10, 0, 1025, 1024, 1024, 1024)
GO
INSERT [dbo].[datasets] ([DatasetID], [name], [isUserCreated], [ScratchID], [schemaname], [SourceDatasetID], [minLim], [maxLim], [maxTime], [dt], [timeinc], [timeoff], [thigh], [xhigh], [yhigh], [zhigh]) VALUES (4, N'isotropic1024coarse', NULL, NULL, NULL, NULL, 0, 1073741823, 2.05, 0.0002, 10, 0, 1025, 1024, 1024, 1024)
GO
INSERT [dbo].[datasets] ([DatasetID], [name], [isUserCreated], [ScratchID], [schemaname], [SourceDatasetID], [minLim], [maxLim], [maxTime], [dt], [timeinc], [timeoff], [thigh], [xhigh], [yhigh], [zhigh]) VALUES (5, N'isotropic1024fine', NULL, NULL, NULL, NULL, 0, 1073741823, 0.0198, 0.0002, 1, 0, 1025, 1024, 1024, 1024)
GO
INSERT [dbo].[datasets] ([DatasetID], [name], [isUserCreated], [ScratchID], [schemaname], [SourceDatasetID], [minLim], [maxLim], [maxTime], [dt], [timeinc], [timeoff], [thigh], [xhigh], [yhigh], [zhigh]) VALUES (6, N'channel', NULL, NULL, NULL, NULL, 0, 5637144575, 25.9935, 0.0013, 5, 13200, 400, 2048, 512, 1536)
GO
INSERT [dbo].[datasets] ([DatasetID], [name], [isUserCreated], [ScratchID], [schemaname], [SourceDatasetID], [minLim], [maxLim], [maxTime], [dt], [timeinc], [timeoff], [thigh], [xhigh], [yhigh], [zhigh]) VALUES (7, N'mixing', NULL, NULL, NULL, NULL, 0, 1073741823, 40.44, 0.04, 1, 1, 1015, 1024, 1024, 1024)
GO
INSERT [dbo].[datasets] ([DatasetID], [name], [isUserCreated], [ScratchID], [schemaname], [SourceDatasetID], [minLim], [maxLim], [maxTime], [dt], [timeinc], [timeoff], [thigh], [xhigh], [yhigh], [zhigh]) VALUES (8, N'mhddev_hamilton', NULL, NULL, NULL, NULL, 0, 640000, 2.05, 0.0002, 10, 0, 2, 64, 64, 64)
GO
INSERT [dbo].[datasets] ([DatasetID], [name], [isUserCreated], [ScratchID], [schemaname], [SourceDatasetID], [minLim], [maxLim], [maxTime], [dt], [timeinc], [timeoff], [thigh], [xhigh], [yhigh], [zhigh]) VALUES (9, N'isotropic1024coarse_suetest_test1', 1, 1, N'suetest_test1', 4, 0, 134217727, 0.02, 0.0002, 10, 0, 100, 511, 511, 511)
GO
INSERT [dbo].[datasets] ([DatasetID], [name], [isUserCreated], [ScratchID], [schemaname], [SourceDatasetID], [minLim], [maxLim], [maxTime], [dt], [timeinc], [timeoff], [thigh], [xhigh], [yhigh], [zhigh]) VALUES (10, N'isotropic1024coarse_wsid_1539980082_myvelocity1', 1, 1, N'wsid_1539980082', 4, 0, 134217727, 0.02, 0.0002, 10, 0, 100, 511, 511, 511)
GO
SET IDENTITY_INSERT [dbo].[datasets] OFF
GO