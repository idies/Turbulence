@echo off

echo This script will load the Turbulence.dll assembly in to all DLMSDB nodes

set dll="C:\Users\kalin\Documents\Turbulence-git\Turbulence\Turbulence\bin\Release\Turbulence.dll"

echo -- Load Turbulence ASsembly > exec.sql
echo USE turblib >> exec.sql
FileToHex.exe %dll% >> exec.sql

type alterclr.sql >> exec.sql

REM echo Checking server versions...
REM FOR /F %%i IN (turb_dbs.txt) DO osql -h-1 -E -S %%i -Q "select @@version"

echo Loading...
echo Loading... > exec.log
FOR /F %%i IN (turb_dbs.txt) DO osql -E -S %%i -i exec.sql >> exec.log
