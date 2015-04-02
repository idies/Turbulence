--=================================================================================
-- Stored procedures for TurbScratch DB
--=================================================================================
use [TurbScratch]
-----------------------------------------------------------------------------------
-- spCreateZindexTable
-- Creates zindex lookup table for user created tables in TurbScratch DB
-- TODO: handle errors like name collissions, what happens if tables have no data, etc
-----------------------------------------------------------------------------------

if object_id('spCreateZindexTable') is not null
	drop procedure spCreateZindexTable
go

create procedure spCreateZindexTable
	@schemaname sysname,	--name of user schema
	@tablename sysname,		--name of user created table
	@thigh int				--index of max timestep in user created table
as
begin
	--supress DONE_IN_PROC notifications
	set nocount on

	declare @doExecute bit
	declare @sql nvarchar(max)
	
	--set to 0 for testing
	set @doExecute = 1

	set @sql = 'CREATE TABLE ['+@schemaname+'].[zindex_'+@tablename+'] (
			[X] [int] NOT NULL,
			[Y] [int] NOT NULL,
			[Z] [int] NOT NULL,
			[zindex] [bigint] NOT NULL,
		 CONSTRAINT [pk_zindex_'+@tablename+'] PRIMARY KEY CLUSTERED 
		(
			[zindex] ASC
		)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = ON, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) on [PRIMARY]
		) ON [PRIMARY]'


	if @doExecute=0
		print @sql
	else 
		exec sp_executesql @sql


set @sql='
		INSERT INTO ['+@schemaname+'].[zindex_'+@tablename+']
			(x,
			 Y, 
			 Z, 
			 zindex)
		SELECT 
		turblib.dbo.GetMortonX(m.zindex),
		turblib.dbo.GetMortonY(m.zindex),
		turblib.dbo.GetMortonZ(m.zindex),
		m.zindex
		FROM ['+@schemaname+'].['+@tablename+'] as m
		where timestep = '+ cast(@thigh as nvarchar)

	if @doExecute=0
		print @sql
	else 
		exec sp_executesql @sql

set @sql='
		CREATE NONCLUSTERED INDEX zindex_x_y_z_'+@tablename+'
		ON ['+@schemaname+'].[zindex_'+@tablename+'] ([X],[Y],[Z])
		INCLUDE ([zindex])'

	if @doExecute=0
		print @sql
	else 
		exec sp_executesql @sql

set @sql='
		GRANT SELECT on  ['+@schemaname+'].[zindex_'+@tablename+'] TO [turbquery];
		GRANT SELECT on   ['+@schemaname+'].[zindex_'+@tablename+'] TO [turbweb];'

	if @doExecute=0
		print @sql
	else 
		exec sp_executesql @sql

--select * from dbo.zindex
end
go


/*testing
	exec spCreateZindexTable 'suetest_test1', 'velocity_9', '100'
*/


---------------------------------------------------------------------------------------------------------------
-- spCreateSchema
-- create schema to hold user created dataset.
-- TODO: handle errors if schema already exists
---------------------------------------------------------------------------------------------------------------

if object_id('spCreateSchema') is not null
	drop procedure spCreateSchema
go

create procedure spCreateSchema
	@schemaname sysname
as
begin
	
	declare @doExecute bit
	declare @sql nvarchar (max)

	--set to 0 for testing
	set @doExecute = 1

	set @sql = 'CREATE SCHEMA ['+@schemaname+']'

		if @doExecute=0
		print @sql
	else 
		exec sp_executesql @sql
end
go

/*
	exec spCreateSchema 'sue99'
*/


---------------------------------------------------------------------------------------------------------------
-- spCreateTable
-- create table to hold user created dataset.  Table is created with primary key / clustered index.
-- Schema must already be created by spCreateSchema
-- TODO: handle errors if tablename already exists
---------------------------------------------------------------------------------------------------------------

if object_id('spCreateTable') is not null
	drop procedure spCreateTable
go

create procedure spCreateTable
	@schemaname sysname,	--name of user schema
	@tablename sysname,		--name of user created table
	@components int			--number of components in table (get this from turbinfo.datafields)
as
begin
	set nocount on
	
	--set to 0 for testing
	declare @doExecute bit = 1
	
	declare @sql nvarchar(max)

	--these are constants -- sizes in bytes
	declare @componentSize int = 2048   
	declare @headerSize int = 24

	declare @datasize int

	--datasize is (size of one component * num components) + headersize
	set @datasize = (@componentSize * @components) + @headerSize

	set @sql='
	CREATE TABLE ['+@schemaname+'].['+@tablename+'] (
		timestep int not null,
		zindex bigint not null,
		data varbinary('+cast(@datasize as nvarchar)+'),
		CONSTRAINT [pk_timestep_zindex_'+@tablename+'] PRIMARY KEY CLUSTERED 
		(
			[timestep] ASC,
			[zindex] ASC
		)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = ON, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) on [PRIMARY]
		) ON [PRIMARY]  '

		if (@doExecute = 0)
			print @sql
		else 
			 exec sp_executesql @sql

			 set @sql='
		GRANT SELECT on  ['+@schemaname+'].['+@tablename+'] TO [turbquery];
		GRANT SELECT on   ['+@schemaname+'].['+@tablename+'] TO [turbweb];'

	if @doExecute=0
		print @sql
	else 
		exec sp_executesql @sql


end
go
/*
	exec spCreateTable 'sue99', 'myVelocity99', 3
	exec spCreateTable 'sue99', 'myPr', 1
*/



