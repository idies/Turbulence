@echo ***                        ***
@echo **  Search3D SQL Installer  **  by Tamas Budavari and Gerard Lemson, 2007-2012
@echo ***                        ***
@echo off

set server=%1
set dbname=%2
set action=%3

if not defined server goto usage
if not defined dbname goto usage
if defined action (
  echo requested action: %action%
) else (
  echo default action: full
)

:step1
sqlcmd -S %server% -d %dbname% -E -b -i Uninstall.sql
if not errorlevel 1 goto step2
echo !!! UNINSTALL ERROR !!!
goto :usage

:step2
sqlcmd -S %server% -d %dbname% -E -b -i Undeploy.sql
if not errorlevel 1 goto testaction
echo !!! UNDEPLOY ERROR !!!
goto :usage


:testaction
if defined action goto testundeploy
goto :step3

:testundeploy
if %action% == undeploy goto done

:step3
sqlcmd -S %server% -d %dbname% -E -b -i Deploy.sql
if not errorlevel 1 goto step4
echo !!! DEPLOY ERROR !!!
goto :usage

:step4
sqlcmd -S %server% -d %dbname% -E -b -i Install.sql
if not errorlevel 1 goto end
echo !!! INSTALL ERROR !!!
goto :usage

:done
echo DONE
goto end


:usage
echo Please specify the server and the database and possibly an action to take!
echo Syntax:   Install.bat <server> <database> [<action>]
echo Possible actions: full (default) | undeploy 
echo Example:  Install.bat localhost Test 

:end
