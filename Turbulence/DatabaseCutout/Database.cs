using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using Turbulence.SQLInterface;
using System.Threading;


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
    public string infodb;
    public string infodb_server;
    protected int[] gridResolution; // Dimensions of the entire grid given az [z,y,x]
    int atomDim;                // length of side of a single data atom
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
    public bool development = false;
    private double dx;           // the grid separation in the x-dimension
    private double dy;           // the grid separation in the y-dimension
    private double dz;           // the grid separation in the z-dimension
    private bool channel_grid;   // flag indicating if we are working with the non-uniform gird for the channel flow dataset
    private bool rmhd;   // flag indicating if we are working with the rmhd 2048 dataset
    private GridPoints gridPointsY; // grid values for the non-uniform y dimension in the case of channel flow
    public float Dt;            // used for converting from floating point time to timestep values stored in the DB
    private int timeInc;         // time increment between timesteps stored in the DB
    private int timeOff;        // timestep offset (t = dt * (timestep - timeOff); for channel flow timestep 132010 = time 0)
    public bool smallAtoms;     // indicator of whether the atoms stored in the DB are small with no edge replicaiton
                                // or large with a replicated edge of length 4 on each side
    List<ServerBoundaries> serverBoundaries; // info about the spatial partitioning of the data across servers
    const int MAX_READ_LENGTH = 256000000;
    const int MAX_NUMBER_THRESHOLD_POINTS = 1024 * 1024;
    const double DENSITY_CONSTANT = 80.0;
    //New: db type either file or database.  0 is databse, 1 is file
    public int dbtype;
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
        this.infodb = infodb; //This is the infodb, not the server that holds infodb.  Do this elsewhere
        /*Update infodb to one that is currently accepting requests*/
        this.infodb_server = GetTurbinfoServer();  //This is the server that holds infodb.
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
        this.dbtype = 0;  //We set to database as dbtype initially since majority is database.
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
            case DataInfo.DataSets.isotropic4096:
            case DataInfo.DataSets.strat4096:
                this.gridResolution = new int[] { 4096, 4096, 4096 };
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
                    String cString = String.Format("Server={0};Database={1};Asynchronous Processing=false;Trusted_Connection=True;Pooling=false; Connect Timeout = 600;",
                        server_name, codeDatabase[0]);
                    SqlConnection sqlConn = new SqlConnection(cString);
                    sqlConn.Open();
                    gridPointsY.GetGridPointsFromDB(sqlConn, dataset.ToString());
                    sqlConn.Close();
                }
                else
                {
                    throw new Exception("Servers not initialized!");
                }
                break;
            case DataInfo.DataSets.bl_zaki:
                this.gridResolution = new int[] { 2048, 224, 3320 };
                this.dx = 0.292210466252391;
                this.dz = 0.117187500000000;
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
                    String cString = String.Format("Server={0};Database={1};Asynchronous Processing=false;Trusted_Connection=True;Pooling=false; Connect Timeout = 600;",
                        server_name, codeDatabase[0]);
                    //String cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;Integrated Security=true;Pooling=false; Connect Timeout = 600;",
                    //    server_name, codeDatabase[0], ConfigurationManager.AppSettings["turbquery_uid"], ConfigurationManager.AppSettings["turbquery_password"]);
                    SqlConnection sqlConn = new SqlConnection(cString);
                    sqlConn.Open();
                    gridPointsY.GetGridPointsFromDB(sqlConn, dataset.ToString());
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
            case DataInfo.DataSets.isotropic4096:
            case DataInfo.DataSets.strat4096:
            case DataInfo.DataSets.bl_zaki:
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
            case DataInfo.DataSets.isotropic4096: //There is only one timestep for this dataset so this isn't really necessary.
                this.Dt = 0.0002F;
                this.timeInc = 1;
                break;
            case DataInfo.DataSets.strat4096:
                this.Dt = 1F;
                this.timeInc = 1;
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
            case DataInfo.DataSets.rmhd:
                this.Dt = 0.0006F;
                this.timeInc = 4;
                break;
            case DataInfo.DataSets.bl_zaki:
                this.Dt = 0.001F;
                this.timeInc = 1;
                break;
            default:
                throw new Exception("Invalid dataset specified!");
        }
    }

    /// Rotates through all known infodbs for redundancy.
    public String GetTurbinfoServer()
    {
        /*This is used to cycle through the turbinfo servers in case one goes down */
        List<String> turbinfoservers = new List<String>();
        //turbinfoservers.Add("dsp033"); /*No SQL server here, just a test*/
        if (infodb == "turbinfo")
        {
            turbinfoservers.Add("gw01"); /*Using this for testing...remove for production*/
            turbinfoservers.Add("mydbsql"); /*backup of DatabaseMap*/
        }
        else
        {
            turbinfoservers.Add("sciserver02"); /*Using this for testing...remove for production*/
            //turbinfoservers.Add("mydbsql"); /*Using this for testing...remove for production*/
        }
        //turbinfoservers.Add("gw03");
        //turbinfoservers.Add("gw04");
        /* Now get a good connection in order of what was provided */
        foreach (var server in turbinfoservers)
        {
            //Console.WriteLine("trying server {0}", server);
            /*Using a short timeout on this connection just to find a server that responds quickly */
            String cString = String.Format("Server={0};Database={1};Asynchronous Processing=false;MultipleActiveResultSets=True;Trusted_Connection=True;Pooling=true;Max Pool Size=250;Min Pool Size=20;Connection Lifetime=7200; Connection Timeout=2", server, this.infodb);
            using (var l_oConnection = new SqlConnection(cString))
            {
                try
                {
                    l_oConnection.Open();
                    return server;
                }
                catch (SqlException)
                {
                    Console.WriteLine("Trying next server");
                }
            }
        }
        /*If all else fails, go back to gw01*/
        if (infodb == "turbinfo")
        {
            return "gw01";
        }
        else
        {
            return "sciserver02";
        }
    }

    /// <summary>
    /// Initialize servers, connections and input data tables.
    /// </summary>
    /// <param name="worker">Worker type used</param>
    public void selectServers(DataInfo.DataSets dataset_enum)
    {
        String dataset = dataset_enum.ToString();
        //String cString = "Server=gw01;Database=turbinfo;Asynchronous Processing=false;MultipleActiveResultSets=True;Trusted_Connection=True;Pooling=true;Max Pool Size=250;Min Pool Size=20;Connection Lifetime=7200";
        //String turbinfoserver = "gw01";
        //turbinfoserver = GetTurbinfoServer();
        String cString = String.Format("Server={0};Database={1};Asynchronous Processing=false;MultipleActiveResultSets=True;Trusted_Connection=True;Pooling=true;Max Pool Size=250;Min Pool Size=20;Connection Lifetime=7200", this.infodb_server, this.infodb);
        SqlConnection conn = new SqlConnection(cString);
        conn.Open();
        SqlCommand cmd = conn.CreateCommand();
        string DBMapTable = "DatabaseMap";
        //if (this.development == true)
        //{
        //    DBMapTable = "DatabaseMapTest";
        //}
        cmd.CommandText = String.Format("select ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, MIN(minLim) as minLim, MAX(maxLim) as maxLim, MIN(minTime) as minTime, MAX(maxTime) as maxTime, dbType " +
            "from {0}..{1} where DatasetName = @dataset " +
            "group by ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, dbType " +
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
                    codeDatabase.Add("turblib_test");
                }
                long minLim = reader.GetSqlInt64(3).Value;
                long maxLim = reader.GetSqlInt64(4).Value;
                int minTime = reader.GetSqlInt32(5).Value;
                int maxTime = reader.GetSqlInt32(6).Value;
                int dbtype = reader.GetSqlInt32(7).Value;
                serverBoundaries.Add(new ServerBoundaries(new Morton3D(minLim), new Morton3D(maxLim), minTime, maxTime));
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
        //String turbinfoserver = GetTurbinfoServer();
        String cString = String.Format("Server={0};Database={1};Asynchronous Processing=false;Trusted_Connection=True;Pooling=true;Max Pool Size=250;Min Pool Size=20;Connection Lifetime=7200",
                this.infodb_server, this.infodb);
        //String cString = "Server=" + turbinfoserver + ";Database=turbinfo;Asynchronous Processing=false;Trusted_Connection=True;Pooling=true;Max Pool Size=250;Min Pool Size=20;Connection Lifetime=7200";
        //String cString = "Server=sciserver02;Database=turbinfo;Asynchronous Processing=false;Trusted_Connection=True;Pooling=true;Max Pool Size=250;Min Pool Size=20;Connection Lifetime=7200";
        SqlConnection conn = new SqlConnection(cString);
        conn.Open();

        SqlCommand cmd = conn.CreateCommand();
        string DBMapTable = "DatabaseMap";
        //if (this.development == true)
        //{
        //    DBMapTable = "DatabaseMapTest";
        //}
        cmd.CommandText = String.Format("select ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, MIN(minLim) as minLim, MAX(maxLim) as maxLim, MIN(minTime) as minTime, MAX(maxTime) as maxTime, dbType " +
            "from {0}..{1} where DatasetName = @dataset " +
            "group by ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, dbType " +
            "order by minLim", infodb, DBMapTable);
        cmd.Parameters.AddWithValue("@dataset", dataset);

        SqlDataReader reader = cmd.ExecuteReader();
        string lastserver = "";
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
                    codeDatabase.Add("turblib_test");
                }
                long minLim = reader.GetSqlInt64(3).Value;
                long maxLim = reader.GetSqlInt64(4).Value;
                int minTime = reader.GetSqlInt32(5).Value;
                int maxTime = reader.GetSqlInt32(6).Value;
                this.dbtype = reader.GetSqlInt32(7).Value; //Not sure we should do this--this assumes all dbs are the same type.  Fix this if we have hybrid filedb/database types.
                                                           /* All servers are added, and then ones in range are selected from this list later */
                serverBoundaries.Add(new ServerBoundaries(new Morton3D(minLim), new Morton3D(maxLim), minTime, maxTime));
                lastserver = reader.GetString(0);
            }
        }
        else
        {
            throw new Exception("Invalid dataset specified:" + dataset + ".");
        }
        //throw new Exception("Found server: " + lastserver + " from server " + cString);
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

    public void GetServerParameters4RawData(int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth,
        int[] serverX, int[] serverY, int[] serverZ, int[] serverXwidth, int[] serverYwidth, int[] serverZwidth,
        int x_stride, int y_stride, int z_stride, int T, int Twidth, int dbtype)
    {
        for (int i = 0; i < this.serverCount; i++)
        {
            if (X + Xwidth > serverBoundaries[i].startx && X <= serverBoundaries[i].endx)
                if (Y + Ywidth > serverBoundaries[i].starty && Y <= serverBoundaries[i].endy)
                    if (Z + Zwidth > serverBoundaries[i].startz && Z <= serverBoundaries[i].endz)
                    {
                        if (T + Twidth > serverBoundaries[i].minTime && T <= serverBoundaries[i].maxTime)
                        {
                            // If we have no workload for this server yet... create a connection
                            if (dbtype == 0)
                            {
                                if (this.connections[i] == null)
                                {
                                    string server_name = this.servers[i];
                                    if (server_name.Contains("_"))
                                        server_name = server_name.Remove(server_name.IndexOf("_"));
                                    String cString = String.Format("Server={0};Database={1};Asynchronous Processing=false;Trusted_Connection=True;Pooling=false; Connect Timeout = 600;",
                                        server_name, databases[i]);

                                    this.connections[i] = new SqlConnection(cString);
                                    this.connections[i].Open();
                                }
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
            //TODO: what is this value?
            else if (GridResolutionX == 4096)
            {
                bit = (int)(sfc / (long)(1 << 27));
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

public class Tuple<T, U>
{
    public T Item1 { get; private set; }
    public U Item2 { get; private set; }

    public Tuple(T item1, U item2)
    {
        Item1 = item1;
        Item2 = item2;
    }
}

public static class Tuple
{
    public static Tuple<T, U> Create<T, U>(T item1, U item2)
    {
        return new Tuple<T, U>(item1, item2);
    }
}
