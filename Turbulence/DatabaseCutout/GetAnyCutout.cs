using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Configuration;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SQLInterface;
using Turbulence.TurbLib.DataTypes;
using System.Text;
using System.IO;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void GetAnyCutout(
        string dataset,
        string fields,
        string authToken,
        string ipaddr,
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
        const bool DEVEL_MODE = false;
        const string infodb = "turbinfo";
        //const string infodb = "turbinfo_test";

        const int MAX_READ_LENGTH = 256000000;
        int atomDim = 8;
        /*Check boundaries*/
        int thigh = tlow + twidth;
        int xhigh = xlow + xwidth;
        int yhigh = ylow + ywidth;
        int zhigh = zlow + zwidth;
        int components;
        Database database = new Database(infodb, DEVEL_MODE);
        AuthInfo authInfo = new AuthInfo(database.infodb, database.infodb_server, DEVEL_MODE);
        Log log = new Log(database.infodb, database.infodb_server, DEVEL_MODE);
        SqlCommand sqlcmd = new SqlCommand();

        long dlsize = DetermineSize(fields, twidth, (xwidth + x_step - 1) / x_step, (ywidth + y_step - 1) / y_step, (zwidth + z_step - 1) / z_step);
        byte[] result = new byte[dlsize];


        if (fields.Contains("p"))
        {
            components = 1;
        }
        else if (fields.Contains("d"))
        {
            components = 1;
        }
        else if (fields.Contains("t"))
        {
            components = 1;
        }
        else
        {
            components = 3;
        }
        dataset = DataInfo.findDataSet(dataset);
        DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
        
        DataInfo.TableNames tablename;
        tablename = DataInfo.getTableName(dataset_enum, fields);
        /* Verify user auth token */
        AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, xwidth * ywidth * zwidth);

        //CheckBoundaries(dataset_enum, tlow, thigh, xlow, xhigh, ylow, yhigh, zlow, zhigh);
        object rowid = null;
        if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
        {
            log.devmode = true; //This makes sure we don't log the monitoring service.
        }
        rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawVelocity,
            (int)TurbulenceOptions.SpatialInterpolation.None,
            (int)TurbulenceOptions.TemporalInterpolation.None,
           xwidth * ywidth * zwidth, tlow * database.Dt * database.TimeInc, thigh * database.Dt * database.TimeInc, null, ipaddr);
        log.UpdateRecordCount(auth.Id, twidth * xwidth * ywidth * zwidth);

        int num_virtual_servers = 1;
        /*Fix timestep offset for channel data */
        if (dataset == "channel")
        {
            tlow = tlow + 132005;
            thigh = thigh + 132005;
        }
        database.Initialize(dataset_enum, num_virtual_servers);

        int serverCount = database.servers.Count;
        int[] serverX = new int[serverCount];
        int[] serverY = new int[serverCount];
        int[] serverZ = new int[serverCount];
        int[] serverXwidth = new int[serverCount];
        int[] serverYwidth = new int[serverCount];
        int[] serverZwidth = new int[serverCount];
        int[] serverTmin = new int[serverCount];
        int[] serverTmax = new int[serverCount];

        //database.selectServers(dataset_enum);
        //throw new Exception("DB Type is: " + database.dbtype.ToString());
        //database.development = false;

        database.GetServerParameters4RawData(xlow, ylow, zlow, xwidth, ywidth, zwidth,
            serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth, x_step, y_step, z_step, tlow, twidth, database.dbtype); //Added time for temporal server location


        //database.GetServerParameters4RawData(xlow, ylow, zlow, xwidth, ywidth, zwidth,
        //   serverX, serverY, serverZ, serverXwidth, serverYwidth, serverZwidth, 1, 1, 1);
        /*Update xwidth. This is required for reassembly when x is strided */
        int destinationIndex;
        bool doFilter = false;
        if ((x_step > 1) || (y_step > 1) || (z_step > 1) || (filter_width > 1))
        {
            doFilter = true;
        }
        else
        {
            doFilter = false;
        }

            /*Now use the parameters to grab the data pieces*/
        for (int s = 0; s < serverCount; s++)
        {

            int size;
            size = ((serverXwidth[s] + x_step - 1) / x_step) * ((serverYwidth[s] + y_step - 1) / y_step) * ((serverZwidth[s] + z_step - 1) / z_step) * components * sizeof(float);
            int readLength = size;
            if (size > 0) /* Only connect to servers that have data for us */
            {

                byte[] rawdata = new byte[size];
                string queryBox = String.Format("box[{0},{1},{2},{3},{4},{5}]", serverX[s], serverY[s], serverZ[s],
                        serverX[s] + serverXwidth[s], serverY[s] + serverYwidth[s], serverZ[s] + serverZwidth[s]);
                String cString = String.Format("Server={0};Database='{1}';Asynchronous Processing=true;User ID='turbquery';Password='aa2465ways2k';Connection Lifetime=7200",
                    database.servers[s], database.codeDatabase[s]);
                SqlConnection connection = new SqlConnection(cString);
                connection.Open();
                sqlcmd = connection.CreateCommand();
                /* Look up this info from the database map table */
                if (database.dbtype == 0)
                {
                    //throw new Exception("We didn't detect as filedb! DB type is: " + database.dbtype.ToString());

                    /*Check to see if we are striding/filtering or not*/
                    if (doFilter)
                    {
                        sqlcmd.CommandText = String.Format("EXEC [{0}].[dbo].[GetFilteredCutout] @serverName, @dbname, @codedb, "
                                                + "@turbinfodb, @turbinfoserver, @datasetid, @field, @blobDim, @timestep, @filter_width, @x_stride, @y_stride, @z_stride, @queryBox ",
                                                database.codeDatabase[s]);
                        sqlcmd.Parameters.AddWithValue("@datasetid", (int)dataset_enum);

                        //sqlcmd.Parameters.AddWithValue("@blobDim", atomDim);
                        sqlcmd.Parameters.AddWithValue("@filter_width", filter_width);
                        sqlcmd.Parameters.AddWithValue("@x_stride", x_step);
                        sqlcmd.Parameters.AddWithValue("@y_stride", y_step);
                        sqlcmd.Parameters.AddWithValue("@z_stride", z_step);
                    }
                    else
                    {
                        sqlcmd.CommandText = String.Format("EXEC [{0}].[dbo].[GetDataCutout] @serverName, @dbname, @codedb, "
                                                + "@turbinfodb, @turbinfoserver, @field, @blobDim, @timestep, @queryBox, @blob OUTPUT ",
                                                database.codeDatabase[s]);
                        //sqlcmd.Parameters.AddWithValue("@dataset", tablename.ToString());
                        SqlParameter outData = new SqlParameter();
                        outData.SqlDbType = SqlDbType.VarBinary;
                        outData.Size = size; // This ensures the proper output size.  On small cutouts, it was setting to 1, causing an error in arraycopy.
                        outData.Direction = ParameterDirection.Output;
                        outData.ParameterName = "@blob";
                        outData.Value = rawdata;
                        sqlcmd.Parameters.Add(outData);
                    }
                }
                else
                {
                    /*Check to see if we are striding/filtering or not*/
                    if (doFilter)
                    {
                        sqlcmd.CommandText = String.Format("EXEC [{0}].[dbo].[GetFilteredCutout] @serverName, @dbname, @codedb, " //TODO: Modify for filedb.
                                                + "@turbinfodb, @turbinfoserver, @datasetid, @field, @blobDim, @timestep, @filter_width, @x_stride, @y_stride, @z_stride, @queryBox ",
                                                database.codeDatabase[s]);                        
                        sqlcmd.Parameters.AddWithValue("@datasetid", (int)dataset_enum);
                        //sqlcmd.Parameters.AddWithValue("@field", tablename.ToString());
                        //sqlcmd.Parameters.AddWithValue("@blobDim", atomDim);
                        sqlcmd.Parameters.AddWithValue("@filter_width", filter_width);
                        sqlcmd.Parameters.AddWithValue("@x_stride", x_step);
                        sqlcmd.Parameters.AddWithValue("@y_stride", y_step);
                        sqlcmd.Parameters.AddWithValue("@z_stride", z_step);
                        //throw new Exception("We are filtering in filedb, but it isn't implemented yet!");
                    }
                    else
                    {
                        sqlcmd.CommandText = String.Format("EXEC [{0}].[dbo].[GetDataFileDBCutout2] @serverName, @dbname, @codedb, "
                                                + "@turbinfodb, @turbinfoserver, @field, @blobDim, @timestep, @queryBox, @blob OUTPUT ",
                                                database.codeDatabase[s]);
                        //sqlcmd.Parameters.AddWithValue("@dataset", tablename.ToString());
                        SqlParameter outData = new SqlParameter();
                        outData.SqlDbType = SqlDbType.VarBinary;
                        outData.Size = size; // This ensures the proper output size.  On small cutouts, it was setting to 1, causing an error in arraycopy.
                        outData.Direction = ParameterDirection.Output;
                        outData.ParameterName = "@blob";
                        outData.Value = rawdata;
                        sqlcmd.Parameters.Add(outData);
                    }

                }
                
                sqlcmd.Parameters.AddWithValue("@field", tablename.ToString());
                sqlcmd.Parameters.AddWithValue("@serverName", database.servers[s]);
                sqlcmd.Parameters.AddWithValue("@dbname", database.databases[s]);
                sqlcmd.Parameters.AddWithValue("@codedb", database.codeDatabase[s]);
                sqlcmd.Parameters.AddWithValue("@turbinfodb", database.infodb);
                sqlcmd.Parameters.AddWithValue("@turbinfoserver", database.infodb_server);
                sqlcmd.Parameters.AddWithValue("@blobDim", atomDim);
                sqlcmd.Parameters.AddWithValue("@timestep", tlow);
                sqlcmd.Parameters.AddWithValue("@queryBox", queryBox);
                sqlcmd.CommandTimeout = 3600;

                if (!doFilter)
                {
                    //size = serverXwidth[s] * serverYwidth[s] * serverZwidth[s] * components * sizeof(float);
                    //readLength = size;
                    //byte[] rawdata = new byte[size];
                    SqlDataReader dr = sqlcmd.ExecuteReader();
                    dr.Close();

                    try
                    {
                        rawdata = (byte[])sqlcmd.Parameters["@blob"].Value;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(
                            String.Format("Error querying filedb.  Inner exception: {0} query result  {1},  {2}, {3}, {4}, {5}, {6}", ex.Message, database.servers[s], database.databases[s], tablename.ToString(), queryBox, tlow, sqlcmd.Parameters["@blob"].Value));
                    }
                }
                else
                {
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
                    reader.Close();

                }
                int sourceIndex = 0;
                //int destinationIndex0 = components * (((serverX[s] - xlow) / x_step) + ((serverY[s] - ylow) / y_step) * ((xwidth) / x_step) + ((serverZ[s] - zlow) / z_step) * ((xwidth) / x_step) *( (ywidth) / y_step)) * sizeof(float);
                int destinationIndex0 = components * (((serverX[s] - xlow) / x_step) + ((serverY[s] - ylow) / y_step) * ((xwidth + x_step - 1) / x_step) + ((serverZ[s] - zlow) / z_step) * ((xwidth + x_step - 1) / x_step) * ((ywidth + y_step - 1) / y_step)) * sizeof(float);
                int c = 0;
                int length = ((serverXwidth[s] + x_step - 1) / x_step) * sizeof(float) * components;
                //int length = ((serverXwidth[s] * components * sizeof(float) ) / x_step)*((serverYwidth[s] * components * sizeof(float) ) / y_step);
                for (int k = 0; k < serverZwidth[s]; k += z_step)
                {
                    destinationIndex = destinationIndex0 + c * ((ywidth + y_step - 1) / y_step) * ((xwidth + x_step - 1) / x_step) * components * sizeof(float);
                    c++;
                    for (int j = 0; j < serverYwidth[s]; j += y_step)
                    {
                        Array.Copy(rawdata, sourceIndex, result, destinationIndex, length);
                        sourceIndex += length;
                        destinationIndex += components * sizeof(float) * ((xwidth + x_step - 1) / x_step);

                    }
                }

                //int destinationIndex = components * (((serverX[s] - xlow) / x_step) + ((serverY[s] - ylow) / y_step) * xwidth + ((serverZ[s] - zlow) / z_step) * xwidth * ywidth) * sizeof(float);
                //Array.Copy(rawdata, 0, result, destinationIndex0, size);
                rawdata = null;

                connection.Close();
                connection = null;
            }
        }
        SqlDataRecord record = new SqlDataRecord(new SqlMetaData("data", SqlDbType.VarBinary, -1));
        //SqlDataRecord record = new SqlDataRecord(new SqlMetaData("data", SqlDbType.VarBinary, -1));
        // Populate the record .  Note: TEST. we did result from above, but now trying to send the raw result.
        record.SetBytes(0, 0, result, 0, result.Length);
        // Send the record to the client if we have data.  Fix this later--not sure why we resend data.

        if (false && authToken == "edu.jhu.pha.turbulence-dev")
        {
            float[] f = new float[result.Length / sizeof(float)];
            for (int i = 0; i < f.Length; i++)
            {
                f[i] = BitConverter.ToSingle(result, i * 4);
            }

            //byte[] bytes = BitConverter.GetBytes(0x2D1509C0);
            //Array.Reverse(bytes);
            //float myFloat = BitConverter.ToSingle(bytes, 0); // Always be correct

            var csv1 = new StringBuilder();
            for (int i = 0; i < f.Length / components; i++)
            {
                //Suggestion made by KyleMit
                var newLine1 = "";
                if (components == 3)
                {
                    newLine1 = string.Format("{0},{1},{2}", f[i * components], f[i * components + 1], f[i * components + 2]);
                }
                else if (components == 1)
                {
                    newLine1 = string.Format("{0}", f[i]);
                }
                csv1.AppendLine(newLine1);
            }
            File.WriteAllText("C:\\Users\\zwu27\\Documents\\vel_pr_cutout2.txt", csv1.ToString());
        }      

        //record.SetBytes(0, 0, buffer, 0, buffer.Length);
        SqlContext.Pipe.Send(record);
        /*Update log record*/
        log.UpdateLogRecord(rowid, database.Bitfield);
        log.Reset();
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

            case DataInfo.DataSets.isotropic1024fine:
            case DataInfo.DataSets.mhd1024:
                if (!(tlow >= 0 && thigh <= 1025 * 10) ||
                    !(xlow >= 0 && xhigh <= 1024) ||
                    !(ylow >= 0 && yhigh <= 1024) ||
                    !(zlow >= 0 && zhigh <= 1024))
                { throw new Exception("The requested region is out of bounds"); }
                break;
            case DataInfo.DataSets.isotropic1024coarse:
                /* Adding 100 timesteps to coarse */
                if (!(tlow >= 0 && thigh <= 1125 * 10) ||
                    !(xlow >= 0 && xhigh <= 1024) ||
                    !(ylow >= 0 && yhigh <= 1024) ||
                    !(zlow >= 0 && zhigh <= 1024))
                { throw new Exception("The requested region is out of bounds"); }
                break;
            case DataInfo.DataSets.isotropic4096:
            case DataInfo.DataSets.strat4096:
                if (!(tlow >= 0 && thigh <= 1) ||
                    !(xlow >= 0 && xhigh <= 4096) ||
                    !(ylow >= 0 && yhigh <= 4096) ||
                    !(zlow >= 0 && zhigh <= 4096))
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
                if (!(tlow >= 0 && thigh <= 4000 * 5) ||
                    !(xlow >= 0 && xhigh <= 2048) ||
                    !(ylow >= 0 && yhigh <= 512) ||
                    !(zlow >= 0 && zhigh <= 1536))
                { throw new Exception("The requested region is out of bounds"); }
                break;
            case DataInfo.DataSets.bl_zaki:
                if (!(tlow >= 0 && thigh <= 7) ||
                    !(xlow >= 0 && xhigh <= 3320) ||
                    !(ylow >= 0 && yhigh <= 224) ||
                    !(zlow >= 0 && zhigh <= 2048))
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
        if (fields.Contains("t")) comps += 1;

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
