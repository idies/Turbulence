
drop table vel

create table vel (
	timestep int not null,
	zindex bigint not null, 
	data varbinary(6168) not null
)

create table pr (
	timestep int not null,
	zindex bigint not null,
	data varbinary(2072) not null
)

alter table vel 
add constraint pk_vel
primary key clustered (timestep, zindex)

alter table pr
add constraint pk_pr
primary key clustered (timestep, zindex)



dbcc traceon(610)

insert pr with (tablock)
select * from gw01.turbdb101.dbo.pr
where timestep = 0

insert pr with (tablock)
select * from gw02.turbdb102.dbo.pr
where timestep = 0

insert pr with (tablock)
select * from gw03.turbdb103.dbo.pr
where timestep = 0

insert pr with (tablock)
select * from gw04.turbdb104.dbo.pr
where timestep = 0

insert pr with (tablock)
select * from gw01.turbdb105.dbo.pr
where timestep = 0

insert pr with (tablock)
select * from gw02.turbdb106.dbo.pr
where timestep = 0


insert pr with (tablock)
select * from gw03.turbdb107.dbo.pr
where timestep = 0

insert pr with (tablock)
select * from gw04.turbdb108.dbo.pr
where timestep = 0

--17:45 for vel



