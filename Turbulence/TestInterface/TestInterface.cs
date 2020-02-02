using System;
using System.Collections.Generic;
using System.Text;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using Turbulence.SQLInterface;
using Turbulence.TurbBatch;
using Turbulence.SciLib;
using TestInterface;
using TurbulenceService;

using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SQLInterface;

using System.Data.Sql;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.IO;

//using HDF.PInvoke;
using hid_t = System.Int64;

namespace TestInterface
{
    class TestInterface
    {
        //[Microsoft.SqlServer.Server.SqlProcedure]
        public static void Main()
        {
            // initialize
            string serverName = "dsp012";
            string dbname = "iso4096db117";
            string codedb = "turbdev_zw";
            string turbinfodb = "turbinfo_test";
            string turbinfoserver = "sciserver02";
            string dataset = "vel";
            int workerType = 64;
            int blobDim = 8;
            float time = 0;
            int spatialInterp = 44;
            int temporalInterp = 0;
            float arg = 0;
            int inputSize = 1;
            string tempTable = "#temp_zw";
            long startz = 2147483648;
            long endz = 2281701375;

            //--------------------------
            TurbServerInfo serverinfo = TurbServerInfo.GetTurbServerInfo(codedb, turbinfodb, turbinfoserver);
            SqlConnection standardConn;
            SqlConnection contextConn;

            string connString;
            if (serverName.Contains("_"))
                connString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverName.Remove(serverName.IndexOf("_")), serverinfo.codeDB);
            else
                connString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverName, serverinfo.codeDB);
            standardConn = new SqlConnection(connString);

            string contextConn_string = String.Format("Data Source='dsp012';Initial Catalog='turbdev_zw';User ID='turbquery';Password='aa2465ways2k';Pooling=false;");
            contextConn= new SqlConnection(contextConn_string);
            //contextConn = new SqlConnection("context connection=true");

            // Check temp table
            //tempTable = SQLUtility.SanitizeTemporaryTable(tempTable);

            // Load information about the requested dataset
            TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, dataset, blobDim, serverinfo);

            // string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);

            // Instantiate a worker class
            contextConn.Open();
            Worker worker = Worker.GetWorker(dbname, table, workerType, spatialInterp, arg, contextConn);

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

                //string temp = string.Format("C:\\www\\zindexlistdb{0}.txt", dbname.Substring(dbname.Indexof("db") + 2,dbname.Length - dbname.IndexOf("db") - 2));
                //System.IO.StreamWriter file = new System.IO.StreamWriter(@temp.ToString(), true);
                //DateTime start = DateTime.Now;
                List<SQLUtility.zlistTable> zlist = new List<SQLUtility.zlistTable>();
                if (table.dbtype == 2)
                {
                    zlist = SQLUtility.fileDB2zlistTable(dbname, standardConn);
                }
                //file.WriteLine(string.Format("Load table {0}: {1}", dbname, DateTime.Now - start));

                //Setup the file

                //string pathSource = "e:\\filedb\\isotropic4096";
                //pathSource = pathSource + "\\" + dbname + "_" + timestep_int + ".bin";
                string pathSource = SQLUtility.getDBfilePath(dbname, timestep_int, table.DataName, standardConn, serverName);
                FileStream filedb = new FileStream(pathSource, FileMode.Open, System.IO.FileAccess.Read);
                //string[] tester = { "In filedb..."};
                //System.IO.File.WriteAllLines(@"e:\filedb\debug.txt", tester);
                //DateTime start = DateTime.Now;
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
                            long offset = 0;
                            if (table.dbtype == 1)
                            {
                                //long fileBlob = thisBlob - startz; /*We need to align the first blob with the start of the file */
                                long fileBlob = thisBlob % 134217728;
                                long z = fileBlob / (table.atomDim * table.atomDim * table.atomDim);
                                offset = z * table.BlobByteSize;
                            }
                            else if (table.dbtype == 2)
                            {
                                //offset = SQLUtility.fileDB2offset(dbname, table, thisBlob, standardConn);
                                //start = DateTime.Now;
                                SQLUtility.zlistTable zresult = zlist.Find(x => (x.startZ <= thisBlob && thisBlob <= x.endZ));
                                offset = (thisBlob - zresult.startZ) / (table.atomDim * table.atomDim * table.atomDim);
                                offset = (zresult.blobBefore + offset) * table.BlobByteSize;
                                //file.WriteLine(string.Format("startZ {0}, endZ {1}, blobBefore {2}, Offset {3}", result.startZ, result.endZ, result.blobBefore, offset));
                                //file.WriteLine(string.Format("Find thisBlob: {0}", DateTime.Now - start));
                            }
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
                //file.WriteLine(string.Format("reading takes: {0}", DateTime.Now - start));
                //file.Close();

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
                //file.Close();
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
                              "ORDER BY timestep, zindex",
                                joinTable, timestep0, timestep1, timestep2, timestep3),
                    contextConn);
                //standardConn);
                cmd.CommandTimeout = 3600;

                List<SQLUtility.zlistTable> zlist = new List<SQLUtility.zlistTable>();
                if (table.dbtype == 2)
                {
                    zlist = SQLUtility.fileDB2zlistTable(dbname, standardConn);
                }

                string pathSource0 = SQLUtility.getDBfilePath(dbname, timestep0, table.DataName, standardConn, serverName);
                string pathSource1 = SQLUtility.getDBfilePath(dbname, timestep1, table.DataName, standardConn, serverName);
                string pathSource2 = SQLUtility.getDBfilePath(dbname, timestep2, table.DataName, standardConn, serverName);
                string pathSource3 = SQLUtility.getDBfilePath(dbname, timestep3, table.DataName, standardConn, serverName);
                FileStream filedb0 = null, filedb1 = null, filedb2 = null, filedb3 = null;

                try
                {
                    filedb0 = new FileStream(pathSource0, FileMode.Open, System.IO.FileAccess.Read);
                }
                catch { }
                try
                {
                    filedb1 = new FileStream(pathSource1, FileMode.Open, System.IO.FileAccess.Read);
                }
                catch { }
                try
                {
                    filedb2 = new FileStream(pathSource2, FileMode.Open, System.IO.FileAccess.Read);
                }
                catch { }
                try
                {
                    filedb3 = new FileStream(pathSource3, FileMode.Open, System.IO.FileAccess.Read);
                }
                catch { }

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int timestep = reader.GetSqlInt32(0).Value;  // Timestep returned
                        long thisBlob = reader.GetSqlInt64(1).Value; // Blob returned

                        if (thisBlob <= endz && thisBlob >= startz)
                        {
                            //long z = thisBlob / (table.atomDim * table.atomDim * table.atomDim);
                            //long offset = z * table.BlobByteSize;
                            long offset = 0;
                            if (table.dbtype == 1)
                            {
                                //long fileBlob = thisBlob - startz; /*We need to align the first blob with the start of the file */
                                long fileBlob = thisBlob % 134217728;
                                long z = fileBlob / (table.atomDim * table.atomDim * table.atomDim);
                                offset = z * table.BlobByteSize;
                            }
                            else if (table.dbtype == 2)
                            {
                                //offset = SQLUtility.fileDB2offset(dbname, table, thisBlob, standardConn);
                                //start = DateTime.Now;
                                SQLUtility.zlistTable zresult = zlist.Find(x => (x.startZ <= thisBlob && thisBlob <= x.endZ));
                                offset = (thisBlob - zresult.startZ) / (table.atomDim * table.atomDim * table.atomDim);
                                offset = (zresult.blobBefore + offset) * table.BlobByteSize;
                                //file.WriteLine(string.Format("startZ {0}, endZ {1}, blobBefore {2}, Offset {3}", result.startZ, result.endZ, result.blobBefore, offset));
                                //file.WriteLine(string.Format("Find thisBlob: {0}", DateTime.Now - start));
                            }

                            // TODO: I'm afratid this is wrong, because if file3 doesn't exist,
                            // the rawdata would use the data from file2
                            if (timestep == timestep0 && filedb0 != null)
                            {
                                filedb0.Seek(offset, SeekOrigin.Begin);
                                int bytes = filedb0.Read(rawdata, 0, table.BlobByteSize);
                            }
                            else if (timestep == timestep1 && filedb1 != null)
                            {
                                filedb1.Seek(offset, SeekOrigin.Begin);
                                int bytes = filedb1.Read(rawdata, 0, table.BlobByteSize);
                            }
                            else if (timestep == timestep2 && filedb2 != null)
                            {
                                filedb2.Seek(offset, SeekOrigin.Begin);
                                int bytes = filedb2.Read(rawdata, 0, table.BlobByteSize);
                            }
                            else if (timestep == timestep3 && filedb3 != null)
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
                try
                {
                    filedb0.Close();
                }
                catch { }
                try
                {
                    filedb1.Close();
                }
                catch { }
                try
                {
                    filedb2.Close();
                }
                catch { }
                try
                {
                    filedb3.Close();
                }
                catch { }
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

            //scan_count = 0;
            //logical_reads = 0;
            //physical_reads = 0;
            //read_ahead_reads = 0;

            // We should not have to manually call the garbage collector.
            // System.GC.Collect();


        }
    }
}
