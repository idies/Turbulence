USE [mhdlib]
GO

/****** Object:  StoredProcedure [dbo].[GetRawData]    Script Date: 05/11/2011 16:59:36 ******/
--IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetRawData]') AND type in (N'P', N'PC'))
--DROP PROCEDURE [dbo].[GetRawData]
--GO

/****** Object:  StoredProcedure [dbo].[GetRawData]    Script Date: 05/11/2011 16:28:00 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[GetRawData]
	@dbname [nvarchar] (1000),
	@tableName [nvarchar](1000),
	@time [int],
	@X [int],
	@Y [int],
	@Z [int],
	@Xwidth [int],
	@Ywidth [int],
	@Zwidth [int]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    declare @sqlCmd nvarchar(4000)
    declare @Rank int

    create table #tempzindex
    (zindex int)
    
    set @sqlCmd = 'insert into #tempzindex
				select i.zindex
				from '+@dbname+'..zindex as i
				where 
				i.X >= @X and i.X < @X + @Xwidth and 
				i.Y >= @Y and i.Y < @Y + @Ywidth and 
				i.Z >= @Z and i.Z < @Z + @Zwidth;'
	exec sp_executesql @sqlCmd, N'@X int, @Y int, @Z int, @Xwidth int, @Ywidth int, @Zwidth int', 
				@X, @Y, @Z, @Xwidth, @Ywidth, @Zwidth
    
    set @sqlCmd = 'select @RankOut = RealArray.Rank(data)
				from ' + @dbname + '..' + @tableName + '
				where timestep = @time and zindex = (select top 1 zindex from #tempzindex)'
    
    exec sp_executesql @sqlCmd, N'@time int, @RankOut int output', @time, @RankOut = @Rank output
    
    create table #tempArrayTable
    ( offset varbinary(8000), data varbinary(Max))
    
    -- If a vector field we need to use Vector_4 (3d array storing 3 values)
    -- Depending on how the vector field is stored the number of components is either first or last
    -- e.g. IntArray.Vector_4(0,X,Y,Z) vs InArray.Vector_4(X,Y,Z,0)
    IF @Rank > 3 
	BEGIN
		set @sqlCmd = 'insert into #tempArrayTable 
					select IntArray.Vector_4(0,
							dbo.GetMortonX(t.zindex) - @X,
							dbo.GetMortonY(t.zindex) - @Y,
							dbo.GetMortonZ(t.zindex) - @Z) 
							AS offset,
							RealArray.ConvertToRealArrayMax(data) AS data 
					from ' + @dbname + '..' + @tableName + ' as t, #tempzindex
					where t.zindex = #tempzindex.zindex and timestep = @time; '
		exec sp_executesql @sqlCmd, N'@time int, @X int, @Y int, @Z int', @time, @X, @Y, @Z
		
		drop table #tempzindex
		
		select RealArrayMax.Raw(
			RealArrayMax.FromSubarrayTable('#tempArrayTable', IntArray.Vector_4(3,@Xwidth,@Ywidth,@Zwidth))) as data
	END
	-- If a scalar field we need to use Vector_3 (3d array storing a single value at each point)
	ELSE
	BEGIN
		set @sqlCmd = 'insert into #tempArrayTable 
					select IntArray.Vector_3(dbo.GetMortonX(t.zindex) - @X,
							dbo.GetMortonY(t.zindex) - @Y,
							dbo.GetMortonZ(t.zindex) - @Z) AS offset,
							RealArray.ConvertToRealArrayMax(data) AS data 
					from ' + @dbname + '..' + @tableName + ' as t, #tempzindex
					where t.zindex = #tempzindex.zindex and timestep = @time; '
		exec sp_executesql @sqlCmd, N'@time int, @X int, @Y int, @Z int', @time, @X, @Y, @Z
		
		drop table #tempzindex
		
		select RealArrayMax.Raw(
			RealArrayMax.FromSubarrayTable('#tempArrayTable', IntArray.Vector_3(@Xwidth,@Ywidth,@Zwidth))) as data
	END
	drop table #tempArrayTable
END

GO

GRANT EXECUTE ON [dbo].[GetRawData] TO [turbquery]
GO

