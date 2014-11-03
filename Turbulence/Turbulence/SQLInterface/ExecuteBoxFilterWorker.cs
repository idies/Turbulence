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
    public static void ExecuteBoxFilterWorker(string serverName,
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
        if (serverName.Length > 4)
            connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName.Remove(4), codedb);
        else
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

            //int firstLine, lastLine, firstPoint, lastPoint;
            //int data_index, sumsx, sumsy, sumsz, sums_index0, sums_index1, data_index1;
            //ulong sums_index, temp_sums_index = 0;
            //double[] temp_sum = new double[table.Components];

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

                    //sums_index0 = atom.GetBaseX - startx;

                    //for (int atomz = 0; atomz < atom.GetSide; atomz++)
                    //{
                    //    sumsz = atomz + atom.GetBaseZ - startz;
                    //    sums_index1 = sums_index0 + sumsz * ywidth * xwidth;
                    //    data_index1 = atomz * atom.GetSide * atom.GetSide * table.Components;
                    //    for (int atomy = 0; atomy < atom.GetSide; atomy++)
                    //    {
                    //        sumsy = atomy + atom.GetBaseY - starty;
                    //        sums_index = (ulong)((sums_index1 + sumsy * xwidth) * table.Components);
                    //        data_index = data_index1 + atomy * atom.GetSide * table.Components;
                    //        for (int atomx = 0; atomx < atom.GetSide; atomx++)
                    //        {
                    //            sumsx = atomx + sums_index0;
                    //            //data_index = (atomz * atom.GetSide * atom.GetSide + atomy * atom.GetSide + atomx) * table.Components;
                    //            //sums_index = (ulong)((sumsz * ywidth * xwidth + sumsy * xwidth + sumsx) * table.Components);
                    //            for (int component = 0; component < table.Components; component++)
                    //            {
                    //                sums[sums_index + (ulong)component] += atom.data[data_index + component];
                    //                temp_sum[component] = sums[sums_index + (ulong)component];
                    //            }

                    //            // We need to update point (x+1,y,z)
                    //            // Unless x+1 is greater than or equal to xwidth
                    //            if (sumsx + 1 < xwidth)
                    //            {
                    //                temp_sums_index = sums_index + (ulong)table.Components;
                    //                for (int component = 0; component < table.Components; component++)
                    //                {
                    //                    sums[temp_sums_index + (ulong)component] += temp_sum[component];
                    //                }
                    //            }
                    //            // We need to update point (x,y+1,z)
                    //            // Unless y+1 is greater than or equal to ywidth
                    //            if (sumsy + 1 < ywidth)
                    //            {
                    //                temp_sums_index = sums_index + (ulong)(xwidth * table.Components);
                    //                for (int component = 0; component < table.Components; component++)
                    //                {
                    //                    sums[temp_sums_index + (ulong)component] += temp_sum[component];
                    //                }
                    //            }
                    //            // We need to update point (x+1,y+1,z)
                    //            // Unless x+1 is greater than or equal to xwidth
                    //            // or y+1 is greater than or equal to ywidth
                    //            if ((sumsx + 1 < xwidth) && (sumsy + 1 < ywidth))
                    //            {
                    //                temp_sums_index = sums_index + (ulong)(xwidth * table.Components + table.Components);
                    //                for (int component = 0; component < table.Components; component++)
                    //                {
                    //                    sums[temp_sums_index + (ulong)component] -= temp_sum[component];
                    //                }
                    //            }
                    //            // We need to update point (x,y,z+1)
                    //            // Unless z+1 is greater than or equal to zwidth
                    //            if (sumsz + 1 < zwidth)
                    //            {
                    //                temp_sums_index = sums_index + (ulong)(ywidth * xwidth * table.Components);
                    //                for (int component = 0; component < table.Components; component++)
                    //                {
                    //                    sums[temp_sums_index + (ulong)component] += temp_sum[component];
                    //                }
                    //            }
                    //            // We need to update point (x+1,y,z+1)
                    //            // Unless x+1 is greater than or equal to xwidth
                    //            // or z+1 is greater than or equal to zwidth
                    //            if ((sumsx + 1 < xwidth) && (sumsz + 1 < zwidth))
                    //            {
                    //                temp_sums_index = sums_index + (ulong)(ywidth * xwidth * table.Components + table.Components);
                    //                for (int component = 0; component < table.Components; component++)
                    //                {
                    //                    sums[temp_sums_index + (ulong)component] -= temp_sum[component];
                    //                }
                    //            }
                    //            // We need to update point (x,y+1,z+1)
                    //            // Unless z+1 is greater than or equal to zwidth
                    //            // or y+1 is greater than or equal to ywidth
                    //            if ((sumsz + 1 < zwidth) && (sumsy + 1 < ywidth))
                    //            {
                    //                temp_sums_index = sums_index + (ulong)(ywidth * xwidth * table.Components + xwidth * table.Components);
                    //                for (int component = 0; component < table.Components; component++)
                    //                {
                    //                    sums[temp_sums_index + (ulong)component] -= temp_sum[component];
                    //                }
                    //            }
                    //            // We need to update point (x+,y+1,z+1)
                    //            // Unless x+1 is greater than or equal to xwidth
                    //            // or y+1 is greater than or equal to ywidth
                    //            // or z+1 is greater than or equal to zwidth
                    //            if ((sumsx + 1 < xwidth) && (sumsy + 1 < ywidth) && (sumsz + 1 < zwidth))
                    //            {
                    //                temp_sums_index = sums_index + (ulong)(ywidth * xwidth * table.Components + xwidth * table.Components + table.Components);
                    //                for (int component = 0; component < table.Components; component++)
                    //                {
                    //                    sums[temp_sums_index + (ulong)component] += temp_sum[component];
                    //                }
                    //            }

                    //            sums_index += (ulong)table.Components;
                    //            data_index += table.Components;
                    //        }
                    //    }
                    //}
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
