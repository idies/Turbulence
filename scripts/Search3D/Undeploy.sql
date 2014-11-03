--=================================================================
--   Undeploy.sql
--	 2007-07-18 Tamas Budavari
-------------------------------------------------------------------
-- Undeploys all assemblies
-------------------------------------------------------------------
-- History:
-- [2012-05-14] Tamas: Modular space-filling curve design w/ new SQL interface
-------------------------------------------------------------------

--====================================================================

PRINT '*** DROP ASSEMBLIES ***'
--
BEGIN TRY
	DROP ASSEMBLY [SqlSearch3D]
	PRINT 'Dropped assembly SqlSearch3D'
END TRY
BEGIN CATCH 
	PRINT 'Error dropping assembly SqlSearch3D'
END CATCH
--
BEGIN TRY
	DROP ASSEMBLY SpaceFilling
	PRINT 'Dropped assembly SpaceFilling'
END TRY
BEGIN CATCH 
	PRINT 'Error dropping assembly SpaceFilling'
END CATCH
--
BEGIN TRY
	DROP ASSEMBLY [Geometry]
	PRINT 'Dropped assembly Geometry'
END TRY
BEGIN CATCH 
	PRINT 'Error dropping assembly Geometry'
END CATCH
--
BEGIN TRY
	DROP ASSEMBLY [Antlr3Runtime]
	PRINT 'Dropped assembly Antlr3Runtime'
END TRY
BEGIN CATCH 
	PRINT 'Error dropping assembly Antlr3Runtime'
END CATCH

