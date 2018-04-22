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
/* Added for FileDB*/
using System.IO;

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
    public static void ExecuteParticleTrackingChannelDBWorkerTaskParallel2(
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

        String cString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverinfo.infoDB_server, serverinfo.infoDB);
        SqlConnection turbinfoConn = new SqlConnection(cString);
        turbinfoConn.Open();
        SqlCommand cmd = turbinfoConn.CreateCommand();
        //string DBMapTable = "DatabaseMap";
        cmd.CommandText = String.Format("select ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, MIN(minLim) as minLim, MAX(maxLim) as maxLim, MIN(minTime) as minTime, MAX(maxTime) as maxTime, dbtype, HotSpareActive, HotSpareMachineName " +
            "from {0}..DatabaseMap where DatasetID = @datasetID " +
            "group by ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, dbtype, HotSpareActive, HotSpareMachineName " +
            "order by minLim", serverinfo.infoDB);
        cmd.Parameters.AddWithValue("@datasetID", datasetID);
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    String serverName;
                    if (!reader.GetBoolean(8)) //HotSpareActive=false
                    {
                        serverName = reader.GetString(0);
                    }
                    else
                    {
                        serverName = reader.GetString(9);
                    }
                    String DBName = reader.GetString(1);
                    servers.Add(serverName);
                    databases.Add(DBName);
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
                        connections.Add(new SqlConnection(String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverName, DBName.Substring(0, DBName.Length - 3))));
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

        //List<double> values = (from kvp in grid_points where kvp.Key == (int32)0 select kvp.Value).ToList();
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
        string connString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", localServerCleanName, localDatabase.Substring(0,localDatabase.Length-3));
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

                        //endTimer = DateTime.Now;
                        //preprocessTime += endTimer - startTimer;
                        //startTimer = endTimer;

                        cmd = new SqlCommand(
                            String.Format(@"DECLARE @times table (timestep int NOT NULL) " +
                            "INSERT @times VALUES ({1}) " +
                            "INSERT @times VALUES ({2}) " +
                            "INSERT @times VALUES ({3}) " +
                            "INSERT @times VALUES ({4}) " +

                            "SELECT t.timestep, {0}.zindex " +
                            "FROM @times as t,  {0} " +
                            "ORDER BY timestep, zindex",
                            joinTable, timestep0, timestep1, timestep2, timestep3),
                            connections[s]);
                        cmd.CommandTimeout = 3600;

                        List<SQLUtility.zlistTable> zlist = new List<SQLUtility.zlistTable>();
                        if (table.dbtype == 2)
                        {
                            zlist = SQLUtility.fileDB2zlistTable(databases[s], standardConn);
                        }

                        string pathSource0 = SQLUtility.getDBfilePath(databases[s], timestep0, table.DataName, connections[s], localServer);
                        string pathSource1 = SQLUtility.getDBfilePath(databases[s], timestep1, table.DataName, connections[s], localServer);
                        string pathSource2 = SQLUtility.getDBfilePath(databases[s], timestep2, table.DataName, connections[s], localServer);
                        string pathSource3 = SQLUtility.getDBfilePath(databases[s], timestep3, table.DataName, connections[s], localServer);
                        FileStream filedb0 = null, filedb1 = null, filedb2 = null, filedb3 = null;

                        try
                        {
                            filedb0 = new FileStream(pathSource0, FileMode.Open, System.IO.FileAccess.Read);
                        }
                        catch { }
                        try
                        {
                            filedb1 = new FileStream(pathSource1, FileMode.Open, System.IO.FileAccess.Read);
                        }
                        catch { }
                        try
                        {
                            filedb2 = new FileStream(pathSource2, FileMode.Open, System.IO.FileAccess.Read);
                        }
                        catch { }
                        try
                        {
                            filedb3 = new FileStream(pathSource3, FileMode.Open, System.IO.FileAccess.Read);
                        }
                        catch { }

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                //int basetime = reader.GetSqlInt32(0).Value;  // Base time
                                int timestepRead = reader.GetSqlInt32(0).Value;  // Timestep returned
                                long thisBlob = reader.GetSqlInt64(1).Value; // Blob returned

                                long offset = 0;
                                if (table.dbtype == 1)
                                {
                                    //long fileBlob = thisBlob - startz; /*We need to align the first blob with the start of the file */
                                    long fileBlob = thisBlob % 134217728;
                                    long z = fileBlob / (table.atomDim * table.atomDim * table.atomDim);
                                    offset = z * table.BlobByteSize;
                                }
                                else if (table.dbtype == 2)
                                {
                                    //offset = SQLUtility.fileDB2offset(dbname, table, thisBlob, standardConn);
                                    //start = DateTime.Now;
                                    SQLUtility.zlistTable zresult = zlist.Find(x => (x.startZ <= thisBlob && thisBlob <= x.endZ));
                                    offset = (thisBlob - zresult.startZ) / (table.atomDim * table.atomDim * table.atomDim);
                                    offset = (zresult.blobBefore + offset) * table.BlobByteSize;
                                    //file.WriteLine(string.Format("startZ {0}, endZ {1}, blobBefore {2}, Offset {3}", result.startZ, result.endZ, result.blobBefore, offset));
                                    //file.WriteLine(string.Format("Find thisBlob: {0}", DateTime.Now - start));
                                }

                                // TODO: I'm afratid this is wrong, because if file3 doesn't exist,
                                // the rawdata would use the data from file2
                                if (timestepRead == timestep0 && filedb0 != null)
                                {
                                    filedb0.Seek(offset, SeekOrigin.Begin);
                                    int bytes = filedb0.Read(rawdata, 0, table.BlobByteSize);
                                }
                                else if (timestepRead == timestep1 && filedb1 != null)
                                {
                                    filedb1.Seek(offset, SeekOrigin.Begin);
                                    int bytes = filedb1.Read(rawdata, 0, table.BlobByteSize);
                                }
                                else if (timestepRead == timestep2 && filedb2 != null)
                                {
                                    filedb2.Seek(offset, SeekOrigin.Begin);
                                    int bytes = filedb2.Read(rawdata, 0, table.BlobByteSize);
                                }
                                else if (timestepRead == timestep3 && filedb3 != null)
                                {
                                    filedb3.Seek(offset, SeekOrigin.Begin);
                                    int bytes = filedb3.Read(rawdata, 0, table.BlobByteSize);
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
};
