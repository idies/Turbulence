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

use turblib
use master
grant view server state to turbquery;

use turbdev
exec sp_changedbowner 'sa'

use turblib
exec sp_changedbowner 'sa' 