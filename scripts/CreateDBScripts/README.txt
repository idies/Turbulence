----------------
-- README.txt for CreateDBScripts
-- S.Werner
-- 6/20/2016


This file describes how to create a new, empty isotropic turbulence DB using the scripts in https://github.com/idies/Turbulence/tree/master/scripts/CreateDBScripts -- I plan to extend these scripts (and these instructions) to be useful for other large db's with partitioned tables (turbulence and otherwise.) For now, it just applies creating the new db's for the isotropic turbulence ingest of the new timesteps.

--Prequisites:
1. DSP node or similar with 12-volume RAID setup with paths like c:\data\data1, c:\data\data2
2. SQL 2014 or better
3. ~13 TB of disk space

The files are designed to be run in order, 01-createDB_XXX.sql, 02-PartLimits208.sql, etc


NOTE: scripts 01 and 02 and 12 execute the sql statements in the script.  Most of the other ones just generate the statements and print them to the results pane in SSMS: copy and paste into a new window to execute.  (in the future i plan to turn these scripts into some stored procedures that allow you to run them bit by bit or all at once, for now this is the most flexible way.)

==============================
01-createDB_XXX.sql
==============================
Run in master DB

Creates an empty database for isotropic turbulence data.
This file only creates the PRIMARY filegroup and log file.

-------------
-- IMPORTANT
-------------
 Before you run this script, CTRL-F and replace XXX with the slice number of the DB you wish to create.
 for example 201, 202, etc.
 
 ===============================
 02-PartLimits208.sql
 ===============================
 Run in the DB you created in step 1 (e.g. turbdb201)
 
Create PartLimits table for isotropic turbulence with 16 partitions.
Each turb db requires this table to set up partitions, filegroups, constraints, etc.


================================
03-generateLimitsAndPFN208.sql
================================
Run in the DB you created in step 1 (e.g. turbdb201)

Script to set up partition function, partition scheme, 
filegroups and files for turbulence DB
Run this in the DB you're trying to create.

This script does not run the statements, just generates them.
Copy and paste into new SSMS window to execute.
--------------
IMPORTANT!!!!!
--------------
--This script assumes a 12-way RAID-1 or whatever
--with the volumes laid out like c:\data\data1, c:\data\data2 etc
--if the paths are different, you'll need to change the "ADD FILE" section
--Also, this script assumes that there will be 2 db's per machine, 
--the first DB (turbdb201 for example) will be spread over 4 data1-data4
--and the second DB (turbdb202 for example) will be spread over data5-data8

This script does not run the statements, just generates them.
Copy and paste into new SSMS window to execute.

=========================================
04-createVelTables.sql
=========================================

Creates partitioned velocity table, the associated switch tables, and check constraints on the switch tables.

=========================================
05-createPrTables.sql
=========================================

Creates partitioned pressure table, the associated switch tables, and check constraints on the switch tables.

--------------------------

Now you should be ready to ingest!
After you're done filling all the switch tables, switch them into the large partitioned table.


==========================================
11-generateSwitchIn.sql
==========================================

Generates statements to switch staging tables into large partitioned table
 
--------------
IMPORTANT!!!!!!
--------------

set the @tablename variable in this script to 'pr' or 'vel' 

==========================================
12-permsTweaks.sql
==========================================
Various post-ingest stuff should go in here, like enabling CLR and setting stuff to trustworthy, user permissions, stuff like that.  


TODO:
-turn into stored procedures and parameterize so you don't have to run all these scripts separately
-no real need for separate scripts for creating pr and vel tables, would be eaiser to set tablename and datasize as a variable than trying to maintain 2 scripts
-maybe add conditional drop?
-add other tweaks and setup stuff 




 
 