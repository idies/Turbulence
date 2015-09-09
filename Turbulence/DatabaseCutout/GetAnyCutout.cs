﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Configuration;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SQLInterface;
using Turbulence.TurbLib.DataTypes;



public partial class StoredProcedures
{

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void GetAnyCutout(
        string dataset,
        string fields,
        int tlow,
        int xlow,
        int ylow,
        int zlow,
        int x_step,
        int y_step,
        int z_step,
        int t_step,
        int twidth,
        int xwidth,
        int ywidth,
        int zwidth,
        int filter_width,
        int time_step)
    {
        const int MAX_READ_LENGTH = 256000000;
        int atomDim = 8;
        /*Check boundaries*/
        int thigh = tlow + twidth;
        int xhigh = xlow + xwidth;
        int yhigh = ylow + ywidth;
        int zhigh = zlow + zwidth;
        int components;
        
        SqlCommand sqlcmd = new SqlCommand();
        
        long dlsize = DetermineSize(fields, twidth, xwidth / x_step, ywidth / y_step, zwidth / z_step);
        byte[] result = new byte[dlsize];
        

        if (fields.Contains("p"))
        {
            components = 1;
        }
        else if (fields.Contains("d"))
        {
            components = 1;
        }
        else
        {
            components = 3;
        }
        dataset = DataInfo.findDataSet(dataset);
        DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
        Database database = new Database("turbinfo", false);
        DataInfo.TableNames tablename;
        tablename = DataInfo.getTableName(dataset_enum, fields);


        CheckBoundaries(dataset_enum, tlow, thigh, xlow, xhigh, ylow, yhigh, zlow, zhigh);
        
        int num_virtual_servers = 1;
        database.Initialize(dataset_enum, num_virtual_servers);

        int serverCount = database.servers.Count; //Placeholder for now.
        int[] serverX = new int[serverCount];
        int[] serverY = new int[serverCount];
        int[] serverZ = new int[serverCount];
        int[] serverXwidth = new int[serverCount];
        int[] serverYwidth = new int[serverCount];
        int[] serverZwidth = new int[serverCount];
        database.selectServers(dataset_enum);
        database.development = false;
         
        database.GetServerParameters4RawData(xlow, ylow, zlow, xwidth, ywidth, zwidth,
            serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth, x_step, y_step, z_step);
        database.GetServerParameters4RawData(xlow, ylow, zlow, xwidth, ywidth, zwidth,
            serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth, 1, 1, 1);


        /*Now use the parameters to grab the data pieces*/


        for (int s = 0; s < serverCount; s++)
        {

            int size = serverXwidth[s] / x_step * serverYwidth[s] / y_step * serverZwidth[s] / z_step * components * sizeof(float);
            int readLength = size;
            byte[] rawdata = new byte[size];
            string queryBox = String.Format("box[{0},{1},{2},{3},{4},{5}]", serverX[s], serverY[s], serverZ[s],
                    serverX[s] + serverXwidth[s], serverY[s] + serverYwidth[s], serverZ[s] + serverZwidth[s]);
            String cString = String.Format("Server={0};Database='turblib';Asynchronous Processing=false;Trusted_Connection=True;Connection Lifetime=7200",
                database.servers[s]);
            SqlConnection connection = new SqlConnection(cString);
            connection.Open();
            sqlcmd = connection.CreateCommand();

            /*Fix timestep for channel data */
            if (dataset == "channel")
            {
                tlow = tlow * 5 + 132005;
                thigh = thigh * 5 + 132005;
            }

            /*Check to see if we are striding/filtering or not*/
            if ((x_step > 1) || (y_step > 1) || (z_step > 1) || (filter_width > 1))
            {
                sqlcmd.CommandText = String.Format("EXEC [{0}].[dbo].[GetFilteredCutout] @serverName, @database, @codedb, "
                                        + "@turbinfodb, @datasetid, @field, @blobDim, @timestep, @filter_width, @x_stride, @y_stride, @z_stride, @queryBox ",
                                        database.codeDatabase[s]);
                sqlcmd.Parameters.AddWithValue("@turbinfodb", "turbinfo");
                sqlcmd.Parameters.AddWithValue("@datasetid", (int)dataset_enum);
                sqlcmd.Parameters.AddWithValue("@field", tablename.ToString());
                sqlcmd.Parameters.AddWithValue("@filter_width", filter_width);
                sqlcmd.Parameters.AddWithValue("@x_stride", x_step);
                sqlcmd.Parameters.AddWithValue("@y_stride", y_step);
                sqlcmd.Parameters.AddWithValue("@z_stride", z_step);

            }
            else
            {
                sqlcmd.CommandText = String.Format("EXEC [{0}].[dbo].[GetDataCutout] @serverName, @database, @codedb, "
                                        + "@dataset, @blobDim, @timestep, @queryBox ",
                                        database.codeDatabase[s]);
                sqlcmd.Parameters.AddWithValue("@dataset", tablename.ToString());
            }
            sqlcmd.Parameters.AddWithValue("@serverName", database.servers[s]);
            sqlcmd.Parameters.AddWithValue("@database", database.databases[s]);
            sqlcmd.Parameters.AddWithValue("@codedb", database.codeDatabase[s]);

            sqlcmd.Parameters.AddWithValue("@blobDim", atomDim);
            sqlcmd.Parameters.AddWithValue("@timestep", tlow);
            sqlcmd.Parameters.AddWithValue("@queryBox", queryBox);
            sqlcmd.CommandTimeout = 3600;

            SqlDataReader reader = sqlcmd.ExecuteReader();

            while (reader.Read())
            {
                int bytesread = 0;
                while (bytesread < size)
                {
                    if (size - bytesread > MAX_READ_LENGTH)
                        readLength = MAX_READ_LENGTH;
                    else
                        readLength = size - bytesread;
                    int bytes = (int)reader.GetBytes(0, bytesread, rawdata, bytesread, readLength);
                    if (bytes <= 0)
                        throw new Exception("Unexpected end of cutout!");
                    bytesread += bytes;
                }
            }
            int sourceIndex = 0;
            int destinationIndex0 = components * (((serverX[s] - xlow) / x_step) + ((serverY[s] - ylow) / y_step) * xwidth + ((serverZ[s] - zlow) / z_step) * xwidth * ywidth) * sizeof(float);
            int destinationIndex;
            int length = serverXwidth[s] * components * sizeof(float) / x_step;
            for (int k = 0; k < serverZwidth[s] / z_step; k++)
            {
                destinationIndex = destinationIndex0 + k * (xwidth / x_step) * (ywidth / y_step) * components * sizeof(float);
                for (int j = 0; j < serverYwidth[s] / y_step; j++)
                {
                    Array.Copy(rawdata, sourceIndex, result, destinationIndex, length);
                    sourceIndex += length;
                    destinationIndex += xwidth * components * sizeof(float) / x_step;
                }
            }
            rawdata = null;
            reader.Close();
            connection.Close();
            connection = null;
        }
        SqlDataRecord record = new SqlDataRecord(new SqlMetaData("data", SqlDbType.VarBinary, -1));        
        //SqlDataRecord record = new SqlDataRecord(new SqlMetaData("data", SqlDbType.VarBinary, -1));
        // Populate the record .  Note: TEST. we did result from above, but now trying to send the raw result.
        record.SetBytes(0, 0, result, 0, result.Length);
        // Send the record to the client if we have data.  Fix this later--not sure why we resend data.
           
            
            
        //record.SetBytes(0, 0, buffer, 0, buffer.Length);
        SqlContext.Pipe.Send(record);
            //if (rawdata.Length > 0)
        // {
        //     SqlContext.Pipe.Send(record);
        // }
        //rawdata = null;
        //reader.Close();
        //connection.Close();
        //connection = null;

    }

    
    

    private static void CheckBoundaries(DataInfo.DataSets dataset, int tlow, int thigh, int xlow, int xhigh, int ylow, int yhigh, int zlow, int zhigh)
    {
        switch (dataset)
        {
            case DataInfo.DataSets.isotropic1024coarse:
            case DataInfo.DataSets.isotropic1024fine:
            case DataInfo.DataSets.mhd1024:
                if (!(tlow >= 0 && thigh <= 1025) ||
                    !(xlow >= 0 && xhigh <= 1024) ||
                    !(ylow >= 0 && yhigh <= 1024) ||
                    !(zlow >= 0 && zhigh <= 1024))
                { throw new Exception("The requested region is out of bounds"); }
                break;
            case DataInfo.DataSets.mixing:
                if (!(tlow >= 0 && thigh <= 1015) ||
                    !(xlow >= 0 && xhigh <= 1024) ||
                    !(ylow >= 0 && yhigh <= 1024) ||
                    !(zlow >= 0 && zhigh <= 1024))
                { throw new Exception("The requested region is out of bounds"); }
                break;
            case DataInfo.DataSets.channel:
                if (!(tlow >= 0 && thigh <= 1997) ||
                    !(xlow >= 0 && xhigh <= 2048) ||
                    !(ylow >= 0 && yhigh <= 512) ||
                    !(zlow >= 0 && zhigh <= 1536))
                { throw new Exception("The requested region is out of bounds"); }
                break;
            default:
                throw new Exception(String.Format("Invalid dataset specified!"));
        }
    }

    public static long DetermineSize(string fields, int T, int X, int Y, int Z)
    {
        int comps = 0;
        if (fields.Contains("u")) comps += 3;
        if (fields.Contains("p")) comps += 1;
        if (fields.Contains("a")) comps += 3;
        if (fields.Contains("b")) comps += 3;
        if (fields.Contains("d")) comps += 1;

        return (long)comps * (long)sizeof(float) * (long)(T) * (long)(X) * (long)(Y) * (long)(Z);
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
