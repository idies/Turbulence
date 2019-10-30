using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SQLInterface;
using System.Collections.Generic;
using Turbulence.SQLInterface.workers;
/* Added for FileDB*/
using System.IO;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void GetStridedFileDBDataCutout(
        string serverName,
        string dbname,
        string codedb,
        string turbinfodb,
        string turbinfoserver,
        short datasetID,
        string field,
        int blobDim, 
        int timestep,
        int x_stride,
        int y_stride,
        int z_stride,
        string QueryBox)
    {
        byte[] cutout = null;
        try
        {
            SqlDataRecord record = new SqlDataRecord(new SqlMetaData("data", SqlDbType.VarBinary, -1));
            SqlConnection contextConn;
            contextConn = new SqlConnection("context connection=true");            

            int[] coordinates = new int[6];
            ParseQueryBox(QueryBox, coordinates);

            TurbServerInfo serverinfo = TurbServerInfo.GetTurbServerInfo(codedb, turbinfodb, turbinfoserver);
            TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, field, blobDim, serverinfo);

            contextConn.Open();
            GetFiledbCutout(table, dbname, timestep, coordinates, contextConn, serverName, x_stride, y_stride, z_stride, out cutout);
            contextConn.Close();

            // Populate the record
            record.SetBytes(0, 0, cutout, 0, cutout.Length);
            // Send the record to the client.
            SqlContext.Pipe.Send(record);
            cutout = null;
        }
        catch (Exception ex)
        {
            if (cutout != null)
            {
                cutout = null;
            }
            throw new Exception(String.Format("Error generating filtered cutout.  [Inner Exception: {0}])",
                ex.ToString()));
        }
    }

    private static void GetFiledbCutout(TurbDataTable table, string dbname, int timestep, int[] coordinates, SqlConnection connection, string serverName,
        int x_stride, int y_stride, int z_stride, out byte[] cutout)
    {
        int x_width, y_width, z_width, x, y, z;
        x_width = (coordinates[3] - coordinates[0] - 1) / x_stride + 1;
        y_width = (coordinates[4] - coordinates[1] - 1) / y_stride + 1;
        z_width = (coordinates[5] - coordinates[2] - 1) / z_stride + 1;

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
        string pathSource = SQLUtility.getDBfilePath(dbname, timestep, table.TableName, connection, serverName);
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
        if (zlist.Count > 4096 && filedb.Length < (2147483648 - 0))
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
        List<SQLUtility.zlistTable> zlist_tab = new List<SQLUtility.zlistTable>();
        if (table.dbtype == 2)
        {
            zlist_tab = SQLUtility.fileDB2zlistTable(dbname, connection);
        }

        foreach (Morton3D thisBlob in zlist)
        {
            /* Cutout is small, so read in each blob independently.  This seems to not work well in parallel since the signal for completion isn't triggering */
            long offset = 0;

            if (table.dbtype == 1)
            {
                //long fileBlob = thisBlob - startz; /*We need to align the first blob with the start of the file */
                long fileBlob = thisBlob % 134217728;
                long bnum = fileBlob / (table.atomDim * table.atomDim * table.atomDim);
                offset = bnum * table.BlobByteSize;
            }
            else if (table.dbtype == 2)
            {
                //offset = SQLUtility.fileDB2offset(dbname, table, thisBlob, standardConn);
                //start = DateTime.Now;
                SQLUtility.zlistTable zresult = zlist_tab.Find(x1 => (x1.startZ <= thisBlob && thisBlob <= x1.endZ));
                offset = (thisBlob - zresult.startZ) / (table.atomDim * table.atomDim * table.atomDim);
                offset = (zresult.blobBefore + offset) * table.BlobByteSize;
                //file.WriteLine(string.Format("startZ {0}, endZ {1}, blobBefore {2}, Offset {3}", result.startZ, result.endZ, result.blobBefore, offset));
                //file.WriteLine(string.Format("Find thisBlob: {0}", DateTime.Now - start));
            }
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

            GetSourceDestLen(x, coordinates[0], coordinates[3], atomWidth, x_stride, ref sourceX, ref destinationX, ref lengthX);
            GetSourceDestLen(y, coordinates[1], coordinates[4], atomWidth, y_stride, ref sourceY, ref destinationY, ref lengthY);
            GetSourceDestLen(z, coordinates[2], coordinates[5], atomWidth, z_stride, ref sourceZ, ref destinationZ, ref lengthZ);
            //int bufferLength = lengthX * components * sizeof(float);

            for (int k = 0; k < lengthZ; k += z_stride)
            {
                for (int j = 0; j < lengthY; j += y_stride)
                {
                    int source = ((sourceZ + k) * atomWidth * atomWidth + (sourceY + j) * atomWidth + sourceX) * table.Components * sizeof(float);
                    int dest = ((destinationZ + k / z_stride) * x_width * y_width + (destinationY + j / y_stride) * x_width + destinationX) * table.Components * sizeof(float);
                    for (int i = 0; i < lengthX; i += x_stride)
                    {
                        Array.Copy(rawdata, source, cutout, dest, table.Components * sizeof(float));
                        source += x_stride * table.Components * sizeof(float);
                        dest += table.Components * sizeof(float);
                    }
                }
            }

        }
        //file.WriteLine("Data processing 1:");
        //file.WriteLine(stopWatch.Elapsed);
        //file.WriteLine("Data processing 2:");
        //file.WriteLine(stopWatch1.Elapsed);
        //file.WriteLine("Data processing 3:");
        //file.WriteLine(DateTime.Now - start);
        //file.Close();
    }
};
