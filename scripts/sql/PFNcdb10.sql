USE [channeldb10]
GO

/****** Object:  PartitionFunction [zindexPFN]    Script Date: 12/08/2015 14:43:19 ******/
CREATE PARTITION FUNCTION [zindexPFN](bigint) AS RANGE LEFT FOR VALUES (4434777428, 4440369833, 4445962238, 4451554643, 4457147048, 4462739453, 4468331858, 4473924263, 4479516668, 4485109073, 4490701478, 4496293883, 4501886288, 4507478693, 4513071098, 4518663503, 4524255908, 4529848313, 4535440718, 4541033123, 4546625528, 4552217933, 4557810338)
GO




USE [channeldb10]
GO

/****** Object:  PartitionScheme [zindexPartScheme]    Script Date: 12/08/2015 14:43:00 ******/
CREATE PARTITION SCHEME [zindexPartScheme] AS PARTITION [zindexPFN] TO ([FG01], [FG02], [FG03], [FG04], [FG05], [FG06], [FG07], [FG08], [FG09], [FG10], [FG11], [FG12], [FG13], [FG14], [FG15], [FG16], [FG17], [FG18], [FG19], [FG20], [FG21], [FG22], [FG23], [FG24])
GO


