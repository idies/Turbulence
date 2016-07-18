using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Data.SqlClient;
using System.Net;

namespace TurbulenceService
{
    /// <summary>
    /// Summary description for Log
    /// </summary>
    public class Log
    {
        DateTime start;
        public bool devmode;
        string logdb;

        public Log(string logdb, bool devmode)
        {
            this.logdb = logdb;
            this.devmode = devmode;
            this.Reset();
        }

        public void Reset()
        {
            start = DateTime.Now;
        }

        public void WriteLog(int id, string dataset, int op, int spatial, int temporal, int count, float time, object endTime, object nt, byte[] access)
        {
            if (devmode) { return; }
            int dataset_int = DataInfo.findDataSetInt(dataset);
            UpdateRecordCount(id, count);
            WriteVerboseLogRecord(id, dataset_int, op, spatial, temporal, count, time, endTime, nt, access);
        }

        public object CreateLog(int id, string dataset, int op, int spatial, int temporal, int count, float time, object endTime, object dt)
        {
            if (devmode) { return null; }
            int dataset_int = DataInfo.findDataSetInt(dataset);
            return CreateVerboseLogRecord(id, dataset_int, op, spatial, temporal, count, time, endTime, dt);
        }

        public void UpdateRecordCount(int id, int count)
        {
            if (devmode) { return; }
            String cString = ConfigurationManager.ConnectionStrings[logdb].ConnectionString;
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE [" + logdb + "].[dbo].[particlecount] SET records = records + @records WHERE (uid = @uid)";
            cmd.Parameters.AddWithValue("@records", count);
            cmd.Parameters.AddWithValue("@uid", id);
            cmd.ExecuteNonQuery();
            conn.Close();
        }


        // Write a verbose log record
        public void WriteVerboseLogRecord(int id, int dataset, int op, int spatial, int temporal, int count, float time, object endTime, object nt, byte[] access)
        {
            if (devmode) { return; }
            IPAddress addr = IPAddress.Parse(System.Web.HttpContext.Current.Request.UserHostAddress);
            
            
            String cString = ConfigurationManager.ConnectionStrings[logdb].ConnectionString;
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO [" + logdb + "].[dbo].[usage] ([rowid], [uid], [ip], [dataset], "
                + " [op], [spatial], [temporal], [records], [exectime], [timestep], [endTimestep], [nt], [access]) "
                + " VALUES "
                + " (NEWID(), @uid, @ip, @dataset, @op, @spatial, @temporal, @records, @exectime, @timestep, @endTimestep, @nt, @access) ";
           cmd.Parameters.AddWithValue("@uid", id);
           cmd.Parameters.Add("@ip", SqlDbType.Binary, 4).Value = addr.GetAddressBytes();
           // date is auto-inserted
           // cmd.Parameters.AddWithValue("@date", this.start);
           cmd.Parameters.AddWithValue("@dataset", dataset);
           cmd.Parameters.AddWithValue("@op", op);
           cmd.Parameters.AddWithValue("@spatial", spatial);
           cmd.Parameters.AddWithValue("@temporal", temporal);
           cmd.Parameters.AddWithValue("@records", count);
           TimeSpan timespan = DateTime.Now - this.start;
           cmd.Parameters.AddWithValue("@exectime", (float) timespan.TotalSeconds);
           cmd.Parameters.AddWithValue("@timestep", time);
           cmd.Parameters.AddWithValue("@endTimestep", endTime ?? DBNull.Value);
           cmd.Parameters.AddWithValue("@nt", nt ?? DBNull.Value);
           cmd.Parameters.Add("@access", SqlDbType.VarBinary, access.Length).Value = access;
           cmd.ExecuteNonQuery();
           conn.Close();
        }


        // Create a verbose log record
        // and return the rowid (IDENTITY) value associated with the record
        public object CreateVerboseLogRecord(int id, int dataset, int op, int spatial, int temporal, int count, float time, object endTime, object dt)
        {
            if (devmode) { return null; }
            IPAddress addr = IPAddress.Parse(System.Web.HttpContext.Current.Request.UserHostAddress);

            String cString = ConfigurationManager.ConnectionStrings[logdb].ConnectionString;
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO [" + logdb + "].[dbo].[usage] ([uid], [ip], [dataset], "
                + " [op], [spatial], [temporal], [records], [timestep], [endTimestep], [dt]) "
                + " VALUES "
                + " (@uid, @ip, @dataset, @op, @spatial, @temporal, @records, @timestep, @endTimestep, @dt) ";
            cmd.Parameters.AddWithValue("@uid", id);
            cmd.Parameters.Add("@ip", SqlDbType.Binary, 4).Value = addr.GetAddressBytes();
            // date is auto-inserted
            // cmd.Parameters.AddWithValue("@date", this.start);
            cmd.Parameters.AddWithValue("@dataset", dataset);
            cmd.Parameters.AddWithValue("@op", op);
            cmd.Parameters.AddWithValue("@spatial", spatial);
            cmd.Parameters.AddWithValue("@temporal", temporal);
            cmd.Parameters.AddWithValue("@records", count);
            // exectime is initially NULL and is updated when the query completes
            cmd.Parameters.AddWithValue("@timestep", time);
            cmd.Parameters.AddWithValue("@endTimestep", endTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dt", dt ?? DBNull.Value);
            // access is initially NULL and is updated when the query completes
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT @@IDENTITY";
            object rowid = cmd.ExecuteScalar();
            conn.Close();
            return rowid;
        }


        // Write a verbose log record
        public void UpdateLogRecord(object rowid, byte[] access)
        {
            if (devmode) { return; }

            String cString = ConfigurationManager.ConnectionStrings[logdb].ConnectionString;
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            if (access == null)
            {
                cmd.CommandText = @"UPDATE [" + logdb + "].[dbo].[usage] "
                    + " SET [exectime] = @exectime "
                    + " WHERE rowid = @rowid ";
            }
            else
            {
                cmd.CommandText = @"UPDATE [" + logdb + "].[dbo].[usage] "
                    + " SET [exectime] = @exectime, [access] = @access "
                    + " WHERE rowid = @rowid ";
                cmd.Parameters.Add("@access", SqlDbType.VarBinary, access.Length).Value = access;
            }
            TimeSpan timespan = DateTime.Now - this.start;
            cmd.Parameters.AddWithValue("@exectime", (float)timespan.TotalSeconds);
            cmd.Parameters.AddWithValue("@rowid", rowid);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

    }
}