

use [turbinfo]


if object_id('spAddUserDataset') is not null
	drop procedure [spAddUserDataset]
go


create procedure [spAddUserDataset]
	@name nvarchar(200),
	@schemaname sysname, 
	@sourceDatasetID int,
	@x int, @y int, @z int,
	@xwidth int, @ywidth int, @zwidth int,
	@t int, 
	@twidth int

as
begin

	declare @minLim bigint
	declare @maxLim bigint
	declare @dt float, @maxTime float
	declare @timeinc int, @timeoff int, @thigh int
	
	--get minLim and maxLim
	--set @minLim = turblib.dbo.CreateMortonIndex(@z, @y, @x)
	set @minLim = 0
	set @maxLim = turblib.dbo.CreateMortonIndex (@zwidth, @ywidth, @xwidth)

	--get info about source dataset
	select @dt = dt, @timeinc = timeinc, @timeoff = timeoff 
	from datasets
	where datasetID = @sourceDatasetID


	--get max timestep of user dataset
	set @thigh = @t + (@twidth * @timeinc)

	--get maxTime float
	set @maxTime = @thigh * @dt

	declare @idtab as table (
		datasetID int
	)

	declare @t1 as table (
	[name] [nvarchar](200) NULL,
	[isUserCreated] [bit] NULL,
	[ScratchID] [int] NULL,
	[schemaname] [sysname] NULL,
	[SourceDatasetID] [int] NULL,
	[minLim] [bigint] NULL,
	[maxLim] [bigint] NULL,
	[maxTime] [float] NULL,
	[dt] [float] NULL,
	[timeinc] [int] NULL,
	[timeoff] [int] NULL,
	[thigh] [int] NULL,
	[xhigh] [int] NULL,
	[yhigh] [int] NULL,
	[zhigh] [int] NULL)

	insert @t1 
			([name]
           ,[isUserCreated]
           ,[ScratchID]
           ,[schemaname]
           ,[SourceDatasetID]
           ,[minLim]
           ,[maxLim]
           ,[maxTime]
           ,[dt]
           ,[timeinc]
           ,[timeoff]
           ,[thigh]
           ,[xhigh]
           ,[yhigh]
           ,[zhigh])
		values (
			@name,
			1,
			1,
			@schemaname,
			@sourceDatasetID,
			@minLim, @maxLim,
			@maxTime, @dt,
			@timeinc, @timeoff,
			@thigh,
			@xwidth,
			@ywidth,
			@zwidth )

		--select * from @t1

		insert datasets
		OUTPUT INSERTED.datasetID INTO @idtab
		select * from @t1

		--select datasetID from @idtab
		declare @newDatasetID int
		select @newDatasetID = datasetID from @idtab


		--call addUserDataFields
		exec spAddUserDataFields @newDatasetID, @sourceDatasetID
		

end
go

/*
result = isoFineFiltered.uploadToDatabase("isotropic1024fineFiltered", 89, 10, 512, 512, 512, 64, 64, 64)

exec spAddUserDataset 'testName113', 'testSchema131', 5, 512, 512, 512, 16, 16, 16, 89, 10

declare @idtab as table (datasetID int)
insert @idtab (datasetID) 
 exec spAddUserDataset 'testName9999', 'testSchema9999', 5, 0, 0, 0, 16, 16, 16, 89, 10
--select datasetID from @idtab
select * from datasets where datasetID > 100
select * from datafields

result = isoFineFiltered.uploadToDatabase("isotropic1024fineFiltered", 89, 10, 512, 512, 512, 64, 64, 64)
create procedure [spAddUserDataset]
	@name nvarchar(200),
	@schemaname sysname, 
	@sourceDatasetID int,
	@x int, @y int, @z int,
	@xwidth int, @ywidth int, @zwidth int,
	@t int, 
	@twidth int

*/


if object_id('spAddUserDatafields') is not null
drop procedure [spAddUserDatafields]
go

create procedure [spAddUserDatafields]
	@datasetID int,
	@sourceDatasetID int 
	
as 
begin
	
	insert datafields (name, DatasetID, charname, components, longname, tablename)
	select df.name, @DatasetID, df.charname, df.components, df.longname, (ds.Name + '_' + df.name) as tablename
	from datafields df, datasets ds
	where
	ds.DatasetID = @datasetID
	and df.DatasetID = @sourceDatasetID

end
go

/* test
	
	select * from datafields

	exec spAddUserDatafields 102, 5

	select * from datafields
*/

	





