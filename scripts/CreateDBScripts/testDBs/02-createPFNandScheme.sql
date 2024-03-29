/*
Script to create partition function for testDBs

Run this in the test database.  
Table PartLimits208 must exist in the current database.


IMPORTANT: this script does not execute the commands, it just prints them
in the "messages" window of ssms.  you have to copy/paste to another window in order 
to run them.  (safer this way until we are sure there are no errors.)
*/



---------------------
--  Variables (shouldn't have to change the defaults)
---------------------

declare @nPart int
declare @sliceNum int
declare @dbName sysname


--set @nPart = 16
set @dbName = db_name()



set @sliceNum = cast(right(@dbname, 3) as int)

select @nPart = max(partitionNum)
				from PartLimits208
				where sliceNum = @sliceNum



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

set @sql = '
use ' + @dbName +';

create partition function zindexPFN(bigint) as range left for values('+@clause+')'

print '--==================================================================================================================

-- PARTITON FUNCTION

'
print @sql


--generate partition scheme, all ranges to DATA filegroup

set @sql = '
create partition scheme zindexPartScheme
as partition zindexPFN
all to ( [DATA] )
'
print @sql