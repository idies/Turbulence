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

public partial class StoredProcedures
{
    /// <summary>
    /// Similar to the ExecuteMHDWorker but queries the database for two fields (from two DB tables).
    /// </summary>
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void ExecuteTwoFieldsDBWorker(
        string serverName,
        string dbname,
        string codedb,
        string turbinfodb,
        string turbinfoserver,
        string field1,
        string field2,
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
        TurbServerInfo serverinfo = TurbServerInfo.GetTurbServerInfo(codedb, turbinfodb, turbinfoserver);
        SqlConnection standardConn;
        SqlConnection contextConn;
        string connString;
        if (serverName.Contains("_"))
            connString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverName.Remove(serverName.IndexOf("_")), serverinfo.codeDB);
        else
            connString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverName, serverinfo.codeDB);
        standardConn = new SqlConnection(connString);
        contextConn = new SqlConnection("context connection=true");

        // Load information about the requested dataset
        TurbDataTable table1 = TurbDataTable.GetTableInfo(serverName, dbname, field1, blobDim, serverinfo);
        TurbDataTable table2 = TurbDataTable.GetTableInfo(serverName, dbname, field2, blobDim, serverinfo);

        string tableName1 = String.Format("{0}.dbo.{1}", dbname, table1.TableName);
        string tableName2 = String.Format("{0}.dbo.{1}", dbname, table2.TableName);

        // Instantiate a worker class
        contextConn.Open();
        Worker worker = Worker.GetWorker(table1, table2, workerType, spatialInterp, arg, contextConn);

        float points_per_cube = 0;

        Dictionary<long, List<int>> map;
        Dictionary<int, SQLUtility.MHDInputRequest> input = new Dictionary<int, SQLUtility.MHDInputRequest>(inputSize);
        // Read input data
        map = SQLUtility.ReadTempTableGetAtomsToRead(tempTable, worker, (Worker.Workers)workerType, contextConn, input, inputSize, ref points_per_cube);

        SqlCommand cmd;

        SqlDataRecord record;

        byte[] rawdata1 = new byte[table1.BlobByteSize];
        byte[] rawdata2 = new byte[table2.BlobByteSize];
        
        //For each point detemine the relevant cubes 
        //and create a table to perform query via a JOIN
        standardConn.Open();
        string joinTable = "";
        joinTable = SQLUtility.CreateTemporaryJoinTable(map.Keys, standardConn, points_per_cube);

        record = new SqlDataRecord(worker.GetRecordMetaData());
        SqlContext.Pipe.SendResultsStart(record);

        if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.None)
        {
            // Go through and run values on each point

            // Find nearest timestep
            int timestep_int = SQLUtility.GetNearestTimestep(time, table1);

            TurbulenceBlob blob1 = new TurbulenceBlob(table1);
            TurbulenceBlob blob2 = new TurbulenceBlob(table2);

            string pathSource1 = SQLUtility.getDBfilePath(dbname, timestep_int, tableName1, standardConn, serverName);
            FileStream filedb1 = new FileStream(pathSource1, FileMode.Open, System.IO.FileAccess.Read);
            string pathSource2 = SQLUtility.getDBfilePath(dbname, timestep_int, tableName2, standardConn, serverName);
            FileStream filedb2 = new FileStream(pathSource2, FileMode.Open, System.IO.FileAccess.Read);

            //This is ok I think because it is a temporary table
            cmd = new SqlCommand(
               String.Format(@"SELECT {0}.zindex " +
                          "FROM {0} ORDER BY zindex",
                          joinTable),
                          contextConn);
                          //standardConn);

            //cmd = new SqlCommand(
            //   String.Format(@"SELECT f1.zindex, f1.data, f2.data " +
            //              "FROM {2}, {0} as f1, {1} as f2 WHERE f1.timestep = {3} " +
            //              "AND f2.timestep = f1.timestep " +
            //              "AND {2}.zindex = f1.zindex " +
            //              "AND {2}.zindex = f2.zindex",
            //              tableName1, tableName2, joinTable, timestep_int),
            //              contextConn);
            //              //standardConn);
            cmd.CommandTimeout = 3600;

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
                        /*we assume table1 and table2 have the same atomDim, zindex Range etc..*/
                        /*But the BlobByteSize may be different, because, e.g., u vs p*/
                        long fileBlob = thisBlob % 134217728;
                        long z = fileBlob / (table1.atomDim * table1.atomDim * table1.atomDim);
                        long offset1 = z * table1.BlobByteSize;
                        filedb1.Seek(offset1, SeekOrigin.Begin);
                        int bytes1 = filedb1.Read(rawdata1, 0, table1.BlobByteSize);
                        long offset2 = z * table2.BlobByteSize;
                        filedb2.Seek(offset2, SeekOrigin.Begin);
                        int bytes2 = filedb1.Read(rawdata2, 0, table2.BlobByteSize);
                        //Test
                        //string[] lines= { "Offset chosen = ", offset.ToString(), z.ToString(), table.BlobByteSize.ToString(), thisBlob.ToString(),pathSource, table.atomDim.ToString()};
                        //System.IO.File.WriteAllLines(@"e:\filedb\debug.txt", lines);
                    }

                    //int bytesread = 0;
                    //while (bytesread < table1.BlobByteSize)
                    //{
                    //    int bytes = (int)reader.GetBytes(1, table1.SqlArrayHeaderSize, rawdata1, bytesread, table1.BlobByteSize - bytesread);
                    //    bytesread += bytes;
                    //} 
                    //bytesread = 0;
                    //while (bytesread < table2.BlobByteSize)
                    //{
                    //    int bytes = (int)reader.GetBytes(2, table2.SqlArrayHeaderSize, rawdata2, bytesread, table2.BlobByteSize - bytesread);
                    //    bytesread += bytes;
                    //}

                    blob1.Setup(timestep_int, new Morton3D(thisBlob), rawdata1);
                    blob2.Setup(timestep_int, new Morton3D(thisBlob), rawdata2);

                    foreach (int point in map[thisBlob])
                    {
                        double[] result = worker.GetResult(blob1, blob2, input[point]);
                        for (int r = 0; r < result.Length; r++)
                        {
                            input[point].result[r] += result[r];
                        }
                        input[point].cubesRead++;

                        if (input[point].cubesRead == input[point].numberOfCubes && !input[point].resultSent)
                        {
                            record.SetInt32(0, input[point].request);
                            int r = 0;
                            for (; r < input[point].result.Length; r++)
                            {
                                record.SetSqlSingle(r + 1, (float)input[point].result[r]);
                            }

                            SqlContext.Pipe.SendResultsRow(record);
                            input[point].resultSent = true;

                            input[point].lagInt = null;
                            input[point].result = null;
                            input[point] = null;
                        }
                    }
                }
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
            filedb1.Close();
            filedb2.Close();
            blob1 = null;
            blob2 = null;
        }
        else if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.PCHIP)
        {
            // Perform PCHIP interpolation by querying & grouping 4 timesteps by location
            // We process the results from the database as they come out, and then calculate PCHIP at the end
            // In the case that we have 2 fields they will have the same time increment, offset, dt, etc.
            int basetime = SQLUtility.GetFlooredTimestep(time, table1);

            int timestep0 = basetime - table1.TimeInc;
            int timestep1 = basetime;
            int timestep2 = basetime + table1.TimeInc;
            int timestep3 = basetime + table1.TimeInc * 2;

            float time0 = (timestep0 - table1.TimeOff) * table1.Dt;
            float time1 = (timestep1 - table1.TimeOff) * table1.Dt;
            float time2 = (timestep2 - table1.TimeOff) * table1.Dt;
            float time3 = (timestep3 - table1.TimeOff) * table1.Dt;

            float delta = time2 - time1;

            double[] result;

            TurbulenceBlob blob1 = new TurbulenceBlob(table1);
            TurbulenceBlob blob2 = new TurbulenceBlob(table2);
            cmd = new SqlCommand(
                String.Format(@"DECLARE @times table (timestep int NOT NULL) " +
                          "INSERT @times VALUES ({3}) " +
                          "INSERT @times VALUES ({4}) " +
                          "INSERT @times VALUES ({5}) " +
                          "INSERT @times VALUES ({6}) " +

                          "SELECT f1.timestep, f1.zindex, f1.data, f2.data " +
                          "FROM @times as t, {0} as f1, {1} as f2, {2} " +
                          "WHERE f1.timestep = t.timestep " +
                          "AND f2.timestep = t.timestep " +
                          "AND f1.zindex = {2}.zindex " +
                          "AND f2.zindex = {2}.zindex",
                          tableName1, tableName2, joinTable, timestep0, timestep1, timestep2, timestep3),
                          contextConn);
            cmd.CommandTimeout = 3600;
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int timestep = reader.GetSqlInt32(0).Value;  // Timestep returned
                    long thisBlob = reader.GetSqlInt64(1).Value; // Blob returned
                    
                    int bytesread = 0;
                    while (bytesread < table1.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(2, table1.SqlArrayHeaderSize, rawdata1, bytesread, table1.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }
                    blob1.Setup(timestep, new Morton3D(thisBlob), rawdata1); 
                    bytesread = 0;
                    while (bytesread < table2.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(2, table2.SqlArrayHeaderSize, rawdata2, bytesread, table2.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }
                    blob2.Setup(timestep, new Morton3D(thisBlob), rawdata2);

                    foreach (int point in map[thisBlob])
                    {
                        if (worker == null)
                            throw new Exception("worker is NULL!");
                        if (blob1 == null)
                            throw new Exception("blob1 is NULL!");
                        if (blob2 == null)
                            throw new Exception("blob2 is NULL!");
                        if (input[point] == null)
                            throw new Exception("input[point] is NULL!");

                        result = worker.GetResult(blob1, blob2, input[point]);
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
                            SqlContext.Pipe.SendResultsRow(record);
                            input[point].resultSent = true;

                            input[point].lagInt = null;
                            input[point].result = null;
                            input[point] = null;
                        }
                    }
                }
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
            blob1 = null;
            blob2 = null;
        }
        else
        {
            standardConn.Close();
            contextConn.Close();
            map.Clear();
            map = null;
            input = null;
            rawdata1 = null;
            rawdata2 = null;
            worker = null;
            throw new Exception("Unsupported TemporalInterpolation Type");
        }

        SqlContext.Pipe.SendResultsEnd();

        map.Clear();
        map = null;
        input = null;
        rawdata1 = null;
        rawdata2 = null;
        worker = null;

        scan_count = 0;
        logical_reads = 0;
        physical_reads = 0;
        read_ahead_reads = 0;
    }
};
