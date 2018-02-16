using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SQLInterface;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void GetDataCutout(
        string serverName,
        string dbname,
        string codedb,
        string turbinfodb,
        string turbinfoserver,
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

        // Load information about the requested dataset
        TurbServerInfo serverinfo = TurbServerInfo.GetTurbServerInfo(codedb, turbinfodb, turbinfoserver);
        TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, dataset, blobDim, serverinfo);

        int[] coordinates = new int[6];

        ParseQueryBox(QueryBox, coordinates);

        byte[] cutout;

        connection.Open();
        GetCutout(table, dbname, timestep, coordinates, connection, out cutout);

        // Populate the record
        //record.SetBytes(0, 0, cutout, 0, cutout.Length);
        // Send the record to the client.
        //SqlContext.Pipe.Send(record);
        blob = new SqlBytes(cutout);
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

        byte[] rawdata = new byte[table.BlobByteSize];
        cutout = new byte[table.Components * sizeof(float) * x_width * y_width * z_width];

        string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);
        int atomWidth = table.atomDim;

        //string queryString = String.Format(
        //    "select dbo.GetMortonX(zindex), dbo.GetMortonY(zindex), dbo.GetMortonZ(zindex), data " +
        //    "from {0} as t right join (select * from dbo.MHDCover('{1}')) as c " +
        //    "on t.zindex between c.KeyMin and c.KeyMax " +
        //    "and t.timestep = {2}", tableName, QueryBox, timestep);
        //// Because of the non-square grid for the channel flow DB we can't use fcover
        //if (dbname.Contains("channel") || dbname.Contains("mixing"))
        //{
        //    queryString = String.Format(
        //        "select dbo.GetMortonX(t.zindex), dbo.GetMortonY(t.zindex), dbo.GetMortonZ(t.zindex), t.data " +
        //        "from {7} as t right join " +
        //        "(select zindex from {8}..zindex where " +
        //            "X >= {0} & -{6} and X < {3} and Y >= {1} & -{6} and Y < {4} and Z >= {2} & -{6} and z < {5}) " +
        //        "as c " +
        //        "on t.zindex = c.zindex " +
        //        "and t.timestep = {9}", coordinates[0], coordinates[1], coordinates[2], coordinates[3], coordinates[4], coordinates[5],
        //        atomWidth, tableName, dbname, timestep);
        //}

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
        }
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
                throw new Exception("Atom read is outside of boundaries of query box!");
            else if (coordinate + atomWidth <= upper)
                len = coordinate + atomWidth - lower;
            else
                len = upper - lower;
            source = lower - coordinate;
            dest = 0;
        }
        else if (coordinate >= upper)
            throw new Exception("Atom read is outside of boundaries of query box!");
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