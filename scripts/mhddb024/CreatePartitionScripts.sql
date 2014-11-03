/*

Script to set up partition function, partition scheme, and filegroups
for turbulence DB

This script does not run the statements, just generates them.
Copy and paste into new SSMS window to execute.

S.Werner

*/


---------------------
--  Variables to set
---------------------

declare @nPart int
declare @sliceNum int
declare @dbName sysname

set @nPart = 16
set @sliceNum = 24
set @dbName = 'mhddb024'



declare @count int
----------------------------------------------
--create partition function
----------------------------------------------

declare @sql nvarchar(max)
declare @clause nvarchar(max)

select @clause = coalesce(@clause + ', ', '') + cast(maxLim as nvarchar) 
from PartLimits
where sliceNum=@sliceNum
and partitionNum < @nPart

set @sql = 'create partition function zindexPFN(bigint) as range left for values('+@clause+')'

print @sql

-----------------------------------------------
-- create FGs
-----------------------------------------------

set @count=1
while (@count <= @nPart)
begin
	set @sql = 'alter database ' + @dbName + '
			add filegroup [FG'+ CAST(@count as nvarchar)+']'

	print @sql
	
	set @count=@count+1
end



-----------------------------------------------
--create partition scheme
-----------------------------------------------



set @count=1

set @sql = 'create partition scheme zindexPartScheme as partition zindexPFN to ('
while (@count <= @nPart)
begin
	set @sql = @sql + 'FG'+CAST(@count as nvarchar)
	if (@count < @nPart)
		set @sql = @sql + ','
	set @count = @count + 1
end
set @sql = @sql + ')'

print @sql

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

set @count=1
declare @datavol nvarchar

while (@count <= @nPart)
begin

	if @count % 4 = 0
		set @datavol = CAST(4 as nvarchar)
	else
		set @datavol = CAST(@count % 4 as nvarchar)

	set @sql = 'alter database ' + @dbName + '
	add file(
			name='''+@dbName+'_FG'+CAST(@count as nvarchar)+''',
			filename=''c:\data\data'+@datavol+'\sql_db\'+@dbName+'_FG'+CAST(@count as nvarchar)+''',
			size=500GB,
			filegrowth=100MB)
		to filegroup [FG'+CAST(@count as nvarchar)+']'
		
	print @sql
		
set @count=@count+1
end
