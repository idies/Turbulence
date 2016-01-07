
/*
drop database ts0to4

--restore DB in simple recovery mode
USE [master]
RESTORE DATABASE [ts0to4] FROM  DISK = N'C:\data\data1\ts0to4.bak' WITH  FILE = 1,  NOUNLOAD,  STATS = 5

GO

*/

--set FG's to readonly
alter database ts0to4 modify filegroup FG01 readonly
alter database ts0to4 modify filegroup FG02 readonly
alter database ts0to4 modify filegroup FG03 readonly
alter database ts0to4 modify filegroup FG04 readonly
alter database ts0to4 modify filegroup FG05 readonly
alter database ts0to4 modify filegroup FG06 readonly
alter database ts0to4 modify filegroup FG07 readonly
alter database ts0to4 modify filegroup FG08 readonly
alter database ts0to4 modify filegroup FG09 readonly
alter database ts0to4 modify filegroup FG10 readonly
alter database ts0to4 modify filegroup FG11 readonly
alter database ts0to4 modify filegroup FG12 readonly
alter database ts0to4 modify filegroup FG13 readonly
alter database ts0to4 modify filegroup FG14 readonly
alter database ts0to4 modify filegroup FG15 readonly
alter database ts0to4 modify filegroup FG16 readonly

--change to full recovery mode
alter database ts0to4 set recovery full


--take full backup and log backup
backup database ts0to4 
	to disk = 'c:\data\data1\ts0to4Full_sw5.bak'
	with checksum 
-- backup log
backup log ts0to4 to disk = 'c:\data\data1\ts0to4Full_sw5_log.trn'
	with checksum
go


--set one file to offline
alter database ts0to4 
modify file (name=ts0to4_FG02, OFFLINE)


---restore FG02?
restore database ts0to4 file = 'ts0to4_FG02' 
from disk = 'c:\data\data1\ts0to4Full_sw5.bak'
with recovery

select * from ts0to4.dbo.vel_02