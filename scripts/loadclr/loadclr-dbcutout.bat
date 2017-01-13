@echo off

REM This script will load the Turbulence.dll assembly in to all DLMSDB nodes.
REM
REM The CLR functions signatures are defined in dropcreateclr.sql
REM
REM This script will cause existing connections using the CLR assembly
REM to be killed. If you are updating the production system, and are not
REM changing the function signatures, please use alter.bat instead.
REM


set dll="H:\Turbulence\Turbulence\DatabaseCutout\bin\Debug\DatabaseCutout.dll"

echo -- Load Databasecutout Assembly > exec.sql
echo USE turblib >> exec.sql
FileToHex.exe %dll% >> exec.sql


type dropcreateclrdbcutout.sql >> exec.sql

REM echo Checking server versions...
REM FOR /F %%i IN (turb_dbs.txt) DO osql -h-1 -E -S %%i -Q "select @@version"

echo Loading...
echo Loading... > exec.log
FOR /F %%i IN (turb_dbs_dev.txt) DO osql -E -S %%i -i exec.sql >> exec.log

echo Check exec.log for errors.
