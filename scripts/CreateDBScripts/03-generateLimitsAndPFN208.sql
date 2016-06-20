/*
03-generateLimitsAndPFN208.sql
S.Werner 6/20/2016

Script to set up partition function, partition scheme, 
filegroups and files for turbulence DB

Run this in the DB you're trying to create.

This script does not run the statements, just generates them.
Copy and paste into new SSMS window to execute.


--============
IMPORTANT!!!!!
--============
--This script assumes a 12-way RAID-1 or whatever
--with the volumes laid out like c:\data\data1, c:\data\data2 etc
--if the paths are different, you'll need to change the "ADD FILE" section

--Also, this script assumes that there will be 2 db's per machine, 
--the first DB (turbdb201 for example) will be spread over 4 data1-data4
--and the second DB (turbdb202 for example) will be spread over data5-data8


This script does not run the statements, just generates them.
Copy and paste into new SSMS window to execute.


S.Werner

*/



---------------------
--  Variables (shouldn't have to change the defaults)
---------------------

declare @nPart int
declare @sliceNum int
declare @dbName sysname
declare @nFiles int -- files per FG
declare @startVol int  --e.g. 1 for data1, 5 for data5, etc
declare @nVol int --number of volumes to spread files across (4)

--set @nPart = 16
set @dbName = db_name()
set @nFiles = 4 --files per FG (4)
set @startVol = 1 --1 or 5
set @nVol = 4


set @sliceNum = cast(right(@dbname, 3) as int)

select @nPart = max(partitionNum)
				from PartLimits208
				where sliceNum = @sliceNum



--odd numbered db's are on data1-data4
--even numbered db's are on data5-data8
if (@sliceNum % 2) = 0
	set @startVol = 5
else 
	set @startVol = 1

declare @count int
----------------------------------------------
--create partition function
----------------------------------------------

declare @sql nvarchar(max)
declare @clause nvarchar(max)

select @clause = coalesce(@clause + ', ', '') + cast(maxLim as nvarchar) 
from PartLimits208
where sliceNum=@sliceNum
and partitionNum < @nPart

set @sql = 'create partition function zindexPFN(bigint) as range left for values('+@clause+')'

print '--==================================================================================================================

PARTITON FUNCTION

'
print @sql




-------------------------------------------------
---- create FGs
-------------------------------------------------
print '--==================================================================================================================

-- FILEGROUPS

--==================================================================================================================
   

'


set @count=1
while (@count <= @nPart)
begin
	set @sql = 'alter database ' + @dbName + '
			add filegroup [FG'+ RIGHT('00'+rtrim(CAST(@count as nvarchar)),2)+']'

	print @sql
	
	set @count=@count+1
end



-------------------------------------------------
----create partition scheme
-------------------------------------------------

print '--==================================================================================================================


-- PARTITION SCHEME

--==================================================================================================================


'

set @count=1

set @sql = 'create partition scheme zindexPartScheme as partition zindexPFN to ('
while (@count <= @nPart)
begin
	
		set @sql = @sql + 'FG'+ RIGHT('00'+rtrim(CAST(@count as nvarchar)),2)
	if (@count < @nPart)
		set @sql = @sql + ','
	set @count = @count + 1
end
set @sql = @sql + ')'

print @sql



--/*
--alter database sue4files
--add file
--	(name='sue4files_FG1_1',
--	 filename='c:\data\data1\sql_db\sue4files_FG1_1.ndf',
--	 size=16GB,
--	 filegrowth=100MB)
--to filegroup [FG1]
--*/


-------------------------------------------------
-- generate files
-------------------------------------------------

print '--==================================================================================================================


-- FILES

--==================================================================================================================


'

set @count=1
declare @datavol int

while (@count <= @npart)
begin
	
	declare @fgname nvarchar(12)
	set @fgname = 'FG'+ RIGHT('00'+rtrim(CAST(@count as nvarchar)),2)

	if (@count % @nvol = 0)
		set @datavol = (@startvol + @nvol) - 1
	else 
		set @datavol = @startvol + (@count % @nvol) - 1


		
		declare @fcount int
		set @fcount = 1
		while (@fcount <= @nFiles)
		begin
			
			set @sql = 'alter database ' + @dbName + '		
				add file(
				name='''+@dbName+'_'+@fgname+'_'+cast(@fcount as nvarchar)+''',
				filename=''c:\data\data'+cast(@datavol as nvarchar)+'\sql_db\'+@dbName+'_'+@fgname+'_'+cast(@fcount as nvarchar)+'.ndf'',
				size=200GB,
				filegrowth=100MB)
			to filegroup ['+@fgname+']'

			print @sql
			set @fcount = @fcount + 1
		end

	set @count = @count + 1
end






/*
while (@count <= @nPart)
begin

	declare @fgname nvarchar(12)

	if @count % 4 = 0
		set @datavol = CAST(4 as nvarchar)
	else
		set @datavol = CAST(@count % 4 as nvarchar)
	--if (@count < 10)
	--	set @fgname = 'FG0'+CAST(@count as nvarchar)
	--else 
	--	set @fgname = 'FG'+CAST(@count as nvarchar)

	set @fgname = 'FG'+ RIGHT('00'+rtrim(CAST(@count as nvarchar)),2)

	--file per filegroup
	declare @fcount int 
	set @fcount = 1
	while (@fcount <= @nFiles)
	begin

	set @sql = 'alter database ' + @dbName + '
	add file(
			name='''+@dbName+'_'+@fgname+'_'+cast(@fcount as nvarchar)+''',
			filename=''c:\data\data'+cast(@fcount as nvarchar)+'\sql_db\'+@dbName+'_'+@fgname+'_'+cast(@fcount as nvarchar)+'.ndf'',
			size=200GB,
			filegrowth=100MB)
		to filegroup ['+@fgname+']'
		
	print @sql
	set @fcount = @fcount + 1
	
	end	
set @count=@count+1
end

*/










	

