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

/* Original turubulence.  Remove this in the future*/
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
    public static void ExecuteTurbulenceWorker(
        string database,
        string dataset,
        int workerType,
        float time,
        int spatialInterp,  // TurbulenceOptions.TemporalInterpolation
        int temporalInterp, // TurbulenceOptions.SpatialInterpolation
        int arg,            // Extra argument (not used by all workers)
        string tempTable)
    {
        TimeSpan IOTime = new TimeSpan(0), preProcessTime = new TimeSpan(0), resultTime = new TimeSpan(0), blobSetupTime = new TimeSpan(0);
        DateTime startTime, endTime;

        startTime = DateTime.Now;
        // clean database name
        database = SQLUtility.SanitizeDatabaseName(database);
        // Check temp table
        tempTable = SQLUtility.SanitizeTemporaryTable(tempTable);

        // Load information about the requested dataset
        TurbDataTable table = TurbDataTable.GetTableInfo(dataset);

        string tableName = String.Format("{0}.dbo.{1}", database, table.TableName);

        // Instantiate a worker class
        SqlConnection conn = new SqlConnection("context connection=true");
        conn.Open();
        Worker worker = Worker.GetWorker(table, workerType, spatialInterp, arg, conn);
        conn.Close();

        // Bitmask to ignore low order bits of address
        //long mask = ~(long)(table.BlobDim * table.BlobDim * table.BlobDim - 1);

        //SQLUtility.InputRequest[] input;
        Dictionary<long, List<SQLUtility.InputRequest>> input;
        // Read input data
        if (workerType == (int)(Worker.Workers.GetPosition))
        {
            throw new Exception("GetPosition is not currently supported");
            //input = SQLUtility.ReadTrackingTemporaryTable(tempTable);
        }
        else
        {
            //input = SQLUtility.ReadAndSortTemporaryTable(tempTable, mask);
            input = SQLUtility.ReadAndSortTemporaryTable(tempTable);
        }

        // TODO: Replace this deprecated hack.
        int inputSize = SQLUtility.GetTempTableLength(tempTable);


        // Output SQL column names
        //worker.SendSqlOutputHeaders();

        SqlCommand cmd;
        //SqlDataReader reader;
        SqlDataRecord record = new SqlDataRecord(worker.GetRecordMetaData());
        SqlContext.Pipe.SendResultsStart(record);
        byte[] rawdata = new byte[table.BlobByteSize];

        // Create a table to perform query via a JOIN instead of x IN ( ... ) syntax
        string joinTable = SQLUtility.SelectDistinctIntoTemporaryTable(tempTable);

        endTime = DateTime.Now;
        preProcessTime += endTime - startTime;

        //throw new Exception(zindexRegionsString);
        if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.None)
        {
            // Go through and run values on each point

            // Find nearest timestep
            int timestep_int = SQLUtility.GetNearestTimestep(time, table);

            TurbulenceBlob blob = new TurbulenceBlob(table);
            conn = new SqlConnection("context connection=true");
            conn.Open();
            cmd = new SqlCommand(
               String.Format(@"SELECT {0}.zindex, {0}.data " +
                          "FROM {0}, {1} WHERE {0}.timestep = {2} " +
                          "AND {0}.zindex = {1}.zindex",
                          tableName, joinTable, timestep_int), conn);

            startTime = DateTime.Now;
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    // read in the current blob
                    long thisBlob = reader.GetSqlInt64(0).Value;
                    int bytesread = 0;
                    while (bytesread < table.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(1, 0, rawdata, bytesread, table.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }

                    endTime = DateTime.Now;
                    IOTime += endTime - startTime;

                    startTime = DateTime.Now;
                    blob.Setup(timestep_int, new Morton3D(thisBlob), rawdata);

                    endTime = DateTime.Now;
                    blobSetupTime += endTime - startTime;

                    // Only execute related particles
                    foreach (SQLUtility.InputRequest point in input[thisBlob])
                    {
                        startTime = DateTime.Now;
                        double[] result;
                        try
                        {
                             result = worker.GetResult(blob, point);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(String.Format("Error on point X:{0},Y:{1},Z:{2}, blob={3}.  [Inner Exception: {4}])",
                                (double)point.x, (double)point.y, (double)point.z, blob.ToString(),e.ToString()));
                        }
                        record.SetInt32(0, point.request);
                        int r = 0;
                        for (; r < result.Length; r++)
                        {
                                record.SetSqlSingle(r + 1, (float)result[r]);
                        }

                        endTime = DateTime.Now;
                        resultTime += endTime - startTime;

                        SqlContext.Pipe.SendResultsRow(record);
                    }

                    startTime = DateTime.Now;
                }
            }
            conn.Close();
            blob = null;
        }
        else if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.PCHIP)
        {
            // Perform PCHIP interpolation by querying & grouping 4 timesteps by location
            // We process the results from the database as they come out, and then calculate PCHIP at the end
            int basetime = SQLUtility.GetFlooredTimestep(time, table);
            int[] timesteps = { basetime - table.TimeInc,
                        basetime,
                        basetime + table.TimeInc,
                        basetime + table.TimeInc * 2 };
            float[] times = { timesteps[0] * table.Dt,
                timesteps[1] * table.Dt,
                timesteps[2] * table.Dt,
                timesteps[3] * table.Dt,
            };

            //TurbulenceBlob [] blobs = new TurbulenceBlob[4];
            //for (int i = 0; i < blobs.Length; i++)
            //{
            //    blobs[i] = new TurbulenceBlob(table);
            //}
            TurbulenceBlob blob = new TurbulenceBlob(table);
            conn = new SqlConnection("context connection=true");
            conn.Open();
            cmd = new SqlCommand(
                String.Format(@"SELECT {0}.timestep, {0}.zindex, {0}.data " +
                          "FROM {0}, {1} WHERE {0}.timestep IN ({2}, {3}, {4}, {5}) " +
                          "AND {0}.zindex = {1}.zindex",
                          tableName, joinTable, timesteps[0], timesteps[1], timesteps[2], timesteps[3]), conn);
            using (SqlDataReader reader = cmd.ExecuteReader())
            { 
                // TODO: Get rid of the jagged arrays.
                //       This will require a rewrite of the workers
                double[,][] results = new double[inputSize, 4][];
                int[] localResultMap = new int[inputSize];  // store original ID numbers
                while (reader.Read())
                {
                    int timestep = reader.GetSqlInt32(0).Value;  // Timestep returned
                    long thisBlob = reader.GetSqlInt64(1).Value; // Blob returned
                    int bytesread = 0;

                    int PCHIPTime = -1; // Which PCHIPTime time step are we looking at? (0-3)
                    for (int i = 0; i < 4; i++)
                        if (timestep == timesteps[i])
                            PCHIPTime = i;
                    if (PCHIPTime == -1)
                    {
                        throw new Exception(String.Format(@"Unexpected timestep: {0}, looking for {1},{2},{3},{4}",
                            timestep, timesteps[0], timesteps[1], timesteps[2], timesteps[3]));
                    }
                 
                    while (bytesread < table.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(2, 0, rawdata, bytesread, table.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }
                    blob.Setup(timestep, new Morton3D(thisBlob), rawdata);
                    foreach (SQLUtility.InputRequest point in input[thisBlob])
                    {
                        localResultMap[point.localID] = point.request;
                        try
                        {
                            results[point.localID, PCHIPTime] = worker.GetResult(blob, point);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(String.Format("Error on point X:{0},Y:{1},Z:{2}.  [Inner Exception: {3}])",
                                (double)point.x, (double)point.y, (double)point.z, e.ToString()));
                        }
                    }
                }
                /* Now go through results and calculate the interpolated value */

                for (int i = 0; i < inputSize; i++)
                {
                    record.SetInt32(0, localResultMap[i]);
                    for (int r = 0; r < results[i,0].Length; r++)
                    {
                        record.SetSqlSingle(r + 1,
                            TemporalInterpolation.PCHIP(time,
                            times[0], times[1], times[2], times[3],
                            (float)results[i, 0][r], (float)results[i, 1][r], (float)results[i, 2][r], (float)results[i, 3][r]));
                    }
                    SqlContext.Pipe.SendResultsRow(record);
                }
            }
            conn.Close();
            // Encourage garbage collector to clean up.
            blob = null;
        }
        else
        {
            throw new Exception("Unsupported TemporalInterpolation Type");
        }

        input = null;
        rawdata = null;
        worker = null;
        SqlContext.Pipe.SendResultsEnd();

        // We should not have to manually call the garbage collector.
        // System.GC.Collect();
    }
};
