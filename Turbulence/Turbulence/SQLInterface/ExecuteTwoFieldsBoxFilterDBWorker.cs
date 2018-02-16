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
    /// Stored procedure for the calculation of Box Filteres
    /// based on a pre-computation of box-sums
    /// </summary>
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void ExecuteTwoFieldsBoxFilterDBWorker(string serverName,
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
        long startz2,
        long endz2)
    {
        TurbServerInfo serverinfo = TurbServerInfo.GetTurbServerInfo(codedb, turbinfodb, turbinfoserver);
        SqlConnection standardConn;
        SqlConnection contextConn;
        string connString;
        if (serverName.Contains("_"))
            connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName.Remove(serverName.IndexOf("_")), serverinfo.codeDB);
        else
            connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName, serverinfo.codeDB);
        standardConn = new SqlConnection(connString);
        contextConn = new SqlConnection("context connection=true");

        // Load information about the requested dataset
        TurbDataTable table1 = TurbDataTable.GetTableInfo(serverName, dbname, field1, blobDim, serverinfo);
        TurbDataTable table2 = TurbDataTable.GetTableInfo(serverName, dbname, field2, blobDim, serverinfo);
                
        string tableName1 = String.Format("{0}.dbo.{1}", dbname, table1.TableName);
        string tableName2 = String.Format("{0}.dbo.{1}", dbname, table2.TableName);

        // Instantiate a worker class
        if (workerType != (int)Worker.Workers.GetMHDBoxFilterSGS_SV)
        {
            //contextConn.Close();
            throw new Exception("Invalid worker type specified! This procedure works only with a GetMHDBoxFilterSV or GetMHDBoxFilterSGS_SV worker!");
        }

        contextConn.Open();
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

            string pathSource1 = SQLUtility.getDBfilePath(dbname, timestep_int, tableName1, standardConn);
            FileStream filedb1 = new FileStream(pathSource1, FileMode.Open, System.IO.FileAccess.Read);
            string pathSource2 = SQLUtility.getDBfilePath(dbname, timestep_int, tableName2, standardConn);
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
            //cmd.CommandTimeout = 3600;

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    // read in the current blob
                    long thisBlob = reader.GetSqlInt64(0).Value;
                    if (thisBlob <= endz2 && thisBlob >= startz2)
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
