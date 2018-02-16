using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib.DataTypes;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using Turbulence.SQLInterface;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public partial class StoredProcedures
{
    /// <summary>
    /// A single interface to multiple database functions.
    /// 
    /// This is currently a mess and should be cleaned up, but
    /// at this point we do have the majority of the unique logic
    /// for each of the calculation functions removed.
    /// </summary>
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void ExecuteParticleTrackingChannelWorkerTaskParallel(
        string codedb,
        string turbinfodb,
        string turbinfoserver,
        string localServer,
        string localDatabase,
        short datasetID,
        string tableName,
        int atomDim,
        int workerType,
        int spatialInterp,          // TurbulenceOptions.SpatialInterpolation
        int temporalInterp,         // TurbulenceOptions.TemporalInterpolation
        int inputSize,
        string tempTable,
        float time,
        float endTime,
        float dt)
    {
        //TimeSpan IOTime = new TimeSpan(0), preProcessTime = new TimeSpan(0), resultTime = new TimeSpan(0),
        //    MemoryTime = new TimeSpan(0), resultSendingTime = new TimeSpan(0),
        //    ReadTempTableGetCubesToRead = new TimeSpan(0), GetCubesForEachPoint = new TimeSpan(0);
        //DateTime startTimer, endTimer, initialTimeStamp;

        //initialTimeStamp = startTimer = DateTime.Now;

        string localServerCleanName = localServer;
        if (localServer.Contains("_"))
            localServerCleanName = localServer.Remove(localServer.IndexOf("_"));            

        List<string> servers = new List<string>();
        List<string> databases = new List<string>();
        List<string> codeDatabase = new List<string>();
        List<ServerBoundaries> serverBoundaries = new List<ServerBoundaries>();
        List<SqlConnection> connections = new List<SqlConnection>();

        SqlConnection contextConn;
        contextConn = new SqlConnection("context connection=true");

        TurbServerInfo serverinfo = TurbServerInfo.GetTurbServerInfo(codedb, turbinfodb, turbinfoserver);

        String cString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverinfo.infoDB_server, serverinfo.infoDB);
        SqlConnection turbinfoConn = new SqlConnection(cString);
        turbinfoConn.Open();
        SqlCommand cmd = turbinfoConn.CreateCommand();
        string DBMapTable = "DatabaseMap";
        //if (development == true)
        //{
        //    DBMapTable = "DatabaseMapTest";
        //}
        cmd.CommandText = String.Format("select ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, MIN(minLim) as minLim, MAX(maxLim) as maxLim, MIN(minTime) as minTime, MAX(maxTime) as maxTime " +
            "from {0}..{1} where DatasetID = @datasetID " +
            "group by ProductionMachineName, ProductionDatabaseName, CodeDatabaseName " +
            "order by minLim", serverinfo.infoDB, DBMapTable);
        cmd.Parameters.AddWithValue("@datasetID", datasetID);
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    String serverName = reader.GetString(0);
                    String DBName = reader.GetString(1);
                    servers.Add(serverName);
                    databases.Add(DBName);
                    //if (development == false)
                    //{
                    //    codeDatabase.Add(reader.GetString(2));
                    //}
                    //else
                    //{
                    //    codeDatabase.Add("turblib_test");
                    //}
                    long minLim = reader.GetSqlInt64(3).Value;
                    long maxLim = reader.GetSqlInt64(4).Value;
                    int minTime = reader.GetInt32(5);
                    int maxTime = reader.GetInt32(6);
                    serverBoundaries.Add(new ServerBoundaries(new Morton3D(minLim), new Morton3D(maxLim), minTime, maxTime));
                    if (serverName.CompareTo(localServerCleanName) == 0)
                    {
                        // We'll use the context connection in this case.
                        connections.Add(contextConn);
                    }
                    else
                    {
                        connections.Add(new SqlConnection(String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName, DBName)));
                    }
                }
            }
            else
            {
                throw new Exception("Invalid dataset specified.");
            }
        }
        turbinfoConn.Close();

        // Load information about the requested dataset
        TurbDataTable table = TurbDataTable.GetTableInfo(localServerCleanName, localDatabase, tableName, atomDim, serverinfo);

        // Instantiate a worker class
        //Turbulence.SQLInterface.workers.GetPositionWorker worker =
        //    new Turbulence.SQLInterface.workers.GetPositionWorker(table,
        //        (TurbulenceOptions.SpatialInterpolation)spatialInterp,
        //        (TurbulenceOptions.TemporalInterpolation)temporalInterp);
        // PJ 2015
        contextConn.Open();
        GridPoints gridPointsY = new GridPoints(table.GridResolutionY);
        gridPointsY.GetGridPointsFromDB(contextConn, localDatabase.ToString());
        double[] grid_points_y = new double[table.GridResolutionY];
        for (int i = 0; i < table.GridResolutionY; i++)
        {
            grid_points_y[i] = gridPointsY.GetGridValue(i);
        }

        Turbulence.SQLInterface.workers.GetChannelPositionWorker worker =
            new Turbulence.SQLInterface.workers.GetChannelPositionWorker(databases[0], table,
                (TurbulenceOptions.SpatialInterpolation)spatialInterp,
                (TurbulenceOptions.TemporalInterpolation)temporalInterp,
                contextConn);

        float points_per_cube = 0;

        Dictionary<long, List<int>>[] map;
        Dictionary<int, SQLUtility.TrackingInputRequest> input = new Dictionary<int, SQLUtility.TrackingInputRequest>(inputSize);

        // Read input data
        bool all_done = false;
        int baseTimeStep;
        int number_of_crossings = 0;
        if (temporalInterp == (int)TurbulenceOptions.TemporalInterpolation.None)
            baseTimeStep = SQLUtility.GetNearestTimestep(time, table);
        else
            baseTimeStep = SQLUtility.GetFlooredTimestep(time, table);
        int nextTimeStep = baseTimeStep;
        float nextTime = time;
        map = ReadTempTableGetAtomsToRead(tempTable, worker, contextConn, input, serverBoundaries, ref number_of_crossings);
        cmd = new SqlCommand(String.Format(@"DELETE FROM {0}", tempTable), contextConn);
        try
        {
            cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            throw new Exception(String.Format("Error deleting from temporary table.  [Inner Exception: {0}])",
                e.ToString()));
        }
        //contextConn.Close();

        //endTime = DateTime.Now;
        //ReadTempTableGetCubesToRead += endTime - startTime;
        
        // Output SQL column names
        //worker.SendSqlOutputHeaders();

        //SqlDataReader reader;
        //SqlDataRecord record = new SqlDataRecord(worker.GetRecordMetaData());
        //SqlContext.Pipe.SendResultsStart(record);

        SqlDataRecord record;

        byte[] rawdata = new byte[table.BlobByteSize];
                
        //endTime = DateTime.Now;
        //preProcessTime += endTime - initialTimeStamp;

        //startTime = endTime;

        //record = new SqlDataRecord(new SqlMetaData[] {
        //            new SqlMetaData("ReadTempTableGetCubesToRead Time", SqlDbType.Float),
        //            new SqlMetaData("GetCubesForEachPoint Time", SqlDbType.Float),
        //            new SqlMetaData("PreProcess Time", SqlDbType.Float) });
        //SqlContext.Pipe.SendResultsStart(record);

        //record.SetDouble(0, ReadTempTableGetCubesToRead.TotalSeconds);
        //record.SetDouble(1, GetCubesForEachPoint.TotalSeconds);
        //record.SetDouble(2, preProcessTime.TotalSeconds);
        //SqlContext.Pipe.SendResultsRow(record);
        //SqlContext.Pipe.SendResultsEnd();

        //cmd = new SqlCommand(@"SET STATISTICS IO ON;", conn);
        //cmd.CommandType = CommandType.Text;
        //cmd.ExecuteNonQuery();

        SqlConnection standardConn;
        string connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", localServerCleanName, localDatabase);
        standardConn = new SqlConnection(connString);
        standardConn.Open();

        record = new SqlDataRecord(GetChannelRecordMetaData());
        SqlContext.Pipe.SendResultsStart(record);

        SQLUtility.TrackingInputRequest point = new SQLUtility.TrackingInputRequest();
        // Bitmask to ignore low order bits of address
        long mask = ~(long)(worker.DataTable.atomDim * worker.DataTable.atomDim * worker.DataTable.atomDim - 1);
        
        List<int> done_points = new List<int>();

        if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.None)
        {
            contextConn.Close();
            contextConn.Dispose();
            standardConn.Close();
            standardConn.Dispose();
            map = null;
            input = null;
            rawdata = null;
            worker = null;
            throw new Exception("Temporal interpolation must be used when performing particle tracking!");
        }
        else if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.PCHIP)
        {
            // Perform PCHIP interpolation by querying & grouping 4 timesteps by location
            // We process the results from the database as they come out, and then calculate PCHIP at the end

            TurbulenceBlob blob = new TurbulenceBlob(table);
            float priordt = 0; //used to store prior DT for final corrector step.
            int failsafe = 0;
            while (!all_done)
            {
                failsafe = failsafe + 1; /*Just to prevent a server lockup */
                if (failsafe > 100000)
                {
                    throw new Exception("Looped too many times, aborting.  Failsafe = " + failsafe.ToString() + " Time is " + time.ToString());
                }
                all_done = true;
                //Go through each server and request the data.
                for (int s = 0; s < servers.Count; s++)
                {
                    if (map[s].Count > 0)
                    {
                        if (connections[s].State != ConnectionState.Open)
                        {
                            connections[s].Open();
                        }

                        //tableName = String.Format("{0}.dbo.{1}", localDatabase, table.TableName);

                        //Create a table to perform query via a JOIN
                        //string joinTable = SQLUtility.CreateTemporaryJoinTable(map[s].Keys, (TurbulenceOptions.TemporalInterpolation)temporalInterp, table.TimeInc, connections[s], points_per_cube);
                        string joinTable;
                        if (servers[s] == localServerCleanName)
                        {
                            joinTable = SQLUtility.CreateTemporaryJoinTable(map[s].Keys, standardConn, points_per_cube);
                        }
                        else
                        {
                            joinTable = SQLUtility.CreateTemporaryJoinTable(map[s].Keys, connections[s], points_per_cube);
                        }
                        
                        int timestep0 = baseTimeStep - table.TimeInc;
                        int timestep1 = baseTimeStep;
                        int timestep2 = baseTimeStep + table.TimeInc;
                        int timestep3 = baseTimeStep + table.TimeInc * 2;

                        tableName = string.Format("{0}.dbo.{1}", databases[s], table.TableName);
                        
                        //endTimer = DateTime.Now;
                        //preprocessTime += endTimer - startTimer;
                        //startTimer = endTimer;

                        string query = String.Format(@"declare @times table (timestep int not null) " +
                                "insert @times values ({2}) " +
                                "insert @times values ({3}) " +
                                "insert @times values ({4}) " +
                                "insert @times values ({5}) " +

                                "select d.timestep, d.zindex, d.data " +
                                "from @times as t, {0} as d, {1} as j " +
                                "where d.timestep = t.timestep and d.zindex = j.zindex ",
                          tableName, joinTable, timestep0, timestep1, timestep2, timestep3);

                        //string query = String.Format(@"DECLARE @times table (timestep int NOT NULL) " +
                        //        "INSERT @times VALUES ({2}) " +
                        //        "INSERT @times VALUES ({3}) " +
                        //        "INSERT @times VALUES ({4}) " +
                        //        "INSERT @times VALUES ({5}) " +

                        //        "SELECT d.timestep, d.zindex, d.data " +
                        //        "FROM @times as t, {0} as d, {1} as j " +
                        //        "WHERE d.timestep = t.timestep AND d.zindex = j.zindex ",
                        //  tableName, joinTable, timestep0, timestep1, timestep2, timestep3);
                        //for (int s = 0; s < servers.Count; s++)
                        //{
                        //    query += String.Format(@"UNION ALL " +
                        //        "SELECT d.timestep, d.zindex, d.data " +
                        //        "FROM @times as t, [{0}].[{1}].dbo.[{2}] as d, {3} as j " +
                        //        "WHERE d.timestep = t.timestep AND d.zindex = j.zindex ", 
                        //        servers[s], databases[s], table.TableName, joinTable);
                        //}
                        cmd = new SqlCommand(query, connections[s]);
                        cmd.CommandTimeout = 3600;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                //int basetime = reader.GetSqlInt32(0).Value;  // Base time
                                int timestepRead = reader.GetSqlInt32(0).Value;  // Timestep returned
                                long thisBlob = reader.GetSqlInt64(1).Value; // Blob returned
                                int bytesread = 0;

                                while (bytesread < table.BlobByteSize)
                                {
                                    int bytes = (int)reader.GetBytes(2, table.SqlArrayHeaderSize, rawdata, bytesread, table.BlobByteSize - bytesread);
                                    bytesread += bytes;
                                }
                                blob.Setup(timestepRead, new Morton3D(thisBlob), rawdata);
                                //endTimer = DateTime.Now;
                                //IOTime += endTimer - startTimer;
                                //startTimer = endTimer;

                                foreach (int i in map[s][thisBlob])
                                {
                                    point = input[i];

                                    if (worker == null)
                                        throw new Exception("worker is NULL!");
                                    if (blob == null)
                                        throw new Exception("blob is NULL!");

                                    point.cubesRead++;
                                    worker.GetResult(blob, ref point, timestepRead, baseTimeStep, time, endTime, dt, ref nextTimeStep, ref nextTime, ref priordt, grid_points_y, databases[s]);
                                }

                                //endTimer = DateTime.Now;
                                //computeTime += endTimer - startTimer;
                                //startTimer = endTimer;
                            }
                        }

                        cmd = new SqlCommand(String.Format(@"DROP TABLE tempdb..{0}", joinTable), connections[s]);
                        try
                        {
                            cmd.CommandTimeout = 600;
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            throw new Exception(String.Format("Error dropping temporary join table.  [Inner Exception: {0}])",
                                e.ToString()));
                        }

                        map[s].Clear();
                    }
                }

                // create new map
                foreach (int input_point in input.Keys)
                {
                    if (input[input_point].done)
                    {
                        done_points.Add(input_point);
                        continue;
                    }
                    // reset the velocity increment
                    input[input_point].vel_inc.x = 0.0f;
                    input[input_point].vel_inc.y = 0.0f;
                    input[input_point].vel_inc.z = 0.0f;
                    input[input_point].cubesRead = 0;
                    input[input_point].numberOfCubes = 0;
                    input[input_point].lagInt = null;
                    AddChannelRequestToMap(ref map, input[input_point], worker, mask, serverBoundaries, ref number_of_crossings);
                    all_done = false;
                }
                foreach (int done_point in done_points)
                {
                    GenerateChannelResultRowFinalPosition(record, input[done_point], endTime, localDatabase);
                    input.Remove(done_point);
                }
                done_points.Clear();

                time = nextTime;
                baseTimeStep = nextTimeStep;
            }
            // Encourage garbage collector to clean up.
            blob = null;
        }
        else
        {
            contextConn.Close();
            contextConn.Dispose();
            standardConn.Close();
            standardConn.Dispose();
            map = null;
            input = null;
            rawdata = null;
            worker = null;
            throw new Exception("Unsupported TemporalInterpolation Type");
        }

        SqlContext.Pipe.SendResultsEnd();

        record = new SqlDataRecord(new SqlMetaData[] {
            new SqlMetaData("number_of_crossings", SqlDbType.Int)
        });
        SqlContext.Pipe.SendResultsStart(record);
        record.SetInt32(0, number_of_crossings);
        SqlContext.Pipe.SendResultsRow(record);
        SqlContext.Pipe.SendResultsEnd();

//        endTimer = DateTime.Now;
//        resultSendingTime += endTimer - startTimer;

//        record = new SqlDataRecord(GetSqlMetaData());
//        SqlContext.Pipe.SendResultsStart(record);

//        record.SetInt32(0, scan_count);
//        record.SetInt32(1, logical_reads);
//        record.SetInt32(2, physical_reads);
//        record.SetInt32(3, read_ahead_reads);
//        record.SetDouble(4, preProcessTime.TotalSeconds);
//        record.SetDouble(5, IOTime.TotalSeconds);
//        record.SetDouble(6, MemoryTime.TotalSeconds);
//        record.SetDouble(7, resultTime.TotalSeconds);
//        record.SetDouble(8, resultSendingTime.TotalSeconds);
//        record.SetDouble(9, (endTimer - initialTimeStamp).TotalSeconds);
//#if MEMORY
//                record.SetInt32(10, memory_bandwidth);
//                record.SetFloat(11, points_per_cube);
//#endif
//        SqlContext.Pipe.SendResultsRow(record);

//        SqlContext.Pipe.SendResultsEnd();

        for (int i = 0; i < connections.Count; i++)
        {
            if (connections[i].State == ConnectionState.Open)
            {
                connections[i].Close();
                connections[i].Dispose();
            }
            map[i].Clear();
        }

        contextConn.Close();
        contextConn.Dispose();
        standardConn.Close();
        standardConn.Dispose();
        map = null;
        input = null;
        rawdata = null;
        worker = null;

        scan_count = 0;
        logical_reads = 0;
        physical_reads = 0;
        read_ahead_reads = 0;

        // We should not have to manually call the garbage collector.
        // System.GC.Collect();
    }

    private static Dictionary<long, List<int>>[] ReadTempTableGetAtomsToRead(
        string tempTable,
        Turbulence.SQLInterface.workers.GetChannelPositionWorker worker,
        SqlConnection conn,
        Dictionary<int, SQLUtility.TrackingInputRequest> input,
        List<ServerBoundaries> serverBoundaries,
        ref int number_of_crossings)
    {
        Dictionary<long, List<int>>[] map = new Dictionary<long, List<int>>[serverBoundaries.Count];
        for (int i = 0; i < serverBoundaries.Count; i++)
        {
            map[i] = new Dictionary<long, List<int>>();
        }

        tempTable = SQLUtility.SanitizeTemporaryTable(tempTable);
        SQLUtility.TrackingInputRequest request;

        // Bitmask to ignore low order bits of address
        long mask = ~(long)(worker.DataTable.atomDim * worker.DataTable.atomDim * worker.DataTable.atomDim - 1);

        //long zindex = 0;
        int result_size = worker.GetResultSize();
        int reqseq = -1;

        string query = "";
        SqlCommand cmd;
        //query += String.Format("SELECT reqseq, timestep, zindex, x, y, z, pre_x, pre_y, pre_z, time, endTime, dt, compute_predictor FROM {0}", tempTable);
        query += String.Format("SELECT reqseq, zindex, x, y, z FROM {0}", tempTable);
        cmd = new SqlCommand(query, conn);
        SqlDataReader reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            reqseq = reader.GetSqlInt32(0).Value;
            request = new SQLUtility.TrackingInputRequest(
                reqseq,
                reader.GetSqlInt64(1).Value,
                new Point3(reader.GetSqlSingle(2).Value, reader.GetSqlSingle(3).Value, reader.GetSqlSingle(4).Value),
                new Point3(),
                new Vector3(),
                true);

            input[reqseq] = request;

            AddChannelRequestToMap(ref map, request, worker, mask, serverBoundaries, ref number_of_crossings);
        }
        reader.Close();

        return map;
    }

    //TODO: Consider moving this method to the worker.
    private static void AddChannelRequestToMap(ref Dictionary<long, List<int>>[] map, SQLUtility.TrackingInputRequest request,
        Turbulence.SQLInterface.workers.GetChannelPositionWorker worker, long mask,
        List<ServerBoundaries> serverBoundaries, ref int number_of_crossings)
    {
        long zindex = 0;

        zindex = request.zindex & mask;

        if (worker.spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
        {
            AddChannelRequestToServerMap(ref map, request, zindex, serverBoundaries);
        }
        else
        {
            int X, Y, Z; float yp;
            if (request.compute_predictor)
            {
                yp = request.pos.y;
                //X = LagInterpolation.CalcNode(request.pos.x, worker.setInfo.Dx);
                //Y = LagInterpolation.CalcNode(request.pos.y, worker.setInfo.Dy);
                //Z = LagInterpolation.CalcNode(request.pos.z, worker.setInfo.Dz);
                X = worker.setInfo.CalcNodeX(request.pos.x, worker.spatialInterp);
                Y = worker.setInfo.CalcNodeY(request.pos.y, worker.spatialInterp);
                Z = worker.setInfo.CalcNodeZ(request.pos.z, worker.spatialInterp);
            }
            else
            {
                yp = request.pre_pos.y;
                //X = LagInterpolation.CalcNode(request.pre_pos.x, worker.setInfo.Dx);
                //Y = LagInterpolation.CalcNode(request.pre_pos.y, worker.setInfo.Dy);
                //Z = LagInterpolation.CalcNode(request.pre_pos.z, worker.setInfo.Dz);
                X = worker.setInfo.CalcNodeX(request.pre_pos.x, worker.spatialInterp);
                Y = worker.setInfo.CalcNodeY(request.pre_pos.y, worker.spatialInterp);
                Z = worker.setInfo.CalcNodeZ(request.pre_pos.z, worker.spatialInterp);
            }
            // For Lagrange Polynomial interpolation we need a cube of data
            //int startz = Z - worker.KernelSize / 2 + 1, starty = Y - worker.KernelSize / 2 + 1, startx = X - worker.KernelSize / 2 + 1;
            //int endz = Z + worker.KernelSize / 2, endy = Y + worker.KernelSize / 2, endx = X + worker.KernelSize / 2;
            // PJ 2016 bug fix: getting the correct stencil for channel
            int startz = worker.GetStencilStartZ(Z, worker.KernelSize);
            int starty = worker.GetStencilStartY(Y, worker.KernelSize);
            int startx = worker.GetStencilStartX(X, worker.KernelSize);
            int endz = worker.periodicZ ? startz + worker.KernelSize - 1 : worker.GetStencilEndZ(Z, worker.KernelSize);
            int endy = worker.periodicX ? starty + worker.KernelSize - 1 : worker.GetStencilEndY(Y, worker.KernelSize);
            int endx = worker.periodicX ? startx + worker.KernelSize - 1 : worker.GetStencilEndX(X, worker.KernelSize);

            // we do not want a request to appear more than once in the list for an atom
            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom
            startz = startz - ((startz % worker.setInfo.atomDim) + worker.setInfo.atomDim) % worker.setInfo.atomDim;
            starty = starty - ((starty % worker.setInfo.atomDim) + worker.setInfo.atomDim) % worker.setInfo.atomDim;
            startx = startx - ((startx % worker.setInfo.atomDim) + worker.setInfo.atomDim) % worker.setInfo.atomDim;

            int assigned_server = -1;
            bool crossing = false;
            for (int z = startz; z <= endz; z += worker.setInfo.atomDim)
            {
                for (int y = starty; y <= endy; y += worker.setInfo.atomDim)
                {
                    for (int x = startx; x <= endx; x += worker.setInfo.atomDim)
                    {
                        // Wrap the coordinates into the grid space
                        int xi = ((x % worker.setInfo.GridResolutionX) + worker.setInfo.GridResolutionX) % worker.setInfo.GridResolutionX;
                        // shouldn't need to wrap in y
                        int yi = ((y % worker.setInfo.GridResolutionY) + worker.setInfo.GridResolutionY) % worker.setInfo.GridResolutionY;
                        if (yi != y)
                        {
                            continue; // no need to add request to server map
                        }
                        int zi = ((z % worker.setInfo.GridResolutionZ) + worker.setInfo.GridResolutionZ) % worker.setInfo.GridResolutionZ;

                        zindex = new Morton3D(zi, yi, xi).Key & mask;

                        int server = AddChannelRequestToServerMap(ref map, request, zindex, serverBoundaries);

                        if (assigned_server != -1 && assigned_server != server)
                        {
                            crossing = true;
                        }
                        assigned_server = server;
                    }
                }
            }
            if (crossing)
            {
                number_of_crossings++;
            }
        }
    }

    private static int AddChannelRequestToServerMap(ref Dictionary<long, List<int>>[] map, 
        SQLUtility.TrackingInputRequest request,
        long zindex,
        List<ServerBoundaries> serverBoundaries)
    {                
        //if (!map.ContainsKey(zindex))
        //{
        //    map[zindex] = new List<int>();
        //}
        //map[zindex].Add(request.request);
        //request.numberOfCubes++;

        for (int i = 0; i < serverBoundaries.Count; i++)
        {
            //NOTE: We assume each node stores a contiguous range of zindexes.
            if (serverBoundaries[i].startKey <= zindex && zindex <= serverBoundaries[i].endKey)
            {
                if (!map[i].ContainsKey(zindex))
                {
                    map[i][zindex] = new List<int>();
                }
                map[i][zindex].Add(request.request);
                request.numberOfCubes++;
                return i;
            }
        }
        return -1;
    }

    static void GenerateChannelResultRowFinalPosition(SqlDataRecord record, SQLUtility.TrackingInputRequest temp_point, float endTime, string database)
    {
        record.SetInt32(0, temp_point.request);
        if (database.Contains("channel"))
        {
            record.SetFloat(1, temp_point.pos.x + 0.45f * endTime); // PJ 2015... add shift at end back to physical location
        }
        else
        {
            record.SetFloat(1, temp_point.pos.x); // PJ 2015... add shift at end back to physical location
        }
        record.SetFloat(2, temp_point.pos.y);
        record.SetFloat(3, temp_point.pos.z);
        SqlContext.Pipe.SendResultsRow(record);
    }

    static SqlMetaData[] GetChannelRecordMetaData()
    {
        return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("X", SqlDbType.Real),
                new SqlMetaData("Y", SqlDbType.Real),
                new SqlMetaData("Z", SqlDbType.Real)
            };
    }
};
