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
using System.Threading.Tasks;
//
/* Added for FileDB*/
using System.IO;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void GetDataFileDBCutout(
        string serverName,
        string dbname,
        string codedb,
        string dataset,
        int blobDim, 
        int timestep,
        string QueryBox,
        out SqlBytes blob)
    {
        //DateTime start = DateTime.Now;
        
        SqlDataRecord record = new SqlDataRecord(new SqlMetaData("data", SqlDbType.VarBinary, -1));
        String connectionString = "Context connection=true;";        
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Load information about the requested dataset
        TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, dataset, blobDim, connection);

        int[] coordinates = new int[6];

        ParseQueryBox(QueryBox, coordinates);

        byte[] cutout;

        GetCutout(table, dbname, timestep, coordinates, connection, out cutout);

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

        //System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Documents and Settings\kalin\My Documents\GetDataCutoutTime.txt", true);
        //file.WriteLine(DateTime.Now - start);
        //file.Close();
    }

    public struct cutout_buffer
    {
        public int[] coordinates;
        public byte[] cutout;
        public int x_width, y_width, z_width;
        public int atomWidth;
        public int components;
        public cutout_buffer(int[] coord, int comp, int aWidth)
        {
            coordinates = coord;
            x_width = coordinates[3] - coordinates[0];
            y_width = coordinates[4] - coordinates[1];
            z_width = coordinates[5] - coordinates[2];
            atomWidth = aWidth;
            components = comp;
            int cutoutbytesize = components * sizeof(float) * x_width * y_width * z_width; 
            cutout = new byte[cutoutbytesize];

        }
    }
    private static void GetCutout(TurbDataTable table, string dbname, int timestep, int[] coordinates, SqlConnection connection, out byte[] cutout)
    {
        int atomWidth = table.atomDim;
        cutout_buffer cbuff = new cutout_buffer(coordinates, table.Components, atomWidth);
        byte[] rawdata = new byte[table.BlobByteSize];

        string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);
        //System.IO.StreamWriter file = new System.IO.StreamWriter(@"d:\filedb\zindexlistdb.txt", true);
        DateTime start = DateTime.Now;
        /*File Db code*/
        string pathSource = "e:\\filedb\\isotropic4096"; //TODO no hardcode
        pathSource = pathSource + "\\" + dbname + "_" + timestep + ".bin";
        //FileStream filedb = new FileStream(pathSource, FileMode.Open, System.IO.FileAccess.Read);

        /* TODO: look for smaller requests (128 cube or smaller), and do file seek instead. Looks to be about 4096 rows*/
        /* Create zindex list without using the database */
        List<long> zlist = new List<long>();
        //System.IO.StreamWriter zfile = new System.IO.StreamWriter(@"d:\filedb\zindexlist.txt", true);
        /* Find coordinates of first and last blob */
        //int xlow = coordinates[0] - coordinates[0] % atomWidth;
        //int ylow = coordinates[1] - coordinates[1]  % atomWidth;
        //int zlow = coordinates[2] - coordinates[2]  % atomWidth;
        int xlow = coordinates[0] & -atomWidth;
        int ylow = coordinates[1] & -atomWidth;
        int zlow = coordinates[2] & -atomWidth;

        int xhigh = (coordinates[3] - 1) - (coordinates[3] - 1) % atomWidth;
        int yhigh = (coordinates[4] - 1) - (coordinates[4] - 1) % atomWidth;
        int zhigh = (coordinates[5] - 1) - (coordinates[5] - 1) % atomWidth;
        /*Iterate through coordinates to create zindex list */
        /* Note: This will add any zindex even if it isn't on this server.  */
        long blob;
        int rowcount = 1;
        for (int i = zlow; i <= zhigh; i = i + atomWidth)
        {
            for (int j = ylow; j <= yhigh; j = j + atomWidth)
            {
                for (int k = xlow; k <= xhigh; k = k + atomWidth)
                {
                    blob = new Morton3D(i, j, k).Key;
                    zlist.Add(blob);
                    //zfile.WriteLine(blob);
                    string coords = k.ToString() + "," + j.ToString() + "," + i.ToString();
                    //zfile.WriteLine(coords);
                    rowcount++;
                }
            }
        }

        //SqlDataReader reader = command.ExecuteReader();

        bool read_entire_file = false;
        byte[] z_rawdata = new byte[table.BlobByteSize]; ; //Initilize for small cutout, reassign below if we are reading in the entire file.
        FileStream filedb = new FileStream(pathSource, FileMode.Open, System.IO.FileAccess.Read); /* This is in case we don't read in the entire file */

        //file.WriteLine("Generation of zindex and blob count took:");
        //file.WriteLine(DateTime.Now - start);
        //file.WriteLine("Number of blobs: {0}", rowcount);

        if (rowcount > 4096)
        {
            start = DateTime.Now;
            z_rawdata = File.ReadAllBytes(pathSource); /*Read it all in at once*/
            read_entire_file = true;
            //file.WriteLine("File Read I/O took:");
            //file.WriteLine(DateTime.Now - start);
        }
        //int MaxCount = 96;
        //file.WriteLine("beginning parallel operation");
        start = DateTime.Now; /* Star the timer */
        //ThreadPool.SetMaxThreads(MaxCount, 96); /*We need to determine how many completion port threads we need */
        int blobnum = 0;
        
        using (var finished = new CountdownEvent(rowcount))
        {
            foreach (long thisBlob in zlist)
            {
                int source0 = 0;
                // string coords = new Morton3D(thisBlob).X.ToString() + "," + new Morton3D(thisBlob).Y.ToString() + "," + new Morton3D(thisBlob).Z.ToString();
                //file.WriteLine(coords);
                //ThreadBlockMove movers = new ThreadBlockMove();
                int rownum = 0;
                
                var capture = thisBlob;
                if (read_entire_file)
                {
                    long bnum = thisBlob / (table.atomDim * table.atomDim * table.atomDim); /*Check this!  Assuming we are small enough to be an int after this division*/
                    source0 = (int)bnum * table.BlobByteSize;
                    /* Get refernce to blob from file and pass the reference to avoid extra data copying */
                    ArraySegment<byte> segment = new ArraySegment<byte>(z_rawdata, source0, table.BlobByteSize);
                    rawdata = segment.Array;
                    //ThreadPool.QueueUserWorkItem(new WaitCallback(movers.BlockMove), new MoverParameters(thisBlob, ref rawdata, ref cbuff));
                    ThreadPool.QueueUserWorkItem(
                        (state) =>
                        {
                            try
                            {
                               // MoverParameters mp = new MoverParameters(thisBlob, ref rawdata, ref cbuff);
                               // BlockMove(mp);
                                BlockMove(thisBlob, ref rawdata, ref cbuff);
                            }
                            finally
                            {
                                finished.Signal();
                            }
                        }, null
                        );
                    rownum++;
                }
                else
                {
                    /* Cutout is small, so read in each blob independently.  This seems to not work well in parallel since the signal for completion isn't triggering */
                    long bnum = thisBlob / (table.atomDim * table.atomDim * table.atomDim);
                    //offset = bnum * table.BlobByteSize;
                    long offset = bnum * table.BlobByteSize;
                    filedb.Seek(offset, SeekOrigin.Begin);
                    int bytes = filedb.Read(rawdata, 0, table.BlobByteSize);
                    //BlockMove(thisBlob, ref rawdata, ref cbuff);
                    //ThreadPool.QueueUserWorkItem(new WaitCallback(movers.BlockMove), new MoverParameters(thisBlob, ref rawdata, ref cbuff));
                    ThreadPool.QueueUserWorkItem(
                        (state) =>
                        {
                            try
                            {
                                //MoverParameters mp = new MoverParameters(thisBlob, ref rawdata, ref cbuff);
                                //BlockMove(mp);
                                BlockMove(thisBlob, ref rawdata, ref cbuff);
                            }
                            finally
                            {
                                finished.Signal();
                            }
                        }, null
                        );
                    rownum++;
                }
                blobnum++; //Blob number
            }

            //file.WriteLine("Waiting for thread pool to end");
            finished.Signal();
            finished.Wait();
            cutout = cbuff.cutout;
            //file.WriteLine(DateTime.Now - start);
            //file.Close();
        }
    }
    public class MoverParameters
    {
        public long zindex;
        public byte[] source_buffer;
        public cutout_buffer cbuff;
        public MoverParameters(long z, ref byte[] sb, ref cutout_buffer cb)
        {
            zindex = z;
            source_buffer = sb;
            cbuff = cb;
        }
    }
        
    /* BlockMove is the method called when the work item is serviced on the thread pool */    
    private static void BlockMove(long zindex, ref byte[] source_buffer, ref cutout_buffer cbuff)

        {
            //long zindex = ((MoverParameters)p).zindex;
            //byte[] source_buffer = ((MoverParameters)p).source_buffer;
            //cutout_buffer cbuff = ((MoverParameters)p).cbuff;
            int x, y, z;
            int sourceX = 0, destinationX = 0, sourceY = 0, destinationY = 0, sourceZ = 0, destinationZ = 0, lengthX = 0, lengthY = 0, lengthZ = 0;
            x = new Morton3D(zindex).X;
            y = new Morton3D(zindex).Y;
            z = new Morton3D(zindex).Z;
            GetSourceDestLen(x, cbuff.coordinates[0], cbuff.coordinates[3], cbuff.atomWidth, ref sourceX, ref destinationX, ref lengthX);
            GetSourceDestLen(y, cbuff.coordinates[1], cbuff.coordinates[4], cbuff.atomWidth, ref sourceY, ref destinationY, ref lengthY);
            GetSourceDestLen(z, cbuff.coordinates[2], cbuff.coordinates[5], cbuff.atomWidth, ref sourceZ, ref destinationZ, ref lengthZ);
            int bufferLength = lengthX * cbuff.components * sizeof(float);
            int dest0 = (destinationX + destinationY * cbuff.x_width) * cbuff.components * sizeof(float);
            for (int k = 0; k < lengthZ; k++)
            {
                int source = (sourceZ + k) * cbuff.atomWidth * cbuff.atomWidth * cbuff.components * sizeof(float);
                int dest = dest0 + (destinationZ + k) * cbuff.x_width * cbuff.y_width * cbuff.components * sizeof(float);
                for (int j = 0; j < lengthY; j++)
                {
                    Array.Copy(source_buffer, source, cbuff.cutout, dest, lengthX * cbuff.components * sizeof(float));
                    source += cbuff.atomWidth * cbuff.components * sizeof(float);
                    dest += cbuff.x_width * cbuff.components * sizeof(float);
                }
            }            
            
        }
 

   
    private static void ParseQueryBox(string QueryBox, int[] coordinates)
    {
        int left_bracket_pos = QueryBox.IndexOf('[');
        int right_bracket_pos = QueryBox.IndexOf(']');
        string box = QueryBox.Substring(left_bracket_pos + 1, right_bracket_pos - left_bracket_pos - 1);
        int i = 0;
        foreach (var s in box.Split(','))
        {
            int num;
            if (int.TryParse(s, out num))
                coordinates[i] = num;
            i++;
        }
    }

    private static void GetSourceDestLen(int coordinate, int lower, int upper, int atomWidth, ref int source, ref int dest, ref int len)
    {
        if (coordinate <= lower)
        {
            if (coordinate + atomWidth <= lower)
                throw new Exception(String.Format("Atom read is outside of boundaries of query box! Coordinate: {0}, atomWidth: {1}, lower: {2}", coordinate, atomWidth, lower));
            else if (coordinate + atomWidth <= upper)
                len = coordinate + atomWidth - lower;
            else
                len = upper - lower;
            source = lower - coordinate;
            dest = 0;
        }
        else if (coordinate >= upper)
            throw new Exception(String.Format("Atom read is outside of boundaries of query box! Coordinate: {0}, atomWidth: {1}, upper: {2}", coordinate, atomWidth, upper));
        else
        {
            if (coordinate + atomWidth <= upper)
                len = atomWidth;
            else
                len = upper - coordinate;
            source = 0;
            dest = coordinate - lower;
        }
    }
};
