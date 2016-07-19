

declare @tablename sysname
declare @cname sysname
declare @sql nvarchar(max)

declare cur cursor for
SELECT   TABLE_NAME, 
         --COLUMN_NAME, 
         --CHECK_CLAUSE, 
         --cc.CONSTRAINT_SCHEMA, 
         cc.CONSTRAINT_NAME 
FROM     INFORMATION_SCHEMA.CHECK_CONSTRAINTS cc 
         INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE c 
           ON cc.CONSTRAINT_NAME = c.CONSTRAINT_NAME
WHERE    TABLE_NAME LIKE '%vel%' 
ORDER BY 
--CONSTRAINT_SCHEMA, 
         TABLE_NAME, 
         COLUMN_NAME 


open cur
fetch next from cur into @tablename, @cname

while (@@FETCH_STATUS = 0)
begin
	set @sql = 'alter table ' + @tablename + ' drop constraint ' + @cname

	print @sql
	
	fetch next from cur into @tablename, @cname
end

close cur
deallocate cur