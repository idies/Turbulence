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
//using Turbulence.TurbLib;
//using Turbulence.TurbLib.DataTypes;
namespace Website
{
    public partial class status_monitor2 : System.Web.UI.Page
    {
        bool error = false;
        protected int sqlConnectionTimeout = 3;
        protected int sqlCommandTimeout = 5;
        edu.jhu.pha.turbulence.TurbulenceService service;

        public void reportError(string name, Exception e)
        {
            error = true;
            errortext.Text = String.Format("{0}\n<p class=\"errorbox\"><b>{1}:</b><br />{2}<br />{3}</p>",
                errortext.Text.ToString(), name, e.Message, e.StackTrace);
        }

        /// <summary>
        /// Connect to each database node and perform several basic tests.\
        /// 
        /// Status information requires:
        /// GRANT VIEW SERVER STATE to [turbquery]
        /// 
        /// TODO: Create another user for status queries.
        /// </summary>
        public DataTable testNodes()
        {
            const string infodb_string = TurbulenceService.TurbulenceService.infodb_string;
            const string infodb_backup_string = TurbulenceService.TurbulenceService.infodb_backup_string;
            const bool development = TurbulenceService.TurbulenceService.DEVEL_MODE;

            List<string> servers_primary = new List<string>(24);
            List<string> servers_backup = new List<string>(24);
            List<string> databases = new List<string>(24);
            List<string> codeDatabases = new List<string>(24);
            List<long> zindex = new List<long>(24);
            List<int> mintime = new List<int>(24);
            List<int> maxtime = new List<int>(24);
            List<int> dbType = new List<int>(24);

            Random random = new Random();

            DateTime startTime = DateTime.Now;
            Database database = new Database(infodb_string, development);
            TimeSpan dtt = DateTime.Now - startTime;
            string turbinfo_connectionString = ConfigurationManager.ConnectionStrings[infodb_string].ConnectionString;
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(turbinfo_connectionString);
            string turbinfoServer_primary = builder.DataSource;
            string turbinfo_primary = builder.InitialCatalog;

            turbinfo_connectionString = ConfigurationManager.ConnectionStrings[infodb_backup_string].ConnectionString;
            builder = new SqlConnectionStringBuilder(turbinfo_connectionString);
            string turbinfoServer_backup = builder.DataSource;
            string turbinfo_backup = builder.InitialCatalog;

            String cString1 = String.Format("Server={0};Database={1};;Asynchronous Processing=true;User ID={2};Password={3};Pooling=true;Max Pool Size=250;Min Pool Size=20;Connection Lifetime=7200",
                database.infodb_server, database.infodb, ConfigurationManager.AppSettings["turbinfo_uid"], ConfigurationManager.AppSettings["turbinfo_password"]);
            String cString;
            SqlConnection conn = new SqlConnection(cString1);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            //cmd.CommandText = String.Format("select ProductionMachineName, ProductionDatabaseName, " +
            //    "CodeDatabaseName, min(minLim) as minLim, max(maxLim) as maxLim, min(minTime) as minTime, max(maxTime) as maxTime, " +
            //    "min(dbType) as dbType, HotSpareMachineName " +
            //    "from {0}..DatabaseMap " +
            //    "group by ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, HotSpareMachineName", database.infodb);
            cmd.CommandText = String.Format("select ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, " +
                "min(minLim) as minLim, max(maxLim) as maxLim, min(minTime) as minTime, max(maxTime) as maxTime, min(dbType) as dbType, " +
                "HotSpareMachineName, min(DatasetID) as DatasetID " +
                "from {0}..DatabaseMap " +
                "where dbType=0 " +
                "group by ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, HotSpareMachineName, DatasetID " +
                "UNION " +
                "select ProductionMachineName, min(ProductionDatabaseName) as ProductionDatabaseName, CodeDatabaseName, " +
                "min(minLim) as minLim, max(maxLim) as maxLim, min(minTime) as minTime, max(maxTime) as maxTime, min(dbType) as dbType, " +
                "HotSpareMachineName, DatasetID " +
                "from {0}..DatabaseMap " +
                "where dbType>0 " +
                "group by ProductionMachineName, CodeDatabaseName, HotSpareMachineName, DatasetID " +
                "order by ProductionMachineName, ProductionDatabaseName, minLim", database.infodb);

            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    long minLim;
                    long maxLim;
                    //int minTime, maxTime;
                    //if (reader.GetSqlInt32(7).Value == 0 || true)
                    //{
                    servers_primary.Add(reader.GetString(0));
                    if (!reader.IsDBNull(8))
                        servers_backup.Add(reader.GetString(8));
                    else
                        servers_backup.Add("-");
                    databases.Add(reader.GetString(1));
                    codeDatabases.Add(reader.GetString(2));
                    minLim = reader.GetSqlInt64(3).Value;
                    maxLim = reader.GetSqlInt64(4).Value;
                    mintime.Add(reader.GetSqlInt32(5).Value);
                    maxtime.Add(reader.GetSqlInt32(6).Value);
                    dbType.Add(reader.GetSqlInt32(7).Value);
                    zindex.Add(minLim + (long)(random.NextDouble() * (maxLim - minLim)));
                    //}
                }
            }
            else
            {
                throw new Exception("No data returned from turbinfo..DatabaseMap.");
            }
            reader.Close();

            //cmd = conn.CreateCommand();
            //cmd.CommandText = String.Format("UPDATE {0}..DatabaseMap SET HotSpareActive = 'false';", database.infodb);
            //cmd.CommandTimeout = sqlCommandTimeout;
            //cmd.ExecuteNonQuery();

            conn.Close();

            DataTable dt = new DataTable("DatabaseTest");
            dt.Columns.Add("Database");
            dt.Columns.Add("Primary server");
            dt.Columns.Add("Backup server");
            dt.Columns.Add("Connect");
            dt.Columns.Add("SQLCLR Size (MB)");
            dt.Columns.Add("Simple CLR Function");
            dt.Columns.Add("Simple Data Query");
            //dt.Columns.Add("Hot Spare Active");
            dt.Columns.Add("Time");

            //dt.Rows.Add("", "", "Primary Database: ", string.Format("{0}.{1}", turbinfoServer_primary, turbinfo_primary), "", "");
            //dt.Rows.Add("", "", "Backup Database: ", string.Format("{0}.{1}", turbinfoServer_backup, turbinfo_backup), "", "");
            if (database.infodb_server == turbinfoServer_primary)
            {
                dt.Rows.Add("DatabaseMap", string.Format("{0}.{1}", turbinfoServer_primary, turbinfo_primary),
                    string.Format("{0}.{1}", turbinfoServer_backup, turbinfo_backup),
                    "Primary", "", "", "", dtt);
                //dt.Rows.Add("", "", "Currently connected to ", "Primary database: ", string.Format("{0}.{1}", turbinfoServer_primary, turbinfo_primary), "");
            }
            else if (database.infodb_server == turbinfoServer_backup)
            {
                dt.Rows.Add("DatabaseMap", string.Format("{0}.{1}", turbinfoServer_primary, turbinfo_primary),
                    string.Format("{0}.{1}", turbinfoServer_backup, turbinfo_backup),
                    "Backup", "", "", "", dtt);
                //dt.Rows.Add("", "", "Primary database is not reachable. ", "Currently connected to Backup database: ", string.Format("{0}.{1}", turbinfoServer_backup, turbinfo_backup), "");
            }
            else
            {
                dt.Rows.Add("DatabaseMap", string.Format("{0}.{1}", turbinfoServer_primary, turbinfo_primary),
                    string.Format("{0}.{1}", turbinfoServer_backup, turbinfo_backup),
                    "False", "", "", "", "");
                //dt.Rows.Add("", "", "Neither Primary nor Backup database is not reachable.", "", "", "");
            }

            for (int i = 0; i < servers_primary.Count; i++)
            {
                dtt = TimeSpan.Zero;
                bool connect = false;
                long memory = -1;
                bool simpleCLR = false;
                bool dataCheck = false;
                object ret = null;
                object ret1 = null;
                startTime = DateTime.Now;
                //string servers = servers_primary[i];
                try
                {
                    //String cString = ConfigurationManager.ConnectionStrings[nodes[i]].ConnectionString;
                    if (dbType[i] == 0)
                    {
                        cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;User ID={2};Password={3};Pooling=false; Connect Timeout = {4};",
                        servers_primary[i], databases[i], ConfigurationManager.AppSettings["turbquery_uid"], ConfigurationManager.AppSettings["turbquery_password"], sqlConnectionTimeout);
                    }
                    else
                    {
                        cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;User ID={2};Password={3};Pooling=false; Connect Timeout = {4};",
                        servers_primary[i], databases[i].Substring(0, databases[i].Length - 3), ConfigurationManager.AppSettings["turbquery_uid"], ConfigurationManager.AppSettings["turbquery_password"], sqlConnectionTimeout);
                    }
                    //string msg = "server: " + servers + " database: " + databases[i] + " dbType: " + dbType[i] + System.Environment.NewLine;
                    //System.IO.File.AppendAllText(@"c:\www\sqloutput-turb4.log", msg);
                    using (conn = new SqlConnection(cString))
                    {
                        int x = 1, y = 1, z = 1;
                        conn.Open();
                        connect = true;

                        cmd = conn.CreateCommand();
                        cmd.CommandTimeout = sqlCommandTimeout;

                        if (Int16.Parse(conn.ServerVersion.Substring(0, 2)) >= 12)
                        {
                            cmd.CommandText = String.Format("SELECT SUM((pages_kb + virtual_memory_committed_kb) * page_size_in_bytes) / (1024*1024) FROM sys.dm_os_memory_clerks WHERE TYPE = 'MEMORYCLERK_SQLCLR'");
                        }
                        else
                        {
                            cmd.CommandText = String.Format("SELECT SUM((single_pages_kb + multi_pages_kb + virtual_memory_committed_kb) * page_size_bytes) / (1024*1024) FROM sys.dm_os_memory_clerks WHERE TYPE = 'MEMORYCLERK_SQLCLR'");
                        }
                        memory = (long)cmd.ExecuteScalar();

                        //cmd.CommandText = String.Format("SELECT [turbdb].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z);
                        //graywulf fix:  database name is the same as the node name

                        if (databases[i].Contains("turb") || databases[i].Contains("channel") || databases[i].Contains("mixing"))
                        {
                            cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                            ret = cmd.ExecuteScalar();
                            cmd.CommandText = String.Format("SELECT * FROM [{0}].[dbo].[vel] AS v " +
                                " WHERE v.zindex = (@zindex & -512) AND timestep = {1}", databases[i], maxtime[i]);
                            cmd.Parameters.AddWithValue("@zindex", zindex[i]);
                            ret1 = cmd.ExecuteReader();
                        }
                        else if (databases[i].Contains("mhd"))
                        {
                            cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                            ret = cmd.ExecuteScalar();
                            cmd.CommandText = String.Format("SELECT * FROM [{0}].[dbo].[velocity08] AS v " +
                                " WHERE v.zindex = (@zindex & -512) AND timestep = {1}", databases[i], maxtime[i]);
                            cmd.Parameters.AddWithValue("@zindex", zindex[i]);
                            ret1 = cmd.ExecuteReader();
                        }
                        else if (databases[i].Contains("iso4096") || databases[i].Contains("strat4096"))
                        {
                            cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                            ret = cmd.ExecuteScalar();
                            //dt.Rows.Add(servers, connect, memory, true, domainadd, DateTime.Now - startTime);
                            //dt.Rows.Add(servers, connect, memory, true, "No test for isotropic4096", DateTime.Now - startTime);
                            //continue;
                        }
                        else if (databases[i].Contains("bl_zaki") || databases[i].Contains("channel5200"))
                        {
                            cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                            ret = cmd.ExecuteScalar();
                            //dt.Rows.Add(servers, connect, memory, true, domainadd, DateTime.Now - startTime);
                            //dt.Rows.Add(servers, connect, memory, true, "No test for bl_zaki", DateTime.Now - startTime);
                            //continue;
                        }
                        else
                        {
                            cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                            ret = cmd.ExecuteScalar();
                            dt.Rows.Add(servers_primary[i], connect, memory, true, "Unknown dataset", DateTime.Now - startTime);
                            //continue;
                        }

                        if (ret != null)
                            simpleCLR = true;

                        if (dbType[i] == 0 && !((SqlDataReader)ret1).HasRows)                        
                            throw new Exception(String.Format("No rows returned from database {0} for slice number {1}, dataset: {2}!", databases[i], zindex[i], databases[i]));
                        else
                            dataCheck = true;
                    }
                }
                catch (Exception e)
                {
                    reportError(servers_primary[i], e);
                }

                dtt = DateTime.Now - startTime;


                bool Primary = connect && simpleCLR && dataCheck;
                char flag_primary = Primary ? (char)(0X2714) : (char)(0X2716);

                bool connect1 = false;
                long memory1 = -1;
                bool simpleCLR1 = false;
                bool dataCheck1 = false;
                ret = null;
                ret1 = null;
                startTime = DateTime.Now;

                char flag_backup = '\0';
                bool Backup = true;
                if (servers_backup[i] != "-" && (!Primary || true))
                {
                    try
                    {
                        //String cString = ConfigurationManager.ConnectionStrings[nodes[i]].ConnectionString;
                        if (dbType[i] == 0)
                        {
                            cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;User ID={2};Password={3};Pooling=false; Connect Timeout = {4};",
                            servers_backup[i], databases[i], ConfigurationManager.AppSettings["turbquery_uid"], ConfigurationManager.AppSettings["turbquery_password"], sqlConnectionTimeout);
                        }
                        else
                        {
                            cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;User ID={2};Password={3};Pooling=false; Connect Timeout = {4};",
                            servers_backup[i], databases[i].Substring(0, databases[i].Length - 3), ConfigurationManager.AppSettings["turbquery_uid"], ConfigurationManager.AppSettings["turbquery_password"], sqlConnectionTimeout);
                        }
                        //string msg = "server: " + servers + " database: " + databases[i] + " dbType: " + dbType[i] + System.Environment.NewLine;
                        //System.IO.File.AppendAllText(@"c:\www\sqloutput-turb4.log", msg);
                        using (conn = new SqlConnection(cString))
                        {
                            int x = 1, y = 1, z = 1;
                            conn.Open();
                            connect1 = true;

                            cmd = conn.CreateCommand();
                            cmd.CommandTimeout = sqlCommandTimeout;

                            if (Int16.Parse(conn.ServerVersion.Substring(0, 2)) >= 12)
                            {
                                cmd.CommandText = String.Format("SELECT SUM((pages_kb + virtual_memory_committed_kb) * page_size_in_bytes) / (1024*1024) FROM sys.dm_os_memory_clerks WHERE TYPE = 'MEMORYCLERK_SQLCLR'");
                            }
                            else
                            {
                                cmd.CommandText = String.Format("SELECT SUM((single_pages_kb + multi_pages_kb + virtual_memory_committed_kb) * page_size_bytes) / (1024*1024) FROM sys.dm_os_memory_clerks WHERE TYPE = 'MEMORYCLERK_SQLCLR'");
                            }
                            memory1 = (long)cmd.ExecuteScalar();

                            //cmd.CommandText = String.Format("SELECT [turbdb].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z);
                            //graywulf fix:  database name is the same as the node name

                            if (databases[i].Contains("turb") || databases[i].Contains("channel") || databases[i].Contains("mixing"))
                            {
                                cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                                ret = cmd.ExecuteScalar();
                                cmd.CommandText = String.Format("SELECT * FROM [{0}].[dbo].[vel] AS v " +
                                    " WHERE v.zindex = (@zindex & -512) AND timestep = {1}", databases[i], maxtime[i]);
                                cmd.Parameters.AddWithValue("@zindex", zindex[i]);
                                ret1 = cmd.ExecuteReader();
                            }
                            else if (databases[i].Contains("mhd"))
                            {
                                cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                                ret = cmd.ExecuteScalar();
                                cmd.CommandText = String.Format("SELECT * FROM [{0}].[dbo].[velocity08] AS v " +
                                    " WHERE v.zindex = (@zindex & -512) AND timestep = {1}", databases[i], maxtime[i]);
                                cmd.Parameters.AddWithValue("@zindex", zindex[i]);
                                ret1 = cmd.ExecuteReader();
                            }
                            else if (databases[i].Contains("iso4096") || databases[i].Contains("strat4096"))
                            {
                                cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                                ret = cmd.ExecuteScalar();
                                //dt.Rows.Add(servers, connect, memory, true, domainadd, DateTime.Now - startTime);
                                //dt.Rows.Add(servers, connect, memory, true, "No test for isotropic4096", DateTime.Now - startTime);
                                //continue;
                            }
                            else if (databases[i].Contains("bl_zaki") || databases[i].Contains("channel5200"))
                            {
                                cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                                ret = cmd.ExecuteScalar();
                                //dt.Rows.Add(servers, connect, memory, true, domainadd, DateTime.Now - startTime);
                                //dt.Rows.Add(servers, connect, memory, true, "No test for bl_zaki", DateTime.Now - startTime);
                                //continue;
                            }
                            else
                            {
                                cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                                ret = cmd.ExecuteScalar();
                                dt.Rows.Add(servers_primary[i], connect, memory, true, "Unknown dataset", DateTime.Now - startTime);
                                //continue;
                            }

                            if (ret != null)
                                simpleCLR1 = true;

                            if (dbType[i] == 0 && !((SqlDataReader)ret1).HasRows)
                                throw new Exception(String.Format("No rows returned from database {0} for slice number {1}, dataset: {2}!", databases[i], zindex[i], databases[i]));
                            else
                                dataCheck1 = true;
                        }
                    }
                    catch (Exception e)
                    {
                        reportError(servers_backup[i], e);
                    }

                    Backup = connect1 && simpleCLR1 && dataCheck1;
                    flag_backup = Backup ? (char)(0X2714) : (char)(0X2716);
                }

                if (!Primary && Backup && servers_backup[i] != "-") // if primary server fails but backup werver works
                {
                    memory = memory1;
                    dtt = DateTime.Now - startTime;
                    //HotActive[i] = true;
                    using (conn = new SqlConnection(cString1))
                    {
                        conn.Open();
                        //string msg = "database: " + databases[i] + " server: " + servers_primary[i] + " dbType: " + dbType[i] + " HotActive: " + (!Primary & Backup) + System.Environment.NewLine;
                        //System.IO.File.AppendAllText(@"c:\www\sqloutput-turb4.log", msg);
                        cmd = conn.CreateCommand();
                        cmd.CommandText = String.Format("UPDATE {0}..DatabaseMap SET HotSpareActive = 'true' " +
                            "WHERE ProductionMachineName = @server AND HotSpareMachineName = @server2 " +
                            "AND CodeDatabaseName = @codebase AND ProductionDatabaseName = @database;", database.infodb);
                        cmd.Parameters.AddWithValue("@server", servers_primary[i]);
                        cmd.Parameters.AddWithValue("@server2", servers_backup[i]);
                        cmd.Parameters.AddWithValue("@codebase", codeDatabases[i]);
                        cmd.Parameters.AddWithValue("@database", databases[i]);
                        cmd.CommandTimeout = sqlCommandTimeout;
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (conn = new SqlConnection(cString1))
                    {
                        conn.Open();
                        cmd = conn.CreateCommand();
                        cmd.CommandText = String.Format("UPDATE {0}..DatabaseMap SET HotSpareActive = 'false' " +
                            "WHERE ProductionMachineName = @server AND HotSpareMachineName = @server2 " +
                            "AND CodeDatabaseName = @codebase AND ProductionDatabaseName = @database;", database.infodb);
                        cmd.Parameters.AddWithValue("@server", servers_primary[i]);
                        cmd.Parameters.AddWithValue("@server2", servers_backup[i]);
                        cmd.Parameters.AddWithValue("@codebase", codeDatabases[i]);
                        cmd.Parameters.AddWithValue("@database", databases[i]);
                        cmd.CommandTimeout = sqlCommandTimeout;
                        cmd.ExecuteNonQuery();
                    }
                }

                if (dbType[i] == 0)
                {
                    dt.Rows.Add(databases[i], servers_primary[i] + flag_primary, servers_backup[i] + flag_backup, connect || connect1, memory, simpleCLR || simpleCLR1, dataCheck || dataCheck1, dtt);
                }
                else
                {
                    dt.Rows.Add(databases[i].Substring(0, databases[i].Length - 3), servers_primary[i] + flag_primary, servers_backup[i] + flag_backup, connect || connect1, memory, simpleCLR || simpleCLR1, "-", dtt);
                }
            }
            return dt;

        }

        /// <summary>
        /// Perform several simple tests on calling the web service.
        /// </summary>
        public DataTable webServiceTest()
        {
            Random random = new Random();
            DataTable dt = new DataTable("WebServiceTest");
            dt.Columns.Add("Test");
            dt.Columns.Add("Result");
            dt.Columns.Add("Time");

            bool pass;
            string test;

            DateTime startTime;

            service = new edu.jhu.pha.turbulence.TurbulenceService();
            //service = new TurbulenceService.TurbulenceService();

            // Error test
            pass = false;
            test = "getVelocity Test";
            startTime = DateTime.Now;
            try
            {
                edu.jhu.pha.turbulence.Vector3[] output;
                int num_servers = 8;
                int num_disks_per_server = 4;
                int server_size = 134217728;
                int partition_size = 8388608;
                edu.jhu.pha.turbulence.Point3[] points = new edu.jhu.pha.turbulence.Point3[num_servers * num_disks_per_server];
                for (int i = 0; i < num_servers; i++)
                {
                    for (int j = 0; j < num_disks_per_server; j++)
                    {
                        points[i] = new edu.jhu.pha.turbulence.Point3();
                        Morton3D z = new Morton3D(i * server_size + j * partition_size + partition_size / 2);
                        points[i].x = z.X * 2.0f * (float)Math.PI / 1024;
                        points[i].y = z.Y * 2.0f * (float)Math.PI / 1024;
                        points[i].z = z.Z * 2.0f * (float)Math.PI / 1024;
                    }
                }
                output = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "isotropic1024", 0.0f,
                    edu.jhu.pha.turbulence.SpatialInterpolation.None, edu.jhu.pha.turbulence.TemporalInterpolation.None, points, null);

                partition_size = 512;
                server_size = 8;
                Random rnd = new Random();
                for (int i = 0; i < server_size; i++)
                {
                    points[i] = new edu.jhu.pha.turbulence.Point3();
                    //Morton3D z = new Morton3D(i * server_size + j * partition_size + partition_size / 2);
                    points[i].x = rnd.Next(4096) * (float)Math.PI * 2.0f / 4096.0f;
                    points[i].y = rnd.Next(4096) * (float)Math.PI * 2.0f / 4096.0f;
                    points[i].z = rnd.Next(4096) * (float)Math.PI * 2.0f / 4096.0f;
                }
                output = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "isotropic4096", 0.0f,
                    edu.jhu.pha.turbulence.SpatialInterpolation.None, edu.jhu.pha.turbulence.TemporalInterpolation.None, points, null);
                output = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "rotstrat4096", 0.0f,
                    edu.jhu.pha.turbulence.SpatialInterpolation.None, edu.jhu.pha.turbulence.TemporalInterpolation.None, points, null);

                partition_size = 512;
                server_size = 8;
                for (int i = 0; i < server_size; i++)
                {
                    points[i] = new edu.jhu.pha.turbulence.Point3();
                    //Morton3D z = new Morton3D(i * server_size + j * partition_size + partition_size / 2);
                    points[i].x = (float)(rnd.NextDouble() * 969.8465) + 30.0724f;
                    points[i].y = (float)(rnd.NextDouble() * 26.296);
                    points[i].z = (float)(rnd.NextDouble() * 240.0);

                }
                output = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "bl_zaki", 0.0f,
                    edu.jhu.pha.turbulence.SpatialInterpolation.None, edu.jhu.pha.turbulence.TemporalInterpolation.None, points, null);

                // There are only 4 servers for the MHD dataset.
                // There will be some extra points, but that is OK.
                num_servers = 4;
                server_size = 268435456;
                partition_size = 16777216;
                for (int i = 0; i < num_servers; i++)
                {
                    for (int j = 0; j < num_disks_per_server; j++)
                    {
                        points[i] = new edu.jhu.pha.turbulence.Point3();
                        Morton3D z = new Morton3D(i * server_size + j * partition_size + partition_size / 2);
                        points[i].x = z.X * 2.0f * (float)Math.PI / 1024;
                        points[i].y = z.Y * 2.0f * (float)Math.PI / 1024;
                        points[i].z = z.Z * 2.0f * (float)Math.PI / 1024;
                    }
                }
                output = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "mhd1024", 0.0f,
                    edu.jhu.pha.turbulence.SpatialInterpolation.None, edu.jhu.pha.turbulence.TemporalInterpolation.None, points, null);

                num_servers = 8;
                server_size = 134217728;
                partition_size = 8388608;
                for (int i = 0; i < num_servers; i++)
                {
                    for (int j = 0; j < num_disks_per_server; j++)
                    {
                        points[i] = new edu.jhu.pha.turbulence.Point3();
                        Morton3D z = new Morton3D(i * server_size + j * partition_size + partition_size / 2);
                        points[i].x = z.X * 2.0f * (float)Math.PI / 1024;
                        points[i].y = z.Y * 2.0f * (float)Math.PI / 1024;
                        points[i].z = z.Z * 2.0f * (float)Math.PI / 1024;
                    }
                }
                output = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "mixing", 4.0f,
                    edu.jhu.pha.turbulence.SpatialInterpolation.None, edu.jhu.pha.turbulence.TemporalInterpolation.None, points, null);

                //output = service.NullOp("edu.jhu.pha.turbulence-monitor", points);

                long[] range_start = { 0, 134217728, 536870912, 671088640, 1073741824, 1207959552, 1610612736, 1744830464, 4294967296, 4429185024, 5368709120, 5502926848 };
                points = new edu.jhu.pha.turbulence.Point3[48];
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new edu.jhu.pha.turbulence.Point3();
                    // This ensures that we hit all servers and all of their disks
                    // We have 4 servers with 12 logical data volumes per server
                    // Incrementing by 5592404 gives us the next partition
                    // There are 3 DBs per node, for now we just check the first one. Hence, (i / 12) * 3 to determine the range start.
                    Morton3D z = new Morton3D((i % 12) * 5592404 + range_start[(i / 12) * 3] + 5592404 / 2);
                    points[i].x = z.X * 8.0f * (float)Math.PI / 2048;
                    points[i].y = z.Y * 2.0f / 512 - 1.0f;
                    points[i].z = z.Z * 3.0f * (float)Math.PI / 1536;
                }
                output = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "channel", 0.0f,
                    edu.jhu.pha.turbulence.SpatialInterpolation.None, edu.jhu.pha.turbulence.TemporalInterpolation.None, points, null);
                pass = true;
            }
            catch (Exception e)
            {
                reportError(test, e);
                pass = false;
            }
            dt.Rows.Add(test, pass, DateTime.Now - startTime);

            // Error test
            //pass = false;
            //test = "Error Test";
            //try
            //{
            //    throw new Exception("Error test -- should fail");
            //}
            //catch (Exception e) { reportError(test, e); }
            //dt.Rows.Add(test, pass);

            return dt;
        }

        /// <summary>
        /// Perform a test of the data cutout service.
        /// </summary>
        /// 
        /* Old cutout service is decomissioned
        public DataTable CutoutServiceTest()
        {
            Random random = new Random();
            DataTable dt = new DataTable("CutoutServiceTest");
            dt.Columns.Add("Test");
            dt.Columns.Add("Result");
            dt.Columns.Add("Time");

            bool pass;
            string test;

            DateTime startTime;

            // Error test
            pass = false;
            test = "Data Cutout Test";
            startTime = DateTime.Now;
            try
            {
                // Make a request for a small file (single point) from the cutout service
                string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/') + "/";
                String dlurl = String.Format(baseUrl + "cutout/download.aspx/{0}/{1}/{2}/{3},{4}/{5},{6}/{7},{8}/{9},{10}/",
                    Server.UrlEncode("edu.jhu.pha.turbulence-monitor"), "mhd1024", "u", 0, 1, 0, 1, 0, 1, 0, 1);
                WebRequest request = WebRequest.Create(dlurl);
                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = responseStream.Read(buffer, 0, 4096);
                    while (bytesRead > 0)
                    {
                        bytesRead = responseStream.Read(buffer, 0, 4096);
                    }
                }

                pass = true;
            }
            catch (Exception e)
            {
                reportError(test, e);
                pass = false;
            }
            dt.Rows.Add(test, pass, DateTime.Now - startTime);

            return dt;
        }
         * 
         */

        public DataTable BetaCutoutServiceTest()
        {
            Random random = new Random();
            DataTable dt = new DataTable("BetaCutoutServiceTest");
            dt.Columns.Add("Test");
            dt.Columns.Add("Result");
            dt.Columns.Add("Time");

            bool pass;
            string test;

            DateTime startTime;

            // Error test
            pass = false;
            test = "Data Cutout Test";
            startTime = DateTime.Now;
            try
            {
                // Make a request for a small file (single point) from the cutout service
                string baseUrl = "http://dsp033.pha.jhu.edu/";
                String dlurl = String.Format(baseUrl + "jhtdb/getcutout/{0}/{1}/{2}/{3},{4}/{5},{6}/{7},{8}/{9},{10}/",
                    Server.UrlEncode("edu.jhu.pha.turbulence-monitor"), "mhd1024", "u", 0, 1, 0, 1, 0, 1, 0, 1);
                WebRequest request = WebRequest.Create(dlurl);
                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = responseStream.Read(buffer, 0, 4096);
                    while (bytesRead > 0)
                    {
                        bytesRead = responseStream.Read(buffer, 0, 4096);
                    }
                }

                pass = true;
            }
            catch (Exception e)
            {
                reportError(test, e);
                pass = false;
            }
            dt.Rows.Add(test, pass, DateTime.Now - startTime);

            return dt;
        }


        protected void Page_Load(object sender, EventArgs e)
        {
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

            dbstatusgrid.DataSource = testNodes();
            dbstatusgrid.DataBind();

            wsstatusgrid.DataSource = webServiceTest();
            wsstatusgrid.DataBind();

            //cutoutstatusgrid.DataSource = CutoutServiceTest();
            //cutoutstatusgrid.DataBind();


            betacutoutstatusgrid.DataSource = BetaCutoutServiceTest();
            betacutoutstatusgrid.DataBind();

            // Something set an error.  Change status code.
            if (error)
            {
                errorheader.Visible = true;
                Response.StatusCode = 500;
            }

        }


    }

}