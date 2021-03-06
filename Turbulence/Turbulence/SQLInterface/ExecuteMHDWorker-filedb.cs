﻿using System;
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
/* Added for FileDB*/
using System.IO;
/* NOTE: This is the experimental filedb version of ExecuteMHDWorker! */

public partial class StoredProcedures
{
    static Regex io_regex = new Regex(@"Scan count ([0-9]+), logical reads ([0-9]+), physical reads ([0-9]+), read-ahead reads ([0-9]+)", RegexOptions.Compiled);
    static int scan_count = 0;
    static int logical_reads = 0;
    static int physical_reads = 0;
    static int read_ahead_reads = 0;

    /// <summary>
    /// A single interface to multiple database functions.
    /// 
    /// This is currently a mess and should be cleaned up, but
    /// at this point we do have the majority of the unique logic
    /// for each of the calculation functions removed.
    /// </summary>
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void ExecuteMHDWorker(
        string serverName,
        string dbname,
        string codedb,
        string dataset,
        int workerType,
        int blobDim,
        float time,
        int spatialInterp,  // TurbulenceOptions.SpatialInterpolation
        int temporalInterp, // TurbulenceOptions.TemporalInterpolation
        float arg,          // Extra argument (not used by all workers)
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
        contextConn.Open();

        // Check temp table
        //tempTable = SQLUtility.SanitizeTemporaryTable(tempTable);

        // Load information about the requested dataset
        TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, dataset, blobDim, contextConn);

        string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);

        // Instantiate a worker class
        Worker worker = Worker.GetWorker(table, workerType, spatialInterp, arg, contextConn);

        float points_per_cube = 0;

        //Dictionary<long, HashSet<int>> map;
        //Dictionary<long, List<SQLUtility.MHDInputRequest>> map;
        Dictionary<long, List<int>> map;
        Dictionary<int, SQLUtility.MHDInputRequest> input = new Dictionary<int, SQLUtility.MHDInputRequest>(inputSize);
        // Read input data

        map = SQLUtility.ReadTempTableGetAtomsToRead(tempTable, worker, (Worker.Workers)workerType, contextConn, input, inputSize, ref points_per_cube);
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
        joinTable = SQLUtility.CreateTemporaryJoinTable(map.Keys, standardConn, points_per_cube);

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


        //throw new Exception(zindexRegionsString);
        if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.None)
        {
            // Go through and run values on each point

            // Find nearest timestep
            int timestep_int = SQLUtility.GetNearestTimestep(time, table);

            TurbulenceBlob blob = new TurbulenceBlob(table);
            /* Modified for filedb */


            /* cmd = new SqlCommand(
               String.Format(@"SELECT {0}.zindex, {0}.data " +
                          "FROM {1}, {0} WHERE {0}.timestep = {2} " +
                          "AND {1}.zindex = {0}.zindex",
                          tableName, joinTable, timestep_int),
                          contextConn);
                          //standardConn);
            cmd.CommandTimeout = 3600;
            */

            cmd = new SqlCommand(
               String.Format(@"SELECT {0}.zindex " +
                          "FROM {0} ORDER BY zindex",
                           joinTable),
                          contextConn);
            //standardConn);
            cmd.CommandTimeout = 3600;

            //conn.InfoMessage += new SqlInfoMessageEventHandler(InfoMessageHandler);

            //endTime = DateTime.Now;
            //resultSendingTime += endTime - startTime;

            //startTime = endTime;
            //Setup the file
            string pathSource = "d:\\filedb";
            pathSource = pathSource + "\\" + dbname + "_" + timestep_int + ".bin";
            
            FileStream filedb = new FileStream(pathSource, FileMode.Open, System.IO.FileAccess.Read);
            

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                //do
                //{
                while (reader.Read())
                {
                    // read in the current blob
                    long thisBlob = reader.GetSqlInt64(0).Value;
                    //int bytesread = 0;
                    //while (bytesread < table.BlobByteSize)
                    // {
                    //int bytes = (int)reader.GetBytes(1, table.SqlArrayHeaderSize, rawdata, bytesread, table.BlobByteSize - bytesread);
                    long z = thisBlob / (table.atomDim*table.atomDim*table.atomDim);
                    //long offset = z * table.BlobByteSize; //This is hardcoded for testing, will need to adjust for 1 component like pressure
                    long offset = z* table.BlobByteSize;
                    filedb.Seek(offset, SeekOrigin.Begin);
                    //Test
                    
                   // string[] lines= { "Offset chosen = ", offset.ToString(), z.ToString(), table.BlobByteSize.ToString(), thisBlob.ToString(),pathSource, table.atomDim.ToString()};
                    //System.IO.File.WriteAllLines(@"d:\filedb\debug.txt", lines);
                    
                    int bytes = filedb.Read(rawdata, 0, table.BlobByteSize);
                    
                            
                       // bytesread += bytes;
                    //}
                    //endTime = DateTime.Now;
                    //IOTime += endTime - startTime;

                    //startTime = endTime;
                    blob.Setup(timestep_int, new Morton3D(thisBlob), rawdata);
                    //endTime = DateTime.Now;
                    //MemoryTime += endTime - startTime;

                    //startTime = endTime;

                    // Only execute related particles
                    //for (int i = 0; i < input[thisBlob].Count; i++)
                    //foreach (SQLUtility.MHDInputRequest point in map[thisBlob])
                    foreach (int point in map[thisBlob])
                    {
                        //point = input[thisBlob][i];
                        double[] result = worker.GetResult(blob, input[point]);
                        for (int r = 0; r < result.Length; r++)
                        {
                            input[point].result[r] += result[r];
                        }
                        input[point].cubesRead++;
                        //endTime = DateTime.Now;
                        //resultTime += endTime - startTime;

                        //startTime = endTime;
#if MEMORY
                            if (input[point].cubesRead == 1)
                                num_active_points++;
                            if (num_active_points > memory_bandwidth)
                                memory_bandwidth = num_active_points;
#endif

                        if (input[point].cubesRead == input[point].numberOfCubes && !input[point].resultSent)
                        {
                            record.SetInt32(0, input[point].request);
                            int r = 0;
                            for (; r < input[point].result.Length; r++)
                            {
                                record.SetSqlSingle(r + 1, (float)input[point].result[r]);
                            }

                            //record.SetInt32(r + 1, input[point].cubesRead);
                            SqlContext.Pipe.SendResultsRow(record);
                            input[point].resultSent = true;

                            input[point].lagInt = null;
                            input[point].result = null;
                            input[point] = null;
#if MEMORY
                                num_active_points--;
#endif
                        }
                        //endTime = DateTime.Now;
                        //resultSendingTime += endTime - startTime;

                        //input[thisBlob][i] = point;

                        //startTime = endTime;
                    }
                }
                //} while (reader.NextResult());
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
            cmd = new SqlCommand(String.Format(@"DROP TABLE tempdb..{0}", joinTable), contextConn);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Error dropping temporary table.  [Inner Exception: {0}])",
                    e.ToString()));
            }
            standardConn.Close();
            contextConn.Close();
            blob = null;
        }
        else if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.PCHIP)
        {
            // Perform PCHIP interpolation by querying & grouping 4 timesteps by location
            // We process the results from the database as they come out, and then calculate PCHIP at the end
            int basetime = SQLUtility.GetFlooredTimestep(time, table);

            int timestep0 = basetime - table.TimeInc;
            int timestep1 = basetime;
            int timestep2 = basetime + table.TimeInc;
            int timestep3 = basetime + table.TimeInc * 2;

            float time0 = (timestep0 - table.TimeOff) * table.Dt;
            float time1 = (timestep1 - table.TimeOff) * table.Dt;
            float time2 = (timestep2 - table.TimeOff) * table.Dt;
            float time3 = (timestep3 - table.TimeOff) * table.Dt;

            float delta = time2 - time1;

            double[] result;

            TurbulenceBlob blob = new TurbulenceBlob(table);
            cmd = new SqlCommand(
                String.Format(@"DECLARE @times table (timestep int NOT NULL) " +
                          "INSERT @times VALUES ({2}) " +
                          "INSERT @times VALUES ({3}) " +
                          "INSERT @times VALUES ({4}) " +
                          "INSERT @times VALUES ({5}) " +

                          "SELECT {0}.timestep, {0}.zindex, {0}.data " +
                          "FROM @times as t, {0}, {1} " +
                          "WHERE {0}.timestep = t.timestep AND {0}.zindex = {1}.zindex",
                          tableName, joinTable, timestep0, timestep1, timestep2, timestep3),
                          contextConn);
                          //standardConn);
            cmd.CommandTimeout = 3600;
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int timestep = reader.GetSqlInt32(0).Value;  // Timestep returned
                    long thisBlob = reader.GetSqlInt64(1).Value; // Blob returned
                    int bytesread = 0;

                    while (bytesread < table.BlobByteSize)
                    {
                        //int bytes = (int)reader.GetBytes(2, 0, rawdata, bytesread, table.BlobByteSize - bytesread);
                        int bytes = (int)reader.GetBytes(2, table.SqlArrayHeaderSize, rawdata, bytesread, table.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }
                    blob.Setup(timestep, new Morton3D(thisBlob), rawdata);
                    //for (int i = 0; i < input[thisBlob].Count; i++)
                    foreach (int point in map[thisBlob])
                    {
                        if (worker == null)
                            throw new Exception("worker is NULL!");
                        if (blob == null)
                            throw new Exception("blob is NULL!");
                        if (input[point] == null)
                            throw new Exception("input[point] is NULL!");
                        result = worker.GetResult(blob, input[point]);
                        for (int r = 0; r < result.Length; r++)
                        {
                            if (timestep == timestep0)
                            {
                                input[point].result[r] += -result[r] * (time - time1) * (1 + (time - time1) * (-1 + (time - time2) / delta) / delta) / 2 / delta;
                            }
                            else if (timestep == timestep1)
                            {
                                input[point].result[r] += result[r] * (1 + ((time - time1) * (time - time1) * (-2 + 3 * (time - time2) / delta) / 2 / delta / delta));
                            }
                            else if (timestep == timestep2)
                            {
                                input[point].result[r] += result[r] * (time - time1) * (1 + (time - time1) * (1 - 3 * (time - time2) / delta) / delta) / 2 / delta;
                            }
                            else if (timestep == timestep3)
                            {
                                input[point].result[r] += result[r] * (time - time1) * (time - time1) * (time - time2) / 2 / delta / delta / delta;
                            }
                        }
                        input[point].cubesRead++;

                        if (input[point].cubesRead == 4 * input[point].numberOfCubes && !input[point].resultSent)
                        {
                            record.SetInt32(0, input[point].request);
                            int r = 0;
                            for (; r < input[point].result.Length; r++)
                            {
                                record.SetSqlSingle(r + 1, (float)input[point].result[r]);
                            }
                            //record.SetInt32(r + 1, input[point].cubesRead);
                            SqlContext.Pipe.SendResultsRow(record);
                            input[point].resultSent = true;

                            input[point].lagInt = null;
                            input[point].result = null;
                            input[point] = null;
                        }
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
            cmd = new SqlCommand(String.Format(@"DROP TABLE tempdb..{0}", joinTable), contextConn);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Error dropping temporary table.  [Inner Exception: {0}])",
                    e.ToString()));
            }
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

    /// <summary>
    /// customize batching
    /// A single interface to multiple database functions.
    /// 
    /// This is currently a mess and should be cleaned up, but
    /// at this point we do have the majority of the unique logic
    /// for each of the calculation functions removed.
    /// </summary>
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void ExecuteMHDWorkerBatch(string serverName,
        string dbname,
        string codedb,
        string dataset,
        int workerType,
        int blobDim,
        float time,
        string boundary,
        int arg,            // Extra argument (not used by all workers)
        string tempTable)
    {
        SqlConnection standardConn;
        SqlConnection contextConn;
        string connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName, codedb);
        standardConn = new SqlConnection(connString);
        contextConn = new SqlConnection("context connection=true");
        contextConn.Open();

        // Load information about the requested dataset
        TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, dataset, blobDim, contextConn);

        string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);

        // -----------------------------------------------
        // construct query boundary for batch execution and invoke a different computation per point
        string[] queryStr = boundary.Split(';');
        Worker[] worker = new Worker[queryStr.Length];
        int[] boundaries = new int[queryStr.Length];
        int[] result_size = new int[queryStr.Length];
        int[] nOrder = new int[queryStr.Length];
        int[] spatialInterp = new int[queryStr.Length];
        TurbulenceOptions.TemporalInterpolation[] temporalInterp = new TurbulenceOptions.TemporalInterpolation[queryStr.Length];

        for (int i = 0; i < queryStr.Length; ++i)
        {
            string[] component = queryStr[i].Split(',');
            boundaries[i] = int.Parse(component[0]);
            spatialInterp[i] = int.Parse(component[1]);
            temporalInterp[i] = (TurbulenceOptions.TemporalInterpolation)int.Parse(component[2]);
            // Instantiate a worker class
            worker[i] = Worker.GetWorker(table, workerType, spatialInterp[i], arg, contextConn);
            result_size[i] = worker[i].GetResultSize();
            nOrder[i] = int.Parse(component[4]);
        }
        // -----------------------------------------------


        // Bitmask to ignore low order bits of address
        long mask = ~(long)(table.atomDim * table.atomDim * table.atomDim - 1);

        float points_per_cube = 0;

        // TODO: Replace this deprecated hack.
        int inputSize = SQLUtility.GetTempTableLength(tempTable);

        //Dictionary<long, HashSet<int>> map;
        //Dictionary<long, List<SQLUtility.MHDInputRequest>> map;
        Dictionary<long, List<int>> map;
        Dictionary<int, SQLUtility.MHDInputRequest> input = new Dictionary<int, SQLUtility.MHDInputRequest>(inputSize);
        // Read input data
        if (workerType != (int)(Worker.Workers.GetVelocity))
        {
            throw new Exception("Function type " + workerType + " is not currently supported");
            //input = SQLUtility.ReadTrackingTemporaryTable(tempTable);
        }
        else
        {
            map = SQLUtility.ReadTempTableGetCubesToReadBatch(tempTable, worker, boundaries, result_size, nOrder, contextConn, input, time, ref points_per_cube);
            contextConn.Close();
        }

        SqlCommand cmd;
        SqlDataRecord record;

        byte[] rawdata = new byte[table.BlobByteSize];

        long SqlArrayHeader = 0; // 6 * sizeof(int);

        standardConn.Open();
        string joinTable = SQLUtility.CreateTemporaryJoinTable(map.Keys, standardConn, points_per_cube);

#if MEMORY
        int num_active_points = 0;
        int memory_bandwidth = 0;
#endif
        //float[] result;

        record = new SqlDataRecord(worker[0].GetRecordMetaData());
        SqlContext.Pipe.SendResultsStart(record);


        //throw new Exception(zindexRegionsString);
        if ((TurbulenceOptions.TemporalInterpolation)temporalInterp[0] ==
            TurbulenceOptions.TemporalInterpolation.None)
        {
            // Go through and run values on each point

            // Find nearest timestep
            int timestep_int = SQLUtility.GetNearestTimestep(time, table);

            TurbulenceBlob blob = new TurbulenceBlob(table);

            cmd = new SqlCommand(
               String.Format(@"SELECT {0}.zindex, {0}.data " +
                          "FROM {1}, {0} WHERE {0}.timestep = {2} " +
                          "AND {1}.zindex = {0}.zindex",
                          tableName, joinTable, timestep_int), standardConn);
            cmd.CommandTimeout = 3600;

            //conn.InfoMessage += new SqlInfoMessageEventHandler(InfoMessageHandler);

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                //do
                //{
                while (reader.Read())
                {
                    // read in the current blob
                    long thisBlob = reader.GetSqlInt64(0).Value;
                    int bytesread = 0;
                    while (bytesread < table.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(1, SqlArrayHeader, rawdata, bytesread, table.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }

                    blob.Setup(timestep_int, new Morton3D(thisBlob), rawdata);

                    // Only execute related particles
                    //for (int i = 0; i < input[thisBlob].Count; i++)
                    //foreach (SQLUtility.MHDInputRequest point in map[thisBlob])
                    foreach (int point in map[thisBlob])
                    {
                        // ---------------------------------
                        // determine the corresponding query
                        int queryIndex = 0;
                        for (int i = 0; i < boundaries.Length; ++i)
                        {
                            if (input[point].request <= boundaries[i])
                            {
                                queryIndex = i;
                                break;
                            }
                        }
                        // ---------------------------------

                        //point = input[thisBlob][i];
                        double[] result = worker[queryIndex].GetResult(blob, input[point]);
                        for (int r = 0; r < result.Length; r++)
                        {
                            input[point].result[r] += result[r];
                        }
                        input[point].cubesRead++;

#if MEMORY
                        if (input[point].cubesRead == 1)
                            num_active_points++;
                        if (num_active_points > memory_bandwidth)
                            memory_bandwidth = num_active_points;
#endif

                        if (input[point].cubesRead == input[point].numberOfCubes && !input[point].resultSent)
                        {
                            record.SetInt32(0, input[point].request);
                            int r = 0;
                            for (; r < input[point].result.Length; r++)
                            {
                                record.SetSqlSingle(r + 1, (float)input[point].result[r]);
                            }
                            record.SetInt32(r + 1, input[point].cubesRead);
                            SqlContext.Pipe.SendResultsRow(record);
                            input[point].resultSent = true;

                            input[point].lagInt = null;
                            input[point].result = null;
                            input[point] = null;
#if MEMORY
                            num_active_points--;
#endif
                        }
                    }
                }
            }

            standardConn.Close();
            blob = null;
        }
        else
        {
            standardConn.Close();
            map.Clear();
            map = null;
            input = null;
            rawdata = null;
            worker = null;
            throw new Exception("Unsupported TemporalInterpolation Type");
        }

        SqlContext.Pipe.SendResultsEnd();

        map.Clear();
        map = null;
        input = null;
        rawdata = null;
        worker = null;
    }

    static void InfoMessageHandler(object sender, SqlInfoMessageEventArgs e)
    {
        Match match = io_regex.Match(e.Message);

        // Here we check the Match instance.
        if (match.Success)
        {
            // Finally, we get the Group value and display it.
            //string key = match.Groups[1].Value;
            scan_count += Convert.ToInt32(match.Groups[1].Value);
            //Console.WriteLine("Scan Count: " + scan_count);
            logical_reads += Convert.ToInt32(match.Groups[2].Value);
            //Console.WriteLine("Logical Reads: " + logical_reads);
            physical_reads += Convert.ToInt32(match.Groups[3].Value);
            //Console.WriteLine("Physical Reads: " + physical_reads);
            read_ahead_reads += Convert.ToInt32(match.Groups[4].Value);
            //Console.WriteLine("Read-ahead Reads: " + read_ahead_reads);
        }

    }

    static SqlMetaData[] GetSqlMetaData()
    {
        return new SqlMetaData[] {
            new SqlMetaData("Scan count", SqlDbType.Int),
            new SqlMetaData("Logical Reads", SqlDbType.Int),
            new SqlMetaData("Physical Reads", SqlDbType.Int),
            new SqlMetaData("Read-ahead Reads", SqlDbType.Int),
            new SqlMetaData("Pre-processing Time", SqlDbType.Float),
            new SqlMetaData("I/O Time", SqlDbType.Float),
            new SqlMetaData("Memory Copy Time", SqlDbType.Float),
            new SqlMetaData("Get Result Time", SqlDbType.Float),
            new SqlMetaData("Result Sending Time", SqlDbType.Float),
            new SqlMetaData("Total Execution Time", SqlDbType.Float)
#if MEMORY
            ,new SqlMetaData("Memory Bandwith", SqlDbType.Int)
            ,new SqlMetaData("Avg. Points/Cube", SqlDbType.Real)
#endif
        };
    }
};
