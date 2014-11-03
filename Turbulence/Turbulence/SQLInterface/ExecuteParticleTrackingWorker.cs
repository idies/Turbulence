using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
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
    public static void ExecuteParticleTrackingWorker(
        string serverName,
        string database,
        string codedb,
        string dataset,
        int workerType,
        int spatialInterp,          // TurbulenceOptions.SpatialInterpolation
        int temporalInterp,         // TurbulenceOptions.TemporalInterpolation
        int inputSize,
        string tempTable)
    {
        //TimeSpan IOTime = new TimeSpan(0), preProcessTime = new TimeSpan(0), resultTime = new TimeSpan(0),
        //    MemoryTime = new TimeSpan(0), resultSendingTime = new TimeSpan(0),
        //    ReadTempTableGetCubesToRead = new TimeSpan(0), GetCubesForEachPoint = new TimeSpan(0);
        //DateTime startTime, endTime, initialTimeStamp;

        //initialTimeStamp = startTime = DateTime.Now;

        SqlConnection standardConn;
        SqlConnection contextConn;
        string connString;
        if (serverName.Contains("_"))
            connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName.Remove(serverName.IndexOf("_")), codedb);
        else
            connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName, codedb);
        standardConn = new SqlConnection(connString);
        contextConn = new SqlConnection("context connection=true");
        //contextConn = new SqlConnection(String.Format("Data Source={0};Initial Catalog={1};User ID=turbquery;Password=dHaGo9486sal;Pooling=false;", serverName, codedb));
        contextConn.Open();

        // Check temp table
        //tempTable = SQLUtility.SanitizeTemporaryTable(tempTable);

        // Load information about the requested dataset
        TurbDataTable table = TurbDataTable.GetTableInfo(serverName, database, dataset, 8, contextConn);

        string tableName = String.Format("{0}.dbo.{1}", database, table.TableName);

        // Instantiate a worker class
        Turbulence.SQLInterface.workers.GetPositionWorker worker =
            new Turbulence.SQLInterface.workers.GetPositionWorker(table,
                (TurbulenceOptions.SpatialInterpolation)spatialInterp,
                (TurbulenceOptions.TemporalInterpolation)temporalInterp);

        float points_per_cube = 0;

        Dictionary<SQLUtility.TimestepZindexKey, List<int>> map;
        Dictionary<int, SQLUtility.TrackingInputRequest> input = new Dictionary<int, SQLUtility.TrackingInputRequest>(inputSize);

        // Read input data
        bool all_done = false;
        map = SQLUtility.ReadTempTableGetAtomsToRead_ParticleTracking(tempTable, worker, contextConn, input, inputSize, ref points_per_cube, ref all_done);
        //contextConn.Close();

        //endTime = DateTime.Now;
        //ReadTempTableGetCubesToRead += endTime - startTime;


        // Output SQL column names
        //worker.SendSqlOutputHeaders();

        SqlCommand cmd;
        //SqlDataReader reader;
        //SqlDataRecord record = new SqlDataRecord(worker.GetRecordMetaData());
        //SqlContext.Pipe.SendResultsStart(record);

        SqlDataRecord record;

        byte[] rawdata = new byte[table.BlobByteSize];

        // Create a table to perform query via a JOIN
        //string joinTable = SQLUtility.SelectDistinctIntoTemporaryTable(tempTable);

        //For each point detemine the relevant cubes 
        //and create a table to perform query via a JOIN

        //startTime = DateTime.Now;

        standardConn.Open();
        string joinTable = "";
        //joinTable = SQLUtility.SelectDistinctIntoTemporaryTable(tempTable, contextConn);
        joinTable = SQLUtility.CreateTemporaryJoinTable(map.Keys, (TurbulenceOptions.TemporalInterpolation)temporalInterp, table.TimeInc, standardConn, points_per_cube);

        //endTime = DateTime.Now;
        //preProcessTime += endTime - initialTimeStamp;

        //GetCubesForEachPoint += endTime - startTime;

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

#if MEMORY
        int num_active_points = 0;
        int memory_bandwidth = 0;
#endif
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
            throw new Exception("Temporal interpolation must be used when performing particle tracking!");
        }
        else if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.PCHIP)
        {
            // Perform PCHIP interpolation by querying & grouping 4 timesteps by location
            // We process the results from the database as they come out, and then calculate PCHIP at the end

            TurbulenceBlob blob = new TurbulenceBlob(table);
            //bool all_done = false;
            int iteration_number = 0;

            // If all_done is true at iteration 0 a point has crossed the boundary
            // we need to still process it as it has been assigned to the other servers as well
            // the partial velocity increments will be added at the Web server
            while (!all_done || (all_done && iteration_number == 0))
            {
                all_done = true;
                cmd = new SqlCommand(
                    String.Format(@"SELECT {1}.basetime, {0}.timestep, {0}.zindex, {0}.data " +
                                    "FROM {0}, {1} " +
                                    "WHERE {0}.timestep = {1}.timestep AND {0}.zindex = {1}.zindex",
                              tableName, joinTable),
                              contextConn);
                //standardConn);
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
                        foreach (int i in map[key])
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

                    //}
                    //catch (Exception ex)
                    //{
                    //    throw new Exception(String.Format("Error performing PCHIP interpolation on point {9}: time={0}, times[0]={1}, times[1]={2}, times[2]={3}, times[3]={4}, " +
                    //        "\n[Inner Exception: {5}])", time, time0, time1, time2, time3,
                    //        ex.InnerException));
                    //}
                }
                cmd = new SqlCommand(String.Format(@"DROP TABLE tempdb..{0}", joinTable), contextConn);
                try
                {
                    cmd.CommandTimeout = 600;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    throw new Exception(String.Format("Error dropping temporary table.  [Inner Exception: {0}])",
                        e.ToString()));
                }

                // create new map
                map.Clear();
                //done_points.Clear();
                foreach (int input_point in input.Keys)
                {
                    if (input[input_point].done || input[input_point].crossed_boundary)
                    {
                        all_done = true;
                        break;
                    }
                    // reset the velocity increment
                    input[input_point].vel_inc.x = 0.0f;
                    input[input_point].vel_inc.y = 0.0f;
                    input[input_point].vel_inc.z = 0.0f;
                    input[input_point].cubesRead = 0;
                    input[input_point].numberOfCubes = 0;
                    input[input_point].lagInt = null;
                    SQLUtility.AddRequestToMap(ref map, input[input_point], worker, mask);
                    all_done = false;

                    //if (!input[input_point].done && !input[input_point].crossed_boundary)
                    //{
                    //    // reset the velocity increment
                    //    input[input_point].vel_inc.x = 0.0f;
                    //    input[input_point].vel_inc.y = 0.0f;
                    //    input[input_point].vel_inc.z = 0.0f;
                    //    input[input_point].cubesRead = 0;
                    //    input[input_point].numberOfCubes = 0;
                    //    input[input_point].lagInt = null;
                    //    SQLUtility.AddRequestToMap(ref map, input[input_point], worker, mask, ref have_crossing);
                    //    all_done = false;
                    //}
                    //else
                    //{
                    //    GenerateResultRow(record, input[input_point]);
                    //    done_points.Add(input_point);
                    //}
                }
                //foreach (int done_point in done_points)
                //{
                //    input.Remove(done_point);
                //}
                //// create new join table
                //joinTable = SQLUtility.CreateTemporaryJoinTable(map.Keys, (TurbulenceOptions.TemporalInterpolation)temporalInterp, table.TimeInc, standardConn, points_per_cube);
                //iteration_number++;

                if (all_done)
                {
                    foreach (int input_point in input.Keys)
                    {
                        GenerateResultRow(record, input[input_point]);
                    }
                }
                else
                {
                    // create new join table
                    joinTable = SQLUtility.CreateTemporaryJoinTable(map.Keys, (TurbulenceOptions.TemporalInterpolation)temporalInterp, table.TimeInc, standardConn, points_per_cube);
                }
                iteration_number++;
            }
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
            //cmd = new SqlCommand(String.Format(@"DROP TABLE tempdb..{0}", joinTable), contextConn);
            //try
            //{
            //    cmd.ExecuteNonQuery();
            //}
            //catch (Exception e)
            //{
            //    throw new Exception(String.Format("Error dropping temporary table.  [Inner Exception: {0}])",
            //        e.ToString()));
            //}
            standardConn.Close();
            contextConn.Close();
            // Encourage garbage collector to clean up.
            blob = null;
        }
        else
        {
            standardConn.Close();
            contextConn.Close();
            map.Clear();
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

        map.Clear();
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

    static void GenerateResultRow(SqlDataRecord record, SQLUtility.TrackingInputRequest temp_point)
    {
        record.SetInt32(0, temp_point.request);
        record.SetFloat(1, temp_point.pos.x);
        record.SetFloat(2, temp_point.pos.y);
        record.SetFloat(3, temp_point.pos.z);
        record.SetFloat(4, temp_point.pre_pos.x);
        record.SetFloat(5, temp_point.pre_pos.y);
        record.SetFloat(6, temp_point.pre_pos.z);
        record.SetFloat(7, temp_point.vel_inc.x);
        record.SetFloat(8, temp_point.vel_inc.y);
        record.SetFloat(9, temp_point.vel_inc.z);
        record.SetInt32(10, temp_point.timeStep);
        record.SetFloat(11, temp_point.time);
        record.SetFloat(12, temp_point.endTime);
        record.SetFloat(13, temp_point.dt);
        record.SetBoolean(14, temp_point.compute_predictor);
        record.SetBoolean(15, temp_point.done);
        SqlContext.Pipe.SendResultsRow(record);
    }
};
