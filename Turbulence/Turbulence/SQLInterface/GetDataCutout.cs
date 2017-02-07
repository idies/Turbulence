using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SQLInterface;
/* Added for FileDB*/
using System.IO;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void GetDataCutout(
        string serverName,
        string dbname,
        string codedb,
        string dataset,
        int blobDim, 
        int timestep,
        string QueryBox)
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
        record.SetBytes(0, 0, cutout, 0, cutout.Length);
        // Send the record to the client.
        SqlContext.Pipe.Send(record);
        
        connection.Close();

        //System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Documents and Settings\kalin\My Documents\GetDataCutoutTime.txt", true);
        //file.WriteLine(DateTime.Now - start);
        //file.Close();
    }

    private static void GetCutout(TurbDataTable table, string dbname, int timestep, int[] coordinates, SqlConnection connection, out byte[] cutout)
    {
        int x_width, y_width, z_width, x, y, z;
        x_width = coordinates[3] - coordinates[0];
        y_width = coordinates[4] - coordinates[1];
        z_width = coordinates[5] - coordinates[2];

        //byte[] rawdata = new byte[table.BlobByteSize];
        int cutoutbytesize = table.Components * sizeof(float) * x_width * y_width * z_width; //temp fix to test TODO Fix this
        cutout = new byte[cutoutbytesize];

        string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);
        int atomWidth = table.atomDim;
        /*File Db code*/
        /*Idea is to read in from low to high morton index into a buffer, then create a new buffer and unpack into row major(or column major?) order*/

        string pathSource = "d:\\filedb";
        pathSource = pathSource + "\\" + dbname + "_" + timestep + ".bin";
        //FileStream filedb = new FileStream(pathSource, FileMode.Open, System.IO.FileAccess.Read);

        /*Get zindex range to query REMOVED, querying entire file now.*/
        long skey;
        skey = new Morton3D(coordinates[0], coordinates[1], coordinates[2]).Key;
        //ekey = new Morton3D(coordinates[3], coordinates[4], coordinates[5]).Key;
        /* Now find nearest blob zindex*/
        skey = skey - skey % table.BlobByteSize; /*Find start zindex for blob*/
        //ekey = ekey - ekey % table.BlobByteSize;
        //long difference = ekey - skey; /*We *should* be small enough to cast to int*/
        //int buffer_size = ((int)difference/(table.atomDim ^ 3)) * table.BlobByteSize;
        //byte[] z_rawdata = new byte[buffer_size];

               
        //long offset = skey * table.BlobByteSize;
        //filedb.Seek(offset, SeekOrigin.Begin);
        //Test
        // string[] lines= { "Offset chosen = ", offset.ToString(), z.ToString(), table.BlobByteSize.ToString(), thisBlob.ToString(),pathSource, table.atomDim.ToString()};
        //System.IO.File.WriteAllLines(@"d:\filedb\debug.txt", lines);
        //int bytes = filedb.Read(z_rawdata, 0, buffer_size); /*Read it all in at once*/
        /* Read in entire file. */
        byte[] z_rawdata = File.ReadAllBytes(pathSource); /*Read it all in at once*/

     
        //int blobcount = (int) Math.Ceiling((float) (cutoutbytesize / (table.atomDim ^ 3)));
       // blobcount--; //test decrement blobcount by one. Not sure if this is right.
        //long iter_zindex = skey;
        //Instead of getting data, we simply get the z-indicies for the blobs that makeup our cutout.
        string queryString = String.Format(
                "select dbo.GetMortonX(zindex),dbo.GetMortonY(zindex),dbo.GetMortonZ(zindex),  zindex from {8}..zindex where " +
                    "X >= {0} & -{6} and X < {3} and Y >= {1} & -{6} and Y < {4} and Z >= {2} & -{6} and z < {5} ", coordinates[0], coordinates[1], coordinates[2], coordinates[3], coordinates[4], coordinates[5],
                atomWidth, tableName, dbname, timestep);

        SqlCommand command = new SqlCommand(
            queryString, connection);
        using (SqlDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {

                x = reader.GetSqlInt32(0).Value;
                y = reader.GetSqlInt32(1).Value;
                z = reader.GetSqlInt32(2).Value;
                int sourceX = 0, destinationX = 0, sourceY = 0, destinationY = 0, sourceZ = 0, destinationZ = 0, lengthX = 0, lengthY = 0, lengthZ = 0;

                GetSourceDestLen(x, coordinates[0], coordinates[3], atomWidth, ref sourceX, ref destinationX, ref lengthX);
                GetSourceDestLen(y, coordinates[1], coordinates[4], atomWidth, ref sourceY, ref destinationY, ref lengthY);
                GetSourceDestLen(z, coordinates[2], coordinates[5], atomWidth, ref sourceZ, ref destinationZ, ref lengthZ);

                //int source0 = (sourceX + sourceY * atomWidth) * table.Components * sizeof(float);
                int dest0 = (destinationX + destinationY * x_width) * table.Components * sizeof(float);
                long thisBlob = reader.GetSqlInt64(3).Value;
                long bnum = thisBlob / (table.atomDim * table.atomDim * table.atomDim);
                long source0 = bnum * table.BlobByteSize;

                for (int k = 0; k < (lengthZ); k++)
                {
                    //int source = source0 + (sourceZ + k) * atomWidth * atomWidth * table.Components * sizeof(float);
                    int dest = dest0 + (destinationZ + k) * x_width * y_width * table.Components * sizeof(float);
                    long source = source0 + (sourceZ + k) * atomWidth * atomWidth * table.Components * sizeof(float);
                    //long source = source0 + 
                    for (int j = 0; j < lengthY; j++)
                    {

                        // Array.Copy(rawdata, source, cutout, dest, lengthX * table.Components * sizeof(float)); //src, srcidx, dest, destidx, size
                        try
                        {
                            Array.Copy(z_rawdata, source, cutout, dest, lengthX * table.Components * sizeof(float));

                        }
                        catch (Exception e)
                        {
                            throw new Exception(String.Format("Error copying array.  source is {1} size {2}, dest is {3} size {4}  xyz={5},{6},{7} bnum={8} [Inner Exception: {0}])",
                                e.ToString(), source, cutoutbytesize, dest, cutout.Length, x, y, z, bnum));
                        }
                        source += atomWidth * table.Components * sizeof(float);
                        dest += x_width * table.Components * sizeof(float);
                    }
                    //throw new Exception(String.Format("Processed the following: {0} {1} {2} {3} {4}", source, dest, source0, dest0, lengthX));
                   
                }

            }
        }
        /*Replace below with filedb code */
            /*
            string queryString = String.Format(
                "select dbo.GetMortonX(t.zindex), dbo.GetMortonY(t.zindex), dbo.GetMortonZ(t.zindex), t.data " +
                "from {7} as t right join " +
                "(select zindex from {8}..zindex where " +
                    "X >= {0} & -{6} and X < {3} and Y >= {1} & -{6} and Y < {4} and Z >= {2} & -{6} and z < {5}) " +
                "as c " +
                "on t.zindex = c.zindex " +
                "and t.timestep = {9}", coordinates[0], coordinates[1], coordinates[2], coordinates[3], coordinates[4], coordinates[5],
                atomWidth, tableName, dbname, timestep);

            SqlCommand command = new SqlCommand(
                queryString, connection);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    x = reader.GetSqlInt32(0).Value;
                    y = reader.GetSqlInt32(1).Value;
                    z = reader.GetSqlInt32(2).Value;
                    int bytesread = 0;
                    while (bytesread < table.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(3, table.SqlArrayHeaderSize, rawdata, bytesread, table.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }
                    int sourceX = 0, destinationX = 0, sourceY = 0, destinationY = 0, sourceZ = 0, destinationZ = 0, lengthX = 0, lengthY = 0, lengthZ = 0;

                    GetSourceDestLen(x, coordinates[0], coordinates[3], atomWidth, ref sourceX, ref destinationX, ref lengthX);
                    GetSourceDestLen(y, coordinates[1], coordinates[4], atomWidth, ref sourceY, ref destinationY, ref lengthY);
                    GetSourceDestLen(z, coordinates[2], coordinates[5], atomWidth, ref sourceZ, ref destinationZ, ref lengthZ);

                    int source0 = (sourceX + sourceY * atomWidth) * table.Components * sizeof(float);
                    int dest0 = (destinationX + destinationY * x_width) * table.Components * sizeof(float);

                    for (int k = 0; k < lengthZ; k++)
                    {
                        int source = source0 + (sourceZ + k) * atomWidth * atomWidth * table.Components * sizeof(float);
                        int dest = dest0 + (destinationZ + k) * x_width * y_width * table.Components * sizeof(float);
                        for (int j = 0; j < lengthY; j++)
                        {
                            Array.Copy(rawdata, source, cutout, dest, lengthX * table.Components * sizeof(float));
                            source += atomWidth * table.Components * sizeof(float);
                            dest += x_width * table.Components * sizeof(float);
                        }
                    }
                }
            }*/


        }

    /// <summary>
    /// Given a query box formatted as "box [x1,y1,z1,x2,y2,z2]" extracts the coordinates [x1,y1,z1,x2,y2,z2]
    /// </summary>
    /// <param name="QueryBox"></param>
    /// <param name="coordinates"></param>
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
