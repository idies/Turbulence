using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib.DataTypes;

using System.Collections;


namespace Turbulence.SQLInterface
{
    public class SQLUtility
    {
        public struct InputRequest
        {
            /*public InputRequest(int localID, int request, long zindex, double x, double y, double z)
            {
                this.localID = localID;
                this.request = request;
                this.zindex = zindex;
                this.x = x;
                this.y = y;
                this.z = z;
               
            }*/

            public InputRequest(int localID, int request, float x, float y, float z)
            {
                this.localID = localID;
                this.request = request;
                this.x = x;
                this.y = y;
                this.z = z;
                //this.predictor = new Point3(0, 0, 0);
                //this.velocity = new Vector3(0, 0, 0);
            }

            public void SetValues(int localID, int request, float x, float y, float z)
            {
                this.localID = localID;
                this.request = request;
                this.x = x;
                this.y = y;
                this.z = z;
                //this.predictor = new Point3(0, 0, 0);
                //this.velocity = new Vector3(0, 0, 0);
            }

            public InputRequest(int localID, int request, float x, float y, float z, Point3 predictor, Vector3 V)
            {
                this.localID = localID;
                this.request = request;
                this.x = x;
                this.y = y;
                this.z = z;
                //this.predictor = predictor;
                //this.velocity = V;
            }
            public int localID;
            public int request;
            public float x;
            public float y;
            public float z;
            /*public double x;
            public double y;
            public double z;*/
            // These are for in-database particle tracking.
            //public Point3 predictor;
            //public Vector3 velocity;
        };

        public class TrackingInputRequest
        {
            public int request;
            public int timeStep;
            public long zindex;
            public Point3 pos;
            public Point3 pre_pos;
            public Vector3 vel_inc;
            //public float time;
            public bool compute_predictor;
            public bool crossed_boundary;
            public bool done;
            public int numberOfCubes;
            public int cubesRead;
            public bool resultSent;
            public double[] lagInt;

            public TrackingInputRequest() { }

            public TrackingInputRequest(int request, long zindex,
                Point3 pos, Point3 pre_pos, Vector3 vel, bool flag)
            {
                this.request = request;
                //this.timeStep = timeStep;
                this.zindex = zindex;
                this.pos = pos;
                this.pre_pos = pre_pos;
                this.vel_inc = vel;
                //this.time = time;
                this.compute_predictor = flag;
                this.crossed_boundary = false;
                this.done = false;
                this.cubesRead = 0;
                this.numberOfCubes = 0;
                this.resultSent = false;
                this.lagInt = null;
            }
        };

        public struct TimestepZindexKey
        {
            int timestep;
            long zindex;
            public TimestepZindexKey(int timestep, long zindex)
            {
                this.timestep = timestep;
                this.zindex = zindex;
            }
            public void SetValues(int timestep, long zindex)
            {
                this.timestep = timestep;
                this.zindex = zindex;
            }
            public int Timestep { get { return timestep; } }
            public long Zindex { get { return zindex; } }

            public override int GetHashCode()
            {
                return timestep.GetHashCode() ^ zindex.GetHashCode();
            }

            public override bool Equals(object ob)
            {
                if (ob is TimestepZindexKey)
                {
                    TimestepZindexKey key = (TimestepZindexKey)ob;
                    return timestep == key.timestep && zindex == key.zindex;
                }
                else
                {
                    return false;
                }
            }
        }

        public class MHDInputRequest
        {
            public MHDInputRequest(int request, float x, float y, float z, int result_size, int numberOfCubes)
            {
                this.request = request;
                this.x = x;
                this.y = y;
                this.z = z;
                this.cell_x = 0;
                this.cell_y = 0;
                this.cell_z = 0;
                this.lagInt = null;

                this.result = new double[result_size];
                for (int i = 0; i < result_size; i++)
                {
                    result[i] = 0;
                }
                this.numberOfCubes = 0;
                this.cubesRead = 0;
                this.resultSent = false;

                //this.predictor = new Point3(0, 0, 0);
                //this.velocity = new Vector3(0, 0, 0);
            }
            public int request;
            public float x;
            public float y;
            public float z;
            public int cell_x;
            public int cell_y;
            public int cell_z;
            public double[] lagInt;

            public double[] result;
            public int numberOfCubes;
            public int cubesRead;
            public bool resultSent;
        };

        public class PartialResult
        {
            public PartialResult(long zindex, int result_size, int numPointsInKernel)
            {
                this.zindex = zindex;

                this.result = new double[result_size];
                for (int i = 0; i < result_size; i++)
                {
                    result[i] = 0;
                }
                this.numPointsInKernel = numPointsInKernel;
                this.numPointsProcessed = 0;
                this.norm = 0;
                //this.resultSent = false;
            }
            public PartialResult(long zindex)
            {
                this.zindex = zindex;
            }
            public long zindex;

            public double[] result;
            public int numPointsInKernel;
            public int numPointsProcessed;
            public double norm;
            //public bool resultSent;
        };

        public static string SelectDistinctIntoTemporaryTable(string tempTable)
        {
            tempTable = SanitizeTemporaryTable(tempTable);
            Random random = new Random();
            string tableName = "#query" + random.Next(100000);

            SqlConnection conn = new SqlConnection("context connection=true");
            conn.Open();
            //SqlCommand cmd = new SqlCommand(String.Format("SELECT DISTINCT (zindex & 0xfffc0000) AS zindex INTO {0} FROM {1}",
            SqlCommand cmd = new SqlCommand(String.Format("SELECT DISTINCT zindex INTO {0} FROM {1}",
                tableName, tempTable), conn);
            int count = cmd.ExecuteNonQuery();
            conn.Close();
            return tableName;
        }

        //public static string SelectDistinctIntoTemporaryTable(string tempTable, SqlConnection conn, long mask)
        public static string SelectDistinctIntoTemporaryTable(string tempTable, SqlConnection conn)
        {
            tempTable = SanitizeTemporaryTable(tempTable);
            Random random = new Random();
            string tableName = "#query" + random.Next(100000);

            //SqlCommand cmd = new SqlCommand(String.Format("SELECT DISTINCT (zindex & {2}) AS zindex INTO {0} FROM {1}",
            //SqlCommand cmd = new SqlCommand(String.Format("SELECT DISTINCT (zindex & 0xfffc0000) AS zindex INTO {0} FROM {1}",
            SqlCommand cmd = new SqlCommand(String.Format("SELECT DISTINCT zindex INTO {0} FROM {1}",
                tableName, tempTable), conn);
            int count = cmd.ExecuteNonQuery();
            return tableName;
        }

        public static string CreateTemporaryJoinTable(ICollection<long> Keys,
            SqlConnection sqlConn, float points_per_cube)
        {
            //Random random = new Random();
            //string tableName = "#query" + random.Next(100000);
            string tableName = "##query" + Guid.NewGuid().ToString().Replace("-", "");

            // Create the temporary table, in which the values are to be inserted
            //SqlCommand command = new SqlCommand(String.Format("IF  EXISTS " +
            //    "(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{0}') AND type in (N'U'))" +
            //    "DROP TABLE {0} " +
            //    "CREATE TABLE {0} (zindex bigint)", tableName), sqlConn);
            SqlCommand command = new SqlCommand(String.Format("CREATE TABLE {0} (zindex bigint)", tableName), sqlConn);
            try
            {
                command.CommandTimeout = 3600;
                command.ExecuteNonQuery();
            }
            catch (System.Exception ex)
            {
                throw new Exception(String.Format("Could not create temporary table: {0}", ex.ToString()));
            }

            IDataReader dataReader = SQLInterface.JoinTableDataReader.GetReader(Keys, points_per_cube);

            SqlBulkCopy bulkCopy = new SqlBulkCopy(sqlConn);

            // set the destination table name
            bulkCopy.DestinationTableName = tableName;

            // the batch size may have an effect on performance,
            // the optimal value may need to be determined empirically
            bulkCopy.BatchSize = 100000;
            bulkCopy.BulkCopyTimeout = 3600;

            try
            {
                bulkCopy.WriteToServer(dataReader);
            }
            catch (System.Exception ex)
            {
                throw new Exception(String.Format("Could not write the temporary table to the server: {0}", ex.ToString()));
            }
            dataReader.Close();
            //conn.Close();
            return tableName;
        }
        
        // customize batching
        //same as above, but returns a different map and an array
        public static Dictionary<long, List<int>> ReadTempTableGetCubesToReadBatch(string tempTable, 
            Worker[] worker,
            int[] boundaries, int[] result_sizel, int[] nOrderl, SqlConnection conn,
            Dictionary<int, MHDInputRequest> input, float time,
            ref float points_per_cube)
        {
            //Dictionary<long, HashSet<int>> map = new Dictionary<long, HashSet<int>>();
            Dictionary<long, List<int>> map = new Dictionary<long, List<int>>();
            tempTable = SanitizeTemporaryTable(tempTable);
            MHDInputRequest request;

            int total_points = 0;
            
            //SqlConnection conn = new SqlConnection(connString);
            //conn.Open();
            SqlCommand cmd = new SqlCommand(
                String.Format("SELECT reqseq, x, y, z FROM {0}", tempTable), conn);
            int result_size = 0;
            int kernelSize = 0;
            long mask = 0;
            SqlDataReader reader = cmd.ExecuteReader();

            long zindex = 0;
            HashSet<long> atoms = new HashSet<long>(); //NOTE: HashSet requires .Net 3.5
            while (reader.Read())
            {
                int arequest = reader.GetSqlInt32(0).Value;

                // -----------------------------------------
                // find query boundary and select the appropriate parameter
                for (int i = 0; i < boundaries.Length; ++i)
                {
                    if (arequest <= boundaries[i])
                    {
                        mask = ~(long)(worker[i].DataTable.atomDim * worker[i].DataTable.atomDim * worker[i].DataTable.atomDim - 1);

                        kernelSize = worker[i].KernelSize;
                        result_size = result_sizel[i];

                        request = new MHDInputRequest(
                            arequest,
                            reader.GetSqlSingle(2).Value,
                            reader.GetSqlSingle(3).Value,
                            reader.GetSqlSingle(4).Value,
                            result_size,
                            0
                            );

                        input[arequest] = request;

                        if (kernelSize / 2 > worker[i].DataTable.EdgeRegion)
                        {
                            worker[i].GetAtomsForPoint(request, mask, total_points / map.Keys.Count, map, ref total_points);
                            //worker[i].GetAtomsForPoint(request.x, request.y, request.z, mask, atoms);

                            //foreach (long atom in atoms)
                            //{
                            //    if (!map.ContainsKey(atom))
                            //    {
                            //        //if (map.Keys.Count == 0)
                            //        //    map[atom] = new List<int>();
                            //        //else
                            //        //    map[atom] = new List<int>(total_points / map.Keys.Count);
                            //        map[atom] = new List<int>();
                            //    }

                            //    map[atom].Add(request.request);
                            //    request.numberOfCubes++;
                            //    total_points++;
                            //}

                            //atoms.Clear();
                        }
                        else
                        {
                            zindex = reader.GetSqlInt64(1).Value & mask;

                            request.numberOfCubes = 1;
                            if (!map.ContainsKey(zindex))
                            {
                                map[zindex] = new List<int>();
                            }
                            map[zindex].Add(request.request);
                            total_points++;
                        }

                        break;
                    }
                }
                // -----------------------------------------                
            }
            reader.Close();
            //conn.Close();

            points_per_cube = (float)total_points / map.Keys.Count;

            return map;
        }

        // Same as previous method, but store results in a hashtable for fast access later. //
        //public static Dictionary<long, List<InputRequest>> ReadAndSortTemporaryTable(string tempTable, long mask)
        public static Dictionary<long, List<InputRequest>> ReadAndSortTemporaryTable(string tempTable)
        {
            Dictionary<long, List<InputRequest>> map = new Dictionary<long, List<InputRequest>>();
            tempTable = SanitizeTemporaryTable(tempTable);
            int length = GetTempTableLength(tempTable);
            InputRequest request;

            SqlConnection conn = new SqlConnection("context connection=true");
            conn.Open();
            SqlCommand cmd = new SqlCommand(
                String.Format("SELECT reqseq, zindex, x, y, z FROM {0}", tempTable), conn);
            SqlDataReader reader = cmd.ExecuteReader();

            int localID = 0;
            while (reader.Read())
            {
                request = new InputRequest(localID++,
                    reader.GetSqlInt32(0).Value,
                    reader.GetSqlSingle(2).Value,
                    reader.GetSqlSingle(3).Value,
                    reader.GetSqlSingle(4).Value
                    //reader.GetSqlDouble(2).Value,
                    //reader.GetSqlDouble(3).Value,
                    //reader.GetSqlDouble(4).Value
                    );

                long zindex = reader.GetSqlInt64(1).Value;
                if (!map.ContainsKey(zindex))
                {
                    map[zindex] = new List<InputRequest>();
                }
                map[zindex].Add(request);
            }
            reader.Close();
            conn.Close();

            return map;
        }

        //same as above, but returns a different map and an array
        public static Dictionary<long, List<int>> ReadTempTableGetCubesToRead(string tempTable,
            TurbDataTable table, int result_size, SqlConnection conn,
            Dictionary<int, MHDInputRequest> input, float time,
            ref float points_per_cube, int correcting_pos)
        {
            Dictionary<long, List<int>> map = new Dictionary<long, List<int>>();
            tempTable = SanitizeTemporaryTable(tempTable);
            MHDInputRequest request;

            int total_points = 0;

            string query = "";
            //if (getPosition)
            //    query = String.Format("SELECT reqseq, zindex, x, y, z, pre_x, pre_y, pre_z, Vx, Vy, Vz FROM {0}", tempTable);
            //else
                query = String.Format("SELECT reqseq, zindex, x, y, z FROM {0}", tempTable);
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            long zindex = 0;
            int reqseq = -1;
            while (reader.Read())
            {
                reqseq = reader.GetSqlInt32(0).Value;
                if (!input.ContainsKey(reqseq))
                {
                    request = new MHDInputRequest(
                        reqseq,
                        reader.GetSqlSingle(2).Value,
                        reader.GetSqlSingle(3).Value,
                        reader.GetSqlSingle(4).Value,
                        result_size,
                        0
                        );
                    input[reqseq] = request;
                }
                else
                    request = input[reqseq];

                zindex = reader.GetSqlInt64(1).Value;

                request.numberOfCubes++;
                if (!map.ContainsKey(zindex))
                {
                    map[zindex] = new List<int>();
                }
                map[zindex].Add(request.request);
                total_points++;
            }
            reader.Close();

            points_per_cube = (float)total_points / map.Keys.Count;

            return map;
        }
        
        //same as above, but returns a different map and an array
        public static Dictionary<long, List<int>> ReadTempTableGetAtomsToRead(string tempTable,
            Worker worker, Worker.Workers workerType,
            SqlConnection conn,
            Dictionary<int, MHDInputRequest> input, int inputSize,
            ref float points_per_cube)
        {
            Dictionary<long, List<int>> map = new Dictionary<long, List<int>>();
            tempTable = SanitizeTemporaryTable(tempTable);
            MHDInputRequest request;

            int total_points = 0;

            // Bitmask to ignore low order bits of address
            long mask = ~(long)(worker.DataTable.atomDim * worker.DataTable.atomDim * worker.DataTable.atomDim - 1);
            
            long zindex = 0;
            int result_size = worker.GetResultSize();
            //HashSet<long> atoms = new HashSet<long>(); //NOTE: HashSet requires .Net 3.5
            int reqseq = -1;

            string query = "";
            SqlCommand cmd;
            // NOTE: For channel flow we could use the following query:
            //
            // query += String.Format("SELECT reqseq, zindex, x, y, z, " +
            //        "(SELECT MAX(cell_index) FROM grid_points_y WHERE value <= y) as cell_index FROM {0}", tempTable);
            //
            // However, it seems to give worse performance than reading the grid points 
            //and performing a binary search to find the cell index.
            query += String.Format("SELECT reqseq, zindex, x, y, z FROM {0}", tempTable);
            cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                reqseq = reader.GetSqlInt32(0).Value;
                request = new MHDInputRequest(
                    reqseq,
                    reader.GetSqlSingle(2).Value,
                    reader.GetSqlSingle(3).Value,
                    reader.GetSqlSingle(4).Value,
                    result_size,
                    0
                    );

                request.cell_x = worker.DataTable.CalcNodeX(request.x, worker.spatialInterp);
                request.cell_y = worker.DataTable.CalcNodeY(request.y, worker.spatialInterp);
                request.cell_z = worker.DataTable.CalcNodeZ(request.z, worker.spatialInterp);
                
                if (worker.KernelSize > 0)
                {
                    worker.GetAtomsForPoint(request, mask, 0, map, ref total_points);
                }
                else
                {
                    int x = ((request.cell_x % worker.DataTable.GridResolutionX) + worker.DataTable.GridResolutionX) % worker.DataTable.GridResolutionX;
                    int y = ((request.cell_y % worker.DataTable.GridResolutionY) + worker.DataTable.GridResolutionY) % worker.DataTable.GridResolutionY;
                    int z = ((request.cell_z % worker.DataTable.GridResolutionZ) + worker.DataTable.GridResolutionZ) % worker.DataTable.GridResolutionZ;
                    zindex = new Morton3D(z, y, x) & mask;
                    //zindex = reader.GetSqlInt64(1).Value & mask;

                    request.numberOfCubes = 1;
                    if (!map.ContainsKey(zindex))
                    {
                        map[zindex] = new List<int>();
                    }
                    map[zindex].Add(request.request);
                    total_points++;
                }

                input[reqseq] = request;
            }
            reader.Close();

            points_per_cube = (float)total_points / map.Keys.Count;

            return map;
        }

        public static string ReadTempTableGetAtomsToRead_Filtering(string tempTable,
            Worker worker,
            SqlConnection contextConn,
            SqlConnection standardConn,
            //Dictionary<int, MHDInputRequest> input, 
            HashSet<MHDInputRequest> input,
            ref int xwidth, ref int ywidth, ref int zwidth,
            ref int startx, ref int starty, ref int startz)
        {
            tempTable = SanitizeTemporaryTable(tempTable);
            MHDInputRequest request;

            int result_size = worker.GetResultSize();
            //HashSet<long> atoms = new HashSet<long>(); //NOTE: HashSet requires .Net 3.5
            int reqseq = -1;

            int endx = worker.DataTable.StartX, endy = worker.DataTable.StartY, endz = worker.DataTable.StartZ;
            startx = worker.DataTable.EndX - worker.DataTable.atomDim + 1;
            starty = worker.DataTable.EndY - worker.DataTable.atomDim + 1;
            startz = worker.DataTable.EndZ - worker.DataTable.atomDim + 1;

            // We want to determine the bounding data region covering all of the input points and their kernels
            string query = "";
            SqlCommand cmd;
            query = String.Format("SELECT reqseq, zindex, x, y, z FROM {0}", tempTable);
            cmd = new SqlCommand(query, contextConn);
            SqlDataReader InputTableDataReader = cmd.ExecuteReader();
            float x, y, z;
            int X, Y, Z;
            int lowx, lowy, lowz, highx, highy, highz;
            while (InputTableDataReader.Read())
            {
                reqseq = InputTableDataReader.GetSqlInt32(0).Value;
                x = InputTableDataReader.GetSqlSingle(2).Value;
                y = InputTableDataReader.GetSqlSingle(3).Value;
                z = InputTableDataReader.GetSqlSingle(4).Value;
                request = new MHDInputRequest(reqseq, x, y, z, result_size, 0);

                //input[reqseq] = request;
                input.Add(request);
                
                X = LagInterpolation.CalcNodeWithRound(x, worker.DataTable.Dx);
                Y = LagInterpolation.CalcNodeWithRound(y, worker.DataTable.Dx);
                Z = LagInterpolation.CalcNodeWithRound(z, worker.DataTable.Dx);
                lowx = X - worker.KernelSize / 2;
                lowx = ((lowx % worker.DataTable.GridResolutionX) + worker.DataTable.GridResolutionX) % worker.DataTable.GridResolutionX;
                highx = X + worker.KernelSize / 2;
                highx = ((highx % worker.DataTable.GridResolutionX) + worker.DataTable.GridResolutionX) % worker.DataTable.GridResolutionX;
                if (lowx <= worker.DataTable.StartX)
                {
                    if (worker.DataTable.StartX <= highx)
                        startx = worker.DataTable.StartX;
                }
                else if (lowx <= worker.DataTable.EndX)
                {
                    if (lowx < startx)
                        startx = lowx - lowx % worker.DataTable.atomDim;
                }
                else if (worker.DataTable.StartX <= highx && highx <= worker.DataTable.EndX)
                    startx = worker.DataTable.StartX;

                if (highx >= worker.DataTable.EndX)
                {
                    if (worker.DataTable.EndX >= lowx)
                        endx = worker.DataTable.EndX - worker.DataTable.atomDim + 1;
                }
                else if (highx >= worker.DataTable.StartX)
                {
                    if (highx > endx)
                        endx = highx - highx % worker.DataTable.atomDim;
                }
                else if (worker.DataTable.StartX <= lowx && lowx <= worker.DataTable.EndX)
                    endx = worker.DataTable.EndX - worker.DataTable.atomDim + 1;

                lowy = Y - worker.KernelSize / 2;
                lowy = ((lowy % worker.DataTable.GridResolutionX) + worker.DataTable.GridResolutionX) % worker.DataTable.GridResolutionX;
                highy = Y + worker.KernelSize / 2;
                highy = ((highy % worker.DataTable.GridResolutionX) + worker.DataTable.GridResolutionX) % worker.DataTable.GridResolutionX;
                if (lowy <= worker.DataTable.StartY)
                {
                    if (worker.DataTable.StartY <= highy)
                        starty = worker.DataTable.StartY;
                }
                else if (lowy <= worker.DataTable.EndY)
                {
                    if (lowy < starty)
                        starty = lowy - lowy % worker.DataTable.atomDim;
                }
                else if (worker.DataTable.StartY <= highy && highy <= worker.DataTable.EndY)
                    starty = worker.DataTable.StartY;

                if (highy >= worker.DataTable.EndY)
                {
                    if (worker.DataTable.EndY >= lowy)
                        endy = worker.DataTable.EndY - worker.DataTable.atomDim + 1;
                }
                else if (highy >= worker.DataTable.StartY)
                {
                    if (highy > endy)
                        endy = highy - highy % worker.DataTable.atomDim;
                }
                else if (worker.DataTable.StartY <= lowy && lowy <= worker.DataTable.EndY)
                    endy = worker.DataTable.EndY - worker.DataTable.atomDim + 1;

                lowz = Z - worker.KernelSize / 2;
                lowz = ((lowz % worker.DataTable.GridResolutionX) + worker.DataTable.GridResolutionX) % worker.DataTable.GridResolutionX;
                highz = Z + worker.KernelSize / 2;
                highz = ((highz % worker.DataTable.GridResolutionX) + worker.DataTable.GridResolutionX) % worker.DataTable.GridResolutionX;
                if (lowz <= worker.DataTable.StartZ)
                {
                    if (worker.DataTable.StartZ <= highz)
                        startz = worker.DataTable.StartZ;
                }
                else if (lowz <= worker.DataTable.EndZ)
                {
                    if (lowz < startz)
                        startz = lowz - lowz % worker.DataTable.atomDim;
                }
                else if (worker.DataTable.StartZ <= highz && highz <= worker.DataTable.EndZ)
                    startz = worker.DataTable.StartZ;

                if (highz >= worker.DataTable.EndZ)
                {
                    if (worker.DataTable.EndZ >= lowz)
                        endz = worker.DataTable.EndZ - worker.DataTable.atomDim + 1;
                }
                else if (highz >= worker.DataTable.StartZ)
                {
                    if (highz > endz)
                        endz = highz - highz % worker.DataTable.atomDim;
                }
                else if (worker.DataTable.StartZ <= lowz && lowz <= worker.DataTable.EndZ)
                    endz = worker.DataTable.EndZ - worker.DataTable.atomDim + 1;
            }
            InputTableDataReader.Close();
            
            if (startx > endx || starty > endy || startz > endz)
                throw new Exception("Given input not appropriate for this server!");
            
            xwidth = endx - startx + worker.DataTable.atomDim;
            ywidth = endy - starty + worker.DataTable.atomDim;
            zwidth = endz - startz + worker.DataTable.atomDim;
            
            string jointableName = "##query" + Guid.NewGuid().ToString().Replace("-", "");

            // Create the temporary table, in which the values are to be inserted
            SqlCommand command = new SqlCommand(String.Format(
                //"IF OBJECT_ID(N'tempdb..{0}') IS NOT NULL " +
                //"DROP TABLE {0} " +
                //"CREATE TABLE {0} (zindex bigint, dindex bigint)", jointableName), standardConn);
                "CREATE TABLE {0} (zindex bigint); CREATE CLUSTERED INDEX IDX_zindex ON {0}(zindex)  ", jointableName), standardConn);
            try
            {
                command.ExecuteNonQuery();
            }
            catch (System.Exception ex)
            {
                throw new Exception(String.Format("Could not create temporary table: {0}", ex.ToString()));
            }

            IDataReader JoinTableDataReader = SQLInterface.FilteringJoinTableDataReader.GetReader(startx, starty, startz, endx, endy, endz, worker.DataTable.atomDim);

            SqlBulkCopy bulkCopy = new SqlBulkCopy(standardConn);

            // set the destination table name
            bulkCopy.DestinationTableName = jointableName;

            // the batch size may have an effect on performance,
            // the optimal value may need to be determined empirically
            bulkCopy.BatchSize = 100000;
            bulkCopy.BulkCopyTimeout = 3600;

            try
            {
                bulkCopy.WriteToServer(JoinTableDataReader);
            }
            catch (System.Exception ex)
            {
                throw new Exception(String.Format("Could not write the temporary table to the server: {0}", ex.ToString()));
            }
            JoinTableDataReader.Close();
            return jointableName;
        }


        /// <summary>
        /// Raise an exception if the temporary table is not of the form #[0-9a-zA-Z]*.
        /// </summary>
        /// <remarks>TODO: Move into its own class and use with all database functions.</remarks>
        /// <param name="tempTable">Temporary table name</param>
        /// <returns>Temporary table name</returns>
        public static string SanitizeTemporaryTable(string tempTable)
        {
            // No longer require a temporary table (#) -- useful for testing.
            //if (!System.Text.RegularExpressions.Regex.IsMatch(tempTable, "^#[0-9a-zA-Z]*$"))
            if (!System.Text.RegularExpressions.Regex.IsMatch(tempTable, "^[#_0-9a-zA-Z]*$"))
            {
                throw new ArgumentException("Invalid temporary table name.", tempTable);
            }
            return tempTable;
        }

        public static string SanitizeDatabaseName(string database)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(database, "^[#0-9a-zA-Z]*$"))
            {
                throw new ArgumentException("Invalid database name.", database);
            }
            return database;
        }


        /// <summary>
        /// Return the number of rows in a temporary table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <remarks>
        /// SQL Server specific code to find the length of the table:
        /// SELECT rows FROM [tempdb].[sys].[sysindexes]
        /// WHERE id = OBJECT_ID('tempdb..#tablename') AND indid < 2
        /// </remarks>
        public static int GetTempTableLength(string tableName)
        {
            int count = 0;
            SqlConnection conn = new SqlConnection("context connection=true");
            conn.Open();
            SqlCommand cmd = null;
            if (tableName[0] == '#')
            {
                // Find the length of a temporary table in sysindexes
                cmd = new SqlCommand(String.Format("SELECT rows FROM [tempdb].[sys].[sysindexes] "
                    + " WHERE id = OBJECT_ID('tempdb..{0}') AND indid < 2", tableName),
                    conn);
            }
            else
            {
                // Other tables may be used too, most likely for testing.
                // It is easier to use COUNT (*) than to figure out which database they are in.
                cmd = new SqlCommand(String.Format("SELECT COUNT(*) FROM {0}", tableName), conn);
            }
            SqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            count = reader.GetSqlInt32(0).Value;
            reader.Close();
            conn.Close();
            return count;
        }


        /// <summary>
        /// Return the number of distinct points in a temporary table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <remarks>
        /// </remarks>
        public static int GetInputSize(string tableName)
        {
            int count = 0;
            SqlConnection conn = new SqlConnection("context connection=true");
            conn.Open();
            SqlCommand cmd = new SqlCommand(String.Format("SELECT DISTINCT reqseq FROM {0}", tableName), conn);
            SqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            count = reader.GetSqlInt32(0).Value;
            reader.Close();
            conn.Close();
            return count;
        }


        /// <summary>
        /// Read the request table into a number of arrays for each column
        /// </summary>
        public static void ReadTemporaryTable(string tempTable, int[] req,
                                              long[] blobs,
                                              float[] ux,
                                              float[] uy,
                                              float[] uz)
        {
            tempTable = SanitizeTemporaryTable(tempTable);

            SqlConnection conn = new SqlConnection("context connection=true");
            conn.Open();
            SqlCommand cmd = new SqlCommand(String.Format("SELECT reqseq, zindex , x, y, z FROM {0}", tempTable), conn);
            SqlDataReader reader = cmd.ExecuteReader();

            int i = 0;
            while (reader.Read())
            {
                req[i] = reader.GetSqlInt32(0).Value;
                blobs[i] = reader.GetSqlInt64(1).Value;
                ux[i] = reader.GetSqlSingle(2).Value;
                uy[i] = reader.GetSqlSingle(3).Value;
                uz[i] = reader.GetSqlSingle(4).Value;

                i++;
            }
            reader.Close();
            conn.Close();
        }


        /// <summary>
        /// Round to the nearest timestep stored in the database.
        /// </summary>
        /// <remarks>FIXME: Query database instead of using hard-coded rounding.</remarks>
        /// <param name="time">time</param>
        /// <param name="table">data description table</param>
        /// <returns></returns>
        public static int GetNearestTimestep(float time, TurbDataTable table)
        {
            float timestep = time / table.Dt;
            return (int)Math.Round(timestep / table.TimeInc) * table.TimeInc + table.TimeOff;

        }

        /// <summary>
        /// Floor to the nearest timestep stored in the database.
        /// </summary>
        /// <remarks>FIXME: Query database instead of using hard-coded rounding.</remarks>
        /// <param name="time">time</param>
        /// <param name="table">data description table</param>
        /// <returns></returns>
        public static int GetFlooredTimestep(float time, TurbDataTable table)
        {
            float timestep = time / table.Dt;
            return (int)Math.Floor(timestep / table.TimeInc) * table.TimeInc + table.TimeOff;
        }

        /// <summary>
        /// Determine which points should be used for interpolation
        /// </summary>
        /// <remarks>
        /// Currently coded with static knowledge of the data.
        /// FIXME: This needs to query the database to get a list of times.
        /// </remarks>
        /// <param name="time">Time to be interpolated</param>
        /// <param name="table">Information regarding the current data set.</param>
        /// <returns>Array of 'count' nearest points</returns>
        /* public static int[] GetNearestTimesteps(int itime, int count, TurbDataTable table)
        {
            if (count != 4)
            {
                throw new Exception("Only 4 nearest neighbors are currently supported");
            }
            int[] times = new int[count];

            if (itime == 99) // special case for 99
            {
                times = new int[] { 98, 99, 100, 110 };
            }
            else
            {
                if (itime < 100)
                {
                    times[1] = itime;
                    times[0] = times[1] - 1;
                    times[2] = times[1] + 1;
                    times[3] = times[1] + 1;
                }
                else
                {
                    times[1] = itime - (itime % 10);
                    times[0] = times[1] - 10;
                    times[2] = times[1] + 10;
                    times[3] = times[1] + 10;
                }
            }

            return times;
        }*/

    }

}
