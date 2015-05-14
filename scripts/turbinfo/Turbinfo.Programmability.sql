

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
/*
INSERT INTO [dbo].[datasets]
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


*/
	declare @minLim bigint
	declare @maxLim bigint
	declare @dt float, @maxTime float
	declare @timeinc int, @timeoff int, @thigh int
	
	--get minLim and maxLim
	set @minLim = turblib.dbo.CreateMortonIndex(@z, @y, @x)
	set @maxLim = turblib.dbo.CreateMortonIndex(@z + @zwidth, @y + @ywidth, @z + @zwidth)

	--get info about source dataset
	select @dt = dt, @timeinc = timeinc, @timeoff = timeoff 
	from datasets
	where datasetID = @sourceDatasetID


	--get max timestep of user dataset
	set @thigh = @t + (@twidth * @timeinc)

	--get maxTime float
	set @maxTime = @thigh * @dt

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
			@x + @xwidth,
			@y + @ywidth,
			@z + @zwidth )

		--select * from @t1

		insert datasets
		select * from @t1

		--select * from datasets

end


/*
result = isoFineFiltered.uploadToDatabase("isotropic1024fineFiltered", 89, 10, 512, 512, 512, 64, 64, 64)

exec spAddUserDataset 'testName1', 'testSchema1', 5, 512, 512, 512, 16, 16, 16, 89, 10


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






