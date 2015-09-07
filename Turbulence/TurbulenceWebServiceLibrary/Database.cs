using System;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using Turbulence.SQLInterface;
using System.Threading;

namespace TurbulenceService
{
    /// <summary>
    /// Basic, naive, code for sending requests to database nodes.
    /// </summary>
    /// <remarks>
    /// TODO: Move back into the stand-alone mediator.
    /// TODO: Look up dataset information
    /// TODO: Fix support for dynamic partitioning
    /// </remarks>
    public class Database
    {
        string infodb;
        protected int[] gridResolution; // Dimensions of the entire grid given az [z,y,x]
        public int atomDim;                // length of side of a single data atom
        public List<string> servers;       // name of each server (resolved via web.config)
        public List<string> databases;     // name of the database holding turbulence data
        public List<string> codeDatabase;  // name of database from which turbulence code is executed
        public int serverCount;     // Number of servers to balance against
        SqlConnection[] connections;// SQL Connetion Handles
        SqlCommand[] sqlcmds;
        DataTable[] datatables;     // Used to queue input points for a SqlBulkCopy
        string tempTableName;       // Name of table storing input
        string[] tempTableNames = null;    // Name of tables storing input particles (unique for each connection)
        int[] count;
        byte[] bitfield;            // keep track of data regions accessed
        bool development;
        private double dx;           // the grid separation in the x-dimension
        private double dy;           // the grid separation in the y-dimension
        private double dz;           // the grid separation in the z-dimension
        public bool channel_grid;   // flag indicating if we are working with the non-uniform gird for the channel flow dataset
        public bool rmhd;   // flag indicating if we are working with the rmhd 2048 dataset
        private GridPoints gridPointsY; // grid values for the non-uniform y dimension in the case of channel flow
        public float Dt;            // used for converting from floating point time to timestep values stored in the DB
        private int timeInc;         // time increment between timesteps stored in the DB
        private int timeOff;        // timestep offset (t = dt * (timestep - timeOff); for channel flow timestep 132010 = time 0)
        public bool smallAtoms;     // indicator of whether the atoms stored in the DB are small with no edge replicaiton
        // or large with a replicated edge of length 4 on each side
        public List<ServerBoundaries> serverBoundaries; // info about the spatial partitioning of the data across servers
        const int MAX_READ_LENGTH = 256000000;
        const int MAX_NUMBER_THRESHOLD_POINTS = 1024 * 1024;
        const double DENSITY_CONSTANT = 80.0;

        // zindex ranges stored on each server for the channel flow DB
        //long[] range_start;
        //long[] range_end;

        public int TimeInc { get { return timeInc; } }
        public int TimeOff { get { return timeOff; } }

        public byte[] Bitfield
        {
            get { return bitfield; }
        }

        /// <summary>
        /// Create a new connection manager
        /// </summary>
        /// <param name="development">Use dev/testing server instead of production</param>
        /// <remarks>
        /// </remarks>
        public Database(string infodb, bool development)
        {
            this.infodb = infodb;
            this.servers = new List<string>(8);
            this.databases = new List<string>(8);
            this.codeDatabase = new List<string>(8);
            this.serverBoundaries = new List<ServerBoundaries>(32);
            this.gridResolution = new int[] { 1024, 1024, 1024 };
            this.atomDim = 8;   // The default blob dimension is 8, for the turbulence DB it is 64
            this.Dt = 0.0002F;  // The default time resolution is 0.0002F for the isotropic turbulence DB, for the MHD database it is 0.00025
            this.timeInc = 10;  // The default time increment is 10 for the coarse isotropic and MHD datasets, for the fine dataset it is 1
            this.timeOff = 0;
            this.development = development;
            this.bitfield = new byte[4096 / 8]; // large enough to cover all data regions
            this.tempTableName = "#" + Guid.NewGuid().ToString().Replace("-", "");
            this.smallAtoms = true; // By default we assume that the atoms stored in the database are small
            this.dx = (2.0 * Math.PI) / (double)this.GridResolutionX;
            this.dy = (2.0 * Math.PI) / (double)this.GridResolutionY;
            this.dz = (2.0 * Math.PI) / (double)this.GridResolutionZ;
        }

        /// <summary>
        /// Initialize the database parameters -- server and DB names, grid resolution, etc.
        /// </summary>
        /// <remarks>
        /// The resolution should be set after the servers are selected,
        /// because in the case of the channel flow dataset the DB needs to be queried to
        /// get the grid values.
        /// </remarks>
        /// <param name="dataset"></param>
        public void Initialize(DataInfo.DataSets dataset, int num_virtual_servers)
        {
            setBlobDim(dataset);
            selectServers(dataset, num_virtual_servers);
            setResolution(dataset);
            setDtTimeInc(dataset);
        }

        public int GridResolutionX { get { return gridResolution[2]; } }
        public int GridResolutionY { get { return gridResolution[1]; } }
        public int GridResolutionZ { get { return gridResolution[0]; } }

        private void setResolution(DataInfo.DataSets dataset)
        {
            switch (dataset)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mhd1024:
                case DataInfo.DataSets.mixing:
                    this.gridResolution = new int[] { 1024, 1024, 1024 };
                    this.dx = (2.0 * Math.PI) / (double)this.GridResolutionX;
                    this.dy = (2.0 * Math.PI) / (double)this.GridResolutionY;
                    this.dz = (2.0 * Math.PI) / (double)this.GridResolutionZ;
                    channel_grid = false;
                    break;
                //case DataInfo.DataSets.rmhd:
                //    this.gridResolution = new int[] { 2048, 2048, 2048 };
                //    this.dx = (2.0 * Math.PI) / (double)this.GridResolutionX;
                //    this.dy = (2.0 * Math.PI) / (double)this.GridResolutionY;
                //    this.dz = (2.0 * Math.PI) / (double)this.GridResolutionZ;
                //    channel_grid = false;
                //    rmhd = true;
                //    break;
                case DataInfo.DataSets.channel:
                    this.gridResolution = new int[] { 1536, 512, 2048 };
                    this.dx = (8.0 * Math.PI) / (double)this.GridResolutionX;
                    this.dz = (3.0 * Math.PI) / (double)this.GridResolutionZ;
                    gridPointsY = new GridPoints(this.GridResolutionY);
                    channel_grid = true;
                    // We need to retrieve the y grid values from the database.
                    // TODO: Maybe randomize the node that we query so that we don't always hit the same server...
                    //String cString = ConfigurationManager.ConnectionStrings["dsp085"].ConnectionString;
                    if (servers.Count > 0)
                    {
                        string server_name = this.servers[0];
                        if (server_name.Contains("_"))
                            server_name = server_name.Remove(server_name.IndexOf("_"));
                        String cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;User ID={2};Password={3};Pooling=false; Connect Timeout = 600;",
                            server_name, codeDatabase[0], ConfigurationManager.AppSettings["turbquery_uid"], ConfigurationManager.AppSettings["turbquery_password"]);
                        SqlConnection sqlConn = new SqlConnection(cString);
                        sqlConn.Open();
                        gridPointsY.GetGridPointsFromDB(sqlConn);
                        sqlConn.Close();
                    }
                    else
                    {
                        throw new Exception("Servers not initialized!");
                    }
                    break;
                default:
                    throw new Exception("Invalid dataset specified!");
            }
        }

        public void setBlobDim(DataInfo.DataSets dataset)
        {
            switch (dataset)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mhd1024:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.mixing:
                //case DataInfo.DataSets.rmhd:
                    this.atomDim = 8;
                    this.smallAtoms = true;
                    break;
                default:
                    throw new Exception("Invalid dataset specified!  Dataset: " + dataset);
            }
        }

        public void setDtTimeInc(DataInfo.DataSets dataset)
        {
            switch (dataset)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    this.Dt = 0.0002F;
                    this.timeInc = 1;
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                    this.Dt = 0.0002F;
                    this.timeInc = 10;
                    break;
                case DataInfo.DataSets.mhd1024:
                    this.Dt = 0.00025F;
                    this.timeInc = 10;
                    break;
                case DataInfo.DataSets.channel:
                    this.Dt = 0.0013F;
                    this.timeInc = 5;
                    this.timeOff = 132010;
                    break;
                case DataInfo.DataSets.mixing:
                    this.Dt = 0.04F;
                    this.timeInc = 1;
                    this.timeOff = 1;
                    break;
                //case DataInfo.DataSets.rmhd:
                //    this.Dt = 0.0006F;
                //    this.timeInc = 4;
                //    break;
                default:
                    throw new Exception("Invalid dataset specified!");
            }
        }


        /// <summary>
        /// Initialize servers, connections and input data tables.
        /// </summary>
        /// <param name="worker">Worker type used</param>
        public void selectServers(DataInfo.DataSets dataset_enum)
        {
            String dataset = dataset_enum.ToString();
            String cString = ConfigurationManager.ConnectionStrings[infodb].ConnectionString;
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            string DBMapTable = "DatabaseMap";
            if (this.development == true)
            {
                DBMapTable = "DatabaseMapTest";
            }
            cmd.CommandText = String.Format("select ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, MIN(minLim) as minLim, MAX(maxLim) as maxLim " +
                "from {0}..{1} where DatasetName = @dataset " + 
                "group by ProductionMachineName, ProductionDatabaseName, CodeDatabaseName " +
                "order by minLim", infodb, DBMapTable);
            cmd.Parameters.AddWithValue("@dataset", dataset);
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                servers.Clear();
                databases.Clear();
                codeDatabase.Clear();
                serverBoundaries.Clear();
                while (reader.Read())
                {
                    servers.Add(reader.GetString(0));
                    databases.Add(reader.GetString(1));
                    if (this.development == false)
                    {
                        codeDatabase.Add(reader.GetString(2));
                    }
                    else
                    {
                        codeDatabase.Add("turbdev");
                    }
                    long minLim = reader.GetSqlInt64(3).Value;
                    long maxLim = reader.GetSqlInt64(4).Value;
                    serverBoundaries.Add(new ServerBoundaries(new Morton3D(minLim), new Morton3D(maxLim)));
                }
            }
            else
            {
                throw new Exception("Invalid dataset specified.");
            }
            reader.Close();
            conn.Close();

            this.serverCount = servers.Count;
            this.connections = new SqlConnection[this.serverCount];
            this.sqlcmds = new SqlCommand[this.serverCount];
            this.count = new int[serverCount];
            this.datatables = new DataTable[serverCount];
            //this.tempTableNames = new String[serverCount];
            for (int i = 0; i < serverCount; i++)
            {
                this.count[i] = 0;
                this.connections[i] = null;
                this.sqlcmds[i] = null;
                this.datatables[i] = createInputDataTable();
                // NOTE: We should not need to create unique table names, but we were
                //       running in problems with connection pooling.
                // NOTE: This table name is used for all methods expect for GetPosition
                // NOTE: Since we may need to access the table from a non-context connection (for bulk insert)
                //       the temp tables need to be global and unique for each connection
                //       as in some cases there are more than 1 databases per server
                //this.tempTableNames[i] = "##" + Guid.NewGuid().ToString().Replace("-","");
            }
        }


        /// <summary>
        /// Initialize servers, connections and input data tables.
        /// </summary>
        /// <param name="num_virtual_servers">Number of virtual servers to use. Currently assumes this is multiple of 2.</param>
        public void selectServers(DataInfo.DataSets dataset_enum, int num_virtual_servers)
        {            
            String dataset = dataset_enum.ToString();
            String cString = ConfigurationManager.ConnectionStrings[infodb].ConnectionString;
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            string DBMapTable = "DatabaseMap";
            if (this.development == true)
            {
                //DBMapTable = "DatabaseMapTest";
            }
            cmd.CommandText = String.Format("select ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, MIN(minLim) as minLim, MAX(maxLim) as maxLim " +
                "from {0}..{1} where DatasetName = @dataset " + 
                "group by ProductionMachineName, ProductionDatabaseName, CodeDatabaseName " +
                "order by minLim", infodb, DBMapTable);
            cmd.Parameters.AddWithValue("@dataset", dataset);
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                servers.Clear();
                databases.Clear();
                codeDatabase.Clear();
                serverBoundaries.Clear();
                while (reader.Read())
                {
                    servers.Add(reader.GetString(0));
                    databases.Add(reader.GetString(1));
                    if (this.development == false)
                    {
                        codeDatabase.Add(reader.GetString(2));
                    }
                    else
                    {
                        codeDatabase.Add("turbdev");
                    }
                    long minLim = reader.GetSqlInt64(3).Value;
                    long maxLim = reader.GetSqlInt64(4).Value;
                    serverBoundaries.Add(new ServerBoundaries(new Morton3D(minLim), new Morton3D(maxLim)));
                }
            }
            else
            {
                throw new Exception("Invalid dataset specified.");
            }
            reader.Close();
            conn.Close(); 
            
            if (num_virtual_servers > 1)
            {
                // Each virtual server will be responsible for some part of the data stored on the physical server
                // We need to make sure that the data are partitioned according to the partitioning scheme (z-order)
                // It is somewhat complicated as the data may not form a cube or occupy a contiguous region along the z-curve
                if ((num_virtual_servers & (num_virtual_servers - 1)) != 0)
                    throw new Exception("The number of virtual servers must be a power of 2!");

                List<string> tempServers = new List<string>(servers.Count * num_virtual_servers);
                List<string> tempDatabases = new List<string>(servers.Count * num_virtual_servers);
                List<string> tempCodeDB = new List<string>(servers.Count * num_virtual_servers);
                List<ServerBoundaries> tempServerBoundaries = new List<ServerBoundaries>(servers.Count * num_virtual_servers);
                ServerBoundaries[] VirtualServerBoundaries;
                int currentServer = 0;
                for (int i = 0; i < servers.Count; i++)
                {
                    VirtualServerBoundaries = serverBoundaries[i].getVirtualServerBoundaries(num_virtual_servers);
                    for (int j = 0; j < num_virtual_servers; j++)
                    {
                        currentServer = i * num_virtual_servers + j;
                        tempServers.Add(servers[i] + "_" + num_virtual_servers + "_" + j);
                        tempDatabases.Add(databases[i]);
                        tempCodeDB.Add(codeDatabase[i]);
                        tempServerBoundaries.Add(VirtualServerBoundaries[j]);
                    }
                }

                servers = tempServers;
                databases = tempDatabases;
                codeDatabase = tempCodeDB;
                serverBoundaries = tempServerBoundaries;
            }

            this.serverCount = servers.Count;
            this.connections = new SqlConnection[this.serverCount];
            this.sqlcmds = new SqlCommand[this.serverCount];
            this.count = new int[serverCount];
            this.datatables = new DataTable[serverCount];
            //this.tempTableNames = new String[serverCount];
            for (int i = 0; i < serverCount; i++)
            {
                this.count[i] = 0;
                this.connections[i] = null;
                this.sqlcmds[i] = null;
                this.datatables[i] = createInputDataTable();
                // NOTE: We should not need to create unique table names, but we were
                //       running in problems with connection pooling.
                // NOTE: This table name is used for all methods expect for GetPosition
                // NOTE: Since we may need to access the table from a non-context connection (for bulk insert)
                //       the temp tables need to be global and unique for each connection
                //       as in some cases there are more than 1 databases per server
                //this.tempTableNames[i] = "##" + Guid.NewGuid().ToString().Replace("-","");
            }
        }

        /// TODO: This function is to be merged with the above
        /// <summary>
        /// Initialize servers, connections and input data tables.
        /// </summary>
        /// <param name="num_virtual_servers">Number of virtual servers to use. Currently assumes this is multiple of 2.</param>
        public void selectServers(DataInfo.DataSets dataset_enum, int num_virtual_servers, int worker)
        {
            String dataset = dataset_enum.ToString();
            String cString = ConfigurationManager.ConnectionStrings[infodb].ConnectionString;
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            string DBMapTable = "DatabaseMap";
            if (this.development == true)
            {
                DBMapTable = "DatabaseMapTest";
            }
            cmd.CommandText = String.Format("select ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, MIN(minLim) as minLim, MAX(maxLim) as maxLim " +
                "from {0}..{1} where DatasetName = @dataset " +
                "group by ProductionMachineName, ProductionDatabaseName, CodeDatabaseName " +
                "order by minLim", infodb, DBMapTable);
            cmd.Parameters.AddWithValue("@dataset", dataset);
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                servers.Clear();
                databases.Clear();
                codeDatabase.Clear();
                serverBoundaries.Clear();
                while (reader.Read())
                {
                    servers.Add(reader.GetString(0));
                    databases.Add(reader.GetString(1));
                    if (this.development == false)
                    {
                        codeDatabase.Add(reader.GetString(2));
                    }
                    else
                    {
                        codeDatabase.Add("turbdev");
                    }
                    long minLim = reader.GetSqlInt64(3).Value;
                    long maxLim = reader.GetSqlInt64(4).Value;
                    serverBoundaries.Add(new ServerBoundaries(new Morton3D(minLim), new Morton3D(maxLim)));
                }
            }
            else
            {
                throw new Exception("Invalid dataset specified.");
            }
            reader.Close();
            conn.Close();

            if (num_virtual_servers > 1)
            {
                // Each virtual server will be responsible for some part of the data stored on the physical server
                // We need to make sure that the data are partitioned according to the partitioning scheme (z-order)
                // It is somewhat complicated as the data may not form a cube or occupy a contiguous region along the z-curve
                if ((num_virtual_servers & (num_virtual_servers - 1)) != 0)
                    throw new Exception("The number of virtual servers must be a power of 2!");

                List<string> tempServers = new List<string>(servers.Count * num_virtual_servers);
                List<string> tempDatabases = new List<string>(servers.Count * num_virtual_servers);
                List<ServerBoundaries> tempServerBoundaries = new List<ServerBoundaries>(servers.Count * num_virtual_servers);
                ServerBoundaries[] VirtualServerBoundaries;
                int currentServer = 0;
                for (int i = 0; i < servers.Count; i++)
                {
                    VirtualServerBoundaries = serverBoundaries[i].getVirtualServerBoundaries(num_virtual_servers);
                    for (int j = 0; j < num_virtual_servers; j++)
                    {
                        currentServer = i * num_virtual_servers + j;
                        tempServers.Add(servers[i] + "_" + num_virtual_servers + "_" + j);
                        tempDatabases.Add(databases[i]);
                        tempServerBoundaries.Add(VirtualServerBoundaries[j]);
                    }
                }

                servers = tempServers;
                databases = tempDatabases;
                serverBoundaries = tempServerBoundaries;
            }

            //if (dataset_enum == DataInfo.DataSets.isotropic1024coarse)
            //{
            //    codeDatabase = "turblib";

            //    servers = new string[] { "gw01", "gw13", "gw02", "gw14", "blackbox5", "gw15", "gw12", "gw16" };
            //    databases = new string[] { "turbdb101", "turbdb102", "turbdb103", "turbdb104", "turbdb105", "turbdb106", "turbdb107", "turbdb108" };

            //    if (this.development == true)
            //    {
            //        codeDatabase = "turbdev";
            //    }
            //}
            //else if (dataset_enum == DataInfo.DataSets.mhd1024)
            //{
            //    codeDatabase = "mhdlib"; //NOTE: Should be "mhdlib" when pushing to production                
            //    servers = new string[] { "gw21", "gw22", "gw23", "gw24" };
            //    databases = new string[] { "mhddb021", "mhddb022", "mhddb023", "mhddb024" };

            //    if (this.development == true)
            //    {
            //        codeDatabase = "turbdev";
            //    }
            //}
            //serverBoundaries = new ServerBoundaries[servers.Length];
            //ServerBoundaries entireGrid = new ServerBoundaries(0, GridResolutionX - 1, 0, GridResolutionX - 1, 0, GridResolutionX - 1);
            //serverBoundaries = entireGrid.getVirtualServerBoundaries(servers.Length);

            //if (num_virtual_servers > 1)
            //{
            //    // Each virtual server will be responsible for some part of the data stored on the physical server
            //    // We need to make sure that the data is partitioned according to the partitioning scheme (z-order)
            //    // It is somewhat complicated as the data may not form a cube or occupy a contiguous region along the z-curve
            //    if ((num_virtual_servers & (num_virtual_servers - 1)) != 0)
            //        throw new Exception("The number of virtual servers must be a power of 2!");

            //    string[] tempServers = new string[servers.Length * num_virtual_servers];
            //    string[] tempDatabases = new string[servers.Length * num_virtual_servers];
            //    ServerBoundaries[] tempServerBoundaries = new ServerBoundaries[servers.Length * num_virtual_servers];
            //    ServerBoundaries[] VirtualServerBoundaries;
            //    int currentServer = 0;
            //    for (int i = 0; i < servers.Length; i++)
            //    {
            //        VirtualServerBoundaries = serverBoundaries[i].getVirtualServerBoundaries(num_virtual_servers);
            //        for (int j = 0; j < num_virtual_servers; j++)
            //        {
            //            currentServer = i * num_virtual_servers + j;
            //            tempServers[currentServer] = servers[i] + "_" + num_virtual_servers + "_" + j;
            //            tempDatabases[currentServer] = databases[i];
            //            tempServerBoundaries[currentServer] = VirtualServerBoundaries[j];
            //        }
            //    }

            //    servers = tempServers;
            //    databases = tempDatabases;
            //    serverBoundaries = tempServerBoundaries;
            //}

            this.serverCount = servers.Count;
            this.connections = new SqlConnection[this.serverCount];
            this.sqlcmds = new SqlCommand[this.serverCount];
            this.count = new int[serverCount];
            this.datatables = new DataTable[serverCount];
            //this.tempTableNames = new String[serverCount];
            for (int i = 0; i < serverCount; i++)
            {
                this.count[i] = 0;
                this.connections[i] = null;
                this.sqlcmds[i] = null;
                if (worker == (int)Worker.Workers.GetPositionDBEvaluation)
                    this.datatables[i] = createInputDataTableForParticleTracking();
                else
                    this.datatables[i] = createInputDataTable();
                // NOTE: We should not need to create unique table names, but we were
                //       running in problems with connection pooling.
                // NOTE: This table name is used for all methods expect for GetPosition
                // NOTE: Since we may need to access the table from a non-context connection (for bulk insert)
                //       the temp tables need to be global and unique for each connection
                //       as in some cases there are more than 1 databases per server
                //this.tempTableNames[i] = "##" + Guid.NewGuid().ToString().Replace("-","");
            }
        }

        /// <summary>
        /// check if there are any open connections and close them
        /// </summary>
        /// 
        public void checkAndCloseConnections()
        {
            if (connections != null)
            {
                for (int i = 0; i < connections.Length; i++)
                {
                    if (connections[i] != null && connections[i].State == ConnectionState.Open)
                        connections[i].Close();
                }
            }
        }

        /// <summary>
        /// Create a DataTable with the correct schema for input to the database nodes.
        /// This table will be Clone()d.
        /// </summary>
        /// <returns></returns>
        private DataTable createInputDataTable()
        {
            DataTable dt = new DataTable("InputTable");

            DataColumn reqseq = new DataColumn("reqseq");
            reqseq.DataType = typeof(int);
            dt.Columns.Add(reqseq);

            DataColumn zindex = new DataColumn("zindex");
            zindex.DataType = typeof(long);
            dt.Columns.Add(zindex);

            DataColumn x = new DataColumn("x");
            x.DataType = typeof(float);
            dt.Columns.Add(x);

            DataColumn y = new DataColumn("y");
            y.DataType = typeof(float);
            dt.Columns.Add(y);

            DataColumn z = new DataColumn("z");
            z.DataType = typeof(float);
            dt.Columns.Add(z);

            dt.BeginLoadData();

            return dt;
        }

        /// <summary>
        /// Create a DataTable with the correct schema for input to the database nodes.
        /// This table will be Clone()d.
        /// </summary>
        /// <returns></returns>
        private DataTable createInputDataTableForParticleTracking()
        {
            DataTable dt = new DataTable("InputTable");

            DataColumn reqseq = new DataColumn("reqseq");
            reqseq.DataType = typeof(int);
            dt.Columns.Add(reqseq);

            DataColumn timestep = new DataColumn("timestep");
            timestep.DataType = typeof(int);
            dt.Columns.Add(timestep);

            DataColumn zindex = new DataColumn("zindex");
            zindex.DataType = typeof(long);
            dt.Columns.Add(zindex);

            DataColumn x = new DataColumn("x");
            x.DataType = typeof(float);
            dt.Columns.Add(x);

            DataColumn y = new DataColumn("y");
            y.DataType = typeof(float);
            dt.Columns.Add(y);

            DataColumn z = new DataColumn("z");
            z.DataType = typeof(float);
            dt.Columns.Add(z);

            DataColumn pre_x = new DataColumn("pre_x");
            pre_x.DataType = typeof(float);
            dt.Columns.Add(pre_x);

            DataColumn pre_y = new DataColumn("pre_y");
            pre_y.DataType = typeof(float);
            dt.Columns.Add(pre_y);

            DataColumn pre_z = new DataColumn("pre_z");
            pre_z.DataType = typeof(float);
            dt.Columns.Add(pre_z);

            DataColumn time = new DataColumn("time");
            time.DataType = typeof(float);
            dt.Columns.Add(time);

            DataColumn endTime = new DataColumn("endTime");
            endTime.DataType = typeof(float);
            dt.Columns.Add(endTime);

            DataColumn deltaT = new DataColumn("dt");
            deltaT.DataType = typeof(float);
            dt.Columns.Add(deltaT);

            DataColumn flag = new DataColumn("compute_predictor");
            flag.DataType = typeof(bool);
            dt.Columns.Add(flag);

            dt.BeginLoadData();

            return dt;
        }

        /// <summary>
        /// Create a DataTable with the correct schema for particle tracking 
        /// for input to the database nodes.
        /// </summary>
        /// <returns></returns>
        private DataTable createTrackingInputDataTable()
        {
            DataTable dt = new DataTable("InputTable");

            DataColumn reqseq = new DataColumn("reqseq");
            reqseq.DataType = typeof(int);
            dt.Columns.Add(reqseq);

            DataColumn timestep = new DataColumn("timestep");
            timestep.DataType = typeof(int);
            dt.Columns.Add(timestep);

            DataColumn zindex = new DataColumn("zindex");
            zindex.DataType = typeof(long);
            dt.Columns.Add(zindex);

            DataColumn x = new DataColumn("x");
            x.DataType = typeof(float);
            dt.Columns.Add(x);

            DataColumn y = new DataColumn("y");
            y.DataType = typeof(float);
            dt.Columns.Add(y);

            DataColumn z = new DataColumn("z");
            z.DataType = typeof(float);
            dt.Columns.Add(z);

            DataColumn pre_x = new DataColumn("pre_x");
            pre_x.DataType = typeof(float);
            dt.Columns.Add(pre_x);

            DataColumn pre_y = new DataColumn("pre_y");
            pre_y.DataType = typeof(float);
            dt.Columns.Add(pre_y);

            DataColumn pre_z = new DataColumn("pre_z");
            pre_z.DataType = typeof(float);
            dt.Columns.Add(pre_z);

            DataColumn vel_x = new DataColumn("vel_x");
            vel_x.DataType = typeof(float);
            dt.Columns.Add(vel_x);

            DataColumn vel_y = new DataColumn("vel_y");
            vel_y.DataType = typeof(float);
            dt.Columns.Add(vel_y);

            DataColumn vel_z = new DataColumn("vel_z");
            vel_z.DataType = typeof(float);
            dt.Columns.Add(vel_z);

            DataColumn time = new DataColumn("time");
            time.DataType = typeof(float);
            dt.Columns.Add(time);

            DataColumn endTime = new DataColumn("endTime");
            endTime.DataType = typeof(float);
            dt.Columns.Add(endTime);

            DataColumn flag = new DataColumn("flag");
            flag.DataType = typeof(bool);
            dt.Columns.Add(flag);

            DataColumn done = new DataColumn("done");
            done.DataType = typeof(bool);
            dt.Columns.Add(done);

            dt.BeginLoadData();

            return dt;
        }

        // Clean up
        public void Clear()
        {
            for (int i = 0; i < serverCount; i++)
            {
                this.count[i] = 0;
                this.connections[i] = null;
                this.sqlcmds[i] = null;
            }
        }

        /// <summary>
        /// Convert from radians to integral coordinates on the cube for the x-dimension.
        /// </summary>
        /// <param name="xp">Input Coordinate</param>
        /// <param name="round">Round to nearest integer (true) or floor (false).</param>
        /// <returns>Integer value [0-DIM)</returns>
        public int GetIntLocX(float xp, bool round)
        {
            int x;
            if (round)
            {
                x = Turbulence.SciLib.LagInterpolation.CalcNodeWithRound(xp, dx);
            }
            else
            {
                x = Turbulence.SciLib.LagInterpolation.CalcNode(xp, dx);
            }

            return ((x % GridResolutionX) + GridResolutionX) % GridResolutionX;
        }
        /// <summary>
        /// Convert from radians to integral coordinates on the cube for the y-dimension.
        /// </summary>
        /// <param name="yp">Input Coordinate</param>
        /// <param name="round">Round to nearest integer (true) or floor (false).</param>
        /// <returns>Integer value [0-DIM)</returns>
        public int GetIntLocY(float yp, bool round, int kernelSizeY)
        {
            int y;
            if (channel_grid)
            {
                if (kernelSizeY == 0)
                    y = gridPointsY.GetCellIndexRound(yp);
                else
                    y = gridPointsY.GetCellIndex(yp, 0.0);
                if (y < 0 || y > GridResolutionY - 1)
                {
                    throw new Exception(String.Format("The given y value({0}) is outside of the allowed range [{1},{2}]!", yp,
                        gridPointsY.GetGridValue(0), gridPointsY.GetGridValue(GridResolutionY - 1)));
                }
            }
            else
            {
                if (round)
                {
                    y = Turbulence.SciLib.LagInterpolation.CalcNodeWithRound(yp, dy);
                }
                else
                {
                    y = Turbulence.SciLib.LagInterpolation.CalcNode(yp, dy);
                }
            }
            return ((y % GridResolutionY) + GridResolutionY) % GridResolutionY;
        }
        /// <summary>
        /// Convert from radians to integral coordinates on the cube for the z-dimension.
        /// </summary>
        /// <param name="zp">Input Coordinate</param>
        /// <param name="round">Round to nearest integer (true) or floor (false).</param>
        /// <returns>Integer value [0-DIM)</returns>
        public int GetIntLocZ(float zp, bool round)
        {
            int z;
            if (round)
            {
                z = Turbulence.SciLib.LagInterpolation.CalcNodeWithRound(zp, dz);
            }
            else
            {
                z = Turbulence.SciLib.LagInterpolation.CalcNode(zp, dz);
            }

            return ((z % GridResolutionZ) + GridResolutionZ) % GridResolutionZ;
        }

        public int GetKernelStart(int n, int kernelSize)
        {
            // (kernelSize + 1)/2 produces Ceil(kernelSize/2)
            return n - (kernelSize + 1) / 2 + 1;
        }

        public void GetServerParameters4RawData(int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth,
            int[] serverX, int[] serverY, int[] serverZ, int[] serverXwidth, int[] serverYwidth, int[] serverZwidth)
        {
            for (int i = 0; i < this.serverCount; i++)
            {
                if (X + Xwidth > serverBoundaries[i].startx && X <= serverBoundaries[i].endx)
                    if (Y + Ywidth > serverBoundaries[i].starty && Y <= serverBoundaries[i].endy)
                        if (Z + Zwidth > serverBoundaries[i].startz && Z <= serverBoundaries[i].endz)
                        {
                            // If we have no workload for this server yet... create a connection
                            if (this.connections[i] == null)
                            {
                                string server_name = this.servers[i];
                                if (server_name.Contains("_"))
                                    server_name = server_name.Remove(server_name.IndexOf("_"));
                                String cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;User ID={2};Password={3};Pooling=false; Connect Timeout = 600;",
                                    server_name, databases[i], ConfigurationManager.AppSettings["turbquery_uid"], ConfigurationManager.AppSettings["turbquery_password"]);
                                this.connections[i] = new SqlConnection(cString);
                                this.connections[i].Open();
                            }
                            GetServerParameters(X, Xwidth, serverBoundaries[i].startx, serverBoundaries[i].endx, ref serverX[i], ref serverXwidth[i]);
                            GetServerParameters(Y, Ywidth, serverBoundaries[i].starty, serverBoundaries[i].endy, ref serverY[i], ref serverYwidth[i]);
                            GetServerParameters(Z, Zwidth, serverBoundaries[i].startz, serverBoundaries[i].endz, ref serverZ[i], ref serverZwidth[i]);

                            //For logging purposes we store the 64^3 regions accessed by the query in the usage Log
                            Morton3D access;
                            for (int x_i = serverX[i] & (-64); x_i <= serverX[i] + serverXwidth[i] - atomDim; x_i += 64)
                                for (int y_i = serverY[i] & (-64); y_i <= serverY[i] + serverYwidth[i] - atomDim; y_i += 64)
                                    for (int z_i = serverZ[i] & (-64); z_i <= serverZ[i] + serverZwidth[i] - atomDim; z_i += 64)
                                    {
                                        access = new Morton3D(z_i, y_i, x_i);
                                        SetBit(access);
                                    }
                        }
            }
        }

        private void GetServerParameters(int query_start, int query_width, int server_start, int server_end, ref int start, ref int width)
        {
            start = query_start < server_start ? server_start : query_start;
            width = query_start + query_width <= server_end ? query_start + query_width - start : server_end + 1 - start;
        }

        public void GetServerParameters4RawData(int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth,
            int[] serverX, int[] serverY, int[] serverZ, int[] serverXwidth, int[] serverYwidth, int[] serverZwidth, 
            int x_stride, int y_stride, int z_stride)
        {
            for (int i = 0; i < this.serverCount; i++)
            {
                if (X + Xwidth > serverBoundaries[i].startx && X <= serverBoundaries[i].endx)
                    if (Y + Ywidth > serverBoundaries[i].starty && Y <= serverBoundaries[i].endy)
                        if (Z + Zwidth > serverBoundaries[i].startz && Z <= serverBoundaries[i].endz)
                        {
                            // If we have no workload for this server yet... create a connection
                            if (this.connections[i] == null)
                            {
                                string server_name = this.servers[i];
                                if (server_name.Contains("_"))
                                    server_name = server_name.Remove(server_name.IndexOf("_"));
                                String cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;User ID={2};Password={3};Pooling=false; Connect Timeout = 600;",
                                    server_name, databases[i], ConfigurationManager.AppSettings["turbquery_uid"], ConfigurationManager.AppSettings["turbquery_password"]);
                                this.connections[i] = new SqlConnection(cString);
                                this.connections[i].Open();
                            }
                            GetServerParameters(X, Xwidth, serverBoundaries[i].startx, serverBoundaries[i].endx, ref serverX[i], ref serverXwidth[i], x_stride);
                            GetServerParameters(Y, Ywidth, serverBoundaries[i].starty, serverBoundaries[i].endy, ref serverY[i], ref serverYwidth[i], y_stride);
                            GetServerParameters(Z, Zwidth, serverBoundaries[i].startz, serverBoundaries[i].endz, ref serverZ[i], ref serverZwidth[i], z_stride);

                            //For logging purposes we store the 64^3 regions accessed by the query in the usage Log
                            Morton3D access;
                            for (int x_i = serverX[i] & (-64); x_i <= serverX[i] + serverXwidth[i] - atomDim; x_i += 64)
                                for (int y_i = serverY[i] & (-64); y_i <= serverY[i] + serverYwidth[i] - atomDim; y_i += 64)
                                    for (int z_i = serverZ[i] & (-64); z_i <= serverZ[i] + serverZwidth[i] - atomDim; z_i += 64)
                                    {
                                        access = new Morton3D(z_i, y_i, x_i);
                                        SetBit(access);
                                    }
                        }
            }
        }

        private void GetServerParameters(int query_start, int query_width, int server_start, int server_end, ref int start, ref int width, int stride)
        {
            // We want to start either at the starting position or if that is not within the server boundaries
            // the smallest position higher than or equal to the server starting point, which is in increments of the "step".
            start = query_start < server_start ? (server_start - query_start + stride - 1) / stride * stride + query_start : query_start;
            width = query_start + query_width <= server_end ? query_start + query_width - start : server_end + 1 - start;
            // Make the width be one larger than a multiple of the step.
            width = (width - 1) / stride * stride + 1;
        }

        /// <summary>
        /// Clear the SQL Server Cache for the specified server
        /// </summary>
        /// <remarks>
        /// Used when gathering performance statistics
        /// </remarks>
        public void ClearDBCache()
        {
            foreach (string server in servers)
            {
                SqlConnection conn;
                if (server.Contains("_"))
                    conn = new SqlConnection(String.Format("Server={0};Integrated Security=true;", server.Remove(server.IndexOf("_"))));
                else
                    conn = new SqlConnection(String.Format("Server={0};Integrated Security=true;", server));
                conn.Open();
                SqlCommand sqlcmd = conn.CreateCommand();
                sqlcmd.CommandText = "DBCC DROPCLEANBUFFERS";
                sqlcmd.ExecuteNonQuery();
                sqlcmd.CommandText = "DBCC FREEPROCCACHE";
                sqlcmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        /// <summary>
        /// Insert a single point into a DataTable for future execution.
        /// The conneciton to the database will also be opened, and table created
        /// if it has not yet been done.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="id"></param>
        /// <param name="sfc"></param>
        /// <param name="z"></param>
        /// <param name="y"></param>
        /// <param name="x"></param>
        private void InsertIntoTempTable(int server, int id, Morton3D sfc, float z, float y, float x, bool updateCount)
        {
            // If we have no workload for this server yet... create a connection
            if (this.connections[server] == null)
            {
                string server_name = this.servers[server];
                if (server_name.Contains("_"))
                    server_name = server_name.Remove(server_name.IndexOf("_"));
                //String cString = ConfigurationManager.ConnectionStrings[server_name].ConnectionString;
                String cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;User ID={2};Password={3};Pooling=false; Connect Timeout = 600;",
                    server_name, databases[server], ConfigurationManager.AppSettings["turbquery_uid"], ConfigurationManager.AppSettings["turbquery_password"]);

                //String cString = ConfigurationManager.ConnectionStrings[this.servers[server]].ConnectionString;
                this.connections[server] = new SqlConnection(cString);
                this.connections[server].Open();
                sqlcmds[server] = this.connections[server].CreateCommand();

                //if (development)
                //    ClearDBCache(server); //NOTE: This should not be called when the code is pushed to production

                // In order to make the table name unique among servers we append the server id to the end of it
                // (in the case of the turbulence dataset we have multiple databases residing on the same server, which creates the possibility of name collisions)
                //sqlcmds[server].CommandText = String.Format("CREATE TABLE {0} ( reqseq INT, zindex BIGINT, x REAL, y REAL, z REAL )", tempTableNames[server]);
                sqlcmds[server].CommandText = String.Format("CREATE TABLE {0} ( reqseq INT, zindex BIGINT, x REAL, y REAL, z REAL )", tempTableName);
                sqlcmds[server].CommandTimeout = 3600;
                //sqlcmds[server].CommandText = String.Format("CREATE TABLE {0} ( reqseq INT, zindex BIGINT, x DOUBLE PRECISION, y DOUBLE PRECISION, z DOUBLE PRECISION )", tempTableName);
                sqlcmds[server].ExecuteNonQuery();
            }
            if (updateCount)
                count[server] += 1;


            // New: Use the datatables instead
            try
            {
                DataRow newRow = datatables[server].NewRow();
                newRow["reqseq"] = id;
                newRow["zindex"] = sfc.Key;
                newRow["x"] = x;
                newRow["y"] = y;
                newRow["z"] = z;
                datatables[server].Rows.Add(newRow);

                //TODO: Need to enable logging and make it work for the channel flow DB.
                SetBit(sfc); // Logging
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Could not add input point {0} to data table! [Inner Exception: {1}]", id, ex.ToString()));
            }

            //if (datatables[server].Rows.Count >= 1000000)
            //{
            //    DoBulkInsert();
            //    datatables[server].Clear();
            //    for (int i = 0; i < serverCount; i++)
            //        count[i] = 0;
            //    datatables[server].BeginLoadData();
            //}
        }

        /// <summary>
        /// Insert a single point into a DataTable for particle tracking for future execution.
        /// The conneciton to the database will also be opened, and table created
        /// if it has not yet been done.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="id"></param>
        /// <param name="tInfo"</param>
        /// <param name="time_round"</param>
        /// <param name="dataset"</param>
        private void InsertIntoTrackingTempTable(int server, int id, Morton3D sfc, ParticleTracking point, bool updateCount)
        {
            // If we have no workload for this server yet... create a connection
            if (this.connections[server] == null)
            {
                string server_name = this.servers[server];
                if (server_name.Contains("_"))
                    server_name = server_name.Remove(server_name.IndexOf("_"));
                //String cString = ConfigurationManager.ConnectionStrings[server_name].ConnectionString;
                String cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;User ID={2};Password={3};Pooling=false; Connect Timeout = 600;",
                    server_name, databases[server], ConfigurationManager.AppSettings["turbquery_uid"], ConfigurationManager.AppSettings["turbquery_password"]);
                this.connections[server] = new SqlConnection(cString);
                this.connections[server].Open();
                sqlcmds[server] = this.connections[server].CreateCommand();
                //sqlcmds[server].CommandText = String.Format("CREATE TABLE {0} ( reqseq INT, zindex BIGINT, " +
                //    "x REAL, y REAL, z REAL, " +
                //    "pre_x REAL, pre_y REAL, pre_z REAL, " +
                //    "Vx REAL, Vy REAL, Vz REAL)", tempTableNames[server]);
                sqlcmds[server].CommandText = String.Format("CREATE TABLE {0} ( reqseq INT, zindex BIGINT, " +
                    "x REAL, y REAL, z REAL, " +
                    "pre_x REAL, pre_y REAL, pre_z REAL, " +
                    "Vx REAL, Vy REAL, Vz REAL)", tempTableName);
                try
                {
                    sqlcmds[server].ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    throw new Exception(String.Format("Error creating temporary table!\n" +
                        "Inner Exception: {0}", e.ToString()));
                }
            }
            if (updateCount)
                count[server] += 1;

            // New: Use the datatables instead
            DataRow newRow = datatables[server].NewRow();
            newRow["reqseq"] = id;
            newRow["zindex"] = sfc.Key;
            newRow["x"] = point.x;
            newRow["y"] = point.y;
            newRow["z"] = point.z;
            newRow["pre_x"] = point.predictor.x;
            newRow["pre_y"] = point.predictor.y;
            newRow["pre_z"] = point.predictor.z;
            newRow["Vx"] = point.velocity.x;
            newRow["Vy"] = point.velocity.y;
            newRow["Vz"] = point.velocity.z;
            datatables[server].Rows.Add(newRow);

            SetBit(sfc); // Logging
        }

        /// <summary>
        /// Insert a single point into a DataTable for particle tracking for future execution.
        /// The conneciton to the database will also be opened, and table created
        /// if it has not yet been done.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="id"></param>
        /// <param name="tInfo"</param>
        /// <param name="time_round"</param>
        /// <param name="dataset"</param>
        private void InsertIntoTrackingTempTable(int server, int id, Morton3D sfc, TrackingInfo point)
        {
            // If we have no workload for this server yet... create a connection
            if (this.connections[server] == null)
            {
                string server_name = this.servers[server];
                if (server_name.Contains("_"))
                    server_name = server_name.Remove(server_name.IndexOf("_"));
                //String cString = ConfigurationManager.ConnectionStrings[server_name].ConnectionString;
                String cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;User ID={2};Password={3};Pooling=false; Connect Timeout = 600;",
                    server_name, databases[server], ConfigurationManager.AppSettings["turbquery_uid"], ConfigurationManager.AppSettings["turbquery_password"]);
                this.connections[server] = new SqlConnection(cString);
                this.connections[server].Open();
                sqlcmds[server] = this.connections[server].CreateCommand();
                sqlcmds[server].CommandText = String.Format("CREATE TABLE {0} ( reqseq INT, timestep INT, zindex BIGINT, " +
                    "x REAL, y REAL, z REAL, " +
                    "pre_x REAL, pre_y REAL, pre_z REAL, " +
                    "time REAL, endTime REAL, dt REAL, compute_predictor BIT )", tempTableName);
                try
                {
                    sqlcmds[server].ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    throw new Exception(String.Format("Error creating temporary table!\n" +
                        "Inner Exception: {0}", e.ToString()));
                }
            }

            count[server] += 1;

            // New: Use the datatables instead
            DataRow newRow = datatables[server].NewRow();
            newRow["reqseq"] = id;
            newRow["timestep"] = point.timeStep;
            newRow["zindex"] = sfc.Key;
            newRow["x"] = point.position.x;
            newRow["y"] = point.position.y;
            newRow["z"] = point.position.z;
            newRow["pre_x"] = point.predictor.x;
            newRow["pre_y"] = point.predictor.y;
            newRow["pre_z"] = point.predictor.z;
            newRow["time"] = point.time;
            newRow["endTime"] = point.endTime;
            newRow["dt"] = point.dt;
            newRow["compute_predictor"] = point.compute_predictor;
            datatables[server].Rows.Add(newRow);

            SetBit(sfc); // Logging
        }

        /// <summary>
        /// Sets a bit in the bitfield recording which regions of space are used.
        /// </summary>
        /// <param name="sfc">Bit to set to true</param>
        private void SetBit(Morton3D sfc)
        {
            int bit;
            if (!channel_grid)
            {
                if (rmhd)
                {
                    int num_x_regions = GridResolutionX / 128;
                    int num_y_regions = GridResolutionY / 128;

                    bit = sfc.X / 128 + sfc.Y / 128 * num_x_regions + sfc.Z / 128 * num_x_regions * num_y_regions;
                }
                else
                {
                    bit = (int)(sfc / (long)(1 << 18));
                }
            }
            else
            {
                // The channel flow grid is divided into regions of 64x64x96.
                int num_x_regions = GridResolutionX / 64;
                int num_y_regions = GridResolutionY / 64;
                bit = sfc.X / 64 + sfc.Y / 64 * num_x_regions + sfc.Z / 96 * num_x_regions * num_y_regions;
            }
            int off = bit / 8;
            bitfield[off] |= (byte)(1 << (bit % 8));
        }

        public void AddWorkloadTrackingPointToMultipleServers(int id, float zp, float yp, float xp, TrackingInfo point, bool round, int kernelSize)
        {
            int Z = GetIntLocZ(zp, round);
            int Y = GetIntLocY(yp, round, kernelSize);
            int X = GetIntLocX(xp, round);
            Morton3D zindex = new Morton3D(Z, Y, X);

            int startz = Z - kernelSize / 2 + 1, starty = Y - kernelSize / 2 + 1, startx = X - kernelSize / 2 + 1;
            int endz = Z + kernelSize / 2, endy = Y + kernelSize / 2, endx = X + kernelSize / 2;

            // The last two conditions have to do with wrap around
            // The beginning and end of each kernel may be outside of the grid space
            // Due to periodicity in space these locations are going to be wrapped around
            // Thus, we need to check if the points should be added to these servers
            for (int i = 0; i < this.serverCount; i++)
            {
                if ((serverBoundaries[i].startx <= startx && startx <= serverBoundaries[i].endx) ||
                    (serverBoundaries[i].startx <= endx && endx <= serverBoundaries[i].endx) ||
                    (startx < serverBoundaries[i].startx && serverBoundaries[i].endx < endx) ||
                    (startx + GridResolutionX <= serverBoundaries[i].endx) ||
                    (serverBoundaries[i].startx <= endx - GridResolutionX))
                {
                    if ((serverBoundaries[i].starty <= starty && starty <= serverBoundaries[i].endy) ||
                        (serverBoundaries[i].starty <= endy && endy <= serverBoundaries[i].endy) ||
                        (starty < serverBoundaries[i].starty && serverBoundaries[i].endy < endy) ||
                            (starty + GridResolutionY <= serverBoundaries[i].endy) ||
                            (serverBoundaries[i].starty <= endy - GridResolutionY))
                    {
                        if ((serverBoundaries[i].startz <= startz && startz <= serverBoundaries[i].endz) ||
                            (serverBoundaries[i].startz <= endz && endz <= serverBoundaries[i].endz) ||
                            (startz < serverBoundaries[i].startz && serverBoundaries[i].endz < endz) ||
                            (startz + GridResolutionZ <= serverBoundaries[i].endz) ||
                            (serverBoundaries[i].startz <= endz - GridResolutionZ))
                        {
                            InsertIntoTrackingTempTable(i, id, zindex, point);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a workload point to multiple servers for exection
        /// depening on the spatial partitioning of the data
        /// </summary>
        public void AddWorkloadPointToMultipleServers(int id, float zp, float yp, float xp, bool round, int kernelSizeZ, int kernelSizeY, int kernelSizeX)
        {
            int Z = GetIntLocZ(zp, round);
            int Y = GetIntLocY(yp, round, kernelSizeY);
            int X = GetIntLocX(xp, round);
            Morton3D zindex = new Morton3D(Z, Y, X);

            int startz = GetKernelStart(Z, kernelSizeZ), starty = GetKernelStart(Y, kernelSizeY), startx = GetKernelStart(X, kernelSizeX);
            int endz = startz + kernelSizeZ - 1, endy = starty + kernelSizeY - 1, endx = startx + kernelSizeX - 1;

            // The last two conditions have to do with wrap around
            // The beginning and end of each kernel may be outside of the grid space
            // Due to periodicity in space these locations are going to be wrapped around
            // Thus, we need to check if the points should be added to these servers
            for (int i = 0; i < this.serverCount; i++)
            {
                if ((serverBoundaries[i].startx <= startx && startx <= serverBoundaries[i].endx) ||
                    (serverBoundaries[i].startx <= endx && endx <= serverBoundaries[i].endx) ||
                    (startx < serverBoundaries[i].startx && serverBoundaries[i].endx < endx) ||
                    (startx + GridResolutionX <= serverBoundaries[i].endx) ||
                    (serverBoundaries[i].startx <= endx - GridResolutionX))
                {
                    if ((serverBoundaries[i].starty <= starty && starty <= serverBoundaries[i].endy) ||
                        (serverBoundaries[i].starty <= endy && endy <= serverBoundaries[i].endy) ||
                        (starty < serverBoundaries[i].starty && serverBoundaries[i].endy < endy) ||
                            (starty + GridResolutionY <= serverBoundaries[i].endy) ||
                            (serverBoundaries[i].starty <= endy - GridResolutionY))
                    {
                        if ((serverBoundaries[i].startz <= startz && startz <= serverBoundaries[i].endz) ||
                            (serverBoundaries[i].startz <= endz && endz <= serverBoundaries[i].endz) ||
                            (startz < serverBoundaries[i].startz && serverBoundaries[i].endz < endz) ||
                            (startz + GridResolutionZ <= serverBoundaries[i].endz) ||
                            (serverBoundaries[i].startz <= endz - GridResolutionZ))
                        {
                            InsertIntoTempTable(i, id, zindex, zp, yp, xp, true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a workload point to multiple servers for exection
        /// depening on the spatial partitioning of the data.
        /// Also updates the min/max coordinates for each server.
        /// </summary>
        /// <remarks>
        /// At the moment we only need to check the y and z dimensions
        /// Each server has all of the data along x
        /// </remarks>
        public void AddWorkloadPointToMultipleServersFiltering(int id, float zp, float yp, float xp, bool round, int kernelSize,
            Point3[] serverMin, Point3[] serverMax)
        {
            int Z = GetIntLocZ(zp, round);
            int Y = GetIntLocY(yp, round, kernelSize);
            int X = GetIntLocX(xp, round);
            Morton3D zindex = new Morton3D(Z, Y, X);

            int startz = Z - kernelSize / 2, starty = Y - kernelSize / 2, startx = X - kernelSize / 2;
            int endz = Z + kernelSize / 2, endy = Y + kernelSize / 2, endx = X + kernelSize / 2;
            startx = ((startx % GridResolutionX) + GridResolutionX) % GridResolutionX;
            starty = ((starty % GridResolutionY) + GridResolutionY) % GridResolutionY;
            startz = ((startz % GridResolutionZ) + GridResolutionZ) % GridResolutionZ;
            endx = ((endx % GridResolutionX) + GridResolutionX) % GridResolutionX;
            endy = ((endy % GridResolutionY) + GridResolutionY) % GridResolutionY;
            endz = ((endz % GridResolutionZ) + GridResolutionZ) % GridResolutionZ;

            // The last two conditions have to do with wrap around
            // The beginning and end of each kernel may be outside of the grid space
            // Due to periodicity in space these locations are going to be wrapped around
            // Thus, we need to check if the points should be added to these servers
            for (int i = 0; i < this.serverCount; i++)
            {
                if ((serverBoundaries[i].startx <= startx && startx <= serverBoundaries[i].endx) ||
                    (serverBoundaries[i].startx <= endx && endx <= serverBoundaries[i].endx) ||
                    (startx < serverBoundaries[i].startx && serverBoundaries[i].endx < endx) ||
                    (startx < serverBoundaries[i].startx && endx < startx) ||
                    (endx < startx && serverBoundaries[i].endx < endx))
                {
                    if ((serverBoundaries[i].starty <= starty && starty <= serverBoundaries[i].endy) ||
                        (serverBoundaries[i].starty <= endy && endy <= serverBoundaries[i].endy) ||
                        (starty < serverBoundaries[i].starty && serverBoundaries[i].endy < endy) ||
                        (starty < serverBoundaries[i].starty && endy < starty) ||
                        (endy < starty && serverBoundaries[i].endy < endy))
                    {
                        if ((serverBoundaries[i].startz <= startz && startz <= serverBoundaries[i].endz) ||
                            (serverBoundaries[i].startz <= endz && endz <= serverBoundaries[i].endz) ||
                            (startz < serverBoundaries[i].startz && serverBoundaries[i].endz < endz) ||
                            (startz < serverBoundaries[i].startz && endz < startz) ||
                            (endz < startz && serverBoundaries[i].endz < endz))
                        {
                            InsertIntoTempTable(i, id, zindex, zp, yp, xp, true);
                            if (serverMin[i].x > startx)
                                if (startx >= serverBoundaries[i].startx)
                                    serverMin[i].x = startx;
                                else
                                    serverMin[i].x = serverBoundaries[i].startx;
                            else if (startx > serverBoundaries[i].endx && startx > endx)
                                serverMin[i].x = serverBoundaries[i].startx;
                            if (serverMin[i].y > starty)
                                if (starty >= serverBoundaries[i].starty)
                                    serverMin[i].y = starty;
                                else
                                    serverMin[i].y = serverBoundaries[i].starty;
                            else if (starty > serverBoundaries[i].endy && starty > endy)
                                serverMin[i].y = serverBoundaries[i].starty;
                            if (serverMin[i].z > startz)
                                if (startz >= serverBoundaries[i].startz)
                                    serverMin[i].z = startz;
                                else
                                    serverMin[i].z = serverBoundaries[i].startz;
                            else if (startz > serverBoundaries[i].endz && startz > endz)
                                serverMin[i].z = serverBoundaries[i].startz;

                            if (serverMax[i].x < endx)
                                if (endx <= serverBoundaries[i].endx)
                                    serverMax[i].x = endx;
                                else
                                    serverMax[i].x = serverBoundaries[i].endx;
                            else if (endx < serverBoundaries[i].startx && endx < startx)
                                serverMax[i].x = serverBoundaries[i].endx;
                            if (serverMax[i].y < endy)
                                if (endy <= serverBoundaries[i].endy)
                                    serverMax[i].y = endy;
                                else
                                    serverMax[i].y = serverBoundaries[i].endy;
                            else if (endy < serverBoundaries[i].starty && endy < starty)
                                serverMax[i].y = serverBoundaries[i].endy;
                            if (serverMax[i].z < endz)
                                if (endz <= serverBoundaries[i].endz)
                                    serverMax[i].z = endz;
                                else
                                    serverMax[i].z = serverBoundaries[i].endz;
                            else if (endz < serverBoundaries[i].startz && endz < startz)
                                serverMax[i].z = serverBoundaries[i].endz;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Commit input tables using BulkInsert
        /// </summary>
        public void DoBulkInsert()
        {
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null && datatables[s].Rows.Count > 0)
                {
                    datatables[s].EndLoadData();
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connections[s], SqlBulkCopyOptions.TableLock, null))
                    {
                        //bulkCopy.DestinationTableName = tempTableNames[s];
                        bulkCopy.DestinationTableName = tempTableName;
                        bulkCopy.WriteToServer(datatables[s]);

                    }
                }
            }
        }

        /// <summary>
        /// Commit input tables using BulkInsert
        /// Customized for batching
        /// </summary>
        public void DoBulkInsertBatch()
        {
            DoBulkInsert();
        }

        /// <summary>
        /// Bulk load a large number of points into database workload tables for execution.
        /// </summary>
        /// <param name="points">Points</param>
        /// <param name="round">True for round, false for floor</param>
        public void AddBulkParticles(Point3[] points, int kernelSizeZ, int kernelSizeY, int kernelSizeX, bool round, float time)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (channel_grid)
                {
                    points[i].x -= 0.45f * time;
                }
                AddWorkloadPointToMultipleServers(i, points[i].z, points[i].y, points[i].x, round, kernelSizeZ, kernelSizeY, kernelSizeX);
            }

            DoBulkInsert();
        }

        /// <summary>
        /// Bulk load a large number of points into database workload tables for execution.
        /// While creating the data tables for the bulk load also computes the workload density.
        /// </summary>
        /// <param name="points">Points</param>
        /// <param name="round">True for round, false for floor</param>
        /// <returns>Return the worker type that should be used.</returns>
        public int AddBulkParticlesFiltering(Point3[] points, int kernelSize, bool round, int worker)
        {
            //long full = new Morton3D(0, 0, DIM).Key; 
            //long full = new Morton3D(DIM - 1, DIM - 1, DIM - 1) + 1;
            //long pernode = full / this.serverCount;  // space assigned to each node

            //ServerBoundaries[] serverBoundaries = new ServerBoundaries[this.serverCount];

            // We have 3 dimensions in most cases
            // As the quantities are vectors
            Point3[] serverMin = new Point3[this.serverCount];
            Point3[] serverMax = new Point3[this.serverCount];
            for (int i = 0; i < this.serverCount; i++)
            {
                //// first zindex stored on this server gives us the coordinates of the 
                //// lower left corner of the data cube
                //Morton3D first_zindex = new Morton3D(pernode * i);
                //// the last index is the computed from the first zindex of the next server
                //// this gives us the coordinates of the upper right corner of the data cube
                //Morton3D last_zindex = new Morton3D(pernode * (i + 1) - 1);
                //serverBoundaries[i].startx = first_zindex.X;
                //serverBoundaries[i].endx = last_zindex.X;
                //serverBoundaries[i].starty = first_zindex.Y;
                //serverBoundaries[i].endy = last_zindex.Y;
                //serverBoundaries[i].startz = first_zindex.Z;
                //serverBoundaries[i].endz = last_zindex.Z;

                serverMin[i].x = serverBoundaries[i].endx;
                serverMin[i].y = serverBoundaries[i].endy;
                serverMin[i].z = serverBoundaries[i].endz;
                serverMax[i].x = serverBoundaries[i].startx;
                serverMax[i].y = serverBoundaries[i].starty;
                serverMax[i].z = serverBoundaries[i].startz;
            }

            for (int i = 0; i < points.Length; i++)
            {
                AddWorkloadPointToMultipleServersFiltering(i, points[i].z, points[i].y, points[i].x, round, kernelSize, serverMin, serverMax);
            }

            DoBulkInsert();

            //int worker;

            double boundingArea = 0.0;

            for (int i = 0; i < serverCount; i++)
            {
                if (connections[i] != null && count[i] > 0)
                    boundingArea += ((serverMax[i].x - serverMin[i].x + 1) * (serverMax[i].y - serverMin[i].y + 1) * (serverMax[i].z - serverMin[i].z + 1));
            }

            double density = (kernelSize + atomDim - 1) * (kernelSize + atomDim - 1) * (kernelSize + atomDim - 1) /
                            (boundingArea + points.Length) *
                            points.Length;

            // For dense workloads we execute the query by means of Summed Volumes
            // The constant is empirically determined (see Filtering Paper)
            if (density > DENSITY_CONSTANT)
            {
                // We check if a box filter of the parameters is desired
                // or the sub-grid stress tensor
                if (worker == (int)Worker.Workers.GetMHDBoxFilter)
                    worker = (int)Worker.Workers.GetMHDBoxFilterSV;
                else if (worker == (int)Worker.Workers.GetMHDBoxFilterSGS)
                    worker = (int)Worker.Workers.GetMHDBoxFilterSGS_SV;
                // This case should be enabled when the Summed Volumes technique is implemented for the 
                // box filter of the gradient.
                //else if (worker == (int)Worker.Workers.GetMHDBoxFilterGradient)
            }
            // For sparse workloads we execute the query by means of I/O Streaming
            else
            {
                // We again check if a box filter of the parameters is desired
                // or the sub-grid stress tensor
                if (worker == (int)Worker.Workers.GetMHDBoxFilter)
                    worker = (int)Worker.Workers.GetMHDBoxFilter;
                else if (worker == (int)Worker.Workers.GetMHDBoxFilterSGS)
                    worker = (int)Worker.Workers.GetMHDBoxFilterSGS;
                else if (worker == (int)Worker.Workers.GetMHDBoxFilterGradient)
                    worker = (int)Worker.Workers.GetMHDBoxFilterGradient;
            }

            return worker;
        }

        /// <summary>
        /// Bulk load a large number of points into database workload tables for execution.
        /// Customized for batch execution
        /// Used for the MHD dataset.
        /// </summary>
        /// <param name="points">Points</param>
        /// <param name="round">True for round, false for floor</param>
        public void AddBulkParticlesBatch(int basei, Point3[] points, bool round, int kernelSize)
        {
            //long full = new Morton3D(0, 0, DIM).Key; 
            long full = new Morton3D(GridResolutionX - 1, GridResolutionX - 1, GridResolutionX - 1) + 1;
            long pernode = full / this.serverCount;  // space assigned to each node

            ServerBoundaries[] serverBoundaries = new ServerBoundaries[this.serverCount];
            for (int i = 0; i < this.serverCount; i++)
            {
                // first zindex stored on this server gives us the coordinates of the 
                // lower left corner of the data cube
                Morton3D first_zindex = new Morton3D(pernode * i);
                // the last index is the computed from the first zindex of the next server
                // this gives us the coordinates of the upper right corner of the data cube
                Morton3D last_zindex = new Morton3D(pernode * (i + 1) - 1);
                serverBoundaries[i].startx = first_zindex.X;
                serverBoundaries[i].endx = last_zindex.X;
                serverBoundaries[i].starty = first_zindex.Y;
                serverBoundaries[i].endy = last_zindex.Y;
                serverBoundaries[i].startz = first_zindex.Z;
                serverBoundaries[i].endz = last_zindex.Z;
            }

            for (int i = 0; i < points.Length; i++)
            {
                AddWorkloadPointToMultipleServers(basei + i, points[i].z, points[i].y, points[i].x, round, kernelSize, kernelSize, kernelSize);
            }

            DoBulkInsert();
        }

        /// <summary>
        /// Bulk load a large number of points into database workload tables for execution.
        /// This method adds particles to a single server only.
        /// </summary>
        /// <param name="points">Points</param>
        /// <param name="round">True for round, false for floor</param>
        public void AddBulkParticlesSingleServer(Point3[] points, int kernelSizeZ, int kernelSizeY, int kernelSizeX, bool round, float time)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (channel_grid)
                {
                    points[i].x -= 0.45f * time;
                }
                int Z = GetIntLocZ(points[i].z, round);
                int Y = GetIntLocY(points[i].y, round, kernelSizeY);
                int X = GetIntLocX(points[i].x, round);
                Morton3D zindex = new Morton3D(Z, Y, X);

                //TODO: Flag is for debuggin purposes only
                bool flag = false;
                for (int s = 0; s < this.serverCount; s++)
                {
                    if ((serverBoundaries[s].startx <= X && X <= serverBoundaries[s].endx))
                    {
                        if ((serverBoundaries[s].starty <= Y && Y <= serverBoundaries[s].endy))
                        {
                            if ((serverBoundaries[s].startz <= Z && Z <= serverBoundaries[s].endz))
                            {
                                InsertIntoTempTable(s, i, zindex, points[i].z, points[i].y, points[i].x, true);

                                if (flag)
                                {
                                    throw new Exception("Particle assigned to more than one server!");
                                }
                                flag = true;
                            }
                        }
                    }
                }

                if (!flag)
                {
                    throw new Exception("Particle not assigned to any server!");
                }
            }
            DoBulkInsert();
        }

        /// <summary>
        /// Check if the given input points wrap around in x
        /// We only check x, as the data is already split along y and z
        /// among different servers
        /// </summary>
        /// <param name="points">The list of input locations</param>
        /// <param name="kernelSize">The size of the kernel to be applied</param>
        /// <param name="round">True for round, false for floor</param>
        public bool CheckInputForWrapAround(Point3[] points, int kernelSize, bool round)
        {
            for (int i = 0; i < points.Length; i++)
            {
                int Z = GetIntLocZ(points[i].z, round);
                int Y = GetIntLocY(points[i].y, round, kernelSize);
                int X = GetIntLocX(points[i].x, round);

                int startz = Z - kernelSize / 2, starty = Y - kernelSize / 2, startx = X - kernelSize / 2;
                int endz = Z + kernelSize / 2, endy = Y + kernelSize / 2, endx = X + kernelSize / 2;
                //if (startx < 0 || starty < 0 || startz < 0 || endx >= DIM || endy >= DIM || endz >= DIM)
                if (startx < 0 || endx >= GridResolutionX)
                    return true;
            }
            return false;
        }

        private int GetXYZResults(IAsyncResult[] asyncRes, Vector3[] result)
        {
            int records = 0;
            // Now go through and fetch results...
            // FIXME: This should be done through callbacks.
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    int id;
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        id = reader.GetSqlInt32(0).Value;
                        if (result[id].Equals(null))
                        {
                            result[id] = new Vector3(reader.GetSqlSingle(1).Value,
                                reader.GetSqlSingle(2).Value,
                                reader.GetSqlSingle(3).Value);
                            records++;
                        }
                        else
                        {
                            result[id].x += reader.GetSqlSingle(1).Value;
                            result[id].y += reader.GetSqlSingle(2).Value;
                            result[id].z += reader.GetSqlSingle(3).Value;
                        }
                    }
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }
            }
            return records;
        }

        private int GetXYZPResults(IAsyncResult[] asyncRes, Vector3P[] result)
        {
            int records = 0;
            // Now go through and fetch results...
            // FIXME: This should be done through callbacks.
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    int id;
                    while (reader.Read())
                    {
                        id = reader.GetSqlInt32(0).Value;
                        result[id] = new Vector3P(reader.GetSqlSingle(1).Value,
                            reader.GetSqlSingle(2).Value,
                            reader.GetSqlSingle(3).Value,
                            reader.GetSqlSingle(4).Value);
                        records++;
                    }
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }
            }
            return records;
        }

        private int GetPressureResults(IAsyncResult[] asyncRes, Pressure[] result)
        {
            int records = 0;
            // Now go through and fetch results...
            // FIXME: This should be done through callbacks.
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    int id;
                    while (reader.Read())
                    {
                        id = reader.GetSqlInt32(0).Value;
                        if (result[id].Equals(null))
                        {
                            result[id] = new Pressure(reader.GetSqlSingle(1).Value);
                            records++;
                        }
                        else
                        {
                            result[id].p += reader.GetSqlSingle(1).Value;
                        }
                    }
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }
            }
            return records;
        }

        // Pressure results where the pressure is stored in the first component of a vector field
        private int GetPressureResults(IAsyncResult[] asyncRes, Vector3[] result)
        {
            int records = 0;
            // Now go through and fetch results...
            // FIXME: This should be done through callbacks.
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    int id;
                    while (reader.Read())
                    {
                        id = reader.GetSqlInt32(0).Value;
                        if (result[id].Equals(null))
                        {
                            result[id] = new Vector3(reader.GetSqlSingle(1).Value, 0.0f, 0.0f);
                            records++;
                        }
                        else
                        {
                            result[id].x += reader.GetSqlSingle(1).Value;
                        }
                    }
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }
            }
            return records;
        }

        private int GetPressureHessianResults(IAsyncResult[] asyncRes, PressureHessian[] result)
        {
            int records = 0;
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    int id;
                    while (reader.Read())
                    {
                        id = reader.GetSqlInt32(0).Value;
                        if (result[id].Equals(null))
                        {
                            result[id] = new PressureHessian(
                                reader.GetSqlSingle(1).Value,
                                reader.GetSqlSingle(2).Value,
                                reader.GetSqlSingle(3).Value,
                                reader.GetSqlSingle(4).Value,
                                reader.GetSqlSingle(5).Value,
                                reader.GetSqlSingle(6).Value);
                            records++;
                        }
                        else
                        {
                            result[id].d2pdxdx += reader.GetSqlSingle(1).Value;
                            result[id].d2pdxdy += reader.GetSqlSingle(2).Value;
                            result[id].d2pdxdz += reader.GetSqlSingle(3).Value;
                            result[id].d2pdydy += reader.GetSqlSingle(4).Value;
                            result[id].d2pdydz += reader.GetSqlSingle(5).Value;
                            result[id].d2pdzdz += reader.GetSqlSingle(6).Value;
                        }
                    }
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }
            }
            return records;
        }

        private int GetVelocityHessianResults(IAsyncResult[] asyncRes, VelocityHessian[] result)
        {
            int records = 0;
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    int id;
                    while (reader.Read())
                    {
                        id = reader.GetSqlInt32(0).Value;
                        if (result[id].Equals(null))
                        {
                            result[id] = new VelocityHessian(
                                reader.GetSqlSingle(1).Value,
                                reader.GetSqlSingle(2).Value,
                                reader.GetSqlSingle(3).Value,
                                reader.GetSqlSingle(4).Value,
                                reader.GetSqlSingle(5).Value,
                                reader.GetSqlSingle(6).Value,
                                reader.GetSqlSingle(7).Value,
                                reader.GetSqlSingle(8).Value,
                                reader.GetSqlSingle(9).Value,
                                reader.GetSqlSingle(10).Value,
                                reader.GetSqlSingle(11).Value,
                                reader.GetSqlSingle(12).Value,
                                reader.GetSqlSingle(13).Value,
                                reader.GetSqlSingle(14).Value,
                                reader.GetSqlSingle(15).Value,
                                reader.GetSqlSingle(16).Value,
                                reader.GetSqlSingle(17).Value,
                                reader.GetSqlSingle(18).Value);
                            records++;
                        }
                        else
                        {
                            result[id].d2uxdxdx += reader.GetSqlSingle(1).Value;
                            result[id].d2uxdxdy += reader.GetSqlSingle(2).Value;
                            result[id].d2uxdxdz += reader.GetSqlSingle(3).Value;
                            result[id].d2uxdydy += reader.GetSqlSingle(4).Value;
                            result[id].d2uxdydz += reader.GetSqlSingle(5).Value;
                            result[id].d2uxdzdz += reader.GetSqlSingle(6).Value;
                            result[id].d2uydxdx += reader.GetSqlSingle(7).Value;
                            result[id].d2uydxdy += reader.GetSqlSingle(8).Value;
                            result[id].d2uydxdz += reader.GetSqlSingle(9).Value;
                            result[id].d2uydydy += reader.GetSqlSingle(10).Value;
                            result[id].d2uydydz += reader.GetSqlSingle(11).Value;
                            result[id].d2uydzdz += reader.GetSqlSingle(12).Value;
                            result[id].d2uzdxdx += reader.GetSqlSingle(13).Value;
                            result[id].d2uzdxdy += reader.GetSqlSingle(14).Value;
                            result[id].d2uzdxdz += reader.GetSqlSingle(15).Value;
                            result[id].d2uzdydy += reader.GetSqlSingle(16).Value;
                            result[id].d2uzdydz += reader.GetSqlSingle(17).Value;
                            result[id].d2uzdzdz += reader.GetSqlSingle(18).Value;
                        }
                    }
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }

            }
            return records;
        }

        private int GetVelocityLaplacianResults(IAsyncResult[] asyncRes, Vector3[] result)
        {
            int records = 0;
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    int id;
                    while (reader.Read())
                    {
                        id = reader.GetSqlInt32(0).Value;
                        if (result[id].Equals(null))
                        {
                            result[id] = new Vector3(
                                reader.GetSqlSingle(1).Value,
                                reader.GetSqlSingle(2).Value,
                                reader.GetSqlSingle(3).Value);
                            records++;
                        }
                        else
                        {
                            result[id].x += reader.GetSqlSingle(1).Value;
                            result[id].y += reader.GetSqlSingle(2).Value;
                            result[id].z += reader.GetSqlSingle(3).Value;
                        }
                    }
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }
            }
            return records;
        }

        private int GetVelocityGradientResults(IAsyncResult[] asyncRes, VelocityGradient[] result)
        {
            int records = 0;
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    int id;
                    while (reader.Read())
                    {
                        id = reader.GetSqlInt32(0).Value;
                        if (result[id].Equals(null))
                        {
                            result[id] = new VelocityGradient(
                                reader.GetSqlSingle(1).Value,
                                reader.GetSqlSingle(2).Value,
                                reader.GetSqlSingle(3).Value,
                                reader.GetSqlSingle(4).Value,
                                reader.GetSqlSingle(5).Value,
                                reader.GetSqlSingle(6).Value,
                                reader.GetSqlSingle(7).Value,
                                reader.GetSqlSingle(8).Value,
                                reader.GetSqlSingle(9).Value
                                );
                            records++;
                        }
                        else
                        {
                            result[id].duxdx += reader.GetSqlSingle(1).Value;
                            result[id].duxdy += reader.GetSqlSingle(2).Value;
                            result[id].duxdz += reader.GetSqlSingle(3).Value;
                            result[id].duydx += reader.GetSqlSingle(4).Value;
                            result[id].duydy += reader.GetSqlSingle(5).Value;
                            result[id].duydz += reader.GetSqlSingle(6).Value;
                            result[id].duzdx += reader.GetSqlSingle(7).Value;
                            result[id].duzdy += reader.GetSqlSingle(8).Value;
                            result[id].duzdz += reader.GetSqlSingle(9).Value;
                        }
                    }
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }
            }
            return records;
        }

        private void GetRawResults(IAsyncResult[] asyncRes, byte[] result, int components,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth,
            int[] serverX, int[] serverY, int[] serverZ, int[] serverXwidth, int[] serverYwidth, int[] serverZwidth)
        {
            // Now go through and fetch results...
            // FIXME: This should be done through callbacks.
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    int size = serverXwidth[s] * serverYwidth[s] * serverZwidth[s] * components * sizeof(float);
                    int readLength = size;
                    byte[] rawdata = new byte[size];
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    while (reader.Read())
                    {
                        int bytesread = 0;
                        while (bytesread < size)
                        {
                            if (size - bytesread > MAX_READ_LENGTH)
                                readLength = MAX_READ_LENGTH;
                            else
                                readLength = size - bytesread;
                            int bytes = (int)reader.GetBytes(0, bytesread, rawdata, bytesread, readLength);
                            if (bytes <= 0)
                                throw new Exception("Unexpected end of cutout!");
                            bytesread += bytes;
                        }
                    }

                    int sourceIndex = 0;
                    int destinationIndex0 = components * (serverX[s] - X + (serverY[s] - Y) * Xwidth + (serverZ[s] - Z) * Xwidth * Ywidth) * sizeof(float);
                    int destinationIndex;
                    int length = serverXwidth[s] * components * sizeof(float);
                    for (int k = 0; k < serverZwidth[s]; k++)
                    {
                        destinationIndex = destinationIndex0 + k * Xwidth * Ywidth * components * sizeof(float);
                        for (int j = 0; j < serverYwidth[s]; j++)
                        {
                            Array.Copy(rawdata, sourceIndex, result, destinationIndex, length);
                            sourceIndex += length;
                            destinationIndex += Xwidth * components * sizeof(float);
                        }
                    }
                    rawdata = null;
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }
            }
        }

        private int GetSGSResults(IAsyncResult[] asyncRes, SGSTensor[] result)
        {
            int records = 0;
            // Now go through and fetch results...
            // FIXME: This should be done through callbacks.
            Vector3[] velocity_filter = new Vector3[result.Length];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    int id;
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        id = reader.GetSqlInt32(0).Value;
                        if (result[id].Equals(null))
                        {
                            result[id] = new SGSTensor(reader.GetSqlSingle(1).Value,
                                reader.GetSqlSingle(2).Value,
                                reader.GetSqlSingle(3).Value,
                                reader.GetSqlSingle(4).Value,
                                reader.GetSqlSingle(5).Value,
                                reader.GetSqlSingle(6).Value);
                            velocity_filter[id] = new Vector3(reader.GetSqlSingle(7).Value,
                                reader.GetSqlSingle(8).Value,
                                reader.GetSqlSingle(9).Value);
                            records++;
                        }
                        else
                        {
                            result[id].xx += reader.GetSqlSingle(1).Value;
                            result[id].yy += reader.GetSqlSingle(2).Value;
                            result[id].zz += reader.GetSqlSingle(3).Value;
                            result[id].xy += reader.GetSqlSingle(4).Value;
                            result[id].xz += reader.GetSqlSingle(5).Value;
                            result[id].yz += reader.GetSqlSingle(6).Value;
                            velocity_filter[id].x += reader.GetSqlSingle(7).Value;
                            velocity_filter[id].y += reader.GetSqlSingle(8).Value;
                            velocity_filter[id].z += reader.GetSqlSingle(9).Value;
                        }
                    }
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }
            }
            for (int i = 0; i < result.Length; i++)
            {
                result[i].xx -= velocity_filter[i].x * velocity_filter[i].x;
                result[i].yy -= velocity_filter[i].y * velocity_filter[i].y;
                result[i].zz -= velocity_filter[i].z * velocity_filter[i].z;
                result[i].xy -= velocity_filter[i].x * velocity_filter[i].y;
                result[i].xz -= velocity_filter[i].x * velocity_filter[i].z;
                result[i].yz -= velocity_filter[i].y * velocity_filter[i].z;
            }
            return records;
        }

        private int GetTwoFieldsSGSResults(IAsyncResult[] asyncRes, VelocityGradient[] result)
        {
            int records = 0;
            // Now go through and fetch results...
            // FIXME: This should be done through callbacks.
            Vector3[] field1_filter = new Vector3[result.Length];
            Vector3[] field2_filter = new Vector3[result.Length];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    int id;
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        id = reader.GetSqlInt32(0).Value;
                        if (result[id].Equals(null))
                        {
                            result[id] = new VelocityGradient(reader.GetSqlSingle(1).Value,
                                reader.GetSqlSingle(4).Value,
                                reader.GetSqlSingle(7).Value,
                                reader.GetSqlSingle(2).Value,
                                reader.GetSqlSingle(5).Value,
                                reader.GetSqlSingle(8).Value,
                                reader.GetSqlSingle(3).Value,
                                reader.GetSqlSingle(6).Value,
                                reader.GetSqlSingle(9).Value);
                            field1_filter[id] = new Vector3(reader.GetSqlSingle(10).Value,
                                reader.GetSqlSingle(11).Value,
                                reader.GetSqlSingle(12).Value);
                            field2_filter[id] = new Vector3(reader.GetSqlSingle(13).Value,
                                reader.GetSqlSingle(14).Value,
                                reader.GetSqlSingle(15).Value);
                            records++;
                        }
                        else
                        {
                            result[id].duxdx += reader.GetSqlSingle(1).Value;
                            result[id].duydx += reader.GetSqlSingle(2).Value;
                            result[id].duzdx += reader.GetSqlSingle(3).Value;
                            result[id].duxdy += reader.GetSqlSingle(4).Value;
                            result[id].duydy += reader.GetSqlSingle(5).Value;
                            result[id].duzdy += reader.GetSqlSingle(6).Value;
                            result[id].duxdz += reader.GetSqlSingle(7).Value;
                            result[id].duydz += reader.GetSqlSingle(8).Value;
                            result[id].duzdz += reader.GetSqlSingle(9).Value;
                            field1_filter[id].x += reader.GetSqlSingle(10).Value;
                            field1_filter[id].y += reader.GetSqlSingle(11).Value;
                            field1_filter[id].z += reader.GetSqlSingle(12).Value;
                            field2_filter[id].x += reader.GetSqlSingle(13).Value;
                            field2_filter[id].y += reader.GetSqlSingle(14).Value;
                            field2_filter[id].z += reader.GetSqlSingle(15).Value;
                        }
                    }
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }
            }
            for (int i = 0; i < result.Length; i++)
            {
                result[i].duxdx -= field1_filter[i].x * field2_filter[i].x;
                result[i].duydx -= field1_filter[i].x * field2_filter[i].y;
                result[i].duzdx -= field1_filter[i].x * field2_filter[i].z;
                result[i].duxdy -= field1_filter[i].y * field2_filter[i].x;
                result[i].duydy -= field1_filter[i].y * field2_filter[i].y;
                result[i].duzdy -= field1_filter[i].y * field2_filter[i].z;
                result[i].duxdz -= field1_filter[i].z * field2_filter[i].x;
                result[i].duydz -= field1_filter[i].z * field2_filter[i].y;
                result[i].duydz -= field1_filter[i].z * field2_filter[i].z;
            }
            return records;
        }

        private int GetTwoFieldsSGSResults(IAsyncResult[] asyncRes, Vector3[] result)
        {
            int records = 0;
            // Now go through and fetch results...
            // FIXME: This should be done through callbacks.
            Vector3[] field1_filter = new Vector3[result.Length];
            float[] field2_filter = new float[result.Length];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    int id;
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        id = reader.GetSqlInt32(0).Value;
                        if (result[id].Equals(null))
                        {
                            result[id] = new Vector3(reader.GetSqlSingle(1).Value,
                                reader.GetSqlSingle(2).Value,
                                reader.GetSqlSingle(3).Value);
                            field1_filter[id] = new Vector3(reader.GetSqlSingle(4).Value,
                                reader.GetSqlSingle(5).Value,
                                reader.GetSqlSingle(6).Value);
                            field2_filter[id] = reader.GetSqlSingle(7).Value;
                            records++;
                        }
                        else
                        {
                            result[id].x += reader.GetSqlSingle(1).Value;
                            result[id].y += reader.GetSqlSingle(2).Value;
                            result[id].z += reader.GetSqlSingle(3).Value;
                            field1_filter[id].x += reader.GetSqlSingle(4).Value;
                            field1_filter[id].y += reader.GetSqlSingle(5).Value;
                            field1_filter[id].z += reader.GetSqlSingle(6).Value;
                            field2_filter[id] += reader.GetSqlSingle(7).Value;
                        }
                    }
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }
            }
            for (int i = 0; i < result.Length; i++)
            {
                result[i].x -= field1_filter[i].x * field2_filter[i];
                result[i].y -= field1_filter[i].y * field2_filter[i];
                result[i].z -= field1_filter[i].z * field2_filter[i];
            }
            return records;
        }

        private int GetTwoFieldsSGSResults(IAsyncResult[] asyncRes, float[] result)
        {
            int records = 0;
            // Now go through and fetch results...
            // FIXME: This should be done through callbacks.
            float[] field1_filter = new float[result.Length];
            float[] field2_filter = new float[result.Length];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                    int id;
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        id = reader.GetSqlInt32(0).Value;
                        if (result[id].Equals(null))
                        {
                            result[id] = reader.GetSqlSingle(1).Value;
                            field1_filter[id] = reader.GetSqlSingle(2).Value;
                            field2_filter[id] = reader.GetSqlSingle(3).Value;
                            records++;
                        }
                        else
                        {
                            result[id] += reader.GetSqlSingle(1).Value;
                            field1_filter[id] += reader.GetSqlSingle(2).Value;
                            field2_filter[id] += reader.GetSqlSingle(3).Value;
                        }
                    }
                    reader.Close();
                    connections[s].Close();
                    connections[s] = null;
                }
            }
            for (int i = 0; i < result.Length; i++)
            {
                result[i] -= field1_filter[i] * field2_filter[i];
            }
            return records;
        }

        private int GetThresholdResults(IAsyncResult[] asyncRes, List<ThresholdInfo> result)
        {
            int records = 0;
            // Now go through and fetch results...
            // FIXME: This should be done through callbacks.
            bool error_flag = false;
            bool exception_flag = false;
            string exception_text = "";
            
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    try
                    {
                        SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                        if (!error_flag)
                        {
                            ThresholdInfo result_point;
                            Morton3D zindex;

                            while (reader.Read() && !reader.IsDBNull(0))
                            {
                                zindex = new Morton3D(reader.GetSqlInt64(0).Value);
                                result_point = new ThresholdInfo(zindex.X, zindex.Y, zindex.Z, (float)reader.GetDouble(1));
                                result.Add(result_point);
                                records++;
                                if (result.Count > MAX_NUMBER_THRESHOLD_POINTS)
                                {
                                    error_flag = true;
                                    break;
                                }
                            }
                        }
                        reader.Close();
                        reader.Dispose();
                        connections[s].Close();
                        connections[s] = null;
                    }
                    catch (Exception ex)
                    {
                        if (!exception_flag)
                        {
                            exception_flag = true;
                            exception_text += ex.Message;
                        }
                    }
                }
            }
            if (error_flag)
            {
                throw new Exception(
                    String.Format("The number of points above the threshold exceeds the maximum number of points allowed: {0}",
                    MAX_NUMBER_THRESHOLD_POINTS));
            }
            if (exception_flag)
            {
                throw new Exception(
                    String.Format("Error getting thresholding results! INNER EXCEPTION: {0}", exception_text));
            }

            return records;
        }

        private IAsyncResult[] ExecuteTurbulenceWorker(string dataset,
            Worker.Workers worker,
            float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            int arg
            )
        {
            // initiate reader requests
            IAsyncResult[] asyncRes = new IAsyncResult[serverCount];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    sqlcmds[s] = connections[s].CreateCommand();
                    //sqlcmds[s].CommandText = "EXEC [dbo].[ExecuteTurbulenceWorker] @database, @dataset, @workerType, @time, "
                    //                         + " @spatialInterp, @temporalInterp, @arg, @tempTable";
                    sqlcmds[s].CommandText = String.Format("EXEC [{0}].[dbo].[ExecuteTurbulenceWorker] @database, @dataset, @workerType, @time, "
                                             + " @spatialInterp, @temporalInterp, @arg, @tempTable",
                                             codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@database", databases[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dataset", dataset);
                    sqlcmds[s].Parameters.AddWithValue("@time", time);
                    sqlcmds[s].Parameters.AddWithValue("@workerType", (int)worker);
                    sqlcmds[s].Parameters.AddWithValue("@spatialInterp", (int)spatial);
                    sqlcmds[s].Parameters.AddWithValue("@temporalInterp", (int)temporal);
                    sqlcmds[s].Parameters.AddWithValue("@arg", arg);
                    //sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableNames[s]);
                    sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableName);
                    sqlcmds[s].CommandTimeout = 3600;
                    asyncRes[s] = sqlcmds[s].BeginExecuteReader(null, sqlcmds[s]);
                }
            }

            return asyncRes;
        }

        private IAsyncResult[] ExecuteMHDWorker(string tableName,
            int worker,
            float time,
            int spatial,
            int temporal,
            float arg
            )
        {
            // initiate reader requests
            IAsyncResult[] asyncRes = new IAsyncResult[serverCount];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null && datatables[s].Rows.Count > 0)
                {
                    sqlcmds[s] = connections[s].CreateCommand();
                    sqlcmds[s].CommandText = String.Format("EXEC [{0}].[dbo].[ExecuteMHDWorker] @serverName, @dbname, @codedb, @dataset, "
                                            + " @workerType, @blobDim, @time, "
                                            + " @spatialInterp, @temporalInterp, @arg, @inputSize, @tempTable",
                                            codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@serverName", servers[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dbname", databases[s]);
                    sqlcmds[s].Parameters.AddWithValue("@codedb", codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dataset", tableName);
                    sqlcmds[s].Parameters.AddWithValue("@workerType", worker);
                    sqlcmds[s].Parameters.AddWithValue("@blobDim", atomDim);
                    sqlcmds[s].Parameters.AddWithValue("@time", time);
                    sqlcmds[s].Parameters.AddWithValue("@spatialInterp", spatial);
                    sqlcmds[s].Parameters.AddWithValue("@temporalInterp", temporal);
                    sqlcmds[s].Parameters.AddWithValue("@arg", arg);
                    sqlcmds[s].Parameters.AddWithValue("@inputSize", count[s]);
                    //sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableNames[s]);
                    sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableName);
                    sqlcmds[s].CommandTimeout = 3600;
                    asyncRes[s] = sqlcmds[s].BeginExecuteReader(null, sqlcmds[s]);
                }
            }

            return asyncRes;
        }

        private IAsyncResult[] ExecuteTwoFieldsWorker(string tableName1, string tableName2,
            int worker,
            float time,
            int spatial,
            int temporal,
            float arg
            )
        {
            // initiate reader requests
            IAsyncResult[] asyncRes = new IAsyncResult[serverCount];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null && datatables[s].Rows.Count > 0)
                {
                    sqlcmds[s] = connections[s].CreateCommand();
                    sqlcmds[s].CommandText = String.Format("EXEC [{0}].[dbo].[ExecuteTwoFieldsWorker] @serverName, @dbname, @codedb, @field1, @field2, "
                                            + " @workerType, @blobDim, @time, "
                                            + " @spatialInterp, @temporalInterp, @arg, @inputSize, @tempTable",
                                            codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@serverName", servers[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dbname", databases[s]);
                    sqlcmds[s].Parameters.AddWithValue("@codedb", codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@field1", tableName1);
                    sqlcmds[s].Parameters.AddWithValue("@field2", tableName2);
                    sqlcmds[s].Parameters.AddWithValue("@workerType", worker);
                    sqlcmds[s].Parameters.AddWithValue("@blobDim", atomDim);
                    sqlcmds[s].Parameters.AddWithValue("@time", time);
                    sqlcmds[s].Parameters.AddWithValue("@spatialInterp", spatial);
                    sqlcmds[s].Parameters.AddWithValue("@temporalInterp", temporal);
                    sqlcmds[s].Parameters.AddWithValue("@arg", arg);
                    sqlcmds[s].Parameters.AddWithValue("@inputSize", count[s]);
                    //sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableNames[s]);
                    sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableName);
                    sqlcmds[s].CommandTimeout = 3600;
                    asyncRes[s] = sqlcmds[s].BeginExecuteReader(null, sqlcmds[s]);
                }
            }

            return asyncRes;
        }

        // customize for batching
        private IAsyncResult[] ExecuteMHDWorkerBatch(string tableName,
            Worker.Workers worker,
            float time,
            string boundary,
            int arg
            )
        {
            // initiate reader requests
            IAsyncResult[] asyncRes = new IAsyncResult[serverCount];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    sqlcmds[s] = connections[s].CreateCommand();
                    sqlcmds[s].CommandText = String.Format("EXEC [{0}].[dbo].[ExecuteMHDWorkerBatch] @serverName, @dbname, @codedb, @dataset, "
                                            + " @workerType, @blobDim, @time, "
                                            + " @boundary, @arg, @tempTable",
                                            codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@serverName", servers[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dbname", databases[s]);
                    sqlcmds[s].Parameters.AddWithValue("@codedb", codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dataset", tableName);
                    sqlcmds[s].Parameters.AddWithValue("@workerType", (int)worker);
                    sqlcmds[s].Parameters.AddWithValue("@blobDim", atomDim);
                    sqlcmds[s].Parameters.AddWithValue("@time", time);
                    sqlcmds[s].Parameters.AddWithValue("@boundary", boundary);
                    sqlcmds[s].Parameters.AddWithValue("@arg", arg);
                    //sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableNames[s]);
                    sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableName);
                    sqlcmds[s].CommandTimeout = 3600;
                    asyncRes[s] = sqlcmds[s].BeginExecuteReader(null, sqlcmds[s]);
                }
            }

            return asyncRes;
        }

        private IAsyncResult[] ExecuteBoxFilterWorker(string tableName,
            int worker,
            float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            float arg
            )
        {
            // initiate reader requests
            IAsyncResult[] asyncRes = new IAsyncResult[serverCount];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    sqlcmds[s] = connections[s].CreateCommand();
                    sqlcmds[s].CommandText = String.Format("EXEC [{0}].[dbo].[ExecuteBoxFilterWorker] @serverName, @dbname, @codedb, @dataset, "
                                            + " @workerType, @blobDim, @time, "
                                            + " @spatialInterp, @temporalInterp, @arg, @inputSize, @tempTable",
                                            codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@serverName", servers[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dbname", databases[s]);
                    sqlcmds[s].Parameters.AddWithValue("@codedb", codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dataset", tableName);
                    sqlcmds[s].Parameters.AddWithValue("@workerType", worker);
                    sqlcmds[s].Parameters.AddWithValue("@blobDim", atomDim);
                    sqlcmds[s].Parameters.AddWithValue("@time", time);
                    sqlcmds[s].Parameters.AddWithValue("@spatialInterp", (int)spatial);
                    sqlcmds[s].Parameters.AddWithValue("@temporalInterp", (int)temporal);
                    sqlcmds[s].Parameters.AddWithValue("@arg", arg);
                    sqlcmds[s].Parameters.AddWithValue("@inputSize", count[s]);
                    //sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableNames[s]);
                    sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableName);
                    sqlcmds[s].CommandTimeout = 3600;
                    asyncRes[s] = sqlcmds[s].BeginExecuteReader(null, sqlcmds[s]);
                }
            }

            return asyncRes;
        }

        private IAsyncResult[] ExecuteBoxFilterWorker(string tableName1, string tableName2,
            int worker,
            float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            float arg
            )
        {
            // initiate reader requests
            IAsyncResult[] asyncRes = new IAsyncResult[serverCount];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    sqlcmds[s] = connections[s].CreateCommand();
                    sqlcmds[s].CommandText = String.Format("EXEC [{0}].[dbo].[ExecuteTwoFieldsBoxFilterWorker] @serverName, @dbname, @codedb, @field1, @field2 "
                                            + " @workerType, @blobDim, @time, "
                                            + " @spatialInterp, @temporalInterp, @arg, @inputSize, @tempTable",
                                            codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@serverName", servers[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dbname", databases[s]);
                    sqlcmds[s].Parameters.AddWithValue("@codedb", codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@field1", tableName1);
                    sqlcmds[s].Parameters.AddWithValue("@field2", tableName2);
                    sqlcmds[s].Parameters.AddWithValue("@workerType", worker);
                    sqlcmds[s].Parameters.AddWithValue("@blobDim", atomDim);
                    sqlcmds[s].Parameters.AddWithValue("@time", time);
                    sqlcmds[s].Parameters.AddWithValue("@spatialInterp", (int)spatial);
                    sqlcmds[s].Parameters.AddWithValue("@temporalInterp", (int)temporal);
                    sqlcmds[s].Parameters.AddWithValue("@arg", arg);
                    sqlcmds[s].Parameters.AddWithValue("@inputSize", count[s]);
                    //sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableNames[s]);
                    sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableName);
                    sqlcmds[s].CommandTimeout = 3600;
                    asyncRes[s] = sqlcmds[s].BeginExecuteReader(null, sqlcmds[s]);
                }
            }

            return asyncRes;
        }

        private IAsyncResult[] ExecuteGetRawData(string dataset,
            int timestep,
            int[] serverX, int[] serverY, int[] serverZ, int[] serverXwidth, int[] serverYwidth, int[] serverZwidth)
        {
            // initiate reader requests
            IAsyncResult[] asyncRes = new IAsyncResult[serverCount];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    string queryBox = String.Format("box[{0},{1},{2},{3},{4},{5}]", serverX[s], serverY[s], serverZ[s],
                        serverX[s] + serverXwidth[s], serverY[s] + serverYwidth[s], serverZ[s] + serverZwidth[s]);
                    sqlcmds[s] = connections[s].CreateCommand();
                    sqlcmds[s].CommandText = String.Format("EXEC [{0}].[dbo].[GetDataCutout] @serverName, @database, @codedb, "
                                            + "@dataset, @blobDim, @timestep, @queryBox ",
                                            codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@serverName", servers[s]);
                    sqlcmds[s].Parameters.AddWithValue("@database", databases[s]);
                    sqlcmds[s].Parameters.AddWithValue("@codedb", codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dataset", dataset);
                    sqlcmds[s].Parameters.AddWithValue("@blobDim", atomDim);
                    sqlcmds[s].Parameters.AddWithValue("@timestep", timestep);
                    sqlcmds[s].Parameters.AddWithValue("@queryBox", queryBox);
                    sqlcmds[s].CommandTimeout = 3600;
                    asyncRes[s] = sqlcmds[s].BeginExecuteReader(null, sqlcmds[s]);
                }
            }

            return asyncRes;
        }

        private IAsyncResult[] ExecuteGetFilteredCutout(DataInfo.DataSets dataset_enum, string field,
            int timestep, int filter_width, int x_stride, int y_stride, int z_stride,
            int[] serverX, int[] serverY, int[] serverZ, int[] serverXwidth, int[] serverYwidth, int[] serverZwidth)
        {
            // initiate reader requests
            IAsyncResult[] asyncRes = new IAsyncResult[serverCount];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    string queryBox = String.Format("box[{0},{1},{2},{3},{4},{5}]", serverX[s], serverY[s], serverZ[s],
                        serverX[s] + serverXwidth[s], serverY[s] + serverYwidth[s], serverZ[s] + serverZwidth[s]);
                    sqlcmds[s] = connections[s].CreateCommand();
                    sqlcmds[s].CommandText = String.Format("EXEC [{0}].[dbo].[GetFilteredCutout] @serverName, @dbname, @codedb, "
                                            + "@turbinfodb, @datasetID, @field, @blobDim, @timestep, @filter_width, @x_stride, @y_stride, @z_stride, @QueryBox ",
                                            codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@serverName", servers[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dbname", databases[s]);
                    sqlcmds[s].Parameters.AddWithValue("@codedb", codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@turbinfodb", infodb);
                    sqlcmds[s].Parameters.AddWithValue("@datasetID", dataset_enum);
                    sqlcmds[s].Parameters.AddWithValue("@field", field);
                    sqlcmds[s].Parameters.AddWithValue("@blobDim", atomDim);
                    sqlcmds[s].Parameters.AddWithValue("@timestep", timestep);
                    sqlcmds[s].Parameters.AddWithValue("@filter_width", filter_width);
                    sqlcmds[s].Parameters.AddWithValue("@x_stride", x_stride);
                    sqlcmds[s].Parameters.AddWithValue("@y_stride", y_stride);
                    sqlcmds[s].Parameters.AddWithValue("@z_stride", z_stride);
                    sqlcmds[s].Parameters.AddWithValue("@QueryBox", queryBox);
                    sqlcmds[s].CommandTimeout = 3600;
                    asyncRes[s] = sqlcmds[s].BeginExecuteReader(null, sqlcmds[s]);
                }
            }

            return asyncRes;
        }

        private IAsyncResult[] ExecuteGetStridedDataCutout(DataInfo.DataSets dataset_enum, string field,
            int timestep, int x_stride, int y_stride, int z_stride,
            int[] serverX, int[] serverY, int[] serverZ, int[] serverXwidth, int[] serverYwidth, int[] serverZwidth)
        {
            // initiate reader requests
            IAsyncResult[] asyncRes = new IAsyncResult[serverCount];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    string queryBox = String.Format("box[{0},{1},{2},{3},{4},{5}]", serverX[s], serverY[s], serverZ[s],
                        serverX[s] + serverXwidth[s], serverY[s] + serverYwidth[s], serverZ[s] + serverZwidth[s]);
                    sqlcmds[s] = connections[s].CreateCommand();
                    sqlcmds[s].CommandText = String.Format("EXEC [{0}].[dbo].[GetStridedDataCutout] @serverName, @dbname, @codedb, "
                                            + "@turbinfodb, @datasetID, @field, @blobDim, @timestep, @x_stride, @y_stride, @z_stride, @QueryBox ",
                                            codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@serverName", servers[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dbname", databases[s]);
                    sqlcmds[s].Parameters.AddWithValue("@codedb", codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@turbinfodb", infodb);
                    sqlcmds[s].Parameters.AddWithValue("@datasetID", dataset_enum);
                    sqlcmds[s].Parameters.AddWithValue("@field", field);
                    sqlcmds[s].Parameters.AddWithValue("@blobDim", atomDim);
                    sqlcmds[s].Parameters.AddWithValue("@timestep", timestep);
                    sqlcmds[s].Parameters.AddWithValue("@x_stride", x_stride);
                    sqlcmds[s].Parameters.AddWithValue("@y_stride", y_stride);
                    sqlcmds[s].Parameters.AddWithValue("@z_stride", z_stride);
                    sqlcmds[s].Parameters.AddWithValue("@QueryBox", queryBox);
                    sqlcmds[s].CommandTimeout = 3600;
                    asyncRes[s] = sqlcmds[s].BeginExecuteReader(null, sqlcmds[s]);
                }
            }

            return asyncRes;
        }

        private IAsyncResult[] ExecuteGetThreshold(DataInfo.DataSets dataset_enum, string tableName, int workerType, 
            int timestep, int spatialInterp, double threshold, 
            int[] serverX, int[] serverY, int[] serverZ, int[] serverXwidth, int[] serverYwidth, int[] serverZwidth)
        {
            // initiate reader requests
            IAsyncResult[] asyncRes = new IAsyncResult[serverCount];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    string queryBox = String.Format("box[{0},{1},{2},{3},{4},{5}]", serverX[s], serverY[s], serverZ[s],
                        serverX[s] + serverXwidth[s], serverY[s] + serverYwidth[s], serverZ[s] + serverZwidth[s]);
                    sqlcmds[s] = connections[s].CreateCommand();
                    sqlcmds[s].CommandText = String.Format("EXEC [{0}].[dbo].[GetThreshold] @datasetID, @serverName, @dbname, @codedb, " +
                        "@cachedb, @turbinfodb, @tableName, @workerType, @blobDim, @timestep, " +
                        "@spatialInterp, @arg, @threshold, @QueryBox ",
                        codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@datasetID", dataset_enum);
                    sqlcmds[s].Parameters.AddWithValue("@serverName", servers[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dbname", databases[s]);
                    sqlcmds[s].Parameters.AddWithValue("@codedb", codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@cachedb", "cachedb");
                    sqlcmds[s].Parameters.AddWithValue("@turbinfodb", infodb);
                    sqlcmds[s].Parameters.AddWithValue("@tableName", tableName);
                    sqlcmds[s].Parameters.AddWithValue("@workerType", workerType);
                    sqlcmds[s].Parameters.AddWithValue("@blobDim", atomDim);
                    sqlcmds[s].Parameters.AddWithValue("@timestep", timestep);
                    sqlcmds[s].Parameters.AddWithValue("@spatialInterp", spatialInterp);
                    sqlcmds[s].Parameters.AddWithValue("@arg", 0);
                    sqlcmds[s].Parameters.AddWithValue("@threshold", threshold);
                    sqlcmds[s].Parameters.AddWithValue("@QueryBox", queryBox);
                    sqlcmds[s].CommandTimeout = 600;
                    asyncRes[s] = sqlcmds[s].BeginExecuteReader(null, sqlcmds[s]);
                }
            }

            return asyncRes;
        }

        public int ExecuteGetVelocity(string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            Vector3[] result)
        {

            IAsyncResult[] asyncRes = ExecuteTurbulenceWorker(dataset,
                Worker.Workers.GetVelocity, time, spatial, temporal, 0);

            return GetXYZResults(asyncRes, result);
        }

        public int ExecuteGetMHDData(DataInfo.TableNames tableName, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            Vector3[] result)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteMHDWorker(tableName.ToString(),
                        worker, time, (int)spatial, (int)temporal, 0);
            return GetXYZResults(asyncRes, result);
        }

        public int ExecuteGetMHDData(DataInfo.TableNames tableName, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            Vector3[] result, float arg)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteMHDWorker(tableName.ToString(),
                        worker, time, (int)spatial, (int)temporal, arg);
            return GetXYZResults(asyncRes, result);
        }

        public int ExecuteGetMHDData(DataInfo.TableNames tableName, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            SGSTensor[] result, float arg)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteMHDWorker(tableName.ToString(),
                        worker, time, (int)spatial, (int)temporal, arg);
            return GetSGSResults(asyncRes, result);
        }

        public int ExecuteGetMHDData(DataInfo.TableNames tableName1, DataInfo.TableNames tableName2, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            VelocityGradient[] result, float arg)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteTwoFieldsWorker(tableName1.ToString(), tableName2.ToString(),
                        worker, time, (int)spatial, (int)temporal, arg);
            return GetTwoFieldsSGSResults(asyncRes, result);
        }

        public int ExecuteGetMHDData(DataInfo.TableNames tableName1, DataInfo.TableNames tableName2, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            Vector3[] result, float arg)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteTwoFieldsWorker(tableName1.ToString(), tableName2.ToString(),
                        worker, time, (int)spatial, (int)temporal, arg);
            return GetTwoFieldsSGSResults(asyncRes, result);
        }

        public int ExecuteGetMHDData(DataInfo.TableNames tableName1, DataInfo.TableNames tableName2, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            float[] result, float arg)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteTwoFieldsWorker(tableName1.ToString(), tableName2.ToString(),
                        worker, time, (int)spatial, (int)temporal, arg);
            return GetTwoFieldsSGSResults(asyncRes, result);
        }

        public int ExecuteGetBoxFilter(DataInfo.TableNames tableName, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            Vector3[] result, float arg)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteBoxFilterWorker(tableName.ToString(),
                        worker, time, spatial, temporal, arg);
            int ret = 0;
            if (tableName == DataInfo.TableNames.pr || tableName == DataInfo.TableNames.pressure08 || tableName == DataInfo.TableNames.isotropic1024fine_pr)
                ret = GetPressureResults(asyncRes, result);
            else
                ret = GetXYZResults(asyncRes, result);
            return ret;
        }

        public int ExecuteGetBoxFilter(DataInfo.TableNames tableName, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            SGSTensor[] result, float arg)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteBoxFilterWorker(tableName.ToString(),
                        worker, time, spatial, temporal, arg);
            return GetSGSResults(asyncRes, result);
        }

        public int ExecuteGetBoxFilter(DataInfo.TableNames tableName1, DataInfo.TableNames tableName2, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            VelocityGradient[] result, float arg)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteBoxFilterWorker(tableName1.ToString(), tableName2.ToString(),
                        worker, time, spatial, temporal, arg);
            return GetTwoFieldsSGSResults(asyncRes, result);
        }

        public int ExecuteGetBoxFilter(DataInfo.TableNames tableName1, DataInfo.TableNames tableName2, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            Vector3[] result, float arg)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteBoxFilterWorker(tableName1.ToString(), tableName2.ToString(),
                        worker, time, spatial, temporal, arg);
            return GetTwoFieldsSGSResults(asyncRes, result);
        }

        public int ExecuteGetBoxFilter(DataInfo.TableNames tableName1, DataInfo.TableNames tableName2, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            float[] result, float arg)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteBoxFilterWorker(tableName1.ToString(), tableName2.ToString(),
                        worker, time, spatial, temporal, arg);
            return GetTwoFieldsSGSResults(asyncRes, result);
        }

        // customized for batching
        public int ExecuteGetMHDDataBatch(DataInfo.TableNames tableName, float time,
            string boundary, Vector3[] result)
        {
            IAsyncResult[] asyncRes;
            switch (tableName)
            {
                case DataInfo.TableNames.velocity08:
                    asyncRes = ExecuteMHDWorkerBatch(tableName.ToString(),
                        Worker.Workers.GetMHDVelocity, time, boundary, 0);
                    break;
                default:
                    return -1;
            }
            return GetXYZResults(asyncRes, result);
        }

        public int ExecuteGetMHDData(DataInfo.TableNames tableName, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            Pressure[] result)
        {
            IAsyncResult[] asyncRes = ExecuteMHDWorker(tableName.ToString(),
                        worker, time, (int)spatial, (int)temporal, 0);
            return GetPressureResults(asyncRes, result);
        }

        public int ExecuteGetPressure(string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            Pressure[] result)
        {

            IAsyncResult[] asyncRes = ExecuteTurbulenceWorker(dataset,
                Worker.Workers.GetPressure, time, spatial, temporal, 0);

            return GetPressureResults(asyncRes, result);
        }

        public int ExecuteGetVelocityAndPressure(string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            Vector3P[] result)
        {

            IAsyncResult[] asyncRes = ExecuteTurbulenceWorker(dataset,
                Worker.Workers.GetVelocityWithPressure, time, spatial, temporal, 0);

            return GetXYZPResults(asyncRes, result);
        }

        public int ExecutePressureHessian(string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            PressureHessian[] result)
        {

            IAsyncResult[] asyncRes = ExecuteTurbulenceWorker(dataset,
                Worker.Workers.GetPressureHessian, time, spatial, temporal, 0);

            return GetPressureHessianResults(asyncRes, result);
        }

        public int ExecuteVelocityHessian(string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            VelocityHessian[] result)
        {

            IAsyncResult[] asyncRes = ExecuteTurbulenceWorker(dataset,
                Worker.Workers.GetVelocityHessian, time, spatial, temporal, 0);

            return GetVelocityHessianResults(asyncRes, result);

        }

        public int ExecuteVelocityLaplacian(string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            Vector3[] result)
        {

            IAsyncResult[] asyncRes = ExecuteTurbulenceWorker(dataset,
                Worker.Workers.GetVelocityLaplacian, time, spatial, temporal, 0);

            return GetVelocityLaplacianResults(asyncRes, result);
        }

        public int ExecuteGetVelocityGradient(string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            VelocityGradient[] result)
        {

            IAsyncResult[] asyncRes = ExecuteTurbulenceWorker(dataset,
                Worker.Workers.GetVelocityGradient, time, spatial, temporal, 0);

            return GetVelocityGradientResults(asyncRes, result);
        }

        /// <remarks>
        /// Also used for LaplacianOfVelocityGradient
        /// </remarks>
        public int ExecuteGetMHDGradient(DataInfo.TableNames tableName, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            VelocityGradient[] result)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteMHDWorker(tableName.ToString(),
                        worker, time, (int)spatial, (int)temporal, 0);
            return GetVelocityGradientResults(asyncRes, result);
        }

        public int ExecuteGetMHDFilterGradient(DataInfo.TableNames tableName, int worker, float time,
            int spatial, float filter_width,
            VelocityGradient[] result)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteMHDWorker(tableName.ToString(),
                        worker, time, spatial, 0, filter_width);
            return GetVelocityGradientResults(asyncRes, result);
        }

        public int ExecuteGetPressureGradient(string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            Vector3[] result)
        {
            IAsyncResult[] asyncRes = ExecuteTurbulenceWorker(dataset,
                Worker.Workers.GetPressureGradient, time, spatial, temporal, 0);

            return GetXYZResults(asyncRes, result);
        }

        public int ExecuteGetMHDLaplacian(DataInfo.TableNames tableName, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            Vector3[] result)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteMHDWorker(tableName.ToString(),
                        worker, time, (int)spatial, (int)temporal, 0);
            return GetVelocityLaplacianResults(asyncRes, result);
        }

        public int ExecuteGetMHDHessian(DataInfo.TableNames tableName, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            VelocityHessian[] result)
        {
            IAsyncResult[] asyncRes;
            asyncRes = ExecuteMHDWorker(tableName.ToString(),
                        worker, time, (int)spatial, (int)temporal, 0);
            return GetVelocityHessianResults(asyncRes, result);
        }

        public int ExecuteGetMHDPressureHessian(DataInfo.TableNames tableName, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            PressureHessian[] result)
        {
            IAsyncResult[] asyncRes = ExecuteMHDWorker(tableName.ToString(),
                        worker, time, (int)spatial, (int)temporal, 0);
            return GetPressureHessianResults(asyncRes, result);
        }

        public int ExecuteGetThreshold(DataInfo.DataSets dataset_enum,
            DataInfo.TableNames tableName, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            double threshold,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth,
            List<ThresholdInfo> result)
        {
            int[] serverX = new int[serverCount];
            int[] serverY = new int[serverCount];
            int[] serverZ = new int[serverCount];
            int[] serverXwidth = new int[serverCount];
            int[] serverYwidth = new int[serverCount];
            int[] serverZwidth = new int[serverCount];

            float t = time / Dt;
            int timestep = (int)Math.Round(t / timeInc) * timeInc + timeOff;

            //if (channel_grid)
            //{
            //    X = (int)Math.Round(X - 0.45 * time / dx);
            //    X = ((X % GridResolutionX) + GridResolutionX) % GridResolutionX;
            //}

            GetServerParameters4RawData(X, Y, Z, Xwidth, Ywidth, Zwidth, serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth);

            IAsyncResult[] asyncRes = ExecuteGetThreshold(dataset_enum, tableName.ToString(), worker, timestep, (int)spatial, threshold, 
                serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth);
            return GetThresholdResults(asyncRes, result);
        }

        public int ExecuteGetPDF(DataInfo.DataSets dataset_enum,
            DataInfo.TableNames tableName, int worker, float time,
            TurbulenceOptions.SpatialInterpolation spatial,
            int binSize, int numberOfBins,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth,
            int[] result, int arg)
        {
            int[] serverX = new int[serverCount];
            int[] serverY = new int[serverCount];
            int[] serverZ = new int[serverCount];
            int[] serverXwidth = new int[serverCount];
            int[] serverYwidth = new int[serverCount];
            int[] serverZwidth = new int[serverCount];

            float t = time / Dt;
            int timestep = (int)Math.Round(t / timeInc) * timeInc + timeOff;

            GetServerParameters4RawData(X, Y, Z, Xwidth, Ywidth, Zwidth, serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth);

            IAsyncResult[] asyncRes = ExecuteGetPDF(dataset_enum, tableName.ToString(), worker, timestep, (int)spatial, binSize, numberOfBins,
                serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth, arg);
            return GetPDFResults(asyncRes, result);
        }

        private IAsyncResult[] ExecuteGetPDF(DataInfo.DataSets dataset_enum, string tableName, int workerType,
            int timestep, int spatialInterp, int binSize, int numberOfBins,
            int[] serverX, int[] serverY, int[] serverZ, int[] serverXwidth, int[] serverYwidth, int[] serverZwidth,
            int arg)
        {
            // initiate reader requests
            IAsyncResult[] asyncRes = new IAsyncResult[serverCount];
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    string queryBox = String.Format("box[{0},{1},{2},{3},{4},{5}]", serverX[s], serverY[s], serverZ[s],
                        serverX[s] + serverXwidth[s], serverY[s] + serverYwidth[s], serverZ[s] + serverZwidth[s]);
                    sqlcmds[s] = connections[s].CreateCommand();
                    sqlcmds[s].CommandText = String.Format("EXEC [{0}].[dbo].[GetPDF] @datasetID, @serverName, @dbname, @codedb, " +
                        "@cachedb, @turbinfodb, @tableName, @workerType, @blobDim, @timestep, " +
                        "@spatialInterp, @arg, @binSize, @numberOfBins, @QueryBox ",
                        codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@datasetID", dataset_enum);
                    sqlcmds[s].Parameters.AddWithValue("@serverName", servers[s]);
                    sqlcmds[s].Parameters.AddWithValue("@dbname", databases[s]);
                    sqlcmds[s].Parameters.AddWithValue("@codedb", codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@cachedb", "cachedb");
                    sqlcmds[s].Parameters.AddWithValue("@turbinfodb", infodb);
                    sqlcmds[s].Parameters.AddWithValue("@tableName", tableName);
                    sqlcmds[s].Parameters.AddWithValue("@workerType", workerType);
                    sqlcmds[s].Parameters.AddWithValue("@blobDim", atomDim);
                    sqlcmds[s].Parameters.AddWithValue("@timestep", timestep);
                    sqlcmds[s].Parameters.AddWithValue("@spatialInterp", spatialInterp);
                    sqlcmds[s].Parameters.AddWithValue("@arg", arg);
                    sqlcmds[s].Parameters.AddWithValue("@binSize", binSize);
                    sqlcmds[s].Parameters.AddWithValue("@numberOfBins", numberOfBins);
                    sqlcmds[s].Parameters.AddWithValue("@QueryBox", queryBox);
                    sqlcmds[s].CommandTimeout = 600;
                    asyncRes[s] = sqlcmds[s].BeginExecuteReader(null, sqlcmds[s]);
                }
            }

            return asyncRes;
        }

        private int GetPDFResults(IAsyncResult[] asyncRes, int[] result)
        {
            int records = 0;
            // Now go through and fetch results...
            // FIXME: This should be done through callbacks.
            bool exception_flag = false;
            string exception_text = "";
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    try
                    {
                        SqlDataReader reader = sqlcmds[s].EndExecuteReader(asyncRes[s]);
                        int bin;
                        int count;
                        while (reader.Read() && !reader.IsDBNull(0))
                        {
                            bin = reader.GetSqlInt32(0).Value;
                            count = reader.GetSqlInt32(1).Value;
                            result[bin] += count;
                            records++;
                        }
                        reader.Close();
                        reader.Dispose();
                        connections[s].Close();
                        connections[s] = null;
                    }
                    catch (Exception ex)
                    {
                        if (!exception_flag)
                        {
                            exception_flag = true;
                            exception_text += ex.Message;
                        }
                    }
                }
            }
            if (exception_flag)
            {
                throw new Exception(
                    String.Format("Error getting thresholding results! INNER EXCEPTION: {0}", exception_text));
            }
            return records;
        }

        public int ExecuteGetPosition(short dataset,
            TurbulenceOptions.SpatialInterpolation spatial,
            TurbulenceOptions.TemporalInterpolation temporal,
            string tableName,
            float time,
            float endTime,
            float dt,
            Point3[] points)
        {
            int numberOfCallbacksNotYetCompleted = 0;
            int number_of_crossings = 0;
            ManualResetEvent doneEvent = new ManualResetEvent(false);
            Exception exception = null;

            string executeStr = "ExecuteParticleTrackingWorkerTaskParallel"; //PJ 2015: call regular or channel flow worker
            if ((DataInfo.DataSets)dataset == DataInfo.DataSets.channel)
            {
                executeStr = "ExecuteParticleTrackingChannelWorkerTaskParallel";
            }

            string turbinfo_connectionString = ConfigurationManager.ConnectionStrings[infodb].ConnectionString;
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(turbinfo_connectionString);
            string turbinfoServer = builder.DataSource;

            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null && datatables[s].Rows.Count > 0)
                {
                    sqlcmds[s] = connections[s].CreateCommand();

                    sqlcmds[s].CommandText = String.Format("EXEC [{0}].[dbo].["
                                                + executeStr + 
                                                "] @turbinfoServer, @turbinfoDB, @localServer, @localDatabase, @datasetID, "
                                                + "@tableName, @atomDim, @workerType, "
                                                + " @spatialInterp, @temporalInterp, @inputSize, @tempTable, @time, @endTime, @dt, @development", codeDatabase[s]);
                    sqlcmds[s].Parameters.AddWithValue("@turbinfoServer", turbinfoServer);
                    sqlcmds[s].Parameters.AddWithValue("@turbinfoDB", "turbinfo");
                    sqlcmds[s].Parameters.AddWithValue("@localServer", servers[s]);
                    sqlcmds[s].Parameters.AddWithValue("@localDatabase", databases[s]);
                    sqlcmds[s].Parameters.AddWithValue("@datasetID", dataset);
                    sqlcmds[s].Parameters.AddWithValue("@tableName", tableName);
                    sqlcmds[s].Parameters.AddWithValue("@atomDim", atomDim);
                    sqlcmds[s].Parameters.AddWithValue("@workerType", (int)Worker.Workers.GetPositionDBEvaluation);
                    sqlcmds[s].Parameters.AddWithValue("@spatialInterp", (int)spatial);
                    sqlcmds[s].Parameters.AddWithValue("@temporalInterp", (int)temporal);
                    sqlcmds[s].Parameters.AddWithValue("@inputSize", count[s]);
                    sqlcmds[s].Parameters.AddWithValue("@tempTable", tempTableName);
                    sqlcmds[s].Parameters.AddWithValue("@time", time);
                    sqlcmds[s].Parameters.AddWithValue("@endTime", endTime);
                    sqlcmds[s].Parameters.AddWithValue("@dt", dt);
                    sqlcmds[s].Parameters.AddWithValue("@development", development);
                    sqlcmds[s].CommandTimeout = 36000;
                    Interlocked.Increment(ref numberOfCallbacksNotYetCompleted);
                    AsyncCallback callback = new AsyncCallback(result =>
                    {
                        HandleCallback(result, points, ref numberOfCallbacksNotYetCompleted, ref number_of_crossings, doneEvent, ref exception);
                    });
                    sqlcmds[s].BeginExecuteReader(callback, new Tuple<int, SqlCommand>(s, sqlcmds[s]));
                }
            }

            doneEvent.WaitOne();

            if (exception != null)
                throw new Exception(exception.Message, exception.InnerException);
            return 0;
        }

        public void HandleCallback(IAsyncResult asyncRes, Point3[] points,
            ref int numberOfCallbacksNotYetCompleted,
            ref int number_of_crossings, ManualResetEvent doneEvent, ref Exception exception)
        {
            Tuple<int, SqlCommand> state = (Tuple<int, SqlCommand>)asyncRes.AsyncState;
            int server_index = state.Item1;
            lock (points)
            {
                try
                {
                    SqlDataReader reader = state.Item2.EndExecuteReader(asyncRes);
                    int id;
                    while (reader.Read())
                    {
                        id = reader.GetSqlInt32(0).Value;

                        points[id] = new Point3(reader.GetSqlSingle(1).Value, reader.GetSqlSingle(2).Value, reader.GetSqlSingle(3).Value);
                    }

                    // Retrieve the number of crossings
                    reader.NextResult();
                    reader.Read();
                    int server_crossings = reader.GetSqlInt32(0).Value;
                    Interlocked.Add(ref number_of_crossings, server_crossings);

                    reader.Close();
                    datatables[server_index].Clear();
                    count[server_index] = 0;
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }
            if (Interlocked.Decrement(ref numberOfCallbacksNotYetCompleted) == 0)
                doneEvent.Set();
        }

        public byte[] GetRawData(DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, float time, int components,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth)
        {
            // we return a cube of data with the specified width
            // for each of the components of the vector or scalar field
            byte[] result = new byte[Xwidth * Ywidth * Zwidth * components * sizeof(float)];
            IAsyncResult[] asyncRes;

            selectServers(dataset_enum);

            //if (channel_grid)
            //{
            //    X = (int)Math.Round(X - 0.45 * time / dx);
            //    X = ((X % GridResolutionX) + GridResolutionX) % GridResolutionX;
            //}

            int[] serverX = new int[serverCount];
            int[] serverY = new int[serverCount];
            int[] serverZ = new int[serverCount];
            int[] serverXwidth = new int[serverCount];
            int[] serverYwidth = new int[serverCount];
            int[] serverZwidth = new int[serverCount];

            float t = time / Dt;
            int timestep = (int)Math.Round(t / timeInc) * timeInc + timeOff;

            GetServerParameters4RawData(X, Y, Z, Xwidth, Ywidth, Zwidth, serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth);

            //DateTime start = DateTime.Now;
            asyncRes = ExecuteGetRawData(tableName.ToString(),
                timestep, serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth);
            GetRawResults(asyncRes, result, components, X, Y, Z, Xwidth, Ywidth, Zwidth, serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth);

            //System.IO.StreamWriter time_log = new System.IO.StreamWriter(@"C:\Documents and Settings\kalin\My Documents\databaseTime.txt", true);
            //time_log.WriteLine(DateTime.Now - start);
            //time_log.Close();

            return result;
        }

        public byte[] GetFilteredData(DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, float time, int components,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, 
            int x_stride, int y_stride, int z_stride, int filter_width)
        {
            IAsyncResult[] asyncRes;

            selectServers(dataset_enum);

            //if (channel_grid)
            //{
            //    X = (int)Math.Round(X - 0.45 * time / dx);
            //    X = ((X % GridResolutionX) + GridResolutionX) % GridResolutionX;
            //}

            int[] serverX = new int[serverCount];
            int[] serverY = new int[serverCount];
            int[] serverZ = new int[serverCount];
            int[] serverXwidth = new int[serverCount];
            int[] serverYwidth = new int[serverCount];
            int[] serverZwidth = new int[serverCount];

            float t = time / Dt;
            int timestep = (int)Math.Round(t / timeInc) * timeInc + timeOff;

            // The width in each dimension should be 1 larger than a multiple of the step.
            // Make sure that that is the case:
            Xwidth = (Xwidth - 1) / x_stride * x_stride + 1;
            Ywidth = (Ywidth - 1) / y_stride * y_stride + 1;
            Zwidth = (Zwidth - 1) / z_stride * z_stride + 1;

            GetServerParameters4RawData(X, Y, Z, Xwidth, Ywidth, Zwidth, serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth, 
                x_stride, y_stride, z_stride);

            if (filter_width == 1)
            {
                //DateTime start = DateTime.Now;
                asyncRes = ExecuteGetStridedDataCutout(dataset_enum, tableName.ToString(),
                    timestep, x_stride, y_stride, z_stride, serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth);
            }
            else
            {
                if (filter_width % 2 == 0)
                {
                    filter_width = filter_width + 1;
                }
                asyncRes = ExecuteGetFilteredCutout(dataset_enum, tableName.ToString(),
                    timestep, filter_width, x_stride, y_stride, z_stride, serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth);
            }

            // The cutout returned from the databases will be a factor of "step" smaller.
            X = X / x_stride;
            Y = Y / y_stride;
            Z = Z / z_stride;
            Xwidth = (Xwidth - 1) / x_stride + 1;
            Ywidth = (Ywidth - 1) / y_stride + 1;
            Zwidth = (Zwidth - 1) / z_stride + 1;
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    serverX[s] = serverX[s] / x_stride;
                    serverY[s] = serverY[s] / y_stride;
                    serverZ[s] = serverZ[s] / z_stride;
                    serverXwidth[s] = (serverXwidth[s] - 1) / x_stride + 1;
                    serverYwidth[s] = (serverYwidth[s] - 1) / y_stride + 1;
                    serverZwidth[s] = (serverZwidth[s] - 1) / z_stride + 1;
                }
            }
            // we return a cube of data with the specified width
            // for each of the components of the vector or scalar field
            byte[] result = new byte[Xwidth * Ywidth * Zwidth * components * sizeof(float)];
            GetRawResults(asyncRes, result, components, X, Y, Z, Xwidth, Ywidth, Zwidth, serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth);

            //System.IO.StreamWriter time_log = new System.IO.StreamWriter(@"C:\Documents and Settings\kalin\My Documents\databaseTime.txt", true);
            //time_log.WriteLine(DateTime.Now - start);
            //time_log.Close();

            return result;
        }

        public void Close()
        {
            for (int s = 0; s < serverCount; s++)
            {
                if (connections[s] != null)
                {
                    if (tempTableNames != null)
                    {
                        if (tempTableNames[s] != null)
                        {
                            SqlCommand cmd = new SqlCommand(
                               String.Format(@"DROP TABLE {0}", tempTableNames[s]), connections[s]);
                            try
                            {
                                cmd.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                throw new Exception(String.Format("Error dropping input temporary table.  [Inner Exception: {0}])",
                                    e.ToString()));
                            }
                        }
                    }

                    connections[s].Close();
                    connections[s] = null;
                }
            }
        }

        ~Database()
        {
            this.Close();
        }
    }
}