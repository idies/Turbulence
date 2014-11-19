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
    /// Stored procedure for the calculation of Box Filteres
    /// based on a pre-computation of box-sums
    /// </summary>
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void TestExecuteBoxFilterWorker(string serverName,
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
        SqlConnection standardConn;
        SqlConnection contextConn;
        string connString;
        connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName, codedb);
        standardConn = new SqlConnection(connString);
        contextConn = new SqlConnection("context connection=true");
        contextConn.Open();

        // Load information about the requested dataset
        TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, dataset, blobDim, contextConn);

        string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);

        // Instantiate a worker class
        if (workerType != (int)Worker.Workers.GetMHDBoxFilterSV &&
            workerType != (int)Worker.Workers.GetMHDBoxFilterSGS_SV)
        {
            contextConn.Close();
            throw new Exception("Invalid worker type specified! This procedure works only with a GetMHDBoxFilterSV or GetMHDBoxFilterSGS_SV worker!");
        }

        Worker worker = Worker.GetWorker(table, workerType, spatialInterp, arg, contextConn);
        //Turbulence.SQLInterface.workers.GetMHDBoxFilter3 worker = new Turbulence.SQLInterface.workers.GetMHDBoxFilter3(table,
        //    (TurbulenceOptions.SpatialInterpolation)spatialInterp,
        //    arg);

        //Dictionary<int, SQLUtility.MHDInputRequest> input = new Dictionary<int, SQLUtility.MHDInputRequest>(inputSize);
        HashSet<SQLUtility.MHDInputRequest> input = new HashSet<SQLUtility.MHDInputRequest>();
        // Read input data
        standardConn.Open();
        int startx = 0, starty = 0, startz = 0, xwidth = 0, ywidth = 0, zwidth = 0;
        string joinTable = SQLUtility.ReadTempTableGetAtomsToRead_Filtering(tempTable, worker, contextConn, standardConn, input,
            ref xwidth, ref ywidth, ref zwidth,
            ref startx, ref starty, ref startz);
        //contextConn.Close();

        SqlCommand cmd;

        SqlDataRecord record;

        record = new SqlDataRecord(worker.GetRecordMetaData());
        SqlContext.Pipe.SendResultsStart(record);

        byte[] rawdata = new byte[table.BlobByteSize];

        //BigArray<double> sums = new BigArray<double>((ulong)xwidth * (ulong)ywidth * (ulong)zwidth * (ulong)worker.GetResultSize());
        if (workerType == (int)Worker.Workers.GetMHDBoxFilterSV)
            ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSV)worker).InitializeSummedVolumes(xwidth, ywidth, zwidth);
        else if (workerType == (int)Worker.Workers.GetMHDBoxFilterSGS_SV)
            ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSGS_SV)worker).InitializeSummedVolumes(xwidth, ywidth, zwidth);
        else
            throw new Exception("Invalid worker type specified! This procedure works only with a GetMHDBoxFilter3 or GetMHDBoxFilterSGS_SV worker!");
        //worker.InitializeSummedVolumes(xwidth, ywidth, zwidth);

        if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.None)
        {
            // Go through and run values on each point

            // Find nearest timestep
            int timestep_int = SQLUtility.GetNearestTimestep(time, table);

            TurbulenceBlob atom = new TurbulenceBlob(table);

            cmd = new SqlCommand(
               String.Format(@"SELECT {0}.zindex, {0}.data " +
                          "FROM {1} INNER JOIN {0} " +
                          "ON {1}.zindex = {0}.zindex " +
                          "WHERE {0}.timestep = {2} " +
                          "ORDER BY {1}.zindex",
                          tableName, joinTable, timestep_int),
                          contextConn);
            cmd.CommandTimeout = 3600;

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
                        int bytes = (int)reader.GetBytes(1, table.SqlArrayHeaderSize, rawdata, bytesread, table.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }

                    atom.Setup(timestep_int, new Morton3D(thisBlob), rawdata);
                    //worker.UpdatedSummedVolumes(atom, startx, starty, startz, xwidth, ywidth, zwidth);
                    if (workerType == (int)Worker.Workers.GetMHDBoxFilterSV)
                        ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSV)worker).UpdateSummedVolumes(atom, startx, starty, startz, xwidth, ywidth, zwidth);
                    else if (workerType == (int)Worker.Workers.GetMHDBoxFilterSGS_SV)
                        ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSGS_SV)worker).UpdateSummedVolumes(atom, startx, starty, startz, xwidth, ywidth, zwidth);
                    else
                        throw new Exception("Invalid worker type specified! This procedure works only with a GetMHDBoxFilter3 or GetMHDBoxFilterSGS_SV worker!");
                }
                //} while (reader.NextResult());
            }

            float[] result;
            foreach (SQLUtility.MHDInputRequest point in input)
            {
                //result = worker.GetResult(sums, point, startx, starty, startz, xwidth, ywidth, zwidth);
                //result = worker.GetResult(point, startx, starty, startz, xwidth, ywidth, zwidth);
                if (workerType == (int)Worker.Workers.GetMHDBoxFilterSV)
                    result = ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSV)worker).GetResult(point, startx, starty, startz, xwidth, ywidth, zwidth);
                else if (workerType == (int)Worker.Workers.GetMHDBoxFilterSGS_SV)
                    result = ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSGS_SV)worker).GetResult(point, startx, starty, startz, xwidth, ywidth, zwidth);
                else
                    throw new Exception("Invalid worker type specified! This procedure works only with a GetMHDBoxFilter3 or GetMHDBoxFilterSGS_SV worker!");
                record.SetInt32(0, point.request);
                for (int r = 0; r < result.Length; r++)
                {
                    record.SetSqlSingle(r + 1, result[r]);
                }
                SqlContext.Pipe.SendResultsRow(record);
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
            atom = null;
        }
        else if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.PCHIP)
        {
            throw new NotImplementedException();
        }
        else
        {
            standardConn.Close();
            //contextConn.Close();
            input = null;
            rawdata = null;
            //worker.DeleteSummedVolumes();
            if (workerType == (int)Worker.Workers.GetMHDBoxFilterSV)
                ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSV)worker).DeleteSummedVolumes();
            else if (workerType == (int)Worker.Workers.GetMHDBoxFilterSGS_SV)
                ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSGS_SV)worker).DeleteSummedVolumes();
            else
                throw new Exception("Invalid worker type specified! This procedure works only with a GetMHDBoxFilter3 or GetMHDBoxFilterSGS_SV worker!");
            worker = null;
            throw new Exception("Unsupported TemporalInterpolation Type");
        }

        SqlContext.Pipe.SendResultsEnd();

        input = null;
        rawdata = null;
        //worker.DeleteSummedVolumes();
        if (workerType == (int)Worker.Workers.GetMHDBoxFilterSV)
            ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSV)worker).DeleteSummedVolumes();
        else if (workerType == (int)Worker.Workers.GetMHDBoxFilterSGS_SV)
            ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSGS_SV)worker).DeleteSummedVolumes();
        else
            throw new Exception("Invalid worker type specified! This procedure works only with a GetMHDBoxFilter3 or GetMHDBoxFilterSGS_SV worker!");
        worker = null;
        //sums = null;

        scan_count = 0;
        logical_reads = 0;
        physical_reads = 0;
        read_ahead_reads = 0;

        // We should not have to manually call the garbage collector.
        // System.GC.Collect();
    }
};
