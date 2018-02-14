using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SQLInterface;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
//using System.Diagnostics;
using System.Threading.Tasks;
//
/* Added for FileDB*/
using System.IO;
using System.Text;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void GetDataFileDBCutout(
        string serverName,
        string dbname,
        string codedb,
        string turbinfodb,
        string turbinfoserver,
        string field,
        int blobDim,
        int timestep,
        string QueryBox,
        out SqlBytes blob)
    {
        //DateTime start = DateTime.Now;

        SqlDataRecord record = new SqlDataRecord(new SqlMetaData("data", SqlDbType.VarBinary, -1));
        String connectionString = "Context connection=true;";
        SqlConnection connection = new SqlConnection(connectionString);

        // Load information about the requested dataset
        TurbServerInfo serverinfo = TurbServerInfo.GetTurbServerInfo(codedb, turbinfodb, turbinfoserver);
        TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, field, blobDim, serverinfo);

        int[] coordinates = new int[6];

        ParseQueryBox(QueryBox, coordinates);

        byte[] cutout;

        connection.Open();
        GetFiledbCutout(table, dbname, timestep, coordinates, connection, out cutout);

        // Populate the record
        /* quick mod send last few bytes to test speed, a 512 cube */
        //record.SetBytes(0,0, cutout, (cutout.Length - 100), 16);
        //record.SetBytes(0, 0, cutout, cutout.Length/2, 16);
        //record.SetBytes(0, 0, cutout, 0, cutout.Length);
        // Send the record to the client.
        //SqlContext.Pipe.Send(record);
        //connection.Snew SqlBinary(cutout);
        //blob = new SqlBinary(cutout);
        blob = new SqlBytes(cutout);
        connection.Close();

        if (false)
        {
            float[] f = new float[cutout.Length / sizeof(float)];
            for (int i = 0; i < cutout.Length / sizeof(float); i++)
            {
                f[i] = BitConverter.ToSingle(cutout, i * 4);
            }

            //byte[] bytes = BitConverter.GetBytes(0x2D1509C0);
            //Array.Reverse(bytes);
            //float myFloat = BitConverter.ToSingle(bytes, 0); // Always be correct

            var csv1 = new StringBuilder();
            for (int i = 0; i < f.Length / table.Components; i++)
            {
                //Suggestion made by KyleMit
                var newLine1 = "";
                if (table.Components == 3)
                {
                    newLine1 = string.Format("{0},{1},{2}", f[i * table.Components], f[i * table.Components + 1], f[i * table.Components + 2]);
                }
                else if (table.Components == 1)
                {
                    newLine1 = string.Format("{0}", f[i]);
                }
                csv1.AppendLine(newLine1);
            }
            File.WriteAllText("C:\\Users\\zwu27\\Documents\\vel_pr_cutout2.txt", csv1.ToString());
        }

        //System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Documents and Settings\kalin\My Documents\GetDataCutoutTime.txt", true);
        //file.WriteLine(DateTime.Now - start);
        //file.Close();
    }


    private static void GetFiledbCutout(TurbDataTable table, string dbname, int timestep, int[] coordinates, SqlConnection connection, out byte[] cutout)
    {
        int x_width, y_width, z_width, x, y, z;
        x_width = coordinates[3] - coordinates[0];
        y_width = coordinates[4] - coordinates[1];
        z_width = coordinates[5] - coordinates[2];

        cutout = new byte[table.Components * sizeof(float) * x_width * y_width * z_width];

        int atomWidth = table.atomDim;
        //cutout_buffer2 cbuff = new cutout_buffer2(coordinates, table.Components, atomWidth);
        byte[] rawdata = new byte[table.BlobByteSize];

        //string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);
        //System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\www\zindexlistdb.txt", true);
        //DateTime start = DateTime.Now;
        /*File Db code*/
        //string pathSource = "e:\\filedb\\isotropic4096"; //TODO no hardcode
        //pathSource = pathSource + "\\" + dbname + "_" + timestep + ".bin";
        string pathSource = SQLUtility.getDBfilePath(dbname, timestep, table.TableName, connection);
        //FileStream filedb = new FileStream(pathSource, FileMode.Open, System.IO.FileAccess.Read);

        /* TODO: look for smaller requests (128 cube or smaller), and do file seek instead. Looks to be about 4096 rows*/
        /* Create zindex list without using the database */
        List<Morton3D> zlist = new List<Morton3D>();
        //System.IO.StreamWriter zfile = new System.IO.StreamWriter(@"d:\filedb\zindexlist.txt", true);
        /* Find coordinates of first and last blob */

        //int rowcount = 1;
        for (int k = coordinates[2] / atomWidth; k <= (coordinates[5] - 1) / atomWidth; k++)
        {
            for (int j = coordinates[1] / atomWidth; j <= (coordinates[4] - 1) / atomWidth; j++)
            {
                for (int i = coordinates[0] / atomWidth; i <= (coordinates[3] - 1) / atomWidth; i++)
                {
                    Morton3D blob = new Morton3D(k * atomWidth, j * atomWidth, i * atomWidth);
                    //long blob = new Morton3D(i, j, k).Key;
                    zlist.Add(blob);
                    //rowcount++;
                }
            }
        }
        zlist.Sort((t1, t2) => -1 * t2.Key.CompareTo(t1.Key));

        //SqlDataReader reader = command.ExecuteReader();

        bool read_entire_file = false;
        byte[] z_rawdata = new byte[table.BlobByteSize]; ; //Initilize for small cutout, reassign below if we are reading in the entire file.
        FileStream filedb = new FileStream(pathSource, FileMode.Open, System.IO.FileAccess.Read); /* This is in case we don't read in the entire file */

        //file.WriteLine("Generation of zindex and blob count took:");
        //file.WriteLine(DateTime.Now - start);
        //file.WriteLine("Number of blobs: {0}", zlist.Count);

        //Stopwatch stopWatch = new Stopwatch();
        //Stopwatch stopWatch1 = new Stopwatch();
        if (zlist.Count > 4096)
        {
            //stopWatch.Start();
            z_rawdata = File.ReadAllBytes(pathSource); /*Read it all in at once*/
            read_entire_file = true;
            //stopWatch.Stop();
            //file.WriteLine("Read whole file took:");
            //file.WriteLine(stopWatch.Elapsed);
        }
        //int MaxCount = 96;
        //file.WriteLine("beginning parallel operation");
        //start = DateTime.Now; /* Star the timer */
        //stopWatch = new Stopwatch();
        //ThreadPool.SetMaxThreads(MaxCount, 96); /*We need to determine how many completion port threads we need */
        byte[] cutout1 = new byte[table.Components * sizeof(float) * x_width * y_width * z_width];
        using (CountdownEvent finished = new CountdownEvent(zlist.Count))
        {
            foreach (Morton3D thisBlob in zlist)
            {
                //finished.AddCount();
                ThreadPool.QueueUserWorkItem(delegate (object state)
                {
                    try
                    {
                        /* Cutout is small, so read in each blob independently.  This seems to not work well in parallel since the signal for completion isn't triggering */
                        long fileBlob = thisBlob % 134217728;
                        long bnum = fileBlob / (table.atomDim * table.atomDim * table.atomDim);
                        //offset = bnum * table.BlobByteSize;
                        long offset = bnum * table.BlobByteSize;
                        if (read_entire_file)
                        {
                            //stopWatch.Start();
                            Array.Copy(z_rawdata, offset, rawdata, 0, table.BlobByteSize);
                            //stopWatch.Stop();
                        }
                        else
                        {
                            //stopWatch1.Start();
                            filedb.Seek(offset, SeekOrigin.Begin);
                            int bytes = filedb.Read(rawdata, 0, table.BlobByteSize);
                            //stopWatch1.Stop();
                        }
                        
                        x = new Morton3D(thisBlob).X;
                        y = new Morton3D(thisBlob).Y;
                        z = new Morton3D(thisBlob).Z;
                        int sourceX = 0, sourceY = 0, sourceZ = 0, lengthX = 0, lengthY = 0, lengthZ = 0;
                        int destinationX = 0, destinationY = 0, destinationZ = 0;
                        //ulong long_destinationX = 0, long_destinationY = 0, long_destinationZ = 0;
                        ulong long_components = (ulong)table.Components;
                        ulong long_x_width = (ulong)x_width;
                        ulong long_y_width = (ulong)y_width;

                        GetSourceDestLen(x, coordinates[0], coordinates[3], atomWidth, ref sourceX, ref destinationX, ref lengthX);
                        GetSourceDestLen(y, coordinates[1], coordinates[4], atomWidth, ref sourceY, ref destinationY, ref lengthY);
                        GetSourceDestLen(z, coordinates[2], coordinates[5], atomWidth, ref sourceZ, ref destinationZ, ref lengthZ);
                        //int bufferLength = lengthX * components * sizeof(float);

                        int source0 = (sourceX + sourceY * atomWidth) * table.Components * sizeof(float);
                        ulong dest0 = ((ulong)destinationX + (ulong)destinationY * long_x_width) * long_components * sizeof(float);

                        for (int k = 0; k < lengthZ; k++)
                        {
                            int source = source0 + (sourceZ + k) * atomWidth * atomWidth * table.Components * sizeof(float);
                            ulong dest = dest0 + ((ulong)destinationZ + (ulong)k) * long_x_width * long_y_width * long_components * sizeof(float);
                            for (int j = 0; j < lengthY; j++)
                            {

                                //Buffer.BlockCopy(rawdata, source, cutout, (int)dest, lengthX * table.Components * sizeof(float));
                                Array.Copy(rawdata, source, cutout1, (int)dest, lengthX * table.Components * sizeof(float));
                                source += atomWidth * table.Components * sizeof(float);
                                dest += long_x_width * long_components * sizeof(float);
                            }
                        }
                    }
                    finally
                    {
                        finished.Signal();
                    }
                }, null);

            }
            finished.Signal();
            finished.Wait();
        }
        cutout = cutout1;
        //file.WriteLine("Data processing 1:");
        //file.WriteLine(stopWatch.Elapsed);
        //file.WriteLine("Data processing 2:");
        //file.WriteLine(stopWatch1.Elapsed);
        //file.WriteLine("Data processing 3:");
        //file.WriteLine(DateTime.Now - start);
        //file.Close();
    }




};
