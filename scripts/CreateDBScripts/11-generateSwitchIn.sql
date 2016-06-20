/*
	11-generateSwitchIn.sql
	S.Werner 6/20/2016

	This script generates switch in statements for pressure or velocity tables.
	Run this script in the DB for which you want to generate the switch statements
	(although they should be the same for each DB with the same number of partitions, really)
	
	--=============
	-- IMPORTANT: set @tablename variable to 'pr' or 'vel' (or whatever your partitioned table / switch tables are named)
	--=============
	
	Note: This script does not execute the statments, just generates them.
	Copy the resulting output from the Results pane into a new SSMS window to execute. 

*/
declare @tablename sysname
declare @dbname sysname
declare @sliceNum int
declare @npart int
declare @sql nvarchar(max)
DECLARE @NewLine AS CHAR(2) = CHAR(13) + CHAR(10)
declare @count int

--===========================================
-- SET THIS!
--===========================================
set @tablename = 'vel'


-------------------------------------------
set @dbname = db_name()
set @sliceNum = cast(right(@dbname, 3) as int)

select @nPart = max(partitionNum)
				from PartLimits208
				where sliceNum = @sliceNum


set @count = 1
while (@count <= @npart)
begin
	
	declare @switchtable sysname

	set @switchtable = @tablename + '_' + RIGHT('00'+rtrim(CAST(@count as nvarchar)),2)

	set @sql = 'ALTER TABLE ' + @switchtable +'
				SWITCH TO ' + @tablename +'
				PARTITON ' + cast(@count as nvarchar)

	print @sql
	print @newline
end








