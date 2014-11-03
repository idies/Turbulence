USE [turblib]
GO

/****** Object:  StoredProcedure [dbo].[spCreateSwitchInTablePr08]    Script Date: 09/20/2013 15:33:01 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[spCreateSwitchInTablePr08]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[spCreateSwitchInTablePr08]
GO

/****** Object:  StoredProcedure [dbo].[spCreateSwitchInTablePr08]    Script Date: 09/20/2013 15:33:01 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


--
CREATE PROCEDURE [dbo].[spCreateSwitchInTablePr08](@dbname sysname, @count int, @sliceNum int, @npart int, @doExecute bit)
AS BEGIN

/*test
exec [dbo].[spCreateSwitchInTablePr08] 11, 102 , 16, 0
*/


declare @sql nvarchar(max)
declare @tablename nvarchar(100)
declare @minLim bigint
declare @maxLim bigint
	
	set nocount on
	
	set @sql = 'select @minLimOut=minLim, @maxLimOut=maxLim from turblib.dbo.PartLimits08 
				where partitionNum=@count and sliceNum=@sliceNum'
	exec sp_executesql @sql, N'@count int, @sliceNum int, @minLimOut bigint output, @maxLimOut bigint output', 
			@count, @sliceNum, 
			@minLimOut = @minLim output, @maxLimOut = @maxLim output
			
	
		set @sql = 'use ' + @dbname + 
		'
		--IF  EXISTS (SELECT * FROM sys.check_constraints WHERE 
		--	object_id = OBJECT_ID(N''ck_p' +CAST(@count as nvarchar)+''') 
		--	AND parent_object_id = OBJECT_ID(N''p_'+RIGHT('00'+CAST(@count as nvarchar(2)),2)+'''))
		--ALTER TABLE p_'+RIGHT('00'+CAST(@count as nvarchar(2)),2)+' 
		--DROP CONSTRAINT ck_p'+CAST(@count as nvarchar)+'

		--IF  EXISTS (SELECT * FROM sys.objects WHERE 
		--	object_id = OBJECT_ID(N''p_'+RIGHT('00'+CAST(@count as nvarchar(2)),2)+''') 
		--	AND type in (N''U''))
		--DROP TABLE p_'+RIGHT('00'+CAST(@count as nvarchar(2)),2)+'

		CREATE TABLE p_'+RIGHT('00'+CAST(@count as nvarchar(2)),2)+'(
		[timestep] [int] NOT NULL,
		[zindex] [bigint] NOT NULL,
		[data] [varbinary](2072) NOT NULL) 
		on FG'+RIGHT('00'+CAST(@count as nvarchar(2)),2) + '
		'
	
	print @sql
	
	if (@doExecute = 1)
		exec sp_executesql @sql

	
	if (@count = 1)
	begin
		set @sql = 'use ' + @dbname + 
		'		
		alter table p_'+RIGHT('00'+CAST(@count as nvarchar(2)),2)+'
		add constraint ck_p'+ CAST(@count as nvarchar)+ '
		check (zindex <= ' + CAST(@maxLim as nvarchar) + ')
		'
	end
	else if (@count = @npart)
	begin
		set @sql = 'use ' + @dbname + 
		'		
		alter table p_'+RIGHT('00'+CAST(@count as nvarchar(2)),2)+'
		add constraint ck_p'+ CAST(@count as nvarchar)+ '
		check (zindex > ' + CAST(@minLim - 1 as nvarchar) + ')
		'
	end			
	
	else
	begin
		set @sql = 'use ' + @dbname + 
		'
		alter table p_'+RIGHT('00'+CAST(@count as nvarchar(2)),2)+'
		add constraint ck_p'+ CAST(@count as nvarchar)+ '
		check (zindex > ' + CAST(@minLim - 1 as nvarchar) + ' and zindex <= ' + CAST(@maxlim as nvarchar) + ')
		'
	end
	
	print @sql
	
	if (@doExecute = 1)
		exec sp_executesql @sql

	
	set @sql = 'use ' + @dbname + 
	'	
		alter table p_'+RIGHT('00'+CAST(@count as nvarchar(2)),2)+'
		add constraint pk_p_'+ CAST(@count as nvarchar)+ '
		primary key clustered(timestep, zindex)
		WITH (SORT_IN_TEMPDB = ON)
		on FG'+RIGHT('00'+CAST(@count as nvarchar(2)),2)
	
	print @sql
	
	if (@doExecute = 1)
		exec sp_executesql @sql
		
end

GO
