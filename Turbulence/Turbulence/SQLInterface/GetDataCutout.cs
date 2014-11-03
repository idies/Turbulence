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

    //TODO: Consider removing this copy and just using the function below
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

    //TODO: Consider removing the copy above and just using this function    
    private static void GetCutout(TurbDataTable table, string dbname, int timestep,
        int[] cutout_coordinates, int[] local_coordinates,
        SqlConnection connection, ref byte[] cutout)
    {
        int x_width, y_width, z_width, x, y, z;
        x_width = cutout_coordinates[3] - cutout_coordinates[0];
        y_width = cutout_coordinates[4] - cutout_coordinates[1];
        z_width = cutout_coordinates[5] - cutout_coordinates[2];

        byte[] rawdata = new byte[table.BlobByteSize];

        string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);
        int atomWidth = table.atomDim;
        string queryString = GetQueryString(local_coordinates, table, atomWidth, tableName, dbname, timestep);

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

                GetSourceDestLenWithWrapAround(x, local_coordinates[0], local_coordinates[3], cutout_coordinates[0], atomWidth, table.GridResolutionX,
                    ref sourceX, ref destinationX, ref lengthX);
                GetSourceDestLenWithWrapAround(y, local_coordinates[1], local_coordinates[4], cutout_coordinates[1], atomWidth, table.GridResolutionY,
                    ref sourceY, ref destinationY, ref lengthY);
                GetSourceDestLenWithWrapAround(z, local_coordinates[2], local_coordinates[5], cutout_coordinates[2], atomWidth, table.GridResolutionZ,
                    ref sourceZ, ref destinationZ, ref lengthZ);

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

    // Overload using a float array.
    private static void GetCutout(TurbDataTable table, string dbname, int timestep,
        int[] cutout_coordinates, int[] local_coordinates,
        SqlConnection connection, ref float[] cutout)
    {
        int x_width, y_width, z_width, x, y, z;
        x_width = cutout_coordinates[3] - cutout_coordinates[0];
        y_width = cutout_coordinates[4] - cutout_coordinates[1];
        z_width = cutout_coordinates[5] - cutout_coordinates[2];

        byte[] rawdata = new byte[table.BlobByteSize];

        string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);
        int atomWidth = table.atomDim;

        string queryString = GetQueryString(local_coordinates, table, atomWidth, tableName, dbname, timestep);

        int sourceX = 0, sourceY = 0, sourceZ = 0, lengthX = 0, lengthY = 0, lengthZ = 0, destinationX = 0, destinationY = 0, destinationZ = 0;

        SqlCommand command = new SqlCommand(
            queryString, connection);
        command.CommandTimeout = 600;
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

                GetSourceDestLenWithWrapAround(x, local_coordinates[0], local_coordinates[3], cutout_coordinates[0], atomWidth, table.GridResolutionX,
                    ref sourceX, ref destinationX, ref lengthX);
                GetSourceDestLenWithWrapAround(y, local_coordinates[1], local_coordinates[4], cutout_coordinates[1], atomWidth, table.GridResolutionY,
                    ref sourceY, ref destinationY, ref lengthY);
                GetSourceDestLenWithWrapAround(z, local_coordinates[2], local_coordinates[5], cutout_coordinates[2], atomWidth, table.GridResolutionZ,
                    ref sourceZ, ref destinationZ, ref lengthZ);

                int source0 = (sourceX + sourceY * atomWidth) * table.Components * sizeof(float);
                int dest0 = (destinationX + destinationY * x_width) * table.Components * sizeof(float);

                for (int k = 0; k < lengthZ; k++)
                {
                    int source = source0 + (sourceZ + k) * atomWidth * atomWidth * table.Components * sizeof(float);
                    int dest = dest0 + (destinationZ + k) * x_width * y_width * table.Components * sizeof(float);
                    for (int j = 0; j < lengthY; j++)
                    {
                        Buffer.BlockCopy(rawdata, source, cutout, dest, lengthX * table.Components * sizeof(float));
                        source += atomWidth * table.Components * sizeof(float);
                        dest += x_width * table.Components * sizeof(float);
                    }
                }
            }
        }
    }

    // Overload using a big float array.
    private static void GetCutout(TurbDataTable table, string dbname, int timestep, 
        int[] cutout_coordinates, int[] local_coordinates, 
        SqlConnection connection, ref BigArray<float> cutout)
    {
        int x_width, y_width, z_width, x, y, z;
        x_width = cutout_coordinates[3] - cutout_coordinates[0];
        y_width = cutout_coordinates[4] - cutout_coordinates[1];
        z_width = cutout_coordinates[5] - cutout_coordinates[2];

        byte[] rawdata = new byte[table.BlobByteSize];
        
        string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);
        int atomWidth = table.atomDim;

        string queryString = GetQueryString(local_coordinates, table, atomWidth, tableName, dbname, timestep);

        int sourceX = 0, sourceY = 0, sourceZ = 0, lengthX = 0, lengthY = 0, lengthZ = 0, destinationX = 0, destinationY = 0, destinationZ = 0;
        ulong long_destinationX = 0, long_destinationY = 0, long_destinationZ = 0;
        ulong long_components = (ulong)table.Components;
        ulong long_x_width = (ulong)x_width;
        ulong long_y_width = (ulong)y_width;

        SqlCommand command = new SqlCommand(
            queryString, connection);
        command.CommandTimeout = 600;
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

                GetSourceDestLenWithWrapAround(x, local_coordinates[0], local_coordinates[3], cutout_coordinates[0], atomWidth, table.GridResolutionX, 
                    ref sourceX, ref destinationX, ref lengthX);
                GetSourceDestLenWithWrapAround(y, local_coordinates[1], local_coordinates[4], cutout_coordinates[1], atomWidth, table.GridResolutionY, 
                    ref sourceY, ref destinationY, ref lengthY);
                GetSourceDestLenWithWrapAround(z, local_coordinates[2], local_coordinates[5], cutout_coordinates[2], atomWidth, table.GridResolutionZ, 
                    ref sourceZ, ref destinationZ, ref lengthZ);
                long_destinationX = (ulong)destinationX;
                long_destinationY = (ulong)destinationY;
                long_destinationZ = (ulong)destinationZ;

                int source0 = (sourceX + sourceY * atomWidth) * table.Components * sizeof(float);
                ulong dest0 = (long_destinationX + long_destinationY * long_x_width) * long_components * sizeof(float);

                for (int k = 0; k < lengthZ; k++)
                {
                    int source = source0 + (sourceZ + k) * atomWidth * atomWidth * table.Components * sizeof(float);
                    ulong dest = dest0 + (long_destinationZ + (ulong)k) * long_x_width * long_y_width * long_components * sizeof(float);
                    for (int j = 0; j < lengthY; j++)
                    {
                        cutout.BlockCopyInto(rawdata, source, dest, lengthX * table.Components * sizeof(float), sizeof(float));
                        source += atomWidth * table.Components * sizeof(float);
                        dest += long_x_width * long_components * sizeof(float);
                    }
                }
            }
        }
    }

    private static string GetQueryString(int[] local_coordinates, TurbDataTable table, int atomWidth, string tableName, string dbname, int timestep)
    {
        int start_z = local_coordinates[2];
        int start_y = local_coordinates[1];
        int start_x = local_coordinates[0];
        int end_z = local_coordinates[5];
        int end_y = local_coordinates[4];
        int end_x = local_coordinates[3];

        if (start_z < 0)
        {
            start_z += table.GridResolutionZ;
            end_z += table.GridResolutionZ;
        }
        else if (start_z >= table.GridResolutionZ)
        {
            start_z -= table.GridResolutionZ;
            end_z -= table.GridResolutionZ;
        }
        if (start_y < 0)
        {
            start_y += table.GridResolutionY;
            end_y += table.GridResolutionY;
        }
        else if (start_y >= table.GridResolutionY)
        {
            start_y -= table.GridResolutionY;
            end_y -= table.GridResolutionY;
        }
        if (start_x < 0)
        {
            start_x += table.GridResolutionX;
            end_x += table.GridResolutionX;
        }
        else if (start_x >= table.GridResolutionX)
        {
            start_x -= table.GridResolutionX;
            end_x -= table.GridResolutionX;
        }

        return String.Format(
            "select dbo.GetMortonX(t.zindex), dbo.GetMortonY(t.zindex), dbo.GetMortonZ(t.zindex), t.data " +
            "from {7} as t inner join " +
            "(select zindex from {8}..zindex where " +
                "X >= {0} & -{6} and X < {3} and Y >= {1} & -{6} and Y < {4} and Z >= {2} & -{6} and z < {5}) " +
            "as c " +
            "on t.zindex = c.zindex " +
            "and t.timestep = {9}",
            start_x, start_y, start_z,
            end_x, end_y, end_z,
            atomWidth, tableName, dbname, timestep);
    }

    private static void GetLocalCoordiantes(int cutout_start_coordinate, int cutout_end_coordiante,
        int grid_start, int grid_end,
        //int gridResolution,
        out int[] local_start_coordinate, out int[] local_end_coordinate)
    {
        int num_regions = 1, max_regions = 3;
        int[] temp_start_coordinates = new int[max_regions];
        int[] temp_end_coordinates = new int[max_regions];
        temp_start_coordinates[0] = cutout_start_coordinate;
        if (cutout_start_coordinate < grid_start)
        {
            temp_start_coordinates[num_regions] = grid_start;
            temp_end_coordinates[0] = grid_start;
            num_regions++;
        }
        if (cutout_end_coordiante > grid_end + 1)
        {
            temp_start_coordinates[num_regions] = grid_end + 1;
            temp_end_coordinates[num_regions - 1] = grid_end + 1;
            num_regions++;
        }
        temp_end_coordinates[num_regions - 1] = cutout_end_coordiante;

        local_start_coordinate = new int[num_regions];
        local_end_coordinate = new int[num_regions];
        for (int i = 0; i < num_regions; i++)
        {
            local_start_coordinate[i] = temp_start_coordinates[i];
            local_end_coordinate[i] = temp_end_coordinates[i];
            //if (temp_start_coordinates[i] < 0)
            //{
            //    local_start_coordinate[i] = temp_start_coordinates[i] + gridResolution;
            //    local_end_coordinate[i] = temp_end_coordinates[i] + gridResolution;
            //}
            //else if (temp_start_coordinates[i] >= gridResolution)
            //{
            //    local_start_coordinate[i] = temp_start_coordinates[i] - gridResolution;
            //    local_end_coordinate[i] = temp_end_coordinates[i] - gridResolution;
            //}
            //else
            //{
            //    local_start_coordinate[i] = temp_start_coordinates[i];
            //    local_end_coordinate[i] = temp_end_coordinates[i];
            //}
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

    private static void GetSourceDestLenWithWrapAround(int coordinate, int local_start, int local_end, int cutout_start, int atomWidth, int gridResolution, 
        ref int source, ref int dest, ref int len)
    {
        if (coordinate + atomWidth <= local_start)
        {
            // This is due to wrap around
            coordinate += gridResolution;
        }
        if (coordinate > local_end)
        {
            // This is due to wrap around
            coordinate -= gridResolution;
        }

        if (coordinate < local_start)
        {
            if (coordinate + atomWidth <= local_start)
                throw new Exception("Atom read is outside of boundaries of query box!");
            else if (coordinate + atomWidth <= local_end)
                len = coordinate + atomWidth - local_start;
            else
                len = local_end - local_start;
            source = local_start - coordinate;
            dest = 0;
        }
        else if (coordinate >= local_end)
            throw new Exception("Atom read is outside of boundaries of query box!");
        else
        {
            if (coordinate + atomWidth <= local_end)
                len = atomWidth;
            else
                len = local_end - coordinate;
            source = 0;
            dest = coordinate - cutout_start;
        }
    }
};
