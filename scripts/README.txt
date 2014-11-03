Steps for creating a new database:

1. Create the DB using/modifying one of the SQL scripts in the scripts directory.
2. Create code database "turblib" using one of the SQL scripts in the scripts directory.
3. Generate PartLimits table in the turblib DB either using the DatabaseSetup 
   C# program found in the ImportData project
   or using the CreatePartLimitsTable.sql script modified accordingly.
4. Create partition function and scheme using\modifying the GenerateLimitsANDPFN.sql script.
5. Create switch-in DB tables using\modifying the spCreateSwitchInTableVel08.sql stored procedure.
   The switch-in tables are used to load the data for each file group into one table. These tables are
   then "switch into" the actual data table. The stored procedure can be execute similar to example in
   spCreateSwitchInTableEXEC.sql.
6. Enable trace flag 610 (DBCC TRACEON(610,-1)) and make sure DBs are in simple or bulk-logged recovery model.
7. Deploy turbulence library to turblib DB.
8. Deploy SqlArray library to turblib DB.
9. After data ingest finishes execute SwitchToPartitionTable to switch the staging tables into the data table.
10. Create turbquery user with appropriate permissions. 