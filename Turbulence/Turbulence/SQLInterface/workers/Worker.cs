using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using Turbulence.SQLInterface;

using System.Collections.Generic;
/* Added for FileDB*/
using System.IO;

namespace Turbulence.SQLInterface
{
    /// <summary>
    /// Generic interface for a database procedure.
    /// 
    /// This interface supports procedures which only operate on points
    /// and their 8*8*8 bounding boxes at a single point in time.
    /// Time interpolation is supported by averaging 
    /// </summary>
    public abstract class Worker
    {
        protected const int MAX_NUMBER_THRESHOLD_POINTS = 1024 * 1024;
        //protected TurbDataTable setInfo;
        protected SqlDataRecord record;
        protected int kernelSize = -1; // This is the size of the kernel of computation

        protected float[] cutout = null;
        protected BigArray<float> big_cutout = null;
        protected int[] cutout_coordinates = null;
        protected bool using_big_cutout;

        public float GetDataItem(ulong index)
        {
            if (using_big_cutout)
            {
                return big_cutout[index];
            }
            else
            {
                return cutout[index];
            }
        }

        public SqlDataRecord Record { get { return record; } }
        public TurbDataTable DataTable { get { return setInfo; } }
        public virtual TurbDataTable setInfo { get; set; }
        public TurbulenceOptions.SpatialInterpolation spatialInterp { get; protected set; }

        public abstract double[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input);
        public abstract double[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input);
        public virtual double[] GetResult(TurbulenceBlob blob1, TurbulenceBlob blob2, SQLUtility.MHDInputRequest input)
        {
            throw new NotImplementedException();
        }
        public virtual HashSet<SQLUtility.PartialResult> GetResult(TurbulenceBlob blob, Dictionary<long, SQLUtility.PartialResult> active_points)
        {
            throw new NotImplementedException();
        }
        //public virtual HashSet<SQLUtility.PartialResult> GetThresholdUsingCutout(int[] coordiantes, double threshold)
        //{
        //    throw new NotImplementedException();
        //}
        public virtual HashSet<SQLUtility.PartialResult> GetThresholdUsingCutout(int[] coordiantes, double threshold, int workertype)
        {
            throw new NotImplementedException();
        }
        public abstract int GetResultSize();
        public abstract SqlMetaData[] GetRecordMetaData();

        public int KernelSize { get { return kernelSize; } }
        //public virtual void GetAtomsForPoint(float xp, float yp, float zp, long mask, HashSet<long> atoms)
        //{
        //    throw new NotImplementedException();
        //}
        public virtual void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points)
        {
            throw new NotImplementedException();
        }

        protected virtual void AddAtoms(int startz, int starty, int startx, int endz, int endy, int endx, HashSet<long> atoms, long mask)
        {
            long zindex;
            // we do not want a request to appear more than once in the list for an atom
            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom and then move from atom to atom
            startz = startz - ((startz % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            starty = starty - ((starty % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            startx = startx - ((startx % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            for (int z = startz; z <= endz; z += setInfo.atomDim)
            {
                for (int y = starty; y <= endy; y += setInfo.atomDim)
                {
                    for (int x = startx; x <= endx; x += setInfo.atomDim)
                    {
                        // Wrap the coordinates into the grid space
                        int xi = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                        int yi = ((y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                        int zi = ((z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

                        if (setInfo.PointInRange(xi, yi, zi))
                        {
                            zindex = new Morton3D(zi, yi, xi).Key & mask;
                            if (!atoms.Contains(zindex))
                                atoms.Add(zindex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Given the coordinates of region where we want to compute the particular field
        /// returns coordinates [startx, starty, startz, endx, endy, endz]
        /// of the data cutout need to perform the entire computation.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public virtual int[] GetCutoutCoordinates(int[] coordinates)
        {
            int half_kernel = KernelSize / 2;
            return new int[] {coordinates[0] - half_kernel, coordinates[1] - half_kernel, coordinates[2] - half_kernel,
                                                coordinates[3] + half_kernel, coordinates[4] + half_kernel, coordinates[5] + half_kernel};
        }

        public virtual void GetData(short datasetID, TurbServerInfo serverinfo, int timestep, int[] coordinates, short dbtype)
        {
            cutout_coordinates = GetCutoutCoordinates(coordinates);
            int x_width, y_width, z_width;
            x_width = cutout_coordinates[3] - cutout_coordinates[0];
            y_width = cutout_coordinates[4] - cutout_coordinates[1];
            z_width = cutout_coordinates[5] - cutout_coordinates[2];
            //cutout = new byte[table.Components * sizeof(float) * x_width * y_width * z_width];
            ulong cutout_size = (ulong)setInfo.Components * (ulong)x_width * (ulong)y_width * (ulong)z_width;
            if (cutout_size > int.MaxValue / sizeof(float))
            {
                big_cutout = new BigArray<float>(cutout_size);
                using_big_cutout = true;
            }
            else
            {
                cutout = new float[cutout_size];
            }

            if (dbtype == 0)
            {
                GetCutout(datasetID, serverinfo, timestep);
            }
            else
            {
                //string outputmsg = "dbtype " + dbtype + System.Environment.NewLine;
                //System.IO.File.AppendAllText(@"c:\www\sqloutput-turb5.log", outputmsg);
                GetCutoutDB(datasetID, serverinfo, timestep, dbtype);
                //GetLocalCutoutDB_new(datasetID, serverinfo, setInfo, timestep, cutout_coordinates);
            }
        }

        protected void GetCutout(short datasetID, TurbServerInfo serverinfo, int timestep)
        {
            //string turbinfoserver = "gw01"; //This shouldn't be hardcoded.  Replace with server selector in the future.
            SqlConnection turbInfoConn = new SqlConnection(
                String.Format("Server={0};Database={1};Trusted_Connection=True;Pooling=false; Connect Timeout = 600;", serverinfo.infoDB_server, serverinfo.infoDB));
            turbInfoConn.Open();
            SqlConnection sqlConn;

            try
            {
                int[] local_coordinates,
                    local_start_coordinates_x, local_end_coordinates_x,
                    local_start_coordinates_y, local_end_coordinates_y,
                    local_start_coordinates_z, local_end_coordinates_z;
                GetLocalCoordiantes(cutout_coordinates[0], cutout_coordinates[3], setInfo.StartX, setInfo.EndX,
                    out local_start_coordinates_x, out local_end_coordinates_x);
                GetLocalCoordiantes(cutout_coordinates[1], cutout_coordinates[4], setInfo.StartY, setInfo.EndY,
                    out local_start_coordinates_y, out local_end_coordinates_y);
                GetLocalCoordiantes(cutout_coordinates[2], cutout_coordinates[5], setInfo.StartZ, setInfo.EndZ,
                    out local_start_coordinates_z, out local_end_coordinates_z);

                local_coordinates = new int[6];
                for (int k = 0; k < local_start_coordinates_z.Length; k++)
                {
                    for (int j = 0; j < local_start_coordinates_y.Length; j++)
                    {
                        for (int i = 0; i < local_start_coordinates_x.Length; i++)
                        {
                            local_coordinates[0] = local_start_coordinates_x[i];
                            local_coordinates[1] = local_start_coordinates_y[j];
                            local_coordinates[2] = local_start_coordinates_z[k];
                            local_coordinates[3] = local_end_coordinates_x[i];
                            local_coordinates[4] = local_end_coordinates_y[j];
                            local_coordinates[5] = local_end_coordinates_z[k];

                            int wrapped_local_z = ((local_coordinates[2] % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;
                            int wrapped_local_y = ((local_coordinates[1] % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                            int wrapped_local_x = ((local_coordinates[0] % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;

                            long zindex = new Morton3D(wrapped_local_z, wrapped_local_y, wrapped_local_x);
                            SqlCommand cmd = turbInfoConn.CreateCommand();
                            cmd.CommandText = String.Format("select ProductionMachineName, ProductionDatabaseName, CodeDatabaseName " +
                                "from {0}..DatabaseMap where DatasetID = @datasetID " +
                                "and minLim <= @zindex and maxLim >= @zindex and minTime <= @timestep and maxTime >= @timestep", serverinfo.infoDB);
                            cmd.Parameters.AddWithValue("@datasetID", datasetID);
                            cmd.Parameters.AddWithValue("@zindex", zindex);
                            cmd.Parameters.AddWithValue("@timestep", timestep);
                            cmd.CommandTimeout = 600;
                            SqlDataReader reader = cmd.ExecuteReader();
                            if (!reader.HasRows)
                            {
                                throw new Exception(
                                    String.Format("The DatabaseMap table does not contain information about this dataset, zindex combination: {0}, {1}",
                                    datasetID, zindex));
                            }
                            reader.Read();
                            string serverName = reader.GetString(0);
                            string dbname = reader.GetString(1);
                            //string codedb = reader.GetString(2);
                            reader.Close();
                            reader.Dispose();

                            sqlConn = new SqlConnection(
                                String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;Connect Timeout = 600;",
                                serverName, serverinfo.codeDB));
                            sqlConn.Open();

                            GetLocalCutout(setInfo, dbname, timestep, local_coordinates, sqlConn);

                            sqlConn.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (turbInfoConn.State == ConnectionState.Open)
                {
                    turbInfoConn.Close();
                }
                throw ex;
            }
            turbInfoConn.Close();
        }

        protected void GetCutoutDB(short datasetID, TurbServerInfo serverinfo, int timestep, int dbtype)
        {
            int[] local_coordinates,
                    local_start_coordinates_x, local_end_coordinates_x,
                    local_start_coordinates_y, local_end_coordinates_y,
                    local_start_coordinates_z, local_end_coordinates_z;
            GetLocalCoordiantes(cutout_coordinates[0], cutout_coordinates[3], setInfo.StartX, setInfo.EndX,
                out local_start_coordinates_x, out local_end_coordinates_x);
            GetLocalCoordiantes(cutout_coordinates[1], cutout_coordinates[4], setInfo.StartY, setInfo.EndY,
                out local_start_coordinates_y, out local_end_coordinates_y);
            GetLocalCoordiantes(cutout_coordinates[2], cutout_coordinates[5], setInfo.StartZ, setInfo.EndZ,
                out local_start_coordinates_z, out local_end_coordinates_z);

            local_coordinates = new int[6];
            for (int k = 0; k < local_start_coordinates_z.Length; k++)
            {
                for (int j = 0; j < local_start_coordinates_y.Length; j++)
                {
                    for (int i = 0; i < local_start_coordinates_x.Length; i++)
                    {
                        local_coordinates[0] = local_start_coordinates_x[i];
                        local_coordinates[1] = local_start_coordinates_y[j];
                        local_coordinates[2] = local_start_coordinates_z[k];
                        local_coordinates[3] = local_end_coordinates_x[i];
                        local_coordinates[4] = local_end_coordinates_y[j];
                        local_coordinates[5] = local_end_coordinates_z[k];

                        GetLocalCutoutDB(datasetID, serverinfo, setInfo, timestep, local_coordinates, dbtype);
                        //string outputmsg = "GetLocalCutoutDB done" + System.Environment.NewLine;
                        //System.IO.File.AppendAllText(@"c:\www\sqloutput-turb5.log", outputmsg);
                    }
                }
            }
        }

        private void GetLocalCoordiantes(int cutout_start_coordinate, int cutout_end_coordiante,
            int grid_start, int grid_end,
            out int[] local_start_coordinate, out int[] local_end_coordinate)
        {
            //TODO: max=3 may be not enough for iso4096?
            int num_regions = 1, max_regions = 3;
            int[] temp_start_coordinates = new int[max_regions];
            int[] temp_end_coordinates = new int[max_regions];
            temp_start_coordinates[0] = cutout_start_coordinate;
            if (cutout_start_coordinate < grid_start)
            {
                temp_start_coordinates[num_regions] = grid_start;
                temp_end_coordinates[0] = grid_start;
                num_regions++;
            }
            if (cutout_end_coordiante > grid_end + 1)
            {
                temp_start_coordinates[num_regions] = grid_end + 1;
                temp_end_coordinates[num_regions - 1] = grid_end + 1;
                num_regions++;
            }
            temp_end_coordinates[num_regions - 1] = cutout_end_coordiante;

            local_start_coordinate = new int[num_regions];
            local_end_coordinate = new int[num_regions];
            for (int i = 0; i < num_regions; i++)
            {
                local_start_coordinate[i] = temp_start_coordinates[i];
                local_end_coordinate[i] = temp_end_coordinates[i];
            }
        }

        protected virtual void GetLocalCutout(TurbDataTable table, string dbname, int timestep,
            int[] local_coordinates,
            SqlConnection connection)
        {
            int x_width, y_width, z_width, x, y, z;
            x_width = cutout_coordinates[3] - cutout_coordinates[0];
            y_width = cutout_coordinates[4] - cutout_coordinates[1];
            z_width = cutout_coordinates[5] - cutout_coordinates[2];

            byte[] rawdata = new byte[table.BlobByteSize];

            string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);
            int atomWidth = table.atomDim;

            string queryString = GetQueryString(local_coordinates, tableName, dbname, timestep);

            int sourceX = 0, sourceY = 0, sourceZ = 0, lengthX = 0, lengthY = 0, lengthZ = 0;
            ulong destinationX = 0, destinationY = 0, destinationZ = 0;
            //ulong long_destinationX = 0, long_destinationY = 0, long_destinationZ = 0;
            ulong long_components = (ulong)table.Components;
            ulong long_x_width = (ulong)x_width;
            ulong long_y_width = (ulong)y_width;

            SqlCommand command = new SqlCommand(
                queryString, connection);
            command.CommandTimeout = 600;
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    x = reader.GetSqlInt32(0).Value;
                    y = reader.GetSqlInt32(1).Value;
                    z = reader.GetSqlInt32(2).Value;
                    int bytesread = 0;
                    while (bytesread < table.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(3, table.SqlArrayHeaderSize, rawdata, bytesread, table.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }

                    GetSourceDestLenWithWrapAround(x, local_coordinates[0], local_coordinates[3], cutout_coordinates[0], atomWidth, table.GridResolutionX,
                        ref sourceX, ref destinationX, ref lengthX);
                    GetSourceDestLenWithWrapAround(y, local_coordinates[1], local_coordinates[4], cutout_coordinates[1], atomWidth, table.GridResolutionY,
                        ref sourceY, ref destinationY, ref lengthY);
                    GetSourceDestLenWithWrapAround(z, local_coordinates[2], local_coordinates[5], cutout_coordinates[2], atomWidth, table.GridResolutionZ,
                        ref sourceZ, ref destinationZ, ref lengthZ);

                    int source0 = (sourceX + sourceY * atomWidth) * table.Components * sizeof(float);
                    ulong dest0 = (destinationX + destinationY * long_x_width) * long_components * sizeof(float);

                    for (int k = 0; k < lengthZ; k++)
                    {
                        int source = source0 + (sourceZ + k) * atomWidth * atomWidth * table.Components * sizeof(float);
                        ulong dest = dest0 + (destinationZ + (ulong)k) * long_x_width * long_y_width * long_components * sizeof(float);
                        for (int j = 0; j < lengthY; j++)
                        {
                            if (using_big_cutout)
                            {
                                big_cutout.BlockCopyInto(rawdata, source, dest, lengthX * table.Components * sizeof(float), sizeof(float));
                            }
                            else
                            {
                                Buffer.BlockCopy(rawdata, source, cutout, (int)dest, lengthX * table.Components * sizeof(float));
                            }
                            source += atomWidth * table.Components * sizeof(float);
                            dest += long_x_width * long_components * sizeof(float);
                        }
                    }
                }
            }
        }

        protected virtual void GetLocalCutoutDB(short datasetID, TurbServerInfo serverinfo, TurbDataTable table, int timestep,
            int[] local_coordinates, int dbtype)
        {
            //string turbinfoserver = "gw01"; //This shouldn't be hardcoded.  Replace with server selector in the future.
            //turbinfoserver = "mydbsql";
            SqlConnection turbInfoConn = new SqlConnection(
                String.Format("Server={0};Database={1};Trusted_Connection=True;Pooling=false; Connect Timeout = 600;", serverinfo.infoDB_server, serverinfo.infoDB));

            //SqlConnection sqlConn;

            int x_width, y_width, z_width, x, y, z;
            x_width = cutout_coordinates[3] - cutout_coordinates[0];
            y_width = cutout_coordinates[4] - cutout_coordinates[1];
            z_width = cutout_coordinates[5] - cutout_coordinates[2];

            byte[] rawdata = new byte[table.BlobByteSize];

            //string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);
            int atomWidth = table.atomDim;

            //string queryString = GetQueryString(local_coordinates, tableName, dbname, timestep);

            List<Morton3D> zindex = new List<Morton3D>();

            int[] QueryLoc = GetQueryDB(local_coordinates);
            // we look for the zindex need to be read
            for (int k = QueryLoc[2] / atomWidth; k <= (QueryLoc[5] - 1) / atomWidth; k++)
            {
                for (int j = QueryLoc[1] / atomWidth; j <= (QueryLoc[4] - 1) / atomWidth; j++)
                {
                    for (int i = QueryLoc[0] / atomWidth; i <= (QueryLoc[3] - 1) / atomWidth; i++)
                    {
                        Morton3D zindex_toread = new Morton3D(k * atomWidth, j * atomWidth, i * atomWidth);
                        if (!zindex.Contains(zindex_toread))
                        {
                            zindex.Add(zindex_toread);
                        }
                    }
                }
            }
            //TODO: am I right? sorting zindex from small to large
            zindex.Sort((t1, t2) => -1 * t2.Key.CompareTo(t1.Key));

            /*then, we find the path/dbname need to be read and the corresponding z-index Limit*/
            List<string> serverName = new List<string>();
            List<string> dbname = new List<string>();
            //List<string> codedb = new List<string>();
            List<long> minLim = new List<long>();
            List<long> maxLim = new List<long>();

            SQLUtility.InsertAtomIntoListDB(datasetID, serverinfo, table, zindex, timestep,
                out serverName, out dbname, out minLim, out maxLim);

            int sourceX = 0, sourceY = 0, sourceZ = 0, lengthX = 0, lengthY = 0, lengthZ = 0;
            ulong destinationX = 0, destinationY = 0, destinationZ = 0;
            //ulong long_destinationX = 0, long_destinationY = 0, long_destinationZ = 0;
            ulong long_components = (ulong)table.Components;
            ulong long_x_width = (ulong)x_width;
            ulong long_y_width = (ulong)y_width;

            //SqlCommand command = new SqlCommand(
            //    queryString, connection);
            //command.CommandTimeout = 600;
            //using (SqlDataReader reader = command.ExecuteReader())
            //{
            //    while (reader.Read())
            /*In order to minimize file open/close operations, we loop through all the files needed, then we search which z-index are inside this file*/
            /*for (int j2 = 0; j2 < zindex.Count; j2++) this could be written in a more efficient way (we could continue searching instead of starting*/
            /*from beginning each time and break inner loop if it's outside the file z-index range). But, this is good for a more general case.*/
            SqlConnection[] sqlConns = new SqlConnection[dbname.Count];
            IAsyncResult[] asyncRes = new IAsyncResult[dbname.Count];
            SqlCommand[] cmds = new SqlCommand[dbname.Count];
            for (int i = 0; i < dbname.Count; i++)
            {
                sqlConns[i] = new SqlConnection(
                                String.Format("Data Source={0};Initial Catalog={1};Asynchronous Processing=true;Trusted_Connection=True;Pooling=false;Connect Timeout = 600;",
                                serverName[i], serverinfo.codeDB));
                sqlConns[i].Open();
                string pathSource = SQLUtility.getDBfilePath(dbname[i], timestep, table.DataName, sqlConns[i]);

                List<Morton3D> zindexQueryList = new List<Morton3D>();
                string zindexQuery = "[";
                for (int j2 = 0; j2 < zindex.Count; j2++)
                {
                    Morton3D zindex2 = zindex[j2];
                    if (minLim[i] <= zindex2 && zindex2 <= maxLim[i])
                    {
                        zindexQuery = zindexQuery + zindex2.ToString() + ",";
                        zindexQueryList.Add(zindex2);
                    }
                }
                zindexQuery = zindexQuery + "]";

                cmds[i] = sqlConns[i].CreateCommand();
                cmds[i].CommandText = String.Format("EXEC [{0}].[dbo].[ExecuteDBFileReader] @serverName, @dbname, @filePath, @BlobByteSize, @atomDim, "
                                                + " @zindexQuery, @zlistCount, @dbtype",
                                                serverinfo.codeDB);
                cmds[i].Parameters.AddWithValue("@serverName", serverName[i]);
                cmds[i].Parameters.AddWithValue("@dbname", dbname[i]);
                cmds[i].Parameters.AddWithValue("@filePath", pathSource);
                cmds[i].Parameters.AddWithValue("@BlobByteSize", table.BlobByteSize);
                cmds[i].Parameters.AddWithValue("@atomDim", table.atomDim);
                cmds[i].Parameters.AddWithValue("@zindexQuery", zindexQuery);
                cmds[i].Parameters.AddWithValue("@zlistCount", zindexQueryList.Count);
                cmds[i].Parameters.AddWithValue("@dbtype", dbtype);
                asyncRes[i] = cmds[i].BeginExecuteReader(null, cmds[i]);
            }

            for (int i = 0; i < dbname.Count; i++)
            {
                using (SqlDataReader reader = cmds[i].EndExecuteReader(asyncRes[i]))
                {
                    while (reader.Read() && !reader.IsDBNull(0))
                    {

                        Morton3D zindex2 = new Morton3D(reader.GetSqlInt64(0).Value);
                        x = zindex2.X;
                        y = zindex2.Y;
                        z = zindex2.Z;
                        rawdata = reader.GetSqlBytes(1).Value;

                        GetSourceDestLenWithWrapAround(x, local_coordinates[0], local_coordinates[3], cutout_coordinates[0], atomWidth, table.GridResolutionX,
                            ref sourceX, ref destinationX, ref lengthX);
                        GetSourceDestLenWithWrapAround(y, local_coordinates[1], local_coordinates[4], cutout_coordinates[1], atomWidth, table.GridResolutionY,
                            ref sourceY, ref destinationY, ref lengthY);
                        GetSourceDestLenWithWrapAround(z, local_coordinates[2], local_coordinates[5], cutout_coordinates[2], atomWidth, table.GridResolutionZ,
                            ref sourceZ, ref destinationZ, ref lengthZ);

                        int source0 = (sourceX + sourceY * atomWidth) * table.Components * sizeof(float);
                        ulong dest0 = (destinationX + destinationY * long_x_width) * long_components * sizeof(float);

                        for (int k = 0; k < lengthZ; k++)
                        {
                            int source = source0 + (sourceZ + k) * atomWidth * atomWidth * table.Components * sizeof(float);
                            ulong dest = dest0 + (destinationZ + (ulong)k) * long_x_width * long_y_width * long_components * sizeof(float);
                            for (int j = 0; j < lengthY; j++)
                            {
                                if (using_big_cutout)
                                {
                                    big_cutout.BlockCopyInto(rawdata, source, dest, lengthX * table.Components * sizeof(float), sizeof(float));
                                }
                                else
                                {
                                    Buffer.BlockCopy(rawdata, source, cutout, (int)dest, lengthX * table.Components * sizeof(float));
                                }
                                source += atomWidth * table.Components * sizeof(float);
                                dest += long_x_width * long_components * sizeof(float);
                            }
                        }
                    }
                }
                sqlConns[i].Close();
            }
            rawdata = null;
        }


        protected void GetSourceDestLenWithWrapAround(int coordinate, int local_start, int local_end, int cutout_start, int atomWidth, int gridResolution,
            ref int source, ref ulong dest, ref int len)
        {
            if (coordinate + atomWidth <= local_start)
            {
                // This is due to wrap around
                coordinate += gridResolution;
            }
            if (coordinate > local_end)
            {
                // This is due to wrap around
                coordinate -= gridResolution;
            }

            if (coordinate < local_start)
            {
                if (coordinate + atomWidth <= local_start)
                    throw new Exception("Atom read is outside of boundaries of query box!");
                else if (coordinate + atomWidth <= local_end)
                    len = coordinate + atomWidth - local_start;
                else
                    len = local_end - local_start;
                source = local_start - coordinate;
                dest = 0;
            }
            else if (coordinate >= local_end)
                throw new Exception("Atom read is outside of boundaries of query box!");
            else
            {
                if (coordinate + atomWidth <= local_end)
                    len = atomWidth;
                else
                    len = local_end - coordinate;
                source = 0;
                dest = (ulong)(coordinate - cutout_start);
            }
        }

        protected string GetQueryString(int[] local_coordinates, string tableName, string dbname, int timestep)
        {
            int start_z = local_coordinates[2];
            int start_y = local_coordinates[1];
            int start_x = local_coordinates[0];
            int end_z = local_coordinates[5];
            int end_y = local_coordinates[4];
            int end_x = local_coordinates[3];

            if (start_z < 0)
            {
                start_z += setInfo.GridResolutionZ;
                end_z += setInfo.GridResolutionZ;
            }
            else if (start_z >= setInfo.GridResolutionZ)
            {
                start_z -= setInfo.GridResolutionZ;
                end_z -= setInfo.GridResolutionZ;
            }
            if (start_y < 0)
            {
                start_y += setInfo.GridResolutionY;
                end_y += setInfo.GridResolutionY;
            }
            else if (start_y >= setInfo.GridResolutionY)
            {
                start_y -= setInfo.GridResolutionY;
                end_y -= setInfo.GridResolutionY;
            }
            if (start_x < 0)
            {
                start_x += setInfo.GridResolutionX;
                end_x += setInfo.GridResolutionX;
            }
            else if (start_x >= setInfo.GridResolutionX)
            {
                start_x -= setInfo.GridResolutionX;
                end_x -= setInfo.GridResolutionX;
            }

            return GetQueryString(start_x, start_y, start_z, end_x, end_y, end_z, tableName, dbname, timestep);
        }

        protected int[] GetQueryDB(int[] local_coordinates)
        {
            int start_z = local_coordinates[2];
            int start_y = local_coordinates[1];
            int start_x = local_coordinates[0];
            int end_z = local_coordinates[5];
            int end_y = local_coordinates[4];
            int end_x = local_coordinates[3];

            if (start_z < 0)
            {
                start_z += setInfo.GridResolutionZ;
                end_z += setInfo.GridResolutionZ;
            }
            else if (start_z >= setInfo.GridResolutionZ)
            {
                start_z -= setInfo.GridResolutionZ;
                end_z -= setInfo.GridResolutionZ;
            }
            if (start_y < 0)
            {
                start_y += setInfo.GridResolutionY;
                end_y += setInfo.GridResolutionY;
            }
            else if (start_y >= setInfo.GridResolutionY)
            {
                start_y -= setInfo.GridResolutionY;
                end_y -= setInfo.GridResolutionY;
            }
            if (start_x < 0)
            {
                start_x += setInfo.GridResolutionX;
                end_x += setInfo.GridResolutionX;
            }
            else if (start_x >= setInfo.GridResolutionX)
            {
                start_x -= setInfo.GridResolutionX;
                end_x -= setInfo.GridResolutionX;
            }
            int[] QueryLoc = new int[] { start_x, start_y, start_z, end_x, end_y, end_z };
            return QueryLoc;
        }

        protected virtual string GetQueryString(int startx, int starty, int startz, int endx, int endy, int endz, string tableName, string dbname, int timestep)
        {
            return String.Format(
                   "select dbo.GetMortonX(t.zindex), dbo.GetMortonY(t.zindex), dbo.GetMortonZ(t.zindex), t.data " +
                   "from {7} as t inner join " +
                   "(select zindex from {8}..zindex where " +
                       "X >= {0} & -{6} and X < {3} and Y >= {1} & -{6} and Y < {4} and Z >= {2} & -{6} and z < {5}) " +
                   "as c " +
                   "on t.zindex = c.zindex " +
                   "and t.timestep = {9}",
                   startx, starty, startz,
                   endx, endy, endz,
                   setInfo.atomDim, tableName, dbname, timestep);
        }

        /*
         * Integer value passed to SQL from the web service.
         * These values are also used for logging.
         */
        public enum Workers
        {
            Sample = 0,
            GetVelocity = 1,
            GetVelocityWithPressure = 2,
            GetVelocityGradient = 3,
            GetPressureGradient = 4,
            GetVelocityHessian = 7,
            GetPressureHessian = 8,
            GetVelocityLaplacian = 5,
            GetLaplacianOfGradient = 6,
            GetPosition = 21,
            GetPositionDBEvaluation = 22,
            GetPressure = 50,
            GetBoxFilterPressure = 90,
            GetBoxFilterVelocity = 91,
            GetBoxFilterVelocityGradient = 92,
            GetBoxFilterSGSStress = 93,
            GetForce = 100,
            NullOp = 999,
            GetVelocityOld = 888,
            GetVelocityWithPressureOld = 889,

            GetMHDVelocity = 56, //NOTE: At some point there was a separate splines worker with id 130 
            GetMHDPressure = 57,
            GetMHDMagnetic = 58,
            GetMHDPotential = 59,
            GetRawVelocity = 60,
            GetRawPressure = 61,
            GetRawMagnetic = 62,
            GetRawPotential = 63,
            GetMHDVelocityGradient = 64, //NOTE: At some point there was a separate splines worker with id 133
            GetMHDMagneticGradient = 65,
            GetMHDPotentialGradient = 66,
            GetMHDPressureGradient = 67,
            GetMHDVelocityLaplacian = 68,
            GetMHDMagneticLaplacian = 69,
            GetMHDPotentialLaplacian = 70,
            GetMHDVelocityHessian = 71, //NOTE: At some point there was a separate splines worker with id 134
            GetMHDMagneticHessian = 72,
            GetMHDPotentialHessian = 73,
            GetMHDPressureHessian = 74,

            GetMHDBoxFilter = 75, //NOTE: At some point there was another box filter worker with id 76 
            GetMHDBoxFilterSV = 77,
            GetMHDBoxFilterSGS = 78,
            GetMHDBoxFilterSGS_SV = 79,
            GetMHDBoxFilterGradient = 80,

            GetCurl = 81,
            GetCurlThreshold = 82,
            GetChannelCurlThreshold = 83,
            GetVelocityThreshold = 84,
            GetMagneticThreshold = 85,
            GetPotentialThreshold = 86,
            GetPressureThreshold = 87,
            GetChannelVelocityThreshold = 88,
            GetChannelPressureThreshold = 89,
            GetQThreshold = 30,
            GetChannelQThreshold = 31,
            GetDensityThreshold = 32,

            GetChannelVelocity = 120,
            GetChannelPressure = 121,
            GetChannelVelocityGradient = 122,
            GetChannelPressureGradient = 123,
            GetChannelVelocityLaplacian = 124,
            GetChannelVelocityHessian = 125,
            GetChannelPressureHessian = 126,

            GetDensity = 150, //NOTE: used to be 140
            GetDensityGradient = 151, //NOTE: used to be 141
            GetDensityHessian = 152, //NOTE: used to be 142
            GetRawDensity = 153, //NOTE: used to be 143

            GetVelocityWorkerDirectOpt = 556,
            GetVelocityWorkerDirectWorst = 557

        }

        public static Worker GetWorker(string dataset, TurbDataTable setInfo, int procedure,
            int spatialInterpOption,
            float arg,
            SqlConnection sqlcon)
        {
            TurbulenceOptions.SpatialInterpolation spatialInterp = (TurbulenceOptions.SpatialInterpolation)spatialInterpOption;
            switch ((Workers)procedure)
            {
                case Workers.Sample:
                    return new workers.SampleWorker(setInfo);
                case Workers.GetVelocity:
                    return new workers.GetVelocityWorker(setInfo, spatialInterp, true, false);
                case Workers.GetVelocityWithPressure:
                    return new workers.GetVelocityWorker(setInfo, spatialInterp, true, true);
                case Workers.GetPressure:
                    return new workers.GetVelocityWorker(setInfo, spatialInterp, false, true);
                case Workers.GetPressureHessian:
                    return new workers.PressureHessian(setInfo, spatialInterp);
                case Workers.GetVelocityHessian:
                    return new workers.VelocityHessian(setInfo, spatialInterp);
                case Workers.GetVelocityGradient:
                    return new workers.GetVelocityGradient(setInfo, spatialInterp);
                case Workers.GetPressureGradient:
                    return new workers.GetPressureGradient(setInfo, spatialInterp);
                case Workers.GetVelocityLaplacian:
                    return new workers.VelocityLaplacian(setInfo, spatialInterp);
                case Workers.GetLaplacianOfGradient:
                    return new workers.LaplacianOfGradient(setInfo, spatialInterp);
                case Workers.GetPosition:
                    return new workers.GetPosition(setInfo, spatialInterp, arg);
                //return new workers.GetPosition(setInfo, spatialInterp, arg);
                case Workers.GetPositionDBEvaluation:
                    return new workers.GetPositionWorker(setInfo, spatialInterp, (TurbulenceOptions.TemporalInterpolation)arg);
                case Workers.GetVelocityOld:
                    return new workers.GetVelocityWorkerOld(setInfo, spatialInterp, false);
                case Workers.GetVelocityWithPressureOld:
                    return new workers.GetVelocityWorkerOld(setInfo, spatialInterp, true);

                case Workers.GetMHDVelocity:
                case Workers.GetMHDMagnetic:
                case Workers.GetMHDPotential:
                case Workers.GetVelocityThreshold:
                case Workers.GetMagneticThreshold:
                case Workers.GetPotentialThreshold:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetSplinesWorker(dataset, setInfo, spatialInterp, 0);
                    }
                    else
                    {
                        return new workers.GetMHDWorker(setInfo, spatialInterp);
                    }
                case Workers.GetMHDPressure:
                case Workers.GetDensity:
                case Workers.GetPressureThreshold:
                case Workers.GetDensityThreshold:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetSplinesWorker(dataset, setInfo, spatialInterp, 0);
                    }
                    else
                    {
                        return new workers.GetMHDPressure(setInfo, spatialInterp);
                    }
                case Workers.GetMHDVelocityGradient:
                case Workers.GetMHDMagneticGradient:
                case Workers.GetMHDPotentialGradient:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetSplinesWorker(dataset, setInfo, spatialInterp, 1);
                    }
                    else
                    {
                        return new workers.GetMHDGradient(setInfo, spatialInterp);
                    }
                case Workers.GetMHDPressureGradient:
                case Workers.GetDensityGradient:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetSplinesWorker(dataset, setInfo, spatialInterp, 1);
                    }
                    else
                    {
                        return new workers.GetMHDPressureGradient(setInfo, spatialInterp);
                    }
                case Workers.GetMHDVelocityLaplacian:
                case Workers.GetMHDMagneticLaplacian:
                case Workers.GetMHDPotentialLaplacian:
                    return new workers.GetMHDLaplacian(setInfo, spatialInterp);
                case Workers.GetMHDVelocityHessian:
                case Workers.GetMHDMagneticHessian:
                case Workers.GetMHDPotentialHessian:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetSplinesWorker(dataset, setInfo, spatialInterp, 2);
                    }
                    else
                    {
                        return new workers.GetMHDHessian(setInfo, spatialInterp);
                    }
                case Workers.GetMHDPressureHessian:
                case Workers.GetDensityHessian:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetSplinesWorker(dataset, setInfo, spatialInterp, 2);
                    }
                    else
                    {
                        return new workers.GetMHDPressureHessian(setInfo, spatialInterp);
                    }

                case Workers.GetMHDBoxFilter:
                    return new workers.GetMHDBoxFilter(setInfo, spatialInterp, arg);

                case Workers.GetMHDBoxFilterSGS:
                    return new workers.GetMHDBoxFilterSGS(setInfo, spatialInterp, arg);

                case Workers.GetMHDBoxFilterSV:
                    return new workers.GetMHDBoxFilterSV(setInfo, spatialInterp, arg);

                case Workers.GetMHDBoxFilterSGS_SV:
                    return new workers.GetMHDBoxFilterSGS_SV(setInfo, spatialInterp, arg);

                case Workers.GetMHDBoxFilterGradient:
                    return new workers.GetMHDBoxFilterGradient(setInfo, spatialInterpOption, arg);

                case Workers.GetCurl:
                case Workers.GetCurlThreshold:
                    return new workers.GetCurl(setInfo, spatialInterp);
                case Workers.GetChannelCurlThreshold:
                    return new workers.GetChannelCurl(dataset, setInfo, spatialInterp, sqlcon);
                case Workers.GetQThreshold:
                    return new workers.GetQ(setInfo, spatialInterp);
                case Workers.GetChannelQThreshold:
                    return new workers.GetChannelQ(dataset, setInfo, spatialInterp, sqlcon);

                case Workers.GetChannelVelocity:
                case Workers.GetChannelVelocityThreshold:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetChannelSplinesWorker(dataset, setInfo, spatialInterp, 0, sqlcon);
                    }
                    else
                    {
                        return new workers.GetChannelVelocity(dataset, setInfo, spatialInterp, sqlcon);
                    }
                case Workers.GetChannelPressure:
                case Workers.GetChannelPressureThreshold:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetChannelSplinesWorker(dataset, setInfo, spatialInterp, 0, sqlcon);
                    }
                    else
                    {
                        return new workers.GetChannelPressure(dataset, setInfo, spatialInterp, sqlcon);
                    }
                case Workers.GetChannelVelocityGradient:
                case Workers.GetChannelPressureGradient:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetChannelSplinesWorker(dataset, setInfo, spatialInterp, 1, sqlcon);
                    }
                    else
                    {
                        return new workers.GetChannelGradient(dataset, setInfo, spatialInterp, sqlcon);
                    }
                case Workers.GetChannelVelocityLaplacian:
                    return new workers.GetChannelLaplacian(dataset, setInfo, spatialInterp, sqlcon);
                case Workers.GetChannelVelocityHessian:
                case Workers.GetChannelPressureHessian:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetChannelSplinesWorker(dataset, setInfo, spatialInterp, 2, sqlcon);
                    }
                    else
                    {
                        return new workers.GetChannelHessian(dataset, setInfo, spatialInterp, sqlcon);
                    }

                default:
                    throw new Exception(String.Format("Unknown worker type: {0}", procedure));
            }
        }

        public static Worker GetWorker(TurbDataTable setInfo1, TurbDataTable setInfo2, int procedure,
            int spatialInterpOption,
            float arg,
            SqlConnection sqlcon)
        {
            TurbulenceOptions.SpatialInterpolation spatialInterp = (TurbulenceOptions.SpatialInterpolation)spatialInterpOption;
            switch ((Workers)procedure)
            {
                case Workers.GetMHDBoxFilterSGS:
                    return new workers.GetMHDBoxFilterSGS(setInfo1, setInfo2, spatialInterp, arg);

                case Workers.GetMHDBoxFilterSGS_SV:
                    return new workers.GetMHDBoxFilterSGS_SV(setInfo1, setInfo2, spatialInterp, arg);

                default:
                    throw new Exception(String.Format("Unknown worker type: {0}", procedure));
            }
        }


    }

}
