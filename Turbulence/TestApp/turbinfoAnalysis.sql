DECLARE	@Row_ID bigint;
SET @Row_ID = 950000000;
select max(rowid) as rowid, CONVERT(DATE,CAST(year(date) AS VARCHAR(4))+'-'+ CAST(month(date) AS VARCHAR(2))+'-'+CAST(day(date) AS VARCHAR(2))) as dates,
		count(*) as requests, 
		sum(cast(records as bigint)) as points,
		sum(exectime) as total_exectime, 
		opID.op_name, opID.op_cat, usage.op as opID,
		DataID.DatasetName, usage.dataset as datasetID,
		users.authkey, usage.uid,
		ipl.city_name, ipl.country_name, usage.ip
--into turblog..usage_summary
from turblog..usage
left join turblog..opID on opID.op=usage.op
left join turblog..DataID on DataID.DatasetID= usage.dataset
left join turblog..users on users.uid= usage.uid
left join turblog..[GeoLite2-City-Blocks-IPv4] as ips on ips.ip_start<CONVERT(VARBINARY(8), usage.ip) and CONVERT(VARBINARY(8), usage.ip)<ips.ip_end
left join turblog..[GeoLite2-City-Locations-en] as ipl on ipl.geoname_id=ips.geoname_id
where rowid>@Row_ID and exectime is not null 
group by CONVERT(DATE,CAST(year(date) AS VARCHAR(4))+'-'+ CAST(month(date) AS VARCHAR(2))+'-'+CAST(day(date) AS VARCHAR(2))), 
		opID.op_cat, opID.op_name, usage.op,
		DataID.DatasetName, usage.dataset,
		users.authkey, usage.uid,
		ipl.city_name, ipl.country_name, usage.ip
--order by rowid
union 
select max(rowid) as rowid, CONVERT(DATE,CAST(year(date) AS VARCHAR(4))+'-'+ CAST(month(date) AS VARCHAR(2))+'-'+CAST(day(date) AS VARCHAR(2))) as dates,
		count(*) as requests, 
		sum(cast(records as bigint)) as points,
		sum(exectime) as total_exectime, 
		opID.op_name, opID.op_cat, usage.op as opID,
		DataID.DatasetName, usage.dataset as datasetID,
		users.authkey, usage.uid,
		ipl.city_name, ipl.country_name, usage.ip
--into turblog..usage_summary
from turblog..usage
left join turblog..opID on opID.op=usage.op
left join turblog..DataID on DataID.DatasetID= usage.dataset
left join turblog..users on users.uid= usage.uid
left join turblog..[GeoLite2-City-Blocks-IPv4] as ips on ips.ip_start<CONVERT(VARBINARY(8), usage.ip) and CONVERT(VARBINARY(8), usage.ip)<ips.ip_end
left join turblog..[GeoLite2-City-Locations-en] as ipl on ipl.geoname_id=ips.geoname_id
where rowid>@Row_ID and exectime is null
group by CONVERT(DATE,CAST(year(date) AS VARCHAR(4))+'-'+ CAST(month(date) AS VARCHAR(2))+'-'+CAST(day(date) AS VARCHAR(2))), 
		opID.op_cat, opID.op_name, usage.op,
		DataID.DatasetName, usage.dataset,
		users.authkey, usage.uid,
		ipl.city_name, ipl.country_name, usage.ip
order by rowid
GO

DECLARE	@startdates DATE;
SET @startdates = '2019-08-01'; 
DECLARE	@enddates DATE;
SET @enddates = '2019-08-04'; 
--DATEPART (DAY,dates) as theday, DATEPART (ISO_WEEK,dates) as theweek,
select sum(total) as total, sum(mhd) as mhd, sum(iso1024coarse) as iso1024coarse, sum(iso1024fine) as iso1024fine, 
	sum(channel) as channel, sum(mixing) as mixing, sum(iso4096) as iso4096, sum(rotstrat4096) as rotstrat4096, 
	sum(bl_zaki) as bl_zaki, sum(other_dataset) as other_dataset, sum(Field) as Field, sum(Gradient) as Fradient, 
	sum(Hessian) as Hessian, sum(Laplacian) as Laplacian, sum(Forcing) as Forcing, sum(Filter) as Filter, 
	sum(Particle) as Particle, sum(Cutout) as Cutout, sum(Threshold) as Threshold, sum(other_op) as other_op, sum(incomplete) as incomplete,
	DATEPART(YEAR, dates) as theyear, DATEPART (MONTH, dates) as themonth
from
(select usage_summary.dates, total, mhd, iso1024coarse, iso1024fine, channel, mixing, iso4096, rotstrat4096, bl_zaki, other_dataset, 
		Field, Gradient, Hessian, Laplacian, Forcing, Filter, Particle, Cutout, Threshold, other_op, incomplete
from turblog..usage_summary
left join
(select dates, sum(points) as total
from turblog..usage_summary
where total_exectime is not null and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jto on jto.dates=usage_summary.dates
left join
(select dates, sum(points) as mhd
from turblog..usage_summary
where total_exectime is not null and datasetID=3 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j3 on j3.dates=usage_summary.dates
left join
(select dates, sum(points) as iso1024coarse
from turblog..usage_summary
where total_exectime is not null and datasetID=4 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j4 on j4.dates=usage_summary.dates
left join
(select dates, sum(points) as iso1024fine
from turblog..usage_summary
where total_exectime is not null and datasetID=5 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j5 on j5.dates=usage_summary.dates
left join
(select dates, sum(points) as channel
from turblog..usage_summary
where total_exectime is not null and datasetID=6 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j6 on j6.dates=usage_summary.dates
left join
(select dates, sum(points) as mixing
from turblog..usage_summary
where total_exectime is not null and datasetID=7 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j7 on j7.dates=usage_summary.dates
left join
(select dates, sum(points) as iso4096
from turblog..usage_summary
where total_exectime is not null and datasetID=10 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j10 on j10.dates=usage_summary.dates
left join
(select dates, sum(points) as rotstrat4096
from turblog..usage_summary
where total_exectime is not null and datasetID=11 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j11 on j11.dates=usage_summary.dates
left join
(select dates, sum(points) as bl_zaki
from turblog..usage_summary
where total_exectime is not null and datasetID=12 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j12 on j12.dates=usage_summary.dates
left join
(select dates, sum(points) as other_dataset
from turblog..usage_summary
where total_exectime is not null and (datasetID not in (3, 4, 5, 6, 7, 10, 11)) and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jo on jo.dates=usage_summary.dates

left join
(select dates, sum(points) as Field
from turblog..usage_summary
where total_exectime is not null and op_cat='Field' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jField on jField.dates=usage_summary.dates
left join
(select dates, sum(points) as Gradient
from turblog..usage_summary
where total_exectime is not null and op_cat='Gradient' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jGradient on jGradient.dates=usage_summary.dates
left join
(select dates, sum(points) as Hessian
from turblog..usage_summary
where total_exectime is not null and op_cat='Hessian' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jHessian on jHessian.dates=usage_summary.dates
left join
(select dates, sum(points) as Laplacian
from turblog..usage_summary
where total_exectime is not null and op_cat='Laplacian' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jLaplacian on jLaplacian.dates=usage_summary.dates
left join
(select dates, sum(points) as Forcing
from turblog..usage_summary
where total_exectime is not null and op_cat='Forcing' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jForcing on jForcing.dates=usage_summary.dates
left join
(select dates, sum(points) as Filter
from turblog..usage_summary
where total_exectime is not null and op_cat='Filter' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jFilter on jFilter.dates=usage_summary.dates
left join
(select dates, sum(points) as Particle
from turblog..usage_summary
where total_exectime is not null and op_cat='Particle' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jParticle  on jParticle .dates=usage_summary.dates
left join
(select dates, sum(points) as Cutout
from turblog..usage_summary
where total_exectime is not null and op_cat='Cutout' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jCutout on jCutout.dates=usage_summary.dates
left join
(select dates, sum(points) as Threshold
from turblog..usage_summary
where total_exectime is not null and op_cat='Threshold' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jThreshold on jThreshold.dates=usage_summary.dates
left join
(select dates, sum(points) as Other_op
from turblog..usage_summary
where total_exectime is not null and op_cat='Others' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jOther_op on jOther_op.dates=usage_summary.dates

left join
(select dates, sum(points) as incomplete
from turblog..usage_summary
where total_exectime is null and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jnull on jnull.dates=usage_summary.dates

where usage_summary.dates>=@startdates and usage_summary.dates<=@enddates
group by usage_summary.dates, total, mhd, iso1024coarse, iso1024fine, channel, mixing, iso4096, rotstrat4096, bl_zaki, other_dataset, 
		Field, Gradient, Hessian, Laplacian, Forcing, filter, Particle, Cutout, Threshold, other_op, incomplete) as t

group by DATEPART (YEAR,t.dates), DATEPART (MONTH,t.dates)
order by theyear, themonth
GO