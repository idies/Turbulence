using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Data.SqlClient;
using System.IO;
using System.Collections.Generic;
using System.Net;
using TurbulenceService;
using System.Linq;
using System.Globalization;
//using Turbulence.TurbLib;
//using Turbulence.TurbLib.DataTypes;
namespace Website
{
    public partial class status_stat : System.Web.UI.Page
    {
        bool error = false;
        protected int sqlConnectionTimeout = 3;
        protected int sqlCommandTimeout = 5;
        //edu.jhu.pha.turbulence.TurbulenceService service;

        public void reportError(string errorstring)
        {
            error = true;
            errortext.Text = errorstring;
        }

        /// <summary>
        /// Connect to each database node and perform several basic tests.\
        /// 
        /// Status information requires:
        /// GRANT VIEW SERVER STATE to [turbquery]
        /// 
        /// TODO: Create another user for status queries.
        /// </summary>
        public DataTable getUsageStat()
        {
            //const string infodb_string = TurbulenceService.TurbulenceService.infodb_string;
            //const string infodb_backup_string = TurbulenceService.TurbulenceService.infodb_backup_string;
            //const bool development = TurbulenceService.TurbulenceService.DEVEL_MODE;
            //Database database = new Database(infodb_string, development);
            //string turbinfoServerdb = database.infodb_server;
            //string turbinfodb = database.infodb;
            DateTime startdate = DateTime.Today.AddDays(-1);
            DateTime enddate = DateTime.Today;
            if (startdateobx.Text != "")
            {
                try
                {
                    startdate = Convert.ToDateTime(startdateobx.Text);
                }
                catch
                {
                    reportError(String.Format("Please enter correct start date."));
                    error = true;
                }
            }
            if (enddatebox.Text != "")
            {
                try
                {
                    enddate = Convert.ToDateTime(enddatebox.Text);
                }
                catch
                {
                    reportError(String.Format("Please enter correct end date."));
                    error = true;
                }
            }

            string turblog_connectionString;
            if (prod_test.Text.Equals("Production"))
            {
                turblog_connectionString = ConfigurationManager.ConnectionStrings["turblog_conn"].ConnectionString;
            }
            else
            {
                turblog_connectionString = ConfigurationManager.ConnectionStrings["turbinfo_test_conn"].ConnectionString;
                
            }
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(turblog_connectionString);
            string turblogServerdb = builder.DataSource;
            string turblogdb = builder.InitialCatalog;

            String cString1 = String.Format("Server={0};Database={1};;Asynchronous Processing=true;User ID={2};Password={3};Pooling=true;Max Pool Size=250;Min Pool Size=20;Connection Lifetime=7200;Connection Timeout=1200",
                turblogServerdb, turblogdb, ConfigurationManager.AppSettings["turbinfo_uid"], ConfigurationManager.AppSettings["turbinfo_password"]);

            SqlConnection conn = new SqlConnection(cString1);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();

            //cmd.CommandTimeout = 30;
            cmd.CommandTimeout = 30;
            cmd.CommandText = String.Format(@"
select max(rowid) from usage where date<CONVERT(DATE,CAST(year(GETDATE()) AS VARCHAR(4))+'-'+ CAST(month(GETDATE()) AS VARCHAR(2))+'-'+CAST(day(GETDATE()) AS VARCHAR(2)));");
            long usage_row_count = (long)cmd.ExecuteScalar();
            long end_rowid = 0;

            bool loopflag = true;
            while (loopflag == true)
            {
                cmd.CommandTimeout = 30;
                cmd.CommandText = String.Format(@"
select max(rowid) from usage_summary;");
                long usage_summary_row_count = (long)cmd.ExecuteScalar();

                if (usage_row_count - usage_summary_row_count > 500000)
                {
                    loopflag = true;
                    end_rowid = usage_summary_row_count + 500000;
                }
                else
                {
                    loopflag = false;
                    end_rowid = usage_row_count;
                }

                //update usage_summary table//
                cmd.CommandTimeout = 2400;
                cmd.CommandText = String.Format(@"
insert into {0}..usage_summary 
select max(rowid) as rowid, CONVERT(DATE,CAST(year(date) AS VARCHAR(4))+'-'+ CAST(month(date) AS VARCHAR(2))+'-'+CAST(day(date) AS VARCHAR(2))) as dates,
		count(*) as requests, 
		sum(cast(records as bigint)) as points,
		sum(exectime) as total_exectime, 
		opID.op_name, opID.op_cat, usage.op as opID,
		DataID.DatasetName, usage.dataset as datasetID,
		users.authkey, usage.uid,
		ipl.city_name, ipl.country_name, usage.ip

from {0}..usage
left join {0}..opID on opID.op=usage.op
left join {0}..DataID on DataID.DatasetID= usage.dataset
left join {0}..users on users.uid= usage.uid
left join {0}..[GeoLite2-City-Blocks-IPv4] as ips on ips.ip_start<CONVERT(VARBINARY(8), usage.ip) and CONVERT(VARBINARY(8), usage.ip)<ips.ip_end
left join {0}..[GeoLite2-City-Locations-en] as ipl on ipl.geoname_id=ips.geoname_id
where rowid>(select max(rowid) from usage_summary) and rowid<={1} and exectime is not null
group by CONVERT(DATE,CAST(year(date) AS VARCHAR(4))+'-'+ CAST(month(date) AS VARCHAR(2))+'-'+CAST(day(date) AS VARCHAR(2))), 
		opID.op_cat, opID.op_name, usage.op,
		DataID.DatasetName, usage.dataset,
		users.authkey, usage.uid,
		ipl.city_name, ipl.country_name, usage.ip--order by rowid
union 
select max(rowid) as rowid, CONVERT(DATE,CAST(year(date) AS VARCHAR(4))+'-'+ CAST(month(date) AS VARCHAR(2))+'-'+CAST(day(date) AS VARCHAR(2))) as dates,
		count(*) as requests, 
		sum(cast(records as bigint)) as points,
		sum(exectime) as total_exectime, 
		opID.op_name, opID.op_cat, usage.op as opID,
		DataID.DatasetName, usage.dataset as datasetID,
		users.authkey, usage.uid,
		ipl.city_name, ipl.country_name, usage.ip
--into {0}..usage_summary
from {0}..usage
left join {0}..opID on opID.op=usage.op
left join {0}..DataID on DataID.DatasetID= usage.dataset
left join {0}..users on users.uid= usage.uid
left join {0}..[GeoLite2-City-Blocks-IPv4] as ips on ips.ip_start<CONVERT(VARBINARY(8), usage.ip) and CONVERT(VARBINARY(8), usage.ip)<ips.ip_end
left join {0}..[GeoLite2-City-Locations-en] as ipl on ipl.geoname_id=ips.geoname_id
where rowid>(select max(rowid) from usage_summary) and rowid<={1} and exectime is null
group by CONVERT(DATE,CAST(year(date) AS VARCHAR(4))+'-'+ CAST(month(date) AS VARCHAR(2))+'-'+CAST(day(date) AS VARCHAR(2))), 
		opID.op_cat, opID.op_name, usage.op,
		DataID.DatasetName, usage.dataset,
		users.authkey, usage.uid,
		ipl.city_name, ipl.country_name, usage.ip
order by rowid", turblogdb, end_rowid);

                System.IO.File.AppendAllText(@"c:\www\sqloutput-turb4.log", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }

//do query//
            string dateparttext = "";
            string grouptext = "";
            string ordertext = "";
            if (queryunit.Text.Equals("month"))
            {
                dateparttext = "DATEPART(YEAR, dates) as theyear, DATEPART (MONTH, dates) as themonth";
                grouptext = "DATEPART(YEAR, t.dates), DATEPART(MONTH, t.dates)";
                ordertext = "theyear desc, themonth desc";
            }
            else if (queryunit.Text.Equals("week"))
            {
                dateparttext = "DATEPART(YEAR, dates) as theyear, DATEPART (WK, dates) as theweek";
                grouptext = "DATEPART(YEAR, t.dates), DATEPART(WK, t.dates)";
                ordertext = "theyear desc, theweek desc";
            }
            else if (queryunit.Text.Equals("day"))
            {
                dateparttext = "dates";
                grouptext = "dates";
                ordertext = "dates desc";
            }

            cmd.CommandTimeout = 30;
            cmd.CommandText = String.Format(@"
select sum(total) as total, sum(mhd) as mhd, sum(iso1024coarse) as iso1024coarse, sum(iso1024fine) as iso1024fine, 
	sum(channel) as channel, sum(mixing) as mixing, sum(iso4096) as iso4096, sum(rotstrat4096) as rotstrat4096, 
	sum(bl_zaki) as bl_zaki, sum(channel5200) as channel5200, sum(other_dataset) as other_dataset, sum(Field) as Field, sum(Gradient) as Gradient, 
	sum(Hessian) as Hessian, sum(Laplacian) as Laplacian, sum(Forcing) as Forcing, sum(Filter) as Filter, 
	sum(Particle) as Particle, sum(Cutout) as Cutout, sum(Threshold) as Threshold, sum(other_op) as other_op, sum(incomplete) as incomplete,
	{1}
from
(select usage_summary.dates, total, mhd, iso1024coarse, iso1024fine, channel, mixing, iso4096, rotstrat4096, bl_zaki, channel5200, other_dataset, 
		Field, Gradient, Hessian, Laplacian, Forcing, Filter, Particle, Cutout, Threshold, other_op, incomplete
from {0}..usage_summary
left join
(select dates, sum({4}) as total
from {0}..usage_summary
where total_exectime is not null and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jto on jto.dates=usage_summary.dates
left join
(select dates, sum({4}) as mhd
from {0}..usage_summary
where total_exectime is not null and datasetID=3 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j3 on j3.dates=usage_summary.dates
left join
(select dates, sum({4}) as iso1024coarse
from {0}..usage_summary
where total_exectime is not null and datasetID=4 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j4 on j4.dates=usage_summary.dates
left join
(select dates, sum({4}) as iso1024fine
from {0}..usage_summary
where total_exectime is not null and datasetID=5 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j5 on j5.dates=usage_summary.dates
left join
(select dates, sum({4}) as channel
from {0}..usage_summary
where total_exectime is not null and datasetID=6 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j6 on j6.dates=usage_summary.dates
left join
(select dates, sum({4}) as mixing
from {0}..usage_summary
where total_exectime is not null and datasetID=7 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j7 on j7.dates=usage_summary.dates
left join
(select dates, sum({4}) as iso4096
from {0}..usage_summary
where total_exectime is not null and datasetID=10 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j10 on j10.dates=usage_summary.dates
left join
(select dates, sum({4}) as rotstrat4096
from {0}..usage_summary
where total_exectime is not null and datasetID=11 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j11 on j11.dates=usage_summary.dates
left join
(select dates, sum({4}) as bl_zaki
from {0}..usage_summary
where total_exectime is not null and datasetID=12 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j12 on j12.dates=usage_summary.dates
left join
(select dates, sum({4}) as channel5200
from {0}..usage_summary
where total_exectime is not null and datasetID=13 and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as j13 on j13.dates=usage_summary.dates
left join
(select dates, sum({4}) as other_dataset
from {0}..usage_summary
where total_exectime is not null and (datasetID not in (3, 4, 5, 6, 7, 10, 11, 12, 13)) and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jo on jo.dates=usage_summary.dates

left join
(select dates, sum({4}) as Field
from {0}..usage_summary
where total_exectime is not null and op_cat='Field' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jField on jField.dates=usage_summary.dates
left join
(select dates, sum({4}) as Gradient
from {0}..usage_summary
where total_exectime is not null and op_cat='Gradient' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jGradient on jGradient.dates=usage_summary.dates
left join
(select dates, sum({4}) as Hessian
from {0}..usage_summary
where total_exectime is not null and op_cat='Hessian' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jHessian on jHessian.dates=usage_summary.dates
left join
(select dates, sum({4}) as Laplacian
from {0}..usage_summary
where total_exectime is not null and op_cat='Laplacian' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jLaplacian on jLaplacian.dates=usage_summary.dates
left join
(select dates, sum({4}) as Forcing
from {0}..usage_summary
where total_exectime is not null and op_cat='Forcing' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jForcing on jForcing.dates=usage_summary.dates
left join
(select dates, sum({4}) as Filter
from {0}..usage_summary
where total_exectime is not null and op_cat='Filter' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jFilter on jFilter.dates=usage_summary.dates
left join
(select dates, sum({4}) as Particle
from {0}..usage_summary
where total_exectime is not null and op_cat='Particle' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jParticle  on jParticle .dates=usage_summary.dates
left join
(select dates, sum({4}) as Cutout
from {0}..usage_summary
where total_exectime is not null and op_cat='Cutout' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jCutout on jCutout.dates=usage_summary.dates
left join
(select dates, sum({4}) as Threshold
from {0}..usage_summary
where total_exectime is not null and op_cat='Threshold' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jThreshold on jThreshold.dates=usage_summary.dates
left join
(select dates, sum({4}) as Other_op
from {0}..usage_summary
where total_exectime is not null and op_cat='Others' and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jOther_op on jOther_op.dates=usage_summary.dates

left join
(select dates, sum({4}) as incomplete
from {0}..usage_summary
where total_exectime is null and dates>=@startdates and dates<=@enddates
group by usage_summary.dates) as jnull on jnull.dates=usage_summary.dates

where usage_summary.dates>=@startdates and usage_summary.dates<=@enddates
group by usage_summary.dates, total, mhd, iso1024coarse, iso1024fine, channel, mixing, iso4096, rotstrat4096, bl_zaki, channel5200, other_dataset, 
		Field, Gradient, Hessian, Laplacian, Forcing, filter, Particle, Cutout, Threshold, other_op, incomplete) as t

group by {2}
order by {3};", turblogdb, dateparttext, grouptext, ordertext, requestspoint.Text.ToLower());
            cmd.Parameters.AddWithValue("@startdates", startdate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@enddates", enddate.ToString("yyyy-MM-dd"));
            //string msg = cmd.CommandText + System.Environment.NewLine;
            //System.IO.File.AppendAllText(@"c:\www\sqloutput-turb4.log", msg);

            List<string> dates = new List<string>(24);
            List<long> total = new List<long>(24);
            List<long> mhd = new List<long>(24);
            List<long> iso1024coarse = new List<long>(24);
            List<long> iso1024fine = new List<long>(24);
            List<long> channel = new List<long>(24);
            List<long> mixing = new List<long>(24);
            List<long> iso4096 = new List<long>(24);
            List<long> rot4096 = new List<long>(24);
            List<long> bl_zaki = new List<long>(24);
            List<long> channel5200 = new List<long>(24);
            List<long> other_dataset = new List<long>(24);
            List<long> field = new List<long>(24);
            List<long> gradient = new List<long>(24);
            List<long> hessian = new List<long>(24);
            List<long> lapacian = new List<long>(24);
            List<long> forcing = new List<long>(24);
            List<long> filter = new List<long>(24);
            List<long> particle = new List<long>(24);
            List<long> cutout = new List<long>(24);
            List<long> threshold = new List<long>(24);
            List<long> other_op = new List<long>(24);
            List<long> incomplete = new List<long>(24);

            DataTable dt = new DataTable("dates");
            dt.Columns.Add("date");
            dt.Columns.Add("Total completed");
            dt.Columns.Add("MHD");
            dt.Columns.Add("iso1024coarse");
            dt.Columns.Add("iso1024fine");
            dt.Columns.Add("channel");
            dt.Columns.Add("mixing");
            dt.Columns.Add("iso4096");
            dt.Columns.Add("rotstrat4096");
            dt.Columns.Add("transition_bl");
            dt.Columns.Add("channel5200");
            dt.Columns.Add("other datasets");
            dt.Columns.Add("Simulation field");
            dt.Columns.Add("Gradient");
            dt.Columns.Add("Hessian");
            dt.Columns.Add("Laplacian");
            dt.Columns.Add("Forcing");
            dt.Columns.Add("Filter");
            dt.Columns.Add("Particle tracking");
            dt.Columns.Add("Cutout");
            dt.Columns.Add("Threshold");
            dt.Columns.Add("Other commands");
            dt.Columns.Add("Incompleted");

            int i = 0;
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (queryunit.Text.Equals("month"))
                    {
                        dates.Add(reader.GetSqlInt32(22).Value.ToString() + "-" + reader.GetSqlInt32(23).Value.ToString());

                    }
                    else if (queryunit.Text.Equals("week"))
                    {
                        dates.Add(reader.GetSqlInt32(22).Value.ToString() + "-" + reader.GetSqlInt32(23).Value.ToString());
                    }
                    else if (queryunit.Text.Equals("day"))
                    {
                        DateTime temp = reader.GetDateTime(22);
                        dates.Add(temp.ToString("yyyy-MM-dd"));
                    }

                    if (!reader.IsDBNull(0))
                        total.Add(reader.GetSqlInt64(0).Value);
                    else
                        total.Add(0);
                    if (!reader.IsDBNull(1))
                        mhd.Add(reader.GetSqlInt64(1).Value);
                    else
                        mhd.Add(0);
                    if (!reader.IsDBNull(2))
                        iso1024coarse.Add(reader.GetSqlInt64(2).Value);
                    else
                        iso1024coarse.Add(0);
                    if (!reader.IsDBNull(3))
                        iso1024fine.Add(reader.GetSqlInt64(3).Value);
                    else
                        iso1024fine.Add(0);
                    if (!reader.IsDBNull(4))
                        channel.Add(reader.GetSqlInt64(4).Value);
                    else
                        channel.Add(0);
                    if (!reader.IsDBNull(5))
                        mixing.Add(reader.GetSqlInt64(5).Value);
                    else
                        mixing.Add(0);
                    if (!reader.IsDBNull(6))
                        iso4096.Add(reader.GetSqlInt64(6).Value);
                    else
                        iso4096.Add(0);
                    if (!reader.IsDBNull(7))
                        rot4096.Add(reader.GetSqlInt64(7).Value);
                    else
                        rot4096.Add(0);
                    if (!reader.IsDBNull(8))
                        bl_zaki.Add(reader.GetSqlInt64(8).Value);
                    else
                        bl_zaki.Add(0);
                    if (!reader.IsDBNull(9))
                        channel5200.Add(reader.GetSqlInt64(9).Value);
                    else
                        channel5200.Add(0);
                    if (!reader.IsDBNull(10))
                        other_dataset.Add(reader.GetSqlInt64(10).Value);
                    else
                        other_dataset.Add(0);

                    if (!reader.IsDBNull(11))
                        field.Add(reader.GetSqlInt64(11).Value);
                    else
                        field.Add(0);
                    if (!reader.IsDBNull(12))
                        gradient.Add(reader.GetSqlInt64(12).Value);
                    else
                        gradient.Add(0);
                    if (!reader.IsDBNull(13))
                        hessian.Add(reader.GetSqlInt64(13).Value);
                    else
                        hessian.Add(0);
                    if (!reader.IsDBNull(14))
                        lapacian.Add(reader.GetSqlInt64(14).Value);
                    else
                        lapacian.Add(0);
                    if (!reader.IsDBNull(15))
                        forcing.Add(reader.GetSqlInt64(15).Value);
                    else
                        forcing.Add(0);
                    if (!reader.IsDBNull(16))
                        filter.Add(reader.GetSqlInt64(16).Value);
                    else
                        filter.Add(0);
                    if (!reader.IsDBNull(17))
                        particle.Add(reader.GetSqlInt64(17).Value);
                    else
                        particle.Add(0);
                    if (!reader.IsDBNull(18))
                        cutout.Add(reader.GetSqlInt64(18).Value);
                    else
                        cutout.Add(0);
                    if (!reader.IsDBNull(19))
                        threshold.Add(reader.GetSqlInt64(19).Value);
                    else
                        threshold.Add(0);
                    if (!reader.IsDBNull(20))
                        other_op.Add(reader.GetSqlInt64(20).Value);
                    else
                        other_op.Add(0);
                    if (!reader.IsDBNull(21))
                        incomplete.Add(reader.GetSqlInt64(21).Value);
                    else
                        incomplete.Add(0);
                    i++;
                }
            }
            else
            {
                reportError(String.Format("No data returned from usage_summary."));
                //throw new Exception("No data returned from usage_summary.");
            }
            reader.Close();
            conn.Close();
            //i = i - 1;

            if (easyreading.Text.Equals("False"))
            {
                dt.Rows.Add("Total", total.Sum(), mhd.Sum(), iso1024coarse.Sum(), iso1024fine.Sum(),
                    channel.Sum(), mixing.Sum(), iso4096.Sum(), rot4096.Sum(), bl_zaki.Sum(), channel5200.Sum(), other_dataset.Sum(),
                    field.Sum(), gradient.Sum(), hessian.Sum(), lapacian.Sum(), forcing.Sum(), filter.Sum(),
                    particle.Sum(), cutout.Sum(), threshold.Sum(), other_op.Sum(), incomplete.Sum());
            }
            else
            {
                dt.Rows.Add("Total", ToKMB(total.Sum()), ToKMB(mhd.Sum()), ToKMB(iso1024coarse.Sum()), ToKMB(iso1024fine.Sum()),
                    ToKMB(channel.Sum()), ToKMB(mixing.Sum()), ToKMB(iso4096.Sum()), ToKMB(rot4096.Sum()), ToKMB(bl_zaki.Sum()), ToKMB(channel5200.Sum()), ToKMB(other_dataset.Sum()),
                    ToKMB(field.Sum()), ToKMB(gradient.Sum()), ToKMB(hessian.Sum()), ToKMB(lapacian.Sum()), ToKMB(forcing.Sum()), ToKMB(filter.Sum()),
                    ToKMB(particle.Sum()), ToKMB(cutout.Sum()), ToKMB(threshold.Sum()), ToKMB(other_op.Sum()), ToKMB(incomplete.Sum()));
            }

            dt.Rows.Add("", "", "", "", "",
                        "", "", "", "", "", "",
                        "", "", "", "", "", "",
                        "", "", "", "", "");

            for (int j = 0; j < i; j++)
            {

                if (easyreading.Text.Equals("False"))
                {
                    dt.Rows.Add(dates[j], total[j], mhd[j], iso1024coarse[j], iso1024fine[j],
                        channel[j], mixing[j], iso4096[j], rot4096[j], bl_zaki[j], channel5200[j], other_dataset[j],
                        field[j], gradient[j], hessian[j], lapacian[j], forcing[j], filter[j],
                        particle[j], cutout[j], threshold[j], other_op[j], incomplete[j]);
                }
                else
                {
                    dt.Rows.Add(dates[j], ToKMB(total[j]), ToKMB(mhd[j]), ToKMB(iso1024coarse[j]), ToKMB(iso1024fine[j]),
                        ToKMB(channel[j]), ToKMB(mixing[j]), ToKMB(iso4096[j]), ToKMB(rot4096[j]), ToKMB(bl_zaki[j]), ToKMB(channel5200[j]), ToKMB(other_dataset[j]),
                        ToKMB(field[j]), ToKMB(gradient[j]), ToKMB(hessian[j]), ToKMB(lapacian[j]), ToKMB(forcing[j]), ToKMB(filter[j]),
                        ToKMB(particle[j]), ToKMB(cutout[j]), ToKMB(threshold[j]), ToKMB(other_op[j]), ToKMB(incomplete[j]));
                }

            }
            //cmd = conn.CreateCommand();
            //cmd.CommandText = String.Format("UPDATE {0}..DatabaseMap SET HotSpareActive = 'false';", database.infodb);
            //cmd.CommandTimeout = sqlCommandTimeout;
            //cmd.ExecuteNonQuery();

            return dt;

        }

        public DataTable getUsageStat_loc()
        {
            //const string infodb_string = TurbulenceService.TurbulenceService.infodb_string;
            //const string infodb_backup_string = TurbulenceService.TurbulenceService.infodb_backup_string;
            //const bool development = TurbulenceService.TurbulenceService.DEVEL_MODE;
            //Database database = new Database(infodb_string, development);
            //string turbinfoServerdb = database.infodb_server;
            //string turbinfodb = database.infodb;
            DateTime startdate = DateTime.Today.AddDays(-1);
            DateTime enddate = DateTime.Today;
            if (startdateobx.Text != "")
            {
                try
                {
                    startdate = Convert.ToDateTime(startdateobx.Text);
                }
                catch
                {
                    reportError(String.Format("Please enter correct start date."));
                    error = true;
                }
            }
            if (enddatebox.Text != "")
            {
                try
                {
                    enddate = Convert.ToDateTime(enddatebox.Text);
                }
                catch
                {
                    reportError(String.Format("Please enter correct end date."));
                    error = true;
                }
            }

            string turblog_connectionString;
            if (prod_test.Text.Equals("Production"))
            {
                turblog_connectionString = ConfigurationManager.ConnectionStrings["turblog_conn"].ConnectionString;
            }
            else
            {
                turblog_connectionString = ConfigurationManager.ConnectionStrings["turbinfo_test_conn"].ConnectionString;

            }
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(turblog_connectionString);
            string turblogServerdb = builder.DataSource;
            string turblogdb = builder.InitialCatalog;

            String cString1 = String.Format("Server={0};Database={1};;Asynchronous Processing=true;User ID={2};Password={3};Pooling=true;Max Pool Size=250;Min Pool Size=20;Connection Lifetime=7200;",
                turblogServerdb, turblogdb, ConfigurationManager.AppSettings["turbinfo_uid"], ConfigurationManager.AppSettings["turbinfo_password"]);

            SqlConnection conn = new SqlConnection(cString1);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();

            string dateparttext = "";
            string grouptext = "";
            string ordertext = "";
            string selecttext = "";
            if (queryunit.Text.Equals("month"))
            {
                dateparttext = "DATEPART(YEAR, dates) as theyear, DATEPART (MONTH, dates) as themonth";
                grouptext = "DATEPART(YEAR, dates), DATEPART(MONTH, dates)";
                ordertext = "theyear desc, themonth desc";
                selecttext = "theyear, themonth";
            }
            else if (queryunit.Text.Equals("week"))
            {
                dateparttext = "DATEPART(YEAR, dates) as theyear, DATEPART (WK, dates) as theweek";
                grouptext = "DATEPART(YEAR, dates), DATEPART(WK, dates)";
                ordertext = "theyear desc, theweek desc";
                selecttext = "theyear, theweek";
                reportError(String.Format("Be careful!!!! " +
                    "The first week of a year starts from Jan 1st, and the other weeks start from Sunday.{0}" +
                    "So, for example, 2000-12-31 (Sun) is the first and only day of Week 54 of 2000, and 2001-01-01 (Mon) to 2001-01-06 (Sat) is the Week 1 of 2001.", Environment.NewLine));
            }
            else if (queryunit.Text.Equals("day"))
            {
                dateparttext = "dates";
                grouptext = "dates";
                ordertext = "dates desc";
                selecttext = "dates";
            }

            string countrytext = "";
            string citygroup = "";            
            if (countrycity.Text.Equals("Country"))
            {
                countrytext = "(case when (country_name is null or country_name='') then 'N/A' else country_name end) as country_name";
                citygroup = "";
            }
            else if (countrycity.Text.Equals("City"))
            {
                countrytext = "((case when (country_name is null or country_name='') then 'N/A' else country_name end) +" +
                    "(case when (city_name is null or city_name='') then '' else (' - ' + city_name) end)) as country_name";
                citygroup = "city_name,";
            }

            string tempTableName = "##" + Guid.NewGuid().ToString().Replace("-", "");
            cmd.CommandTimeout = 30;
            cmd.CommandText = String.Format(@"
select sum({5}) as {5}, {6}, {1}
into tempdb..{4}
from {0}..usage_summary
where total_exectime is not null and dates>=@startdates and dates<=@enddates-- and country_name is not null and country_name!=''
group by country_name, {7} {2}
union
select sum({5}) as {5}, 'Total' as country_name, {1}
from {0}..usage_summary
where total_exectime is not null and dates>=@startdates and dates<=@enddates-- and country_name is not null and country_name!=''
group by {2}
order by {3}", turblogdb, dateparttext, grouptext, ordertext, tempTableName, requestspoint.Text.ToLower(), countrytext, citygroup);
            cmd.Parameters.AddWithValue("@startdates", startdate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@enddates", enddate.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();

            List<string> country = new List<string>(24);
            DataTable dt = new DataTable("dates");
            dt.Columns.Add("date");
            dt.Columns.Add("Total");
            cmd.CommandText = String.Format(@"SELECT DISTINCT country_name from tempdb..{0} ORDER BY country_name", tempTableName);
            SqlDataReader reader = cmd.ExecuteReader();
            int country_no = 0;
            bool NA = false;
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    country.Add(reader.GetString(0));
                    if (country[country_no] != "N/A" && country[country_no] != "Total")
                    {
                        dt.Columns.Add(country[country_no]);
                    }
                    else if (country[country_no] == "N/A")
                    {
                        NA = true;
                    }
                    country_no++;
                }
                if (NA == true)
                {
                    dt.Columns.Add("N/A");
                }
            }
            else
            {
                reportError(String.Format("No country list returned from usage_summary."));
                //throw new Exception("No country list returned from usage_summary.");
            }
            reader.Close();

            cmd.CommandText = String.Format(@"
DECLARE @DynamicPivotQuery AS NVARCHAR(MAX)
DECLARE @ColumnName AS NVARCHAR(MAX)

--Get distinct values of the PIVOT Column 
SELECT @ColumnName= ISNULL(@ColumnName + ',','') 
       + QUOTENAME(country_name)
FROM (SELECT DISTINCT country_name from tempdb..{2}) AS country_name order by country_name
 
--Prepare the PIVOT query using the dynamic 
SET @DynamicPivotQuery = 
  N'SELECT ' + @ColumnName + ', {0}
    FROM tempdb..{2}
    PIVOT(SUM({3}) 
          FOR country_name IN (' + @ColumnName + ')) AS PVTTable order by {1}'
--Execute the Dynamic Pivot Query
EXEC sp_executesql @DynamicPivotQuery;", selecttext, ordertext, tempTableName, requestspoint.Text.ToLower());

            reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    DataRow row = dt.NewRow();

                    for (int j = 0; j < country_no; j++)
                    {
                        if (!reader.IsDBNull(j))
                        {
                            if (easyreading.Text.Equals("False"))
                                row[country[j]] = reader.GetSqlInt64(j).Value;
                            else
                                row[country[j]] = ToKMB(reader.GetSqlInt64(j).Value);
                        }
                        else
                        {
                            row[country[j]] = 0;
                        }
                    }
                    if (queryunit.Text.Equals("month"))
                    {
                        row["date"] = (reader.GetSqlInt32(country_no).Value.ToString() + "-" + reader.GetSqlInt32(country_no + 1).Value.ToString());
                    }
                    else if (queryunit.Text.Equals("week"))
                    {
                        row["date"] = (reader.GetSqlInt32(country_no).Value.ToString() + "-" + reader.GetSqlInt32(country_no + 1).Value.ToString());
                    }
                    else if (queryunit.Text.Equals("day"))
                    {
                        DateTime temp = reader.GetDateTime(country_no);
                        row["date"] = (temp.ToString("yyyy-MM-dd"));
                    }
                    dt.Rows.Add(row);
                }
            }
            else
            {
                reportError(String.Format("No data returned from usage_summary."));
                //throw new Exception("No data returned from usage_summary location table.");
            }
            reader.Close();

            cmd.CommandText = String.Format(@"DROP TABLE tempdb..{0}", tempTableName);
            cmd.ExecuteNonQuery();
            conn.Close();
            //i = i - 1;

            return dt;

        }

        protected void Page_Load(object sender, EventArgs e)
        {
            error = false;
            errortext.Text = "";
            if (!Request.UserHostAddress.StartsWith("128.220") &&
                !Request.UserHostAddress.StartsWith("172.23") &&
                !Request.UserHostAddress.StartsWith("172.16") &&
                !Request.UserHostAddress.StartsWith("192.168.24"))
            {
                //string domainadd = Request.Url.Host;
                if (Request.Url.Host == "turbulence.pha.jhu.edu")
                {
                    throw new Exception("This page may not be run from outside JHU.");
                }

            }

            dbstatusgrid.DataSource = getUsageStat();
            dbstatusgrid.DataBind();
            wsstatusgrid.DataSource = getUsageStat_loc();
            wsstatusgrid.DataBind();

            // Something set an error.  Change status code.
            if (error)
            {
                //errorheader.Visible = true;
                Response.StatusCode = 500;
            }

        }

        protected void point_Click(object sender, EventArgs e)
        {
            
        }

        private static string ToKMB(long num)
        {
            if (num > 999999999)
            {
                return num.ToString("0,,,.###B", CultureInfo.InvariantCulture);
            }
            else
            if (num > 999999)
            {
                return num.ToString("0,,.##M", CultureInfo.InvariantCulture);
            }
            else
            if (num > 999)
            {
                return num.ToString("0,.#K", CultureInfo.InvariantCulture);
            }
            else
            {
                return num.ToString(CultureInfo.InvariantCulture);
            }
        }
        //private static string ToM(this decimal num)
        //{

        //    return num.ToString("0,,.##M", CultureInfo.InvariantCulture);

        //}
    }

}