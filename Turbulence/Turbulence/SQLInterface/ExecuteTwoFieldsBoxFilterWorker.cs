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
    public static void ExecuteTwoFieldsBoxFilterWorker(string serverName,
        string dbname,
        string codedb,
        string field1,
        string field2,
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
        if (serverName.Contains("_"))
            connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName.Remove(serverName.IndexOf("_")), codedb);
        else
            connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName, codedb);
        standardConn = new SqlConnection(connString);
        contextConn = new SqlConnection("context connection=true");
        contextConn.Open();

        // Load information about the requested dataset
        TurbDataTable table1 = TurbDataTable.GetTableInfo(serverName, dbname, field1, blobDim, contextConn);
        TurbDataTable table2 = TurbDataTable.GetTableInfo(serverName, dbname, field2, blobDim, contextConn);

        string tableName1 = String.Format("{0}.dbo.{1}", dbname, table1.TableName);
        string tableName2 = String.Format("{0}.dbo.{1}", dbname, table2.TableName);

        // Instantiate a worker class
        if (workerType != (int)Worker.Workers.GetMHDBoxFilterSGS_SV)
        {
            contextConn.Close();
            throw new Exception("Invalid worker type specified! This procedure works only with a GetMHDBoxFilterSV or GetMHDBoxFilterSGS_SV worker!");
        }

        Worker worker = Worker.GetWorker(table1, table2, workerType, spatialInterp, arg, contextConn);

        HashSet<SQLUtility.MHDInputRequest> input = new HashSet<SQLUtility.MHDInputRequest>();
        // Read input data
        standardConn.Open();
        int startx = 0, starty = 0, startz = 0, xwidth = 0, ywidth = 0, zwidth = 0;
        string joinTable = SQLUtility.ReadTempTableGetAtomsToRead_Filtering(tempTable, worker, contextConn, standardConn, input,
            ref xwidth, ref ywidth, ref zwidth,
            ref startx, ref starty, ref startz);

        SqlCommand cmd;
        SqlDataRecord record;
        record = new SqlDataRecord(worker.GetRecordMetaData());
        SqlContext.Pipe.SendResultsStart(record);

        byte[] rawdata1 = new byte[table1.BlobByteSize];
        byte[] rawdata2 = new byte[table2.BlobByteSize];
        
        ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSGS_SV)worker).InitializeSummedVolumes(xwidth, ywidth, zwidth);

        if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.None)
        {
            // Go through and run values on each point

            // Find nearest timestep
            int timestep_int = SQLUtility.GetNearestTimestep(time, table1);

            TurbulenceBlob atom1 = new TurbulenceBlob(table1);
            TurbulenceBlob atom2 = new TurbulenceBlob(table2);

            cmd = new SqlCommand(
               String.Format(@"SELECT f1.zindex, f1.data, f2.data " +
                          "FROM {2}, {0} as f1, {1} as f2 WHERE f1.timestep = {3} " +
                          "AND f2.timestep = f1.timestep " +
                          "AND {2}.zindex = f1.zindex " +
                          "AND {2}.zindex = f2.zindex",
                          tableName1, tableName2, joinTable, timestep_int),
                          contextConn);
            cmd.CommandTimeout = 3600;

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    // read in the current blob
                    long thisBlob = reader.GetSqlInt64(0).Value;
                    int bytesread = 0;
                    while (bytesread < table1.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(1, table1.SqlArrayHeaderSize, rawdata1, bytesread, table1.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }
                    bytesread = 0;
                    while (bytesread < table2.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(2, table2.SqlArrayHeaderSize, rawdata2, bytesread, table2.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }

                    atom1.Setup(timestep_int, new Morton3D(thisBlob), rawdata1);
                    atom2.Setup(timestep_int, new Morton3D(thisBlob), rawdata2);

                    ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSGS_SV)worker).UpdateSummedVolumes(atom1, atom2, startx, starty, startz, xwidth, ywidth, zwidth);
                }
            }

            float[] result;
            foreach (SQLUtility.MHDInputRequest point in input)
            {
                result = ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSGS_SV)worker).GetResult(point, startx, starty, startz, xwidth, ywidth, zwidth);
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
            atom1 = null;
            atom2 = null;
        }
        else if ((TurbulenceOptions.TemporalInterpolation)temporalInterp ==
            TurbulenceOptions.TemporalInterpolation.PCHIP)
        {
            throw new NotImplementedException();
        }
        else
        {
            standardConn.Close();
            input = null;
            rawdata1 = null;
            rawdata2 = null;
            ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSGS_SV)worker).DeleteSummedVolumes();
            worker = null;
            throw new Exception("Unsupported TemporalInterpolation Type");
        }

        SqlContext.Pipe.SendResultsEnd();

        input = null;
        rawdata1 = null;
        rawdata2 = null;
        ((Turbulence.SQLInterface.workers.GetMHDBoxFilterSGS_SV)worker).DeleteSummedVolumes();
        worker = null;
    }
};
