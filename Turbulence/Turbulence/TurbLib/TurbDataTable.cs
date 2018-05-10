using System;
using System.Text;
using System.Data.SqlClient;
using Turbulence.TurbLib.DataTypes;
using System.Collections.Generic;

namespace Turbulence.TurbLib
{
    /// <summary>
    /// A class to contain all of the meta data associated with a dataset.
    /// TODO: Store this information in the database or in an XML file
    /// </summary>
    public class TurbDataTable
    {
        private string dataName;    // name of this dataset [used as a root for table names]
        private string tableName;   // name of the primary table for blob access
        private int blobDim;        // Dimension length of one side of each blob (32 for 32^3)
        private int edgeRegion;     // Replicated region on the edge of each cube
        protected int[] gridResolution; // Dimensions of the entire grid given az [z,y,x]
        private int components;     // Number of components at each point (4 for Vx,Vy,Vz,p)
        private string[] dataDescription;  // human-readable description of each data field
        private int timestart;      // First time step
        private int timeend;        // Last time step
        private int timeinc;        // integral-increment between timesteps
        private float dt;           // value of dt (t = dt * timestep)
        private int timeOff;        // timestep offset (t = dt * (timestep - timeOff); for channel flow timestep 132010 = time 0)
        private short dbType;        // timestep offset (t = dt * (timestep - timeOff); for channel flow timestep 132010 = time 0)
        protected double dx;           // grid resolution along x
        protected double dy;           // grid resolution along y
        protected double dz;           // grid resolution along z

        private string serverName;  // SQL Server name [used to determine the data boundaries]
        private int SqlArrayHeader;

        private ServerBoundaries serverBoundaries;

        public int atomDim { get { return blobDim; } }
        public int EdgeRegion { get { return edgeRegion; } }
        public int Components { get { return components; } }
        public string[] DataDescription { get { return dataDescription; } }
        public string DataName { get { return dataName; } }
        public string TableName { get { return tableName; } }
        public int TimeStart { get { return timestart; } }
        public int TimeEnd { get { return timeend; } }
        public int TimeInc { get { return timeinc; } }
        public int TimeOff { get { return timeOff; } }
        public short dbtype { get { return dbType; } }
        public int GridResolutionX { get { return gridResolution[2]; } }
        public int GridResolutionY { get { return gridResolution[1]; } }
        public int GridResolutionZ { get { return gridResolution[0]; } }
        public int[] GridResolution { get { return gridResolution; } }
        public float Dt { get { return dt; } }
        public int BlobSide { get { return blobDim + 2 * edgeRegion; } }
        public int BlobByteSize { get { return BlobSide * BlobSide * BlobSide * components * 4; } }
        public int SqlArrayHeaderSize { get { return SqlArrayHeader; } }
        public int StartX { get { return serverBoundaries.startx; } }
        public int StartY { get { return serverBoundaries.starty; } }
        public int StartZ { get { return serverBoundaries.startz; } }
        public int EndX { get { return serverBoundaries.endx; } }
        public int EndY { get { return serverBoundaries.endy; } }
        public int EndZ { get { return serverBoundaries.endz; } }

        //public float Dx { get { return 2.0F * (float)Math.PI / (float)gridResolution; } }
        public float DxFloat { get { return (float)dx; } }
        public double Dx { get { return dx; } }
        public double Dy { get { return dy; } }
        public double Dz { get { return dz; } }

        public virtual int CalcNodeX(double value, TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            int x;
            switch (spatialInterp)
            {
                case TurbulenceOptions.SpatialInterpolation.Lag4:
                case TurbulenceOptions.SpatialInterpolation.Lag6:
                case TurbulenceOptions.SpatialInterpolation.Lag8:
                case TurbulenceOptions.SpatialInterpolation.Fd4Lag4:
                case TurbulenceOptions.SpatialInterpolation.M1Q4:
                case TurbulenceOptions.SpatialInterpolation.M1Q6:
                case TurbulenceOptions.SpatialInterpolation.M1Q8:
                case TurbulenceOptions.SpatialInterpolation.M1Q10:
                case TurbulenceOptions.SpatialInterpolation.M1Q12:
                case TurbulenceOptions.SpatialInterpolation.M1Q14:
                case TurbulenceOptions.SpatialInterpolation.M2Q4:
                case TurbulenceOptions.SpatialInterpolation.M2Q6:
                case TurbulenceOptions.SpatialInterpolation.M2Q8:
                case TurbulenceOptions.SpatialInterpolation.M2Q10:
                case TurbulenceOptions.SpatialInterpolation.M2Q12:
                case TurbulenceOptions.SpatialInterpolation.M2Q14:
                case TurbulenceOptions.SpatialInterpolation.M3Q4:
                case TurbulenceOptions.SpatialInterpolation.M3Q6:
                case TurbulenceOptions.SpatialInterpolation.M3Q8:
                case TurbulenceOptions.SpatialInterpolation.M3Q10:
                case TurbulenceOptions.SpatialInterpolation.M3Q12:
                case TurbulenceOptions.SpatialInterpolation.M3Q14:
                case TurbulenceOptions.SpatialInterpolation.M4Q4:
                case TurbulenceOptions.SpatialInterpolation.M4Q6:
                case TurbulenceOptions.SpatialInterpolation.M4Q8:
                case TurbulenceOptions.SpatialInterpolation.M4Q10:
                case TurbulenceOptions.SpatialInterpolation.M4Q12:
                case TurbulenceOptions.SpatialInterpolation.M4Q14:
                    x = (int)(Math.Floor(value / dx));
                    break;
                case TurbulenceOptions.SpatialInterpolation.None:
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    x = (int)(Math.Round(value / dx));
                    break;
                default:
                    throw new Exception(String.Format("Unsupported interpolation option: {0}!", spatialInterp));
            }
            return x;
        }
        public virtual int CalcNodeY(double value, TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            int y;
            switch (spatialInterp)
            {
                case TurbulenceOptions.SpatialInterpolation.Lag4:
                case TurbulenceOptions.SpatialInterpolation.Lag6:
                case TurbulenceOptions.SpatialInterpolation.Lag8:
                case TurbulenceOptions.SpatialInterpolation.Fd4Lag4:
                case TurbulenceOptions.SpatialInterpolation.M1Q4:
                case TurbulenceOptions.SpatialInterpolation.M1Q6:
                case TurbulenceOptions.SpatialInterpolation.M1Q8:
                case TurbulenceOptions.SpatialInterpolation.M1Q10:
                case TurbulenceOptions.SpatialInterpolation.M1Q12:
                case TurbulenceOptions.SpatialInterpolation.M1Q14:
                case TurbulenceOptions.SpatialInterpolation.M2Q4:
                case TurbulenceOptions.SpatialInterpolation.M2Q6:
                case TurbulenceOptions.SpatialInterpolation.M2Q8:
                case TurbulenceOptions.SpatialInterpolation.M2Q10:
                case TurbulenceOptions.SpatialInterpolation.M2Q12:
                case TurbulenceOptions.SpatialInterpolation.M2Q14:
                case TurbulenceOptions.SpatialInterpolation.M3Q4:
                case TurbulenceOptions.SpatialInterpolation.M3Q6:
                case TurbulenceOptions.SpatialInterpolation.M3Q8:
                case TurbulenceOptions.SpatialInterpolation.M3Q10:
                case TurbulenceOptions.SpatialInterpolation.M3Q12:
                case TurbulenceOptions.SpatialInterpolation.M3Q14:
                case TurbulenceOptions.SpatialInterpolation.M4Q4:
                case TurbulenceOptions.SpatialInterpolation.M4Q6:
                case TurbulenceOptions.SpatialInterpolation.M4Q8:
                case TurbulenceOptions.SpatialInterpolation.M4Q10:
                case TurbulenceOptions.SpatialInterpolation.M4Q12:
                case TurbulenceOptions.SpatialInterpolation.M4Q14:
                    y = (int)(Math.Floor(value / dy));
                    break;
                case TurbulenceOptions.SpatialInterpolation.None:
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    y = (int)(Math.Round(value / dy));
                    break;
                default:
                    throw new Exception(String.Format("Unsupported interpolation option: {0}!", spatialInterp));
            }
            return y;
        }
        public virtual int CalcNodeZ(double value, TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            int z;
            switch (spatialInterp)
            {
                case TurbulenceOptions.SpatialInterpolation.Lag4:
                case TurbulenceOptions.SpatialInterpolation.Lag6:
                case TurbulenceOptions.SpatialInterpolation.Lag8:
                case TurbulenceOptions.SpatialInterpolation.Fd4Lag4:
                case TurbulenceOptions.SpatialInterpolation.M1Q4:
                case TurbulenceOptions.SpatialInterpolation.M1Q6:
                case TurbulenceOptions.SpatialInterpolation.M1Q8:
                case TurbulenceOptions.SpatialInterpolation.M1Q10:
                case TurbulenceOptions.SpatialInterpolation.M1Q12:
                case TurbulenceOptions.SpatialInterpolation.M1Q14:
                case TurbulenceOptions.SpatialInterpolation.M2Q4:
                case TurbulenceOptions.SpatialInterpolation.M2Q6:
                case TurbulenceOptions.SpatialInterpolation.M2Q8:
                case TurbulenceOptions.SpatialInterpolation.M2Q10:
                case TurbulenceOptions.SpatialInterpolation.M2Q12:
                case TurbulenceOptions.SpatialInterpolation.M2Q14:
                case TurbulenceOptions.SpatialInterpolation.M3Q4:
                case TurbulenceOptions.SpatialInterpolation.M3Q6:
                case TurbulenceOptions.SpatialInterpolation.M3Q8:
                case TurbulenceOptions.SpatialInterpolation.M3Q10:
                case TurbulenceOptions.SpatialInterpolation.M3Q12:
                case TurbulenceOptions.SpatialInterpolation.M3Q14:
                case TurbulenceOptions.SpatialInterpolation.M4Q4:
                case TurbulenceOptions.SpatialInterpolation.M4Q6:
                case TurbulenceOptions.SpatialInterpolation.M4Q8:
                case TurbulenceOptions.SpatialInterpolation.M4Q10:
                case TurbulenceOptions.SpatialInterpolation.M4Q12:
                case TurbulenceOptions.SpatialInterpolation.M4Q14:
                    z = (int)(Math.Floor(value / dz));
                    break;
                case TurbulenceOptions.SpatialInterpolation.None:
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    z = (int)(Math.Round(value / dz));
                    break;
                default:
                    throw new Exception(String.Format("Unsupported interpolation option: {0}!", spatialInterp));
            }
            return z;
        }

        public TurbDataTable(string dataName,
            string tableName, int gridResolution, int blobDim,
            int edgeRegion,
            int components, string[] dataDescription,
            float dt, int timestart, int timeend, int timeinc, int timeoff, short dbtype)
        {
            this.dataName = dataName;
            this.tableName = tableName;
            this.gridResolution = new int[] { gridResolution, gridResolution, gridResolution };
            this.blobDim = blobDim;
            this.edgeRegion = edgeRegion;
            this.components = components;
            this.dataDescription = dataDescription;
            this.dt = dt;
            this.timestart = timestart;
            this.timeend = timeend;
            this.timeinc = timeinc;
            this.timeOff = timeoff;
            this.dbType = dbtype;
            this.dx = (2.0 * Math.PI) / (double)gridResolution;
            this.SqlArrayHeader = 6 * sizeof(int);
        }

        public TurbDataTable(string serverName, string dbName, TurbServerInfo serverinfo, string dataName,
            string tableName, int blobDim,
            int edgeRegion,
            int components, string[] dataDescription,
            float dt, int timestart, int timeend, int timeinc, int timeoff, short dbtype)
        {
            this.serverName = serverName;
            this.dataName = dataName;
            this.tableName = tableName;
            this.blobDim = blobDim;
            this.edgeRegion = edgeRegion;
            this.components = components;
            this.dataDescription = dataDescription;
            this.dt = dt;
            this.timestart = timestart;
            this.timeend = timeend;
            this.timeinc = timeinc;
            this.timeOff = timeoff;
            this.dbType = dbtype;
            this.gridResolution = new int[] { 1024, 1024, 1024 };
            // For isotropic4096, it isn't setting correctly, so correct it here.
            if (dbName.Contains("iso4096") || dbName.Contains("strat4096"))
            {
                this.gridResolution[0] = 4096;
                this.gridResolution[1] = 4096;
                this.gridResolution[2] = 4096;
            }

            this.dx = (2.0 * Math.PI) / (double)gridResolution[2];
            this.dy = (2.0 * Math.PI) / (double)gridResolution[1];
            this.dz = (2.0 * Math.PI) / (double)gridResolution[0];

            this.SqlArrayHeader = 6 * sizeof(int);
            this.serverBoundaries = new ServerBoundaries();

            #region VirtualServers

            int num_virtual_servers = -1;
            int server = -1;
            long min_zindex = -1, max_zindex = -1;

            if (serverName.Contains("_"))
            {
                int firstUnderscore = serverName.IndexOf("_");
                int lastUnderscore = serverName.LastIndexOf("_");
                num_virtual_servers = System.Convert.ToInt32(serverName.Substring(firstUnderscore + 1, lastUnderscore - firstUnderscore - 1));
                server = System.Convert.ToInt32(serverName.Substring(lastUnderscore + 1, serverName.Length - lastUnderscore - 1));
            }
            else
            {
                server = 0;
                num_virtual_servers = 1;
            }
            /*We need to do the following in a better way.  This won't work for filedb, since there is no zindex table */
            /*
            SqlCommand command = new SqlCommand(String.Format("SELECT MIN(zindex), MAX(zindex) FROM {0}.dbo.zindex", dbName), conn);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        min_zindex = reader.GetInt64(0);
                        max_zindex = reader.GetInt64(1);
                    }
                }
                else
                {
                    reader.Close();
                    throw new Exception("No rows returned, when requesting zindex range from database!");
                }
            }

            */
            /*New way-- query the databasemap table */
            String cString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverinfo.infoDB_server, serverinfo.infoDB);
            SqlConnection turbinfoConn = new SqlConnection(cString);
            turbinfoConn.Open();
            SqlCommand command = turbinfoConn.CreateCommand();
            command.CommandText = String.Format("SELECT MIN(minLim), MAX(maxLim) FROM {0}.dbo.databasemap " +
                "WHERE productiondatabasename = '{1}'", serverinfo.infoDB, dbName);
            //SqlCommand command = new SqlCommand(String.Format("SELECT MIN(minLim), MAX(maxLim) FROM turbinfo.dbo.databasemap where productiondatabasename = '{1}'", dbName), conn);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        min_zindex = reader.GetInt64(0);
                        max_zindex = reader.GetInt64(1);
                    }
                }
                else
                {
                    reader.Close();
                    throw new Exception("No rows returned, when requesting zindex range from database!");
                }
            }
            turbinfoConn.Close();
            int blobSize = blobDim * blobDim * blobDim;
            //long pernode = (max_zindex + blobSize - min_zindex) / num_virtual_servers;  // space assigned to each node
            //// first zindex stored on this server gives us the coordinates of the 
            //// lower left corner of the data cube
            //Morton3D first_zindex = new Morton3D(min_zindex + pernode * server);
            //// the last index is the computed from the first zindex of the next server
            //// this gives us the coordinates of the upper right corner of the data cube
            //Morton3D last_zindex = new Morton3D(min_zindex + pernode * (server + 1) - 1);
            //startx  = first_zindex.X;
            //endx    = last_zindex.X;
            //starty  = first_zindex.Y;
            //endy    = last_zindex.Y;
            //startz  = first_zindex.Z;
            //endz    = last_zindex.Z;

            Morton3D start = new Morton3D(min_zindex);
            //Morton3D end = new Morton3D(max_zindex + blobSize - 1);  //This won't work for filedb.  It is already "normalized"
            Morton3D end = new Morton3D(max_zindex);
            int Xresolution = end.X - start.X + 1;
            int Yresolution = end.Y - start.Y + 1;
            int Zresolution = end.Z - start.Z + 1;
            serverBoundaries.startx = start.X;
            serverBoundaries.starty = start.Y;
            serverBoundaries.startz = start.Z;
            serverBoundaries.endx = end.X;
            serverBoundaries.endy = end.Y;
            serverBoundaries.endz = end.Z;

            serverBoundaries = serverBoundaries.getVirtualServerBoundaries(num_virtual_servers)[server];

            #endregion
        }

        public bool PointInRange(int x, int y, int z)
        {
            if (serverBoundaries.startx <= x && x <= serverBoundaries.endx)
                if (serverBoundaries.starty <= y && y <= serverBoundaries.endy)
                    if (serverBoundaries.startz <= z && z <= serverBoundaries.endz)
                        return true;
            return false;
        }

        /// <summary>
        /// Information about each of the datasets we have installed.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <remarks>
        /// TODO: This information could easily be stored as XML (or similar) inside the database.
        /// </remarks>
        public static TurbDataTable GetTableInfo(string tableName)
        {
            if (tableName.Equals("isotropic1024coarse") || tableName.Equals("isotropic1024old"))
            {
                return new TurbDataTable("isotropic turbulence with a resolution of 1024",
                    "isotropic1024data", 1024, 64, 4, 4, new string[] { "Ux", "Uy", "Uz", "P" },
                    0.0002f, -10, 4700, 10, 0, 0);
            }
            else if (tableName.Equals("isotropic1024fine") || tableName.Equals("isotropic1024fine_old"))
            {
                return new TurbDataTable("isotropic turbulence with a resolution of 1024",
                    "isotropic1024data", 1024, 64, 4, 4, new string[] { "Ux", "Uy", "Uz", "P" },
                    0.0002f, -1, 100, 1, 0, 0);
            }
            else if (tableName.Equals("isotropic4096"))
            {/* Check to see if this really gets executed...*/
                return new TurbDataTable("isotropic turbulence with a resolution of 4096",
               "isotropic4096", 4096, 64, 4, 4, new string[] { "Ux", "Uy", "Uz", "P" },
               0.0002f, 0, 0, 1, 0, 1);
            }
            else if (tableName.Equals("strat4096"))
            {/* Check to see if this really gets executed...*/
                return new TurbDataTable("roration and stratified turbulence with a resolution of 4096",
               "strat4096", 4096, 64, 4, 4, new string[] { "Ux", "Uy", "Uz", "P" },
               1f, 0, 4, 1, 0, 1);
            }
            else if (tableName.Equals("testing"))
            {
                return new TurbDataTable("testing table",
                    "isotropic1024data", 1024, 64, 4, 4, new string[] { "Ux", "Uy", "Uz", "P" },
                    0.0002f, -1, 100, 1, 0, 0);
            }
            else
            {
                throw new Exception(String.Format("Unknown dataset: {0}", tableName));
            }

        }

        /// <summary>
        /// Information about each of the datasets we have installed.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <remarks>
        /// TODO: This information could easily be stored as XML (or similar) inside the database.
        /// </remarks>
        public static TurbDataTable GetTableInfo(string serverName, string dbName, string tableName, int blobDim, TurbServerInfo serverinfo)
        {
            if (tableName.Equals("isotropic1024fine_vel"))
            {
                return new TurbDataTable(serverName, dbName, serverinfo, "velocity",
                    "vel", blobDim, 0, 3, new string[] { "Ux", "Uy", "Uz" },
                    0.0002f, -1, 100, 1, 0, 0);
            }
            else if (tableName.Equals("isotropic1024fine_pr"))
            {
                return new TurbDataTable(serverName, dbName, serverinfo, "pressure",
                    "pr", blobDim, 0, 1, new string[] { "P" },
                    0.0002f, -1, 100, 1, 0, 0);
            }
            else if (tableName.Equals("testing"))
            {
                return new TurbDataTable(serverName, dbName, serverinfo, "testing table",
                    "velocity08", blobDim, 0, 3, new string[] { "Ux", "Uy", "Uz", "P" },
                    0.0002f, -1, 100, 1, 0, 0);
            }
            else if (tableName.Equals("vel") || tableName.Contains("vel_"))
            {
                if (dbName.Contains("channeldb"))
                {
                    return new ChannelFlowDataTable(serverName, dbName, serverinfo, "velocity",
                        tableName, blobDim, 0, 3, new string[] { "Ux", "Uy", "Uz" },
                        0.0013f, 132005, 152015, 5, 132010, 0);
                }
                else if (dbName.Contains("bl_zaki"))
                {
                    return new ChannelFlowDataTable(serverName, dbName, serverinfo, "velocity",
                        tableName, blobDim, 0, 3, new string[] { "Ux", "Uy", "Uz" },
                        0.25f, 0, 4700, 1, 0, 2);
                }
                else if (dbName.Contains("mixing"))
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "velocity",
                        tableName, blobDim, 0, 3, new string[] { "Ux", "Uy", "Uz" },
                        0.04f, 0, 1014, 1, 1, 0);
                }
                else if (dbName.Contains("rmhd"))
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "velocity",
                        tableName, blobDim, 0, 2, new string[] { "Ux", "Uy", "Uz" },
                        0.00025f, 0, 36, 4, 0, 0);
                }
                else if (dbName.Contains("iso4096"))
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "velocity",
                        tableName, blobDim, 0, 3, new string[] { "Ux", "Uy", "Uz" },
                        0.0002f, 0, 0, 10, 0, 1);
                }
                else if (dbName.Contains("strat4096"))
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "velocity",
                        tableName, blobDim, 0, 3, new string[] { "Ux", "Uy", "Uz" },
                        1f, 0, 4, 1, 0, 1);
                }
                else
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "velocity",
                        tableName, blobDim, 0, 3, new string[] { "Ux", "Uy", "Uz" },
                        0.0002f, -10, 50300, 10, 0, 0);
                }
            }
            else if (tableName.Equals("mag"))
            {
                if (dbName.Contains("rmhd"))
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "velocity",
                        tableName, blobDim, 0, 2, new string[] { "Ux", "Uy", "Uz" },
                        0.00025f, 0, 36, 4, 0, 0);
                }
                else
                {
                    throw new Exception("Invalid dataset and table combination!");
                }
            }
            else if (tableName.Equals("velocity08"))
            {
                if (dbName.Contains("channeldb"))
                {
                    return new ChannelFlowDataTable(serverName, dbName, serverinfo, "velocity",
                        tableName, blobDim, 0, 3, new string[] { "Ux", "Uy", "Uz" },
                        0.0013f, 132005, 152015, 5, 132010, 0);
                }
                else if (dbName.Contains("mixing"))
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "velocity",
                        tableName, blobDim, 0, 3, new string[] { "Ux", "Uy", "Uz" },
                        0.04f, 0, 1014, 1, 1, 0);
                }
                else
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "velocity",
                        tableName, blobDim, 0, 3, new string[] { "Ux", "Uy", "Uz" },
                        0.00025f, -10, 10240, 10, 0, 0);
                }
            }
            else if (tableName.Equals("pr") || tableName.Contains("pr_"))
            {
                if (dbName.Contains("channeldb"))
                {
                    return new ChannelFlowDataTable(serverName, dbName, serverinfo, "pressure",
                        tableName, blobDim, 0, 1, new string[] { "P" },
                        0.0013f, 132005, 152015, 5, 132010, 0);
                }
                else if (dbName.Contains("bl_zaki"))
                {
                    return new ChannelFlowDataTable(serverName, dbName, serverinfo, "pressure",
                        tableName, blobDim, 0, 1, new string[] { "P" },
                        0.25f, 0, 4700, 1, 0, 2);
                }
                else if (dbName.Contains("mixing"))
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "pressure",
                        tableName, blobDim, 0, 1, new string[] { "P" },
                        0.04f, 0, 1014, 1, 1, 0);
                }
                else if (dbName.Contains("iso4096"))
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "pressure",
                        tableName, blobDim, 0, 1, new string[] { "P" },
                        0.0002f, 0, 0, 10, 0, 1);
                }
                else
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "pressure",
                        tableName, blobDim, 0, 1, new string[] { "P" },
                        0.0002f, -10, 10240, 10, 0, 0);
                }
            }
            else if (tableName.Equals("pressure08"))
            {
                if (dbName.Contains("channeldb"))
                {
                    return new ChannelFlowDataTable(serverName, dbName, serverinfo, "pressure",
                        tableName, blobDim, 0, 1, new string[] { "P" },
                        0.0013f, 132005, 152015, 5, 132010, 0);
                }
                else if (dbName.Contains("mixing"))
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "pressure",
                        tableName, blobDim, 0, 1, new string[] { "P" },
                        0.04f, 0, 1014, 1, 1, 0);
                }
                else
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "pressure",
                        tableName, blobDim, 0, 1, new string[] { "P" },
                        0.00025f, -1, 100, 10, 0, 0);
                }
            }
            else if (tableName.Contains("magnetic"))
            {
                return new TurbDataTable(serverName, dbName, serverinfo, "magnetic",
                    tableName, blobDim, 0, 3, new string[] { "Bx", "By", "Bz" },
                    0.00025f, -10, 10240, 10, 0, 0);
            }
            else if (tableName.Contains("potential"))
            {
                return new TurbDataTable(serverName, dbName, serverinfo, "potential",
                    tableName, blobDim, 0, 3, new string[] { "Ax", "Ay", "Az" },
                    0.00025f, -10, 10240, 10, 0, 0);
            }
            else if (tableName.Equals("density"))
            {
                return new TurbDataTable(serverName, dbName, serverinfo, "density",
                    tableName, blobDim, 0, 1, new string[] { "D" },
                    0.04f, 0, 1014, 1, 1, 0);
            }
            else if (tableName.Equals("th") || tableName.Contains("tem"))
            {
                if (dbName.Contains("strat4096"))
                {
                    return new TurbDataTable(serverName, dbName, serverinfo, "temperature",
                    tableName, blobDim, 0, 1, new string[] { "Th" },
                    1f, 0, 4, 1, 0, 1);
                }
                else
                {
                    throw new Exception(String.Format("Unknown dataset: {0}", tableName));
                }
            }
            else
            {
                throw new Exception(String.Format("Unknown dataset: {0}", tableName));
            }

        }

        /// <summary>
        /// Information about each of the datasets we have installed.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <remarks>
        /// TODO: This information could easily be stored as XML (or similar) inside the database.
        /// </remarks>
        public static TurbDataTable GetMHDTableInfo(string tableName, int blobDim)
        {
            if (tableName.Equals("vel") || tableName.Contains("vel_"))
            {
                return new TurbDataTable("velocity", tableName, 1024, blobDim, 0, 3,
                    new string[] { "Ux", "Uy", "Uz" },
                    0.0002f, -1, 100, 10, 0, 0);
            }
            else if (tableName.Equals("velocity08"))
            {
                return new TurbDataTable("velocity", tableName, 1024, blobDim, 0, 3,
                    new string[] { "Ux", "Uy", "Uz" },
                    0.00025f, -1, 100, 10, 0, 0);
            }
            else if (tableName.Equals("pr") || tableName.Contains("pr_"))
            {
                return new TurbDataTable("pressure",
                    tableName, 1024, blobDim, 0, 1, new string[] { "P" },
                    0.0002f, -1, 100, 10, 0, 0);
            }
            else if (tableName.Equals("pressure08"))
            {
                return new TurbDataTable("pressure",
                    tableName, 1024, blobDim, 0, 1, new string[] { "P" },
                    0.00025f, -1, 100, 10, 0, 0);
            }
            else if (tableName.Contains("magnetic"))
            {
                return new TurbDataTable("magnetic",
                    tableName, 1024, blobDim, 0, 3, new string[] { "Bx", "By", "Bz" },
                    0.00025f, -1, 100, 10, 0, 0);
            }
            else if (tableName.Contains("potential"))
            {
                return new TurbDataTable("potential",
                    tableName, 1024, blobDim, 0, 3, new string[] { "Ax", "Ay", "Az" },
                    0.00025f, -1, 100, 10, 0, 0);
            }
            else if (tableName.Contains("temperature"))
            {
                return new TurbDataTable("temperature",
                    tableName, 4096, blobDim, 0, 1, new string[] { "Th" },
                    1f, 0, 4, 1, 0, 0);
            }
            else
            {
                throw new Exception(String.Format("Unknown dataset: {0}", tableName));
            }

        }
    }

    /// <summary>
    /// A class to contain all of the meta data associated with a dataset.
    /// </summary>
    public class ChannelFlowDataTable : TurbDataTable
    {
        private GridPoints gridPointsY; // the grid (cell) values along the y dimension 
                                        //(for the non-uniform grid in the case of channel flow)

        public double[] GridValuesX(int stencil_start_index, int interpolationOrder)
        {
            //int start = cell_index - interpolationOrder / 2 + 1;
            double[] grid_values = new double[interpolationOrder];
            for (int i = 0; i < interpolationOrder; i++)
            {
                grid_values[i] = (stencil_start_index + i) * dx;
            }
            return grid_values;
        }
        public double[] GridValuesY(int stencil_start_index, int interpolationOrder)
        {
            double[] grid_values = new double[interpolationOrder];
            for (int i = 0; i < interpolationOrder; i++)
            {
                if (stencil_start_index + i == -1) //for transition_bl (bl_zaki)
                    grid_values[i] = 0;
                else
                    grid_values[i] = gridPointsY.GetGridValue(stencil_start_index + i);
            }
            return grid_values;
        }
        public double[] GridValuesZ(int stencil_start_index, int interpolationOrder)
        {
            //int start = cell_index - interpolationOrder / 2 + 1;
            double[] grid_values = new double[interpolationOrder];
            for (int i = 0; i < interpolationOrder; i++)
            {
                grid_values[i] = (stencil_start_index + i) * dz;
            }
            return grid_values;
        }
        public double GetGridValueY(int cell_index)
        {
            return gridPointsY.GetGridValue(cell_index);
        }

        public override int CalcNodeY(double value, TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            switch (spatialInterp)
            {
                case TurbulenceOptions.SpatialInterpolation.Lag4:
                case TurbulenceOptions.SpatialInterpolation.Lag6:
                case TurbulenceOptions.SpatialInterpolation.Lag8:
                case TurbulenceOptions.SpatialInterpolation.Fd4Lag4:
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                //return gridPointsY.GetCellIndex(value, 0.0); //TODO: should I correct here?
                case TurbulenceOptions.SpatialInterpolation.M1Q4:
                case TurbulenceOptions.SpatialInterpolation.M1Q6:
                case TurbulenceOptions.SpatialInterpolation.M1Q8:
                case TurbulenceOptions.SpatialInterpolation.M1Q10:
                case TurbulenceOptions.SpatialInterpolation.M1Q12:
                case TurbulenceOptions.SpatialInterpolation.M1Q14:
                case TurbulenceOptions.SpatialInterpolation.M2Q4:
                case TurbulenceOptions.SpatialInterpolation.M2Q6:
                case TurbulenceOptions.SpatialInterpolation.M2Q8:
                case TurbulenceOptions.SpatialInterpolation.M2Q10:
                case TurbulenceOptions.SpatialInterpolation.M2Q12:
                case TurbulenceOptions.SpatialInterpolation.M2Q14:
                case TurbulenceOptions.SpatialInterpolation.M3Q4:
                case TurbulenceOptions.SpatialInterpolation.M3Q6:
                case TurbulenceOptions.SpatialInterpolation.M3Q8:
                case TurbulenceOptions.SpatialInterpolation.M3Q10:
                case TurbulenceOptions.SpatialInterpolation.M3Q12:
                case TurbulenceOptions.SpatialInterpolation.M3Q14:
                case TurbulenceOptions.SpatialInterpolation.M4Q4:
                case TurbulenceOptions.SpatialInterpolation.M4Q6:
                case TurbulenceOptions.SpatialInterpolation.M4Q8:
                case TurbulenceOptions.SpatialInterpolation.M4Q10:
                case TurbulenceOptions.SpatialInterpolation.M4Q12:
                case TurbulenceOptions.SpatialInterpolation.M4Q14:
                //return gridPointsY.GetCellIndex(value); //TODO: should I correct here?
                case TurbulenceOptions.SpatialInterpolation.None:
                    return gridPointsY.GetCellIndexRound(value);
                default:
                    throw new Exception(String.Format("Unsupported interpolation option: {0}!", spatialInterp));
            }
        }

        public ChannelFlowDataTable(string serverName, string dbName, TurbServerInfo serverinfo, string dataName,
            string tableName, int blobDim,
            int edgeRegion,
            int components, string[] dataDescription,
            float dt, int timestart, int timeend, int timeinc, int timeoff, short dbtype)
            : base(serverName, dbName, serverinfo, dataName, tableName, blobDim, edgeRegion, components, dataDescription,
            dt, timestart, timeend, timeinc, timeoff, dbtype)
        {
            if (dbName.Contains("channel"))
            {
                this.gridResolution = new int[] { 1536, 512, 2048 };
                this.dx = (8.0 * Math.PI) / (double)gridResolution[2];
                this.dz = (3.0 * Math.PI) / (double)gridResolution[0];
            }
            else if (dbName.Contains("bl_zaki"))
            {
                this.gridResolution = new int[] { 2048, 224, 3320 };
                this.dx = 0.292210466240511;
                this.dz = 0.117244748412311;
            }

            // dy is not uniform for the channel flow DB
            // we have to store all of the grid points for the y-dimension
            this.dy = 0.0;
            gridPointsY = new GridPoints(gridResolution[1]);
            SqlConnection contextConn = new SqlConnection("context connection=true");
            contextConn.Open();
            gridPointsY.GetGridPointsFromDB(contextConn, dbName);
            contextConn.Close();
        }
    }

    public class TurbServerInfo
    {
        private string codedb;
        private string infodb;    // name of this dataset [used as a root for table names]
        private string infodb_server;   // name of the primary table for blob access

        public string codeDB { get { return codedb; } }
        public string infoDB { get { return infodb; } }
        public string infoDB_server { get { return infodb_server; } }

        public TurbServerInfo(string codedb,
            string infodb, string infodb_server)
        {
            this.codedb = codedb;
            this.infodb = infodb;
            this.infodb_server = infodb_server;
        }

        public static TurbServerInfo GetTurbServerInfo(string codedb,
            string infodb, string infodb_server)
        {
            return new TurbServerInfo(codedb, infodb, infodb_server);
        }
    }
}
