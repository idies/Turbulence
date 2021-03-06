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

public partial class StoredProcedures
{
    /// <summary>
    /// Stored procedure for the calculation of Box Filteres
    /// based on a pre-computation of box-sums
    /// </summary>
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void ExecuteBoxFilterDBWorker(string serverName,
        string dbname,
        string codedb,
        string turbinfodb,
        string turbinfoserver,
        string dataset,
        int workerType,
        int blobDim,
        float time,
        int spatialInterp,  // TurbulenceOptions.SpatialInterpolation
        int temporalInterp, // TurbulenceOptions.TemporalInterpolation
        float arg,          // Extra argument (not used by all workers)
        int inputSize,
        string tempTable,
        long startz2,
        long endz2)
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
        TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, dataset, blobDim, serverinfo);

        string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);

        // Instantiate a worker class
        if (workerType != (int)Worker.Workers.GetMHDBoxFilterSV &&
            workerType != (int)Worker.Workers.GetMHDBoxFilterSGS_SV)
        {
            //contextConn.Close();
            throw new Exception("Invalid worker type specified! This procedure works only with a GetMHDBoxFilterSV or GetMHDBoxFilterSGS_SV worker!");
        }

        contextConn.Open();
        Worker worker = Worker.GetWorker(dbname, table, workerType, spatialInterp, arg, contextConn);
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

            //TurbulenceBlob atom = new TurbulenceBlob(table);

            //cmd = new SqlCommand(
            //   String.Format(@"SELECT {0}.zindex, {0}.data " +
            //              "FROM {1} INNER JOIN {0} " +
            //              "ON {1}.zindex = {0}.zindex " +
            //              "WHERE {0}.timestep = {2} " +
            //              "ORDER BY {1}.zindex",
            //              tableName, joinTable, timestep_int),
            //              contextConn);
            //cmd.CommandTimeout = 3600;

            TurbulenceBlob atom = new TurbulenceBlob(table);
            //string pathSource = "e:\\filedb\\isotropic4096";
            //pathSource = pathSource + "\\" + dbname + "_" + timestep_int + ".bin";
            string pathSource = SQLUtility.getDBfilePath(dbname, timestep_int, table.DataName, standardConn, serverName);
            FileStream filedb = new FileStream(pathSource, FileMode.Open, System.IO.FileAccess.Read);
            //string[] tester = { "In filedb..."};
            //System.IO.File.WriteAllLines(@"e:\filedb\debug.txt", tester);

            cmd = new SqlCommand();
            if (table.dbtype == 0)
            {
                cmd = new SqlCommand(
                   String.Format(@"SELECT {0}.zindex, {0}.data " +
                              "FROM {1}, {0} WHERE {0}.timestep = {2} " +
                              "AND {1}.zindex = {0}.zindex",
                              tableName, joinTable, timestep_int),
                              contextConn);
                //standardConn);
                cmd.CommandTimeout = 3600;
            }
            else if (table.dbtype == 1)
            {
                //This is ok I think because it is a temporary table
                cmd = new SqlCommand(
                   String.Format(@"SELECT {0}.zindex " +
                              "FROM {0} ORDER BY zindex",
                               joinTable),
                              contextConn);
                //standardConn);
                cmd.CommandTimeout = 3600;
                //Setup the file
            }

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                //do
                //{
                while (reader.Read())
                {
                    // read in the current blob
                    long thisBlob = reader.GetSqlInt64(0).Value;
                    if (table.dbtype == 0)
                    {
                        int bytesread = 0;
                        while (bytesread < table.BlobByteSize)
                        {
                            int bytes = (int)reader.GetBytes(1, table.SqlArrayHeaderSize, rawdata, bytesread, table.BlobByteSize - bytesread);
                            bytesread += bytes;
                        }
                    }
                    else if (table.dbtype == 1)
                    {
                        if (thisBlob <= endz2 && thisBlob >= startz2)
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
                        }
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
            filedb.Close();
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
