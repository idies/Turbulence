/* 
	CreatePrTables.sql
	S.Werner 6/20/2016

	Script to create partitioned pr table 
	and one switch table per partition.

	Run this script in the DB for which you wish to create pressure tables.

	Note: This script does not execute the statments, just generates them.
	Copy the resulting output from the Results pane into a new SSMS window to execute.
*/


declare @dbname sysname
declare @sliceNum int
declare @npart int
declare @createPartitionedTable nvarchar(2000)
declare @sql nvarchar(max)
DECLARE @NewLine AS CHAR(2) = CHAR(13) + CHAR(10)

set @dbname = db_name()
set @sliceNum = cast(right(@dbname, 3) as int)

select @nPart = max(partitionNum)
				from PartLimits208
				where sliceNum = @sliceNum


--=======================
-- create partitioned table
--======================

set @createPartitionedTable = 'CREATE TABLE [dbo].[pr](
	[timestep] [int] NOT NULL,
	[zindex] [bigint] NOT NULL,
	[data] [varbinary](2072) NOT NULL,
 CONSTRAINT [pk_pr] PRIMARY KEY CLUSTERED 
(
	[timestep] ASC,
	[zindex] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [zindexPartScheme]([zindex])
) ON [zindexPartScheme]([zindex])'


print @createPartitionedTable
print @newline
print @newline

declare @count int
set @count = 1

while (@count <= @npart)
begin
	declare @minLim bigint, @maxLim bigint
	declare @tablename sysname, @fgname sysname

	select @minLim=minLim, @maxLim=maxLim
	from PartLimits208
	where sliceNum=@sliceNum

	set @tablename = 'pr_' + RIGHT('00'+rtrim(CAST(@count as nvarchar)),2)
	set @fgname = 'FG'+ RIGHT('00'+rtrim(CAST(@count as nvarchar)),2)


	--==============================
	-- create switch tables
	--==============================
	set @sql = 'CREATE TABLE [dbo].['+@tablename +'](
			[timestep] [int] NOT NULL,
			[zindex] [bigint] NOT NULL,
			[data] [varbinary](2072) NOT NULL,
	CONSTRAINT [pk_'+@tablename+'] PRIMARY KEY CLUSTERED 
	(
		[timestep] ASC,
		[zindex] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
	ON ['+@fgname+'])'

	print @sql
	print @newline
	
	
	--===============================
	-- add check constraint
	--===============================

	/*
	create partition function zindexPFN(bigint) as range left for values(679477247, 687865855, 696254463, 704643071, 713031679, 721420287, 729808895, 738197503, 746586111, 754974719, 763363327, 771751935, 780140543, 788529151, 796917759)

	ALTER TABLE [dbo].[pr_01]  WITH CHECK ADD  CONSTRAINT [ck_pr1] CHECK  (([zindex]<=(813694975)))
	ALTER TABLE [dbo].[pr_02]  WITH CHECK ADD  CONSTRAINT [ck_pr2] CHECK  (([zindex]>=(813694976) AND [zindex]<=(822083583)))
	ALTER TABLE [dbo].[pr_16]  WITH CHECK ADD  CONSTRAINT [ck_pr16] CHECK  (([zindex]>=(931135488)))
	*/

	--TODO: this is a little hacky but it works with the way zindexes are set up in turbulence.
	--normally you wouldn't put a check constraint that allows both boundaries, (one side would be > and the other side wold be <= or vice versa)
	--but i can't remember which one is which for turbulence, i'll fix this eventually but this works for now.
	-- i think it should be > minLim and <= maxLim but need to double check, will fix



	if (@count = 1) --first partition, only max lim
	begin
		set @sql='ALTER TABLE [dbo].['+@tablename+'] WITH CHECK ADD CONSTRAINT [ck_'+@tablename+'] CHECK (([zindex]<='+cast(@maxLim as nvarchar)+'))'
	end
	else if (@count = @npart)
	begin
		set @sql='ALTER TABLE [dbo].['+@tablename+'] WITH CHECK ADD CONSTRAINT [ck_'+@tablename+'] CHECK (([zindex]>='+cast(@minLim as nvarchar)+'))'
	end
	else --last partition, only min lim
		set @sql='ALTER TABLE [dbo].['+@tablename+'] WITH CHECK ADD CONSTRAINT [ck_'+@tablename+'] CHECK (([zindex]>='+cast(@minLim as nvarchar) + ' AND [zindex]<='+cast(@maxLim as nvarchar)+'))'

	--add with check
	set @sql = @sql + '; ALTER TABLE  [dbo].['+@tablename+'] CHECK CONSTRAINT  [ck_'+@tablename+']'


	print @sql
	print @newline
	print @newline


	set @count = @count + 1
end
	

	




