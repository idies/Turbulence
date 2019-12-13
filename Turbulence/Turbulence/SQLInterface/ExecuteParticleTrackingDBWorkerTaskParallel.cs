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
    public static void ExecuteParticleTrackingDBWorkerTaskParallel(
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
        int dbtype = 0;

        SqlConnection contextConn;
        contextConn = new SqlConnection("context connection=true");

        TurbServerInfo serverinfo = TurbServerInfo.GetTurbServerInfo(codedb, turbinfodb, turbinfoserver);
        String cString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverinfo.infoDB_server, serverinfo.infoDB);
        SqlConnection turbinfoConn = new SqlConnection(cString);
        turbinfoConn.Open();
        SqlCommand cmd = turbinfoConn.CreateCommand();
        //string DBMapTable = "DatabaseMap";
        cmd.CommandText = String.Format("select ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, MIN(minLim) as minLim, MAX(maxLim) as maxLim, min(minTime) as minTime, max(maxTime) as maxTime, dbtype, HotSpareActive, HotSpareMachineName " +
            "from {0}..DatabaseMap where DatasetID = @datasetID " +
            "group by ProductionMachineName, ProductionDatabaseName, CodeDatabaseName, dbtype, HotSpareActive, HotSpareMachineName " +
            "order by minLim, minTime", serverinfo.infoDB);
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
                    long minLim = reader.GetInt64(3); 
                    long maxLim = reader.GetInt64(4);
                    int minTime = reader.GetInt32(5);  
                    int maxTime = reader.GetInt32(6);
                    dbtype = reader.GetInt32(7);
                    serverBoundaries.Add(new ServerBoundaries(new Morton3D(minLim), new Morton3D(maxLim), minTime, maxTime));
                    if (serverName.CompareTo(localServerCleanName) == 0)
                    {
                        // We'll use the context connection in this case.
                        connections.Add(contextConn);
                    }
                    else
                    {
                        connections.Add(new SqlConnection(String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverName, DBName.Substring(0, DBName.IndexOf("db") + 2))));
                    }
                }
            }
            else
            {
                throw new Exception("Invalid dataset specified.");
            }
        }
        //turbinfoConn.Close();

        // Load information about the requested dataset
        TurbDataTable table = TurbDataTable.GetTableInfo(localServerCleanName, localDatabase, tableName, atomDim, serverinfo);

        // Instantiate a worker class
        contextConn.Open();
        Turbulence.SQLInterface.workers.GetPositionWorker worker =
            new Turbulence.SQLInterface.workers.GetPositionWorker(table,
                (TurbulenceOptions.SpatialInterpolation)spatialInterp,
                (TurbulenceOptions.TemporalInterpolation)temporalInterp);

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
        map = ReadTempTableGetAtomsToRead(tempTable, worker, contextConn, input, serverBoundaries, ref number_of_crossings, baseTimeStep);
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

        SqlDataRecord record;

        byte[] rawdata = new byte[table.BlobByteSize];               

        SqlConnection standardConn;
        string connString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", localServerCleanName, localDatabase.Substring(0, localDatabase.Length - 3));
        standardConn = new SqlConnection(connString);
        standardConn.Open();

        record = new SqlDataRecord(GetRecordMetaData());
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
            int failsafe = 0;
            int norowcount = 0;
            float priordt = 0; //used to store prior DT for final corrector step.
            while (!all_done)
            {
                all_done = true;
                failsafe = failsafe + 1; /*Just to prevent a server lockup */
                if (failsafe > 100000)
                {
                    throw new Exception("Looped too many times, aborting.  Failsafe = "  + failsafe.ToString() + " Time is " + time.ToString() + " Norowcount = " + norowcount.ToString());
                }
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

                        List<int> timelist = new List<int>();
                        List<Morton3D> zindex = new List<Morton3D>();
                        int[] timesteps = new int[4] { timestep0, timestep1, timestep2, timestep3 };
                        int no_timesteps = timesteps.GetLength(0);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                timelist.Add(reader.GetSqlInt32(0).Value); // Timestep returned
                                zindex.Add(new Morton3D(reader.GetSqlInt64(1).Value)); // Blob returned
                            }
                        }

                        SqlCommand[] cmds = new SqlCommand[no_timesteps];
                        for (int i = 0; i < no_timesteps; i++)
                        {
                            string pathSource = SQLUtility.getDBfilePath(databases[s], timesteps[i], table.DataName, connections[s], localServer);

                            List<Morton3D> zindexQueryList = new List<Morton3D>();
                            string zindexQuery = "[";
                            for (int j2 = 0; j2 < zindex.Count; j2++)
                            {
                                Morton3D zindex2 = zindex[j2];
                                if (serverBoundaries[s].startKey <= zindex2 && zindex2 <= serverBoundaries[s].endKey &&
                                    timelist[j2] == timesteps[i])
                                {
                                    zindexQuery = zindexQuery + zindex2.ToString() + ",";
                                    zindexQueryList.Add(zindex2);
                                }
                            }
                            zindexQuery = zindexQuery + "]";

                            cmds[i] = connections[s].CreateCommand();
                            cmds[i].CommandText = String.Format("EXEC [{0}].[dbo].[ExecuteDBFileReader] @serverName, @dbname, @filePath, @BlobByteSize, @atomDim, "
                                                            + " @zindexQuery, @zlistCount, @dbtype",
                                                            serverinfo.codeDB);
                            cmds[i].Parameters.AddWithValue("@serverName", servers[s]);
                            cmds[i].Parameters.AddWithValue("@dbname", databases[s]);
                            cmds[i].Parameters.AddWithValue("@filePath", pathSource);
                            cmds[i].Parameters.AddWithValue("@BlobByteSize", table.BlobByteSize);
                            cmds[i].Parameters.AddWithValue("@atomDim", table.atomDim);
                            cmds[i].Parameters.AddWithValue("@zindexQuery", zindexQuery);
                            cmds[i].Parameters.AddWithValue("@zlistCount", zindexQueryList.Count);
                            cmds[i].Parameters.AddWithValue("@dbtype", dbtype);
                            //asyncRes[i] = cmds[i].BeginExecuteReader(null, cmds[i]);
                        }

                        for (int i = 0; i < no_timesteps; i++)
                        {
                            int timestepRead = timesteps[i];  // Timestep returned
                            using (SqlDataReader reader = cmds[i].ExecuteReader())
                            {
                                while (reader.Read() && !reader.IsDBNull(0))
                                {
                                    long thisBlob = reader.GetSqlInt64(0).Value; // Blob returned
                                    rawdata = reader.GetSqlBytes(1).Value;
                                    blob.Setup(timestepRead, new Morton3D(thisBlob), rawdata);

                                    //endTimer = DateTime.Now;
                                    //IOTime += endTimer - startTimer;
                                    //startTimer = endTimer;

                                    foreach (int j in map[s][thisBlob])
                                    {
                                        point = input[j];

                                        if (worker == null)
                                            throw new Exception("worker is NULL!");
                                        if (blob == null)
                                            throw new Exception("blob is NULL!");

                                        point.cubesRead++;
                                        worker.GetResult(blob, ref point, timestepRead, baseTimeStep, time, endTime, dt, ref nextTimeStep, ref nextTime, ref priordt);
                                    }
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
                    
                    AddRequestToMap(ref map, input[input_point], worker, mask, serverBoundaries, ref number_of_crossings, baseTimeStep );
                    all_done = false;
                }
                foreach (int done_point in done_points)
                {
                    GenerateResultRowFinalPosition(record, input[done_point]);
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
