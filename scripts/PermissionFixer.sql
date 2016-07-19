use turbdev
alter database turbdev
set trustworthy on

use turblib
alter database turblib
set trustworthy on

reconfigure;

use turblib
exec sp_configure 'clr enabled';
exec sp_configure 'clr enabled', '1';
reconfigure;

use turbdev
exec sp_configure 'clr enabled';
exec sp_configure 'clr enabled', '1';
reconfigure;

use turblib
use master
grant view server state to turbquery;

use turbdev
exec sp_changedbowner 'sa'

use turblib
exec sp_changedbowner 'sa'

use turbdev
EXEC sp_change_users_login 'Auto_Fix', 'turbquery'

use turblib
EXEC sp_change_users_login 'Auto_Fix', 'turbquery'

use mhdlib
EXEC sp_change_users_login 'Auto_Fix', 'turbquery'
exec sp_configure 'clr enabled';
exec sp_configure 'clr enabled', '1';
reconfigure;
use mhdlib
exec sp_changedbowner 'sa'
alter database mhdlib
set trustworthy on