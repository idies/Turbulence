using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SQLInterface;
using Turbulence.SQLInterface.workers;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void GetStridedDataCutout(
        string serverName,
        string dbname,
        string codedb,
        string turbinfodb,
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
            contextConn.Open();

            int[] coordinates = new int[6];
            ParseQueryBox(QueryBox, coordinates);

            TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, field, blobDim, contextConn);
            string DBtableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);

            GetCutout(table, dbname, timestep, coordinates, contextConn, x_stride, y_stride, z_stride, out cutout);
            
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

    private static void GetCutout(TurbDataTable table, string dbname, int timestep, int[] coordinates, SqlConnection connection, 
        int x_stride, int y_stride, int z_stride, out byte[] cutout)
    {
        int x_width, y_width, z_width, x, y, z;
        x_width = (coordinates[3] - coordinates[0] - 1) / x_stride + 1;
        y_width = (coordinates[4] - coordinates[1] - 1) / y_stride + 1;
        z_width = (coordinates[5] - coordinates[2] - 1) / z_stride + 1;

        byte[] rawdata = new byte[table.BlobByteSize];
        cutout = new byte[table.Components * sizeof(float) * x_width * y_width * z_width];

        string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);
        int atomWidth = table.atomDim;

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

                GetSourceDestLen(x, coordinates[0], coordinates[3], atomWidth, x_stride, ref sourceX, ref destinationX, ref lengthX);
                GetSourceDestLen(y, coordinates[1], coordinates[4], atomWidth, y_stride, ref sourceY, ref destinationY, ref lengthY);
                GetSourceDestLen(z, coordinates[2], coordinates[5], atomWidth, z_stride, ref sourceZ, ref destinationZ, ref lengthZ);
                
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
        }
    }

    private static void GetSourceDestLen(int coordinate, int lower, int upper, int atomWidth, int stride, ref int source, ref int dest, ref int len)
    {
        if (coordinate <= lower)
        {
            source = lower - coordinate;
            dest = 0;
            if (coordinate + atomWidth <= lower)
                throw new Exception("Atom read is outside of boundaries of query box!");
            else if (coordinate + atomWidth <= upper)
                len = atomWidth - source;
            else
                len = upper - lower;
        }
        else if (coordinate >= upper)
            throw new Exception("Atom read is outside of boundaries of query box!");
        else
        {
            dest = coordinate - lower;
            dest = (dest + stride - 1) / stride;
            source = lower + dest * stride - coordinate;
            if (coordinate + atomWidth <= upper)
                len = atomWidth - source;
            else
                len = upper - coordinate - source;
        }
    }
};
