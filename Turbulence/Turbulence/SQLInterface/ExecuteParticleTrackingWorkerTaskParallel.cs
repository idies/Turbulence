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
    public static void ExecuteParticleTrackingWorkerTaskParallel(
        string turbinfoServer,
        string turbinfoDB,
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
        bool development)
    {
        //TimeSpan IOTime = new TimeSpan(0), preProcessTime = new TimeSpan(0), resultTime = new TimeSpan(0),
        //    MemoryTime = new TimeSpan(0), resultSendingTime = new TimeSpan(0),
        //    ReadTempTableGetCubesToRead = new TimeSpan(0), GetCubesForEachPoint = new TimeSpan(0);
        //DateTime startTime, endTime, initialTimeStamp;

        //initialTimeStamp = startTime = DateTime.Now;

        List<string> servers = new List<string>();
        List<string> databases = new List<string>();
        List<string> codeDatabase = new List<string>();
        List<ServerBoundaries> serverBoundaries = new List<ServerBoundaries>();
        List<SqlConnection> connections = new List<SqlConnection>();

        String cString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", turbinfoServer, turbinfoDB);
        SqlConnection turbinfoConn = new SqlConnection(cString);
        turbinfoConn.Open();
        SqlCommand cmd = turbinfoConn.CreateCommand();
        cmd.CommandText = String.Format("select ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, MIN(minLim) as minLim, MAX(maxLim) as maxLim " +
            "from {0}..DatabaseMap where DatasetID = @datasetID " +
            "group by ProductionMachineName, ProductionDatabaseName, CodeDatabaseName " +
            "order by minLim", turbinfoDB);
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
                    if (development == false)
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
                    connections.Add(new SqlConnection(String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName, DBName)));
                }
            }
            else
            {
                throw new Exception("Invalid dataset specified.");
            }
        }
        turbinfoConn.Close();

        SqlConnection contextConn;
        contextConn = new SqlConnection("context connection=true");
        contextConn.Open();
        
        // Load information about the requested dataset
        TurbDataTable table = TurbDataTable.GetTableInfo(localServer, localDatabase, tableName, atomDim, contextConn);

        tableName = String.Format("{0}.dbo.{1}", localDatabase, table.TableName);

        // Instantiate a worker class
        Turbulence.SQLInterface.workers.GetPositionWorker worker =
            new Turbulence.SQLInterface.workers.GetPositionWorker(table,
                (TurbulenceOptions.SpatialInterpolation)spatialInterp,
                (TurbulenceOptions.TemporalInterpolation)temporalInterp);

        float points_per_cube = 0;

        Dictionary<SQLUtility.TimestepZindexKey, List<int>>[] map;
        Dictionary<int, SQLUtility.TrackingInputRequest> input = new Dictionary<int, SQLUtility.TrackingInputRequest>(inputSize);

        // Read input data
        bool all_done = false;
        map = ReadTempTableGetAtomsToRead(tempTable, worker, contextConn, input, serverBoundaries);
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
        contextConn.Close();

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
        //float[] result;

        record = new SqlDataRecord(worker.GetRecordMetaData());
        SqlContext.Pipe.SendResultsStart(record);

        SQLUtility.TimestepZindexKey key = new SQLUtility.TimestepZindexKey();
        SQLUtility.TrackingInputRequest point = new SQLUtility.TrackingInputRequest();
        // Bitmask to ignore low order bits of address
        long mask = ~(long)(worker.DataTable.atomDim * worker.DataTable.atomDim * worker.DataTable.atomDim - 1);
        //List<int> done_points = new List<int>();

        //throw new Exception(zindexRegionsString);
        if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.None)
        {
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

            while (!all_done)
            {
                all_done = true;
                // Go through each server and request the data.
                for (int s = 0; s < servers.Count; s++)
                {
                    if (map[s].Count > 0)
                    {
                        if (connections[s].State != ConnectionState.Open)
                        {
                            connections[s].Open();
                        }

                        //Create a table to perform query via a JOIN
                        string joinTable = SQLUtility.CreateTemporaryJoinTable(map[s].Keys, (TurbulenceOptions.TemporalInterpolation)temporalInterp, table.TimeInc, connections[s], points_per_cube);
                        
                        cmd = new SqlCommand(
                            String.Format(@"SELECT {1}.basetime, {0}.timestep, {0}.zindex, {0}.data " +
                                            "FROM {0}, {1} " +
                                            "WHERE {0}.timestep = {1}.timestep AND {0}.zindex = {1}.zindex",
                                      tableName, joinTable),
                                      connections[s]);
                        cmd.CommandTimeout = 3600;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int basetime = reader.GetSqlInt32(0).Value;  // Base time
                                int timestep = reader.GetSqlInt32(1).Value;  // Timestep returned
                                long thisBlob = reader.GetSqlInt64(2).Value; // Blob returned
                                key.SetValues(basetime, thisBlob);
                                int bytesread = 0;

                                while (bytesread < table.BlobByteSize)
                                {
                                    //int bytes = (int)reader.GetBytes(2, 0, rawdata, bytesread, table.BlobByteSize - bytesread);
                                    int bytes = (int)reader.GetBytes(3, table.SqlArrayHeaderSize, rawdata, bytesread, table.BlobByteSize - bytesread);
                                    bytesread += bytes;
                                }
                                blob.Setup(timestep, new Morton3D(thisBlob), rawdata);

                                //for (int i = 0; i < map[key].Count; i++)
                                foreach (int i in map[s][key])
                                {
                                    //point = input[map[key][i]];
                                    point = input[i];

                                    //// If a particle has crossed the server boundary and this is not the first iteration
                                    //// it should be sent back to the Web-server for reassignment without computing it's partial position
                                    //// If this is the first iteration it was assigned to all of the servers that have data for it
                                    //// so we should compute it's partial position (the results will be added at the Web-server)
                                    //if (point.crossed_boundary && iteration_number > 0)
                                    //{
                                    //    continue;
                                    //}

                                    if (worker == null)
                                        throw new Exception("worker is NULL!");
                                    if (blob == null)
                                        throw new Exception("blob is NULL!");

                                    point.cubesRead++;
                                    worker.GetResult(blob, ref point, timestep, basetime);
                                }
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
                        GenerateResultRow(record, input[input_point]);
                        input.Remove(input_point);
                        continue;
                    }
                    // reset the velocity increment
                    input[input_point].vel_inc.x = 0.0f;
                    input[input_point].vel_inc.y = 0.0f;
                    input[input_point].vel_inc.z = 0.0f;
                    input[input_point].cubesRead = 0;
                    input[input_point].numberOfCubes = 0;
                    input[input_point].lagInt = null;
                    AddRequestToMap(ref map, input[input_point], worker, mask, serverBoundaries);
                    all_done = false;
                }
            }
            // Encourage garbage collector to clean up.
            blob = null;
        }
        else
        {
            map = null;
            input = null;
            rawdata = null;
            worker = null;
            throw new Exception("Unsupported TemporalInterpolation Type");
        }

        SqlContext.Pipe.SendResultsEnd();

        //endTime = DateTime.Now;

        //resultSendingTime += endTime - startTime;

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
        //        record.SetDouble(9, (endTime - initialTimeStamp).TotalSeconds);
        //#if MEMORY
        //        record.SetInt32(10, memory_bandwidth);
        //        record.SetFloat(11, points_per_cube);
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

    private static Dictionary<SQLUtility.TimestepZindexKey, List<int>>[] ReadTempTableGetAtomsToRead(
        string tempTable,
        Turbulence.SQLInterface.workers.GetPositionWorker worker,
        SqlConnection conn,
        Dictionary<int, SQLUtility.TrackingInputRequest> input,
        List<ServerBoundaries> serverBoundaries)
    {
        Dictionary<SQLUtility.TimestepZindexKey, List<int>>[] map = new Dictionary<SQLUtility.TimestepZindexKey, List<int>>[serverBoundaries.Count];
        tempTable = SQLUtility.SanitizeTemporaryTable(tempTable);
        SQLUtility.TrackingInputRequest request;

        // Bitmask to ignore low order bits of address
        long mask = ~(long)(worker.DataTable.atomDim * worker.DataTable.atomDim * worker.DataTable.atomDim - 1);

        //long zindex = 0;
        int result_size = worker.GetResultSize();
        int reqseq = -1;

        string query = "";
        SqlCommand cmd;
        query += String.Format("SELECT reqseq, timestep, zindex, x, y, z, pre_x, pre_y, pre_z, time, endTime, dt, compute_predictor FROM {0}", tempTable);
        cmd = new SqlCommand(query, conn);
        SqlDataReader reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            reqseq = reader.GetSqlInt32(0).Value;
            request = new SQLUtility.TrackingInputRequest(
                reqseq,
                reader.GetSqlInt32(1).Value,
                reader.GetSqlInt64(2).Value,
                new Point3(reader.GetSqlSingle(3).Value, reader.GetSqlSingle(4).Value, reader.GetSqlSingle(5).Value),
                new Point3(reader.GetSqlSingle(6).Value, reader.GetSqlSingle(7).Value, reader.GetSqlSingle(8).Value),
                new Vector3(),
                reader.GetSqlSingle(9).Value,
                reader.GetSqlSingle(10).Value,
                reader.GetSqlSingle(11).Value,
                reader.GetSqlBoolean(12).Value);

            input[reqseq] = request;

            AddRequestToMap(ref map, request, worker, mask, serverBoundaries);
        }
        reader.Close();

        return map;
    }

    //TODO: Consider moving this method to the worker.
    private static void AddRequestToMap(ref Dictionary<SQLUtility.TimestepZindexKey, List<int>>[] map, SQLUtility.TrackingInputRequest request,
        Turbulence.SQLInterface.workers.GetPositionWorker worker, long mask,
        List<ServerBoundaries> serverBoundaries)
    {
        long zindex = 0;
        SQLUtility.TimestepZindexKey key = new SQLUtility.TimestepZindexKey();

        zindex = request.zindex & mask;
        key.SetValues(request.timeStep, zindex);

        if (worker.spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
        {
            zindex = request.zindex & mask;
            key.SetValues(request.timeStep, zindex);

            AddRequestToServerMap(ref map, request, zindex, key, serverBoundaries);
        }
        else
        {
            int X, Y, Z;
            if (request.compute_predictor)
            {
                X = LagInterpolation.CalcNode(request.pos.x, worker.setInfo.Dx);
                Y = LagInterpolation.CalcNode(request.pos.y, worker.setInfo.Dy);
                Z = LagInterpolation.CalcNode(request.pos.z, worker.setInfo.Dz);
            }
            else
            {
                X = LagInterpolation.CalcNode(request.pre_pos.x, worker.setInfo.Dx);
                Y = LagInterpolation.CalcNode(request.pre_pos.y, worker.setInfo.Dy);
                Z = LagInterpolation.CalcNode(request.pre_pos.z, worker.setInfo.Dz);
            }
            // For Lagrange Polynomial interpolation we need a cube of data 

            int startz = Z - worker.KernelSize / 2 + 1, starty = Y - worker.KernelSize / 2 + 1, startx = X - worker.KernelSize / 2 + 1;
            int endz = Z + worker.KernelSize / 2, endy = Y + worker.KernelSize / 2, endx = X + worker.KernelSize / 2;

            // we do not want a request to appear more than once in the list for an atom
            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom
            startz = startz - ((startz % worker.setInfo.atomDim) + worker.setInfo.atomDim) % worker.setInfo.atomDim;
            starty = starty - ((starty % worker.setInfo.atomDim) + worker.setInfo.atomDim) % worker.setInfo.atomDim;
            startx = startx - ((startx % worker.setInfo.atomDim) + worker.setInfo.atomDim) % worker.setInfo.atomDim;

            for (int z = startz; z <= endz; z += worker.setInfo.atomDim)
            {
                for (int y = starty; y <= endy; y += worker.setInfo.atomDim)
                {
                    for (int x = startx; x <= endx; x += worker.setInfo.atomDim)
                    {
                        // Wrap the coordinates into the grid space
                        int xi = ((x % worker.setInfo.GridResolutionX) + worker.setInfo.GridResolutionX) % worker.setInfo.GridResolutionX;
                        int yi = ((y % worker.setInfo.GridResolutionY) + worker.setInfo.GridResolutionY) % worker.setInfo.GridResolutionY;
                        int zi = ((z % worker.setInfo.GridResolutionZ) + worker.setInfo.GridResolutionZ) % worker.setInfo.GridResolutionZ;

                        zindex = new Morton3D(zi, yi, xi).Key & mask;
                        key.SetValues(request.timeStep, zindex);

                        AddRequestToServerMap(ref map, request, zindex, key, serverBoundaries);
                    }
                }
            }
        }
    }

    private static void AddRequestToServerMap(ref Dictionary<SQLUtility.TimestepZindexKey, List<int>>[] map, 
        SQLUtility.TrackingInputRequest request,
        long zindex, SQLUtility.TimestepZindexKey key, 
        List<ServerBoundaries> serverBoundaries)
    {
        for (int i = 0; i < serverBoundaries.Count; i++)
        {
            //NOTE: We assume each node stores a contiguous range of zindexes.
            if (serverBoundaries[i].startKey <= zindex && serverBoundaries[i].endKey <= zindex)
            {
                if (!map[i].ContainsKey(key))
                {
                    map[i][key] = new List<int>();
                }
                map[i][key].Add(request.request);
                request.numberOfCubes++;
                break;
            }
        }
    }
};
