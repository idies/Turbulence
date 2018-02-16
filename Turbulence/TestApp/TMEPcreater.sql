USE [Turbulence]
GO

--CREATE TABLE [dbo].[#temp_zw] (
--    [reqseq] INT    NULL,
--    [zindex] BIGINT NULL,
--    [x]      REAL   NULL,
--    [y]      REAL   NULL,
--    [z]      REAL   NULL
--);
INSERT INTO [dbo].[#temp_zw]  
VALUES (0, 0, 0, 0, 0); 

SELECT reqseq, zindex, x, y, z FROM #temp_zw