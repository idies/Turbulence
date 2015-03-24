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
    public static void ExecuteGetPositionWorker(
        string serverName,
        string database,
        string codedb,
        string dataset,
        int workerType,
        int blobDim,
        float time,
        int spatialInterp,  // TurbulenceOptions.TemporalInterpolation
        int temporalInterp, // TurbulenceOptions.SpatialInterpolation
        int correcting_pos,            // Extra argument (not used by all workers)
        float dt,
        int inputSize,
        string tempTable)
    {
        // clean database name
        database = SQLUtility.SanitizeDatabaseName(database);
        // Check temp table
        tempTable = SQLUtility.SanitizeTemporaryTable(tempTable);

        SqlConnection standardConn;
        SqlConnection contextConn;
        string connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName, codedb);
        standardConn = new SqlConnection(connString);
        contextConn = new SqlConnection("context connection=true");
        contextConn.Open();

        // Load information about the requested dataset
        TurbDataTable table = TurbDataTable.GetTableInfo(serverName, database, dataset, blobDim, contextConn);

        string tableName = String.Format("{0}.dbo.{1}", database, table.TableName);

        // Instantiate a worker class
        Turbulence.SQLInterface.workers.GetPosition getPosition_worker = 
            (Turbulence.SQLInterface.workers.GetPosition)Worker.GetWorker(table, workerType, spatialInterp, correcting_pos, contextConn);

        Turbulence.SQLInterface.workers.GetVelocityWorker velocity_worker = 
            (Turbulence.SQLInterface.workers.GetVelocityWorker)Worker.GetWorker(table, 
            (int)Worker.Workers.GetVelocity, spatialInterp, correcting_pos, contextConn);

        float points_per_cube = 0;

        Dictionary<long, List<int>> map;
        Dictionary<int, SQLUtility.MHDInputRequest> input = new Dictionary<int, SQLUtility.MHDInputRequest>(inputSize);
        // Read input data
        if (workerType != (int)(Worker.Workers.GetPosition))
        {
            contextConn.Close();
            throw new Exception("ExecuteGetPositionWorker stored procedure only works for a GetPosition worker");
        }
        else
        {
            //map = SQLUtility.ReadTempTableGetCubesToRead(tempTable, table,
            //    velocity_worker.GetResultSize(), contextConn,
            //    input, time,
            //    ref points_per_cube, correcting_pos);
            map = SQLUtility.ReadTempTableGetAtomsToRead(tempTable, getPosition_worker, (Worker.Workers)workerType, contextConn, input, inputSize, ref points_per_cube);
        }

        SqlCommand cmd;
        SqlDataRecord record = new SqlDataRecord(getPosition_worker.GetRecordMetaData());
        SqlContext.Pipe.SendResultsStart(record);
        byte[] rawdata = new byte[table.BlobByteSize];

        // Create a table to perform query via a JOIN instead of x IN ( ... ) syntax
        string joinTable;
        //joinTable = SQLUtility.SelectDistinctIntoTemporaryTable(tempTable, contextConn);
        standardConn.Open();
        joinTable = SQLUtility.CreateTemporaryJoinTable(map.Keys, standardConn, points_per_cube);

        if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.None)
        {
            // Go through and run values on each point

            // Find nearest timestep
            int timestep_int = SQLUtility.GetNearestTimestep(time, table);

            TurbulenceBlob blob = new TurbulenceBlob(table);
            cmd = new SqlCommand(
               String.Format(@"SELECT {0}.zindex, {0}.data " +
                          "FROM {0}, {1} WHERE {0}.timestep = {2} " +
                          "AND {0}.zindex = {1}.zindex",
                          tableName, joinTable, timestep_int),
                          standardConn);
                          //contextConn);
                          //conn);

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    // read in the current blob
                    long thisBlob = reader.GetSqlInt64(0).Value;
                    int bytesread = 0;
                    while (bytesread < table.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(1, table.SqlArrayHeaderSize, rawdata, bytesread, table.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }

                    blob.Setup(timestep_int, new Morton3D(thisBlob), rawdata);

                    // Only execute related particles
                    foreach (int point in map[thisBlob])
                    {
                        //point = input[thisBlob][i];
                        double[] velocity;
                        try
                        {
                            //if (correcting_pos == 1)
                                if (dataset.Equals("isotropic1024coarse") || dataset.Equals("isotropic1024fine"))
                                    velocity = velocity_worker.CalcVelocity(blob, input[point].x, input[point].y, input[point].z);
                                else
                                    velocity = velocity_worker.CalcVelocity(blob, input[point].x, input[point].y, input[point].z, input[point]);
                            //else
                            //    if (dataset.Equals("isotropic1024coarse") || dataset.Equals("isotropic1024fine"))
                            //        velocity = velocity_worker.CalcVelocity(blob, input[point].predictor.x, input[point].predictor.y, input[point].predictor.z);
                            //    else
                            //        velocity = velocity_worker.CalcVelocity(blob, input[point].predictor.x, input[point].predictor.y, input[point].predictor.z, input[point]);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(String.Format("Error on point X:{0},Y:{1},Z:{2}." +
                                "[Inner Exception: {4}])",
                                (double)input[point].x, (double)input[point].y, (double)input[point].z, blob.ToString(), e.ToString()));
                                //,input[point].predictor.x, input[point].predictor.y, input[point].predictor.z, correcting_pos));
                        }
                        for (int r = 0; r < velocity.Length; r++)
                        {
                            input[point].result[r] += velocity[r];
                        }
                        input[point].cubesRead++;

                        if (input[point].cubesRead == input[point].numberOfCubes && !input[point].resultSent)
                        {
                            double[] result = getPosition_worker.GetResult(input[point], dt);
                            record.SetInt32(0, input[point].request);
                            int r = 0;
                            for (; r < result.Length; r++)
                            {
                                record.SetSqlSingle(r + 1, (float)result[r]);
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
            }

            cmd = new SqlCommand(String.Format(@"DELETE FROM {0}", tempTable), contextConn);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Error deleting from temporary table for particle tracking.  [Inner Exception: {0}])",
                    e.ToString()));
            }

            blob = null;
        }
        else if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.PCHIP)
        {
            // Perform PCHIP interpolation by querying & grouping 4 timesteps by location
            // We process the results from the database as they come out
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
            double[] velocity;

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
                          standardConn);
                          //contextConn);
                          //conn);
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int timestep = reader.GetSqlInt32(0).Value;  // Timestep returned
                    long thisBlob = reader.GetSqlInt64(1).Value; // Blob returned
                    int bytesread = 0;

                    while (bytesread < table.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(2, table.SqlArrayHeaderSize, rawdata, bytesread, table.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }
                    blob.Setup(timestep, new Morton3D(thisBlob), rawdata);
                    foreach (int point in map[thisBlob])
                    {
                        try
                        {
                            //if (correcting_pos == 1)
                                if (dataset.Equals("isotropic1024coarse") || dataset.Equals("isotropic1024fine"))
                                    velocity = velocity_worker.CalcVelocity(blob, input[point].x, input[point].y, input[point].z);
                                else
                                    velocity = velocity_worker.CalcVelocity(blob, input[point].x, input[point].y, input[point].z, input[point]);
                            //else
                                //if (dataset.Equals("isotropic1024coarse") || dataset.Equals("isotropic1024fine"))
                                //    velocity = velocity_worker.CalcVelocity(blob, input[point].predictor.x, input[point].predictor.y, input[point].predictor.z);
                                //else
                                //    velocity = velocity_worker.CalcVelocity(blob, input[point].predictor.x, input[point].predictor.y, input[point].predictor.z, input[point]);
                            for (int r = 0; r < velocity.Length; r++)
                            {
                                if (timestep == timestep0)
                                {
                                    input[point].result[r] += -velocity[r] * (time - time1) * (1 + (time - time1) * (-1 + (time - time2) / delta) / delta) / 2 / delta;
                                }
                                else if (timestep == timestep1)
                                {
                                    input[point].result[r] += velocity[r] * (1 + ((time - time1) * (time - time1) * (-2 + 3 * (time - time2) / delta) / 2 / delta / delta));
                                }
                                else if (timestep == timestep2)
                                {
                                    input[point].result[r] += velocity[r] * (time - time1) * (1 + (time - time1) * (1 - 3 * (time - time2) / delta) / delta) / 2 / delta;
                                }
                                else if (timestep == timestep3)
                                {
                                    input[point].result[r] += velocity[r] * (time - time1) * (time - time1) * (time - time2) / 2 / delta / delta / delta;
                                }
                            }
                            input[point].cubesRead++;

                            if (input[point].cubesRead == 4 * input[point].numberOfCubes && !input[point].resultSent)
                            {
                                result = getPosition_worker.GetResult(input[point], dt);
                                record.SetInt32(0, input[point].request);
                                for (int r = 0; r < result.Length; r++)
                                {
                                    record.SetSqlSingle(r + 1, (float)result[r]);
                                }
                                SqlContext.Pipe.SendResultsRow(record);
                                input[point].resultSent = true;

                                input[point].lagInt = null;
                                input[point].result = null;
                                input[point] = null;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception(String.Format("Error on point[{3}] X:{0},Y:{1},Z:{2}." +
                                "[Inner Exception: {4}])",
                                (double)input[point].x, (double)input[point].y, (double)input[point].z, input[point].request, e.ToString()));
                                //,input[point].predictor.x, input[point].predictor.y, input[point].predictor.z, correcting_pos));
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
                throw new Exception(String.Format("Error deleting from temporary table for particle tracking.  [Inner Exception: {0}])",
                    e.ToString()));
            }

            // Encourage garbage collector to clean up.
            blob = null;
        }
        else
        {
            throw new Exception("Unsupported TemporalInterpolation Type");
        }

        //conn.Close();
        //if (!dataset.Equals("isotropic1024coarse") && !dataset.Equals("isotropic1024fine"))
        //{
            contextConn.Close();
            standardConn.Close();
        //}

        input = null;
        rawdata = null;
        velocity_worker = null;
        getPosition_worker = null;

        SqlContext.Pipe.SendResultsEnd();

        // We should not have to manually call the garbage collector.
        // System.GC.Collect();
    }
};
