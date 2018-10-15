
--===================
-- generate insert / select statements
-- to insert into partitioned table


declare @dbname sysname
declare @sourceDB sysname
declare @sliceNum int
declare @npart int
declare @createPartitionedTable nvarchar(2000)
declare @sql nvarchar(max)
declare @ptable sysname
declare @vtable sysname


DECLARE @NewLine AS CHAR(2) = CHAR(13) + CHAR(10)

set @dbname = db_name()
set @sliceNum = cast(right(@dbname, 3) as int)

set @sourceDB = 'Turbdb' + cast(@sliceNum as nvarchar)

select @nPart = max(partitionNum)
				from PartLimits208
				where sliceNum = @sliceNum


declare @count int
set @count = 1


set @sql = 'DBCC TRACEON(610)'
print @sql --to get minimal logging

while (@count <= @npart)
begin
	declare @minLim bigint, @maxLim bigint
	declare @vtablename sysname, @ptablename sysname,  @fgname sysname



	select @minLim=minLim, @maxLim=maxLim
	from PartLimits208
	where sliceNum=@sliceNum
	and PartitionNum = @count


	set @vtable = 'vel'
	set @ptable = 'pr'
	set @vtablename = 'vel_' + RIGHT('00'+rtrim(CAST(@count as nvarchar)),2)
	set @ptablename = 'pr_' + RIGHT('00'+rtrim(CAST(@count as nvarchar)),2)
	-- set @fgname = 'FG'+ RIGHT('00'+rtrim(CAST(@count as nvarchar)),2)
	set @fgname = 'DATA'

	


	set @sql = concat(
					  'INSERT ', @vtablename ,' WITH (TABLOCK)'  ,
					  'SELECT * FROM ', @sourceDB, '.dbo.', @vtable, ' WITH (NOLOCK) ',
					  'WHERE TIMESTEP IN (10250, 10260) ',
					  'AND ZINDEX > ', @minLim, ' AND ZINDEX <= ', @maxLim)

	print @sql
	print @newline

	set @sql = concat( 'INSERT ', @ptablename ,' WITH (TABLOCK)'  ,
					  'SELECT * FROM ', @sourceDB, '.dbo.', @ptable, ' WITH (NOLOCK) ',
					  'WHERE TIMESTEP IN (10250, 10260) ',
					  'AND ZINDEX > ', @minLim, ' AND ZINDEX <= ', @maxLim)
	print @sql

	print @newline

	set @count = @count + 1
end


