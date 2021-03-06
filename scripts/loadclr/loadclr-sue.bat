@echo off

REM This script will load the Turbulence.dll assembly in to all DLMSDB nodes.
REM
REM The CLR functions signatures are defined in dropcreateclr.sql
REM
REM This script will cause existing connections using the CLR assembly
REM to be killed. If you are updating the production system, and are not
REM changing the function signatures, please use alter.bat instead.
REM

REM set dll="C:\Users\kalin\Documents\turbulence\Turbulence\Turbulence\bin\Release\Turbulence.dll"
set dll="H:\GitHub\Turbulence\Turbulence\Turbulence\bin\Debug\Turbulence.dll"
echo -- Load Turbulence Assembly > exec.sql
echo USE turblib >> exec.sql
c:\"program files"\FileToHex.exe %dll% >> exec.sql

type dropcreateclr.sql >> exec.sql

REM echo Checking server versions...
REM FOR /F %%i IN (turb_dbs.txt) DO osql -h-1 -E -S %%i -Q "select @@version"

echo Loading...
echo Loading... > exec.log
FOR /F %%i IN (turb_dbs.txt) DO osql -E -S %%i -i exec.sql >> exec.log

echo Check exec.log for errors.

