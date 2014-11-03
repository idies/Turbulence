--=================================================================
--   Uninstall.sql
--	 2007-07-18 Tamas Budavari
-------------------------------------------------------------------
-- cleans up the SQL routines
-------------------------------------------------------------------
-- History:
-- [2012-05-14] Modular space-filling curve design, new SQL interface
-------------------------------------------------------------------

PRINT '*** DROP UDTs AND UDFs ***'

if exists (select 1 from information_schema.routines 
    where routine_name='ConeSegment' and routine_schema='dbo') 
    drop function dbo.ConeSegment
GO

if exists (select 1 from information_schema.routines 
    where routine_name='Cone' and routine_schema='dbo') 
    drop function dbo.Cone
GO

if exists (select 1 from information_schema.routines 
    where routine_name='Sphere' and routine_schema='dbo') 
    drop function dbo.Sphere
GO

if exists (select 1 from information_schema.routines 
    where routine_name='Box' and routine_schema='dbo') 
    drop function dbo.Box
GO

if exists (select 1 from information_schema.routines 
    where routine_name='fKeyPH' and routine_schema='dbo') 
    drop function dbo.[fKeyPH]
GO
if exists (select 1 from information_schema.routines 
    where routine_name='fKeyZ' and routine_schema='dbo') 
    drop function dbo.[fKeyZ]
GO
if exists (select 1 from information_schema.routines 
    where routine_name='fKey' and routine_schema='dbo') 
    drop function dbo.[fKey]
GO
if exists (select 1 from information_schema.routines 
    where routine_name='fCoverPH' and routine_schema='dbo') 
    drop function dbo.[fCoverPH]
GO
if exists (select 1 from information_schema.routines 
    where routine_name='fCoverZ' and routine_schema='dbo') 
    drop function dbo.[fCoverZ]
GO
if exists (select 1 from information_schema.routines 
    where routine_name='fCover' and routine_schema='dbo') 
    drop function dbo.[fCover]
GO
if exists (select 1 from information_schema.routines 
    where routine_name='fPHCellNeighbours' and routine_schema='dbo') 
    drop function dbo.[fPHCellNeighbours]
GO

--====================================================================
-- Drop User Defined Types
--====================================================================
if exists (select * from information_schema.domains 
    where domain_name = 'Point' and domain_schema = 'dbo')
    drop type dbo.Point
GO
--
if exists (select * from information_schema.domains 
    where domain_name = 'Box' and domain_schema = 'dbo')
    drop type dbo.Box
GO
--
if exists (select * from information_schema.domains 
    where domain_name = 'Cone' and domain_schema = 'dbo')
    drop type dbo.Cone
GO
--
if exists (select * from information_schema.domains 
    where domain_name = 'Sphere' and domain_schema = 'dbo')
    drop type dbo.Sphere
GO
--

if exists (select * from information_schema.domains 
    where domain_name = 'Shape' and domain_schema = 'dbo')
    drop type dbo.Shape
GO
--
if exists (select * from information_schema.domains 
    where domain_name = 'ConeSegment' and domain_schema = 'dbo')
    drop type dbo.ConeSegment
GO
--
PRINT '[Uninstall.sql]: UDTs and UDFs dropped.'
