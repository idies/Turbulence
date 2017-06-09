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
/* Added for FileDB*/
using System.IO;
/* NOTE: This is the experimental filedb version of ExecuteMHDWorker! */

public partial class StoredProcedures
{
    //static Regex io_regex = new Regex(@"Scan count ([0-9]+), logical reads ([0-9]+), physical reads ([0-9]+), read-ahead reads ([0-9]+)", RegexOptions.Compiled);
    //static int scan_count = 0;
    //static int logical_reads = 0;
    //static int physical_reads = 0;
   // static int read_ahead_reads = 0;

    /// <summary>
    /// A single interface to multiple database functions.
    /// 
    /// This is currently a mess and should be cleaned up, but
    /// at this point we do have the majority of the unique logic
    /// for each of the calculation functions removed.
    /// </summary>
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void ExecuteMHDFileDBWorker(
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
        string tempTable,
        long startz,
        long endz)
    {
        //TimeSpan IOTime = new TimeSpan(0), preProcessTime = new TimeSpan(0), resultTime = new TimeSpan(0),
        //    MemoryTime = new TimeSpan(0), resultSendingTime = new TimeSpan(0),
        //    ReadTempTableGetCubesToRead = new TimeSpan(0), GetCubesForEachPoint = new TimeSpan(0);
        //DateTime startTime, endTime, initialTimeStamp;

        //initialTimeStamp = startTime = DateTime.Now;
        //spacing for 4096 is .00153398
        //We are getting rid of sql connection to db the new filedb.
        
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

       // string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);

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

        SqlCommand cmd;
        
        SqlDataRecord record;

        byte[] rawdata = new byte[table.BlobByteSize];
        
        standardConn.Open();
        string joinTable = "";
        joinTable = SQLUtility.CreateTemporaryJoinTable(map.Keys, standardConn, points_per_cube);
        /*
       
#if MEMORY
        int num_active_points = 0;
        int memory_bandwidth = 0;
#endif
        //float[] result;
        */
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
            //This is ok I think because it is a temporary table
            cmd = new SqlCommand(
               String.Format(@"SELECT {0}.zindex " +
                          "FROM {0} ORDER BY zindex",
                           joinTable),
                          contextConn);
            //standardConn);
            cmd.CommandTimeout = 3600;
            //Setup the file
            
            string pathSource = "e:\\filedb\\isotropic4096";
            pathSource = pathSource + "\\" + dbname + "_" + timestep_int + ".bin";
            FileStream filedb = new FileStream(pathSource, FileMode.Open, System.IO.FileAccess.Read);
            //string[] tester = { "In filedb..."};
            //System.IO.File.WriteAllLines(@"e:\filedb\debug.txt", tester);

            //while (reader.Read())
            using (SqlDataReader reader = cmd.ExecuteReader())

            {
                while (reader.Read())
                { 
                    
                    // read in the current blob
                    long thisBlob = reader.GetSqlInt64(0).Value;
                    if (thisBlob <= endz && thisBlob >= startz)
                    { 
                            //Reset blob to line up with beginning of file by taking the modulo of the 512 cube zindex  This could be done by the databasemap maybe.
                            //One possibility is to take the thisblob-zmin. 
                            //thisBlob is the spatial blob.  fileBlob is the corresponding blob in relation to the file. 
                            //long fileBlob = thisBlob - startz; /*We need to align the first blob with the start of the file */
                            long fileBlob = thisBlob % 134217728;
                        long z = fileBlob / (table.atomDim * table.atomDim * table.atomDim);
                        long offset = z * table.BlobByteSize;
                        filedb.Seek(offset, SeekOrigin.Begin);
                        //Test
                        //string[] lines= { "Offset chosen = ", offset.ToString(), z.ToString(), table.BlobByteSize.ToString(), thisBlob.ToString(),pathSource, table.atomDim.ToString()};
                        //System.IO.File.WriteAllLines(@"e:\filedb\debug.txt", lines);

                        int bytes = filedb.Read(rawdata, 0, table.BlobByteSize);
                        blob.Setup(timestep_int, new Morton3D(thisBlob), rawdata);

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
                }
            }
            //} while (reader.NextResult());
            


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
            filedb.Close();
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
            //string joinTable = "none";  //placeholder.  We don't have PCHIP with filedb yet...isotropic4096 only has one timestep.
            TurbulenceBlob blob = new TurbulenceBlob(table);
            cmd = new SqlCommand(
                String.Format(@"DECLARE @times table (timestep int NOT NULL) " +
                          "INSERT @times VALUES ({1}) " +
                          "INSERT @times VALUES ({2}) " +
                          "INSERT @times VALUES ({3}) " +
                          "INSERT @times VALUES ({4}) " +

                          "SELECT t.timestep, {0}.zindex " +
                          "FROM @times as t,  {0} " +
                          "ORDER BY zindex",  
                           joinTable, timestep0, timestep1, timestep2, timestep3),
                          contextConn);
                          //standardConn);
            cmd.CommandTimeout = 3600;
            /*Setup all four files and open them*/
            string pathSource = "e:\\filedb\\isotropic4096";
            string pathSource0 = pathSource + "\\" + dbname + "_" + timestep0 + ".bin";
            FileStream filedb0 = new FileStream(pathSource0, FileMode.Open, System.IO.FileAccess.Read);
            string pathSource1 = pathSource + "\\" + dbname + "_" + timestep1 + ".bin";
            FileStream filedb1 = new FileStream(pathSource1, FileMode.Open, System.IO.FileAccess.Read);
            string pathSource2 = pathSource + "\\" + dbname + "_" + timestep2 + ".bin";
            FileStream filedb2 = new FileStream(pathSource2, FileMode.Open, System.IO.FileAccess.Read);
            string pathSource3 = pathSource + "\\" + dbname + "_" + timestep3 + ".bin";
            FileStream filedb3 = new FileStream(pathSource3, FileMode.Open, System.IO.FileAccess.Read);

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int timestep = reader.GetSqlInt32(0).Value;  // Timestep returned
                    long thisBlob = reader.GetSqlInt64(1).Value; // Blob returned

                    
                    long z = thisBlob / (table.atomDim * table.atomDim * table.atomDim);
                    long offset = z * table.BlobByteSize;
                    if (timestep == timestep0)
                    {
                        filedb0.Seek(offset, SeekOrigin.Begin);
                        int bytes = filedb0.Read(rawdata, 0, table.BlobByteSize);
                    }
                    else if (timestep == timestep1)
                    {
                        filedb1.Seek(offset, SeekOrigin.Begin);
                        int bytes = filedb1.Read(rawdata, 0, table.BlobByteSize);
                    }
                    else if (timestep == timestep2)
                    {
                        filedb2.Seek(offset, SeekOrigin.Begin);
                        int bytes = filedb2.Read(rawdata, 0, table.BlobByteSize);
                    }
                    else if (timestep == timestep3)
                    {
                        filedb3.Seek(offset, SeekOrigin.Begin);
                        int bytes = filedb3.Read(rawdata, 0, table.BlobByteSize);
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
            filedb0.Close();
            filedb1.Close();
            filedb2.Close();
            filedb3.Close();
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
    public static void ExecuteMHDWorkerFileDBBatch(string serverName, //This is not filedb modified yet, but it can't be the same name as in executemhdworker.
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
            /* No more join table on filedb */
            /*
            cmd = new SqlCommand(
               String.Format(@"SELECT {0}.zindex, {0}.data " +
                          "FROM {1}, {0} WHERE {0}.timestep = {2} " +
                          "AND {1}.zindex = {0}.zindex",
                          tableName, joinTable, timestep_int), standardConn);
            cmd.CommandTimeout = 3600;
            */
            cmd = new SqlCommand(
               String.Format(@"SELECT {0}.zindex " +
                          "FROM {0} ORDER BY zindex",
                           joinTable),
                          contextConn);
            //standardConn);
            cmd.CommandTimeout = 3600;
            //Setup the file
            /*We need a better way of doing this--not hardcoded for sure! */
            string pathSource = "e:\\filedb\\isotropic4096";
            pathSource = pathSource + "\\" + dbname + "_" + timestep_int + ".bin";
            FileStream filedb = new FileStream(pathSource, FileMode.Open, System.IO.FileAccess.Read);
            //string[] tester = { "In filedb..." };
            //System.IO.File.WriteAllLines(@"e:\filedb\debug.txt", tester);
            //conn.InfoMessage += new SqlInfoMessageEventHandler(InfoMessageHandler);

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                //do
                //{
                while (reader.Read())
                {
                    // read in the current blob
                    long thisBlob = reader.GetSqlInt64(0).Value;
                    /*
                    int bytesread = 0;
                    while (bytesread < table.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(1, SqlArrayHeader, rawdata, bytesread, table.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }
                    */
                    long z = thisBlob / (table.atomDim * table.atomDim * table.atomDim);
                    long offset = z * table.BlobByteSize;
                    filedb.Seek(offset, SeekOrigin.Begin);
                    //Test
                    //string[] lines = { "Offset chosen = ", offset.ToString(), z.ToString(), table.BlobByteSize.ToString(), thisBlob.ToString(), pathSource, table.atomDim.ToString() };
                    //System.IO.File.WriteAllLines(@"e:\filedb\debug.txt", lines);

                    int bytes = filedb.Read(rawdata, 0, table.BlobByteSize);
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

   
};
