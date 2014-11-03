---------------------
--  Variables to set
---------------------

DECLARE @nPart int
DECLARE @i int
DECLARE @stagingTable sysname
DECLARE @tablePrefix nvarchar(10)
DECLARE @tableName sysname
DECLARE @sql nvarchar(max)
DECLARE @doExecute bit

SET @tablePrefix = 'p'
SET @tableName = 'pr'
SET @nPart = 24
SET @i = 1
SET @doExecute = 0

WHILE (@i <= @nPart)
BEGIN
	SET @stagingTable = @tablePrefix + '_' + RIGHT('00'+CAST(@i as nvarchar(2)),2)
	SET @sql = 'ALTER TABLE ' + @stagingTable + ' WITH CHECK CHECK CONSTRAINT ck_' + @tablePrefix + CAST(@i AS nvarchar(2))
	PRINT @sql	
	if (@doExecute = 1)
		exec sp_executesql @sql
		
	SET @sql = 'ALTER TABLE ' + @stagingTable + ' SWITCH TO ' + @tableName + ' PARTITION ' + CAST(@i AS nvarchar(2))
	PRINT @sql	
	if (@doExecute = 1)
		exec sp_executesql @sql
		
	SET @i = @i + 1
END