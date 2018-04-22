USE turblib
SELECT [turblib].[dbo].[CreateMortonIndex] (50,50,50)

SELECT [turblib].[dbo].[GetMortonX] (1207959552)
UNION ALL
SELECT [turblib].[dbo].[GetMortonY] (1207959552)
UNION ALL
SELECT [turblib].[dbo].[GetMortonZ] (1207959552)