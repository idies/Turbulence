/*

Script to set up partition function, partition scheme, and filegroups
for turbulence DB

This script does not run the statements, just generates them.
Copy and paste into new SSMS window to execute.

S.Werner

*/

use turblib

---------------------
--  Variables to set
---------------------

declare @nPart int
declare @sliceNum int
declare @dbName sysname
declare @doExecute bit

set @nPart = 24
set @sliceNum = 1
set @dbName = '[channeldb01]'
set @doExecute = 1


declare @count int
----------------------------------------------
--create partition function
----------------------------------------------

declare @sql nvarchar(max)
declare @clause nvarchar(max)

select @clause = coalesce(@clause + ', ', '') + cast(maxLim as nvarchar) 
from PartLimits08
where sliceNum=@sliceNum
and partitionNum < @nPart

set @sql = 'use ' + @dbName +
	' create partition function zindexPFN(bigint) as range left for values('+@clause+')'

print @sql

if (@doExecute = 1)
	exec sp_executesql @sql

-----------------------------------------------
-- create FGs
-----------------------------------------------

--set @count=1
--while (@count <= @nPart)
--begin
--	set @sql = 'alter database ' + @dbName + '
--			add filegroup [FG'+ CAST(@count as nvarchar)+']'

--	print @sql
	
--	set @count=@count+1
--end

-----------------------------------------------
--create partition scheme
-----------------------------------------------

set @count=1

set @sql = 'use ' + @dbName +
	' create partition scheme zindexPartScheme as partition zindexPFN to ('
while (@count <= @nPart)
begin
	if (@count < 10)
		set @sql = @sql + 'FG0'+CAST(@count as nvarchar)
	else
		set @sql = @sql + 'FG' +CAST(@count as nvarchar)
	if (@count < @nPart)
		set @sql = @sql + ','
	set @count = @count + 1
end
set @sql = @sql + ')'

print @sql

if (@doExecute = 1)
	exec sp_executesql @sql

/*
alter database sue4files
add file
	(name='sue4files_FG1_1',
	 filename='c:\data\data1\sql_db\sue4files_FG1_1.ndf',
	 size=16GB,
	 filegrowth=100MB)
to filegroup [FG1]
*/


-------------------------------------------------
-- generate files
-------------------------------------------------

--set @count=1
--declare @datavol nvarchar

--while (@count <= @nPart)
--begin

--	if @count % 4 = 0
--		set @datavol = CAST(4 as nvarchar)
--	else
--		set @datavol = CAST(@count % 4 as nvarchar)

--	set @sql = 'alter database ' + @dbName + '
--	add file(
--			name='''+@dbName+'_FG'+CAST(@count as nvarchar)+''',
--			filename=''c:\data\data'+@datavol+'\sql_db\'+@dbName+'_FG'+CAST(@count as nvarchar)+''',
--			size=500GB,
--			filegrowth=100MB)
--		to filegroup [FG'+CAST(@count as nvarchar)+']'
		
--	print @sql
		
--set @count=@count+1
--end

