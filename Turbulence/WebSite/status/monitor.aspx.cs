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
//using TurbulenceService;
//using Turbulence.TurbLib;
//using Turbulence.TurbLib.DataTypes;
namespace Website
{
    public partial class status_monitor : System.Web.UI.Page
    {
        bool error = false;
        protected int sqlConnectionTimeout = 3;
        protected int sqlCommandTimeout = 5;
        edu.jhu.pha.turbulence.TurbulenceService service;
        //TurbulenceService.TurbulenceService service;


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
            List<string> servers = new List<string>(24);
            List<string> databases = new List<string>(24);
            List<string> codeDatabases = new List<string>(24);
            List<long> zindex = new List<long>(24);

            Random random = new Random();

            String cString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, min(minLim) as minLim, max(maxLim) as maxLim " +
                "from turbinfo..DatabaseMap " +
                "group by ProductionMachineName, ProductionDatabaseName, CodeDatabaseName";
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    servers.Add(reader.GetString(0));
                    databases.Add(reader.GetString(1));
                    codeDatabases.Add(reader.GetString(2));
                    long minLim = reader.GetSqlInt64(3).Value;
                    long maxLim = reader.GetSqlInt64(4).Value;
                    zindex.Add(minLim + (long)(random.NextDouble() * (maxLim - minLim)));
                }
            }
            else
            {
                throw new Exception("No data returned from turbinfo..DatabaseMap.");
            }
            reader.Close();
            conn.Close();

            DataTable dt = new DataTable("DatabaseTest");
            dt.Columns.Add("Database");
            dt.Columns.Add("Connect");
            dt.Columns.Add("SQLCLR Size (MB)");
            dt.Columns.Add("Simple CLR Function");
            dt.Columns.Add("Simple Data Query");
            dt.Columns.Add("Time");
            for (int i = 0; i < servers.Count; i++)
            {
                bool connect = false;
                long memory = -1;
                bool simpleCLR = false;
                bool dataCheck = false;
                object ret = null;
                DateTime startTime = DateTime.Now;
                try
                {
                    //String cString = ConfigurationManager.ConnectionStrings[nodes[i]].ConnectionString;
                    cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;User ID={2};Password={3};Pooling=false; Connect Timeout = {4};",
                        servers[i], databases[i], ConfigurationManager.AppSettings["turbquery_uid"], ConfigurationManager.AppSettings["turbquery_password"], sqlConnectionTimeout);

                    using (conn = new SqlConnection(cString))
                    {
                        int x = 1, y = 1, z = 1;
                        conn.Open();
                        connect = true;

                        cmd = conn.CreateCommand();
                        cmd.CommandTimeout = sqlCommandTimeout;

                        if (conn.ServerVersion.StartsWith("12."))
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

                        if (databases[i].Contains("turb"))
                        {
                            cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                            ret = cmd.ExecuteScalar();
                            cmd.CommandText = String.Format("SELECT * FROM [{0}].[dbo].[vel] AS v " +
                                " WHERE v.zindex = (@zindex & -512) AND timestep = 0", databases[i]);
                            cmd.Parameters.AddWithValue("@zindex", zindex[i]);
                            ret = cmd.ExecuteReader();

                        }
                        else if (databases[i].Contains("mhd"))
                        {
                            cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                            ret = cmd.ExecuteScalar();
                            cmd.CommandText = String.Format("SELECT * FROM [{0}].[dbo].[velocity08] AS v " +
                                " WHERE v.zindex = (@zindex & -512) AND timestep = 0", databases[i]);
                            cmd.Parameters.AddWithValue("@zindex", zindex[i]);
                            ret = cmd.ExecuteReader();
                        }
                        else if (databases[i].Contains("channel"))
                        {
                            cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                            ret = cmd.ExecuteScalar();
                            cmd.CommandText = String.Format("SELECT * FROM [{0}].[dbo].[vel] AS v " +
                                " WHERE v.zindex = (@zindex & -512) AND timestep = 132005", databases[i]);
                            cmd.Parameters.AddWithValue("@zindex", zindex[i]);
                            ret = cmd.ExecuteReader();
                        }
                        else if (databases[i].Contains("mixing"))
                        {
                            cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                            ret = cmd.ExecuteScalar();
                            cmd.CommandText = String.Format("SELECT * FROM [{0}].[dbo].[vel] AS v " +
                                " WHERE v.zindex = (@zindex & -512) AND timestep = 100", databases[i]);
                            cmd.Parameters.AddWithValue("@zindex", zindex[i]);
                            ret = cmd.ExecuteReader();
                        }
                        else
                        {
                            cmd.CommandText = String.Format("SELECT [{3}].[dbo].[CreateMortonIndex] ({0},{1},{2})", x, y, z, codeDatabases[i]);
                            ret = cmd.ExecuteScalar();
                            dt.Rows.Add(servers[i], connect, memory, true, "Unknown dataset", DateTime.Now - startTime);
                            continue;
                        }

                        if (((SqlDataReader)ret).HasRows)
                            dataCheck = true;
                        else
                            throw new Exception(String.Format("No rows returned from database {0} for slice number {1}!", databases[i], zindex[i]));

                        simpleCLR = true;

                    }
                }
                catch (Exception e)
                {
                    reportError(servers[i], e);
                }

                dt.Rows.Add(servers[i], connect, memory, simpleCLR, dataCheck, DateTime.Now - startTime);
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
                    edu.jhu.pha.turbulence.SpatialInterpolation.None, edu.jhu.pha.turbulence.TemporalInterpolation.None, points);

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
                    edu.jhu.pha.turbulence.SpatialInterpolation.None, edu.jhu.pha.turbulence.TemporalInterpolation.None, points);

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
                    edu.jhu.pha.turbulence.SpatialInterpolation.None, edu.jhu.pha.turbulence.TemporalInterpolation.None, points);

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
                    edu.jhu.pha.turbulence.SpatialInterpolation.None, edu.jhu.pha.turbulence.TemporalInterpolation.None, points);
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
            test = "Beta Data Cutout Test";
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
                throw new Exception("This page may not be run from outside JHU.");
            }
            
            dbstatusgrid.DataSource = testNodes();
            dbstatusgrid.DataBind();

            wsstatusgrid.DataSource = webServiceTest();
            wsstatusgrid.DataBind();

            cutoutstatusgrid.DataSource = CutoutServiceTest();
            cutoutstatusgrid.DataBind();
            
            
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