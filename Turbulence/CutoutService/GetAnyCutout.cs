using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SQLInterface;
using Turbulence.TurbLib.DataTypes;
using TurbulenceService;

namespace CutoutService
{

    public partial class StoredProcedures
    {
        
        [Microsoft.SqlServer.Server.SqlProcedure]
        public static void GetAnyCutout(           
            string dataset,
            string fields,
            string authToken,
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
        Database database = new Database("turbinfo", true);
        
        //Maximum download size allowed
        long maxsize = 12L * 256L * 350L * 512L; //525MB
        byte[] cutout = null;

        int thigh = tlow + twidth - 1,
            xhigh = xlow + xwidth - 1,
            yhigh = ylow + ywidth - 1,
            zhigh = zlow + zwidth - 1;

        // Number of points in the result set is a function of the step size.
        int tsize = (twidth + time_step - 1) / time_step,
            xsize = (xwidth + x_step - 1) / x_step,
            ysize = (ywidth + y_step - 1) / y_step,
            zsize = (zwidth + z_step - 1) / z_step;
        long size = (long)xsize * (long)ysize * (long)zsize * 3 * sizeof(float);
        long[] datasize = { zsize, ysize, xsize, 3 };
        dataset = DataInfo.findDataSet(dataset);
        DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);

        CheckBoundaries(dataset_enum, tlow, thigh, xlow, xhigh, ylow, yhigh, zlow, zhigh);
        
        //Prevent people from trying to download the entire database
        long dlsize = DetermineSize(fields, tsize, xsize, ysize, zsize);
        if (dlsize > maxsize)
        {
            throw new Exception("Maximum file size exceeded. ");
            
        }
        //DateTime start = DateTime.Now;
        int num_virtual_servers = 1;
        database.Initialize(dataset_enum, num_virtual_servers);

        int pieces = 1, dz = (int)datasize[0];

        //If a single buffer exceeds 2gb, then split into multiple pieces
        //if (size > 2000000000L)
        if (size > 256000000L)
        {
            pieces = (int)Math.Ceiling((float)size / 256000000L);

            //Round up to nearest power of 2
            pieces--;
            pieces |= pieces >> 1;
            pieces |= pieces >> 2;
            pieces |= pieces >> 4;
            pieces |= pieces >> 8;
            pieces |= pieces >> 16;
            pieces++;
            dz = (int)Math.Ceiling((float)zwidth / pieces / 8) * 8;
        }

        DataInfo.TableNames tableName;
        int components;
        string field;

        if (fields.Contains("u"))
        {
            components = 3;
            field = "u";

            tableName = DataInfo.getTableName(dataset_enum, field);
            object rowid = null;
            

            if (time_step == 1 && x_step == 1 && y_step == 1 && z_step == 1 && filter_width == 1)
            {
                //GetRawData_(cutout, dataType, dataspace, field, pieces, dz, dataset_enum, tableName, components, tlow, thigh, xlow, ylow, zlow, xwidth, ywidth, zwidth);
                for (int t = tlow; t <= thigh; t++)
                {
                    DataInfo.verifyTimeInRange(dataset_enum, (float)t * database.Dt * database.TimeInc);
                    for (int p = 0; p < pieces; p++)
                    {
                        int chunklen = (p + 1) * dz <= zsize ? dz : zsize - p * dz;
                        if (chunklen == 0) break;
                        cutout = database.GetRawData(dataset_enum, tableName, t * database.Dt * database.TimeInc, components, xlow, ylow, zlow + p * dz, xsize, ysize, chunklen);
                    }
                }
            }
            else
            {
                for (int t = tlow; t <= thigh; t += t_step)
                {
                   cutout = database.GetFilteredData(dataset_enum, tableName, t * database.Dt * database.TimeInc, components, xlow, ylow, zlow, xwidth, ywidth, zwidth,
                    x_step, y_step, z_step, filter_width);
                }
            }

        }

        if (fields.Contains("b"))
        {
            components = 3;
            field = "b";

            tableName = DataInfo.getTableName(dataset_enum, field);
            object rowid = null;
            
            if (time_step == 1 && x_step == 1 && y_step == 1 && z_step == 1 && filter_width == 1)
            {
                for (int t = tlow; t <= thigh; t++)
                {
                    DataInfo.verifyTimeInRange(dataset_enum, (float)t * database.Dt * database.TimeInc);
                    for (int p = 0; p < pieces; p++)
                    {
                        int chunklen = (p + 1) * dz <= zsize ? dz : zsize - p * dz;
                        if (chunklen == 0) break;
                        cutout = database.GetRawData(dataset_enum, tableName, t * database.Dt * database.TimeInc, components, xlow, ylow, zlow + p * dz, xsize, ysize, chunklen);
                    }
                }
            }
            else
            {
                for (int t = tlow; t <= thigh; t += t_step)
                {
                    cutout = database.GetFilteredData(dataset_enum, tableName, t * database.Dt * database.TimeInc, components, xlow, ylow, zlow, xwidth, ywidth, zwidth,
                    x_step, y_step, z_step, filter_width);
                }
            }

          
        }

        if (fields.Contains("a"))
        {
            components = 3;
            field = "a";

            tableName = DataInfo.getTableName(dataset_enum, field);
      
            
            if (time_step == 1 && x_step == 1 && y_step == 1 && z_step == 1 && filter_width == 1)
            {
                for (int t = tlow; t <= thigh; t++)
                {
                    DataInfo.verifyTimeInRange(dataset_enum, (float)t * database.Dt * database.TimeInc);
                    for (int p = 0; p < pieces; p++)
                    {
                        int chunklen = (p + 1) * dz <= zsize ? dz : zsize - p * dz;
                        if (chunklen == 0) break;
                        cutout = database.GetRawData(dataset_enum, tableName, t * database.Dt * database.TimeInc, components, xlow, ylow, zlow + p * dz, xsize, ysize, chunklen);
                    }
                }
            }
            else
            {
                for (int t = tlow; t <= thigh; t += t_step)
                {
                    cutout = database.GetFilteredData(dataset_enum, tableName, t * database.Dt * database.TimeInc, components, xlow, ylow, zlow, xwidth, ywidth, zwidth,
                    x_step, y_step, z_step, filter_width);
                }
            }

        }

        size = (long)xsize * (long)ysize * (long)zsize * 1 * sizeof(float);
        datasize[3] = 1;
        

        //If a single buffer exceeds 2gb, then split into multiple pieces
        //if (size > 2000000000L)
        if (size > 256000000L)
        {
            pieces = (int)Math.Ceiling((float)size / 256000000L);

            //Round up to nearest power of 2
            pieces--;
            pieces |= pieces >> 1;
            pieces |= pieces >> 2;
            pieces |= pieces >> 4;
            pieces |= pieces >> 8;
            pieces |= pieces >> 16;
            pieces++;
            dz = (int)Math.Ceiling((float)zwidth / pieces / 8) * 8;
        }

        if (fields.Contains("p"))
        {
            components = 1;
            field = "p";

            tableName = DataInfo.getTableName(dataset_enum, field);
    
           
            if (time_step == 1 && x_step == 1 && y_step == 1 && z_step == 1 && filter_width == 1)
            {
                for (int t = tlow; t <= thigh; t++)
                {
                    DataInfo.verifyTimeInRange(dataset_enum, (float)t * database.Dt * database.TimeInc);
                    for (int p = 0; p < pieces; p++)
                    {
                        int chunklen = (p + 1) * dz <= zsize ? dz : zsize - p * dz;
                        if (chunklen == 0) break;
                        cutout = database.GetRawData(dataset_enum, tableName, t * database.Dt * database.TimeInc, components, xlow, ylow, zlow + p * dz, xsize, ysize, chunklen);
                    }
                }
            }
            else
            {
                for (int t = tlow; t <= thigh; t += t_step)
                {
                    cutout = database.GetFilteredData(dataset_enum, tableName, t * database.Dt * database.TimeInc, components, xlow, ylow, zlow, xwidth, ywidth, zwidth,
                    x_step, y_step, z_step, filter_width);
                }
            }

        }

        if (fields.Contains("d"))
        {
            components = 1;
            field = "d";

            tableName = DataInfo.getTableName(dataset_enum, field);
        
          
            if (time_step == 1 && x_step == 1 && y_step == 1 && z_step == 1 && filter_width == 1)
            {
                for (int t = tlow; t <= thigh; t++)
                {
                    DataInfo.verifyTimeInRange(dataset_enum, (float)t * database.Dt * database.TimeInc);
                    for (int p = 0; p < pieces; p++)
                    {
                        int chunklen = (p + 1) * dz <= zsize ? dz : zsize - p * dz;
                        if (chunklen == 0) break;
                        cutout = database.GetRawData(dataset_enum, tableName, t * database.Dt * database.TimeInc, components, xlow, ylow, zlow + p * dz, xsize, ysize, chunklen);
                    }
                }
            }
            else
            {
                for (int t = tlow; t <= thigh; t += t_step)
                {
                    cutout = database.GetFilteredData(dataset_enum, tableName, t * database.Dt * database.TimeInc, components, xlow, ylow, zlow, xwidth, ywidth, zwidth,
                    x_step, y_step, z_step, filter_width);
                }
            }

       
            
            SqlDataRecord record = new SqlDataRecord(new SqlMetaData("data", SqlDbType.VarBinary, -1));
            String connectionString = "Context connection=true;";        
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            // Populate the record
            record.SetBytes(0, 0, cutout, 0, cutout.Length);
            // Send the record to the client.
            SqlContext.Pipe.Send(record);
        }


        }
      
        private static void CheckBoundaries(DataInfo.DataSets dataset, int tlow, int thigh, int xlow, int xhigh, int ylow, int yhigh, int zlow, int zhigh)
        {
            switch (dataset)
            {
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.mhd1024:
                    if (!(tlow >= 0 && thigh < 1025) ||
                       !(xlow >= 0 && xhigh < 1024) ||
                       !(ylow >= 0 && yhigh < 1024) ||
                       !(zlow >= 0 && zhigh < 1024))
                    { throw new Exception("The requested region is out of bounds"); }
                    break;
                case DataInfo.DataSets.mixing:
                    if (!(tlow >= 0 && thigh < 1015) ||
                       !(xlow >= 0 && xhigh < 1024) ||
                       !(ylow >= 0 && yhigh < 1024) ||
                       !(zlow >= 0 && zhigh < 1024))
                    { throw new Exception("The requested region is out of bounds"); }
                    break;
                case DataInfo.DataSets.channel:
                    if (!(tlow >= 0 && thigh < 1997) ||
                       !(xlow >= 0 && xhigh < 2048) ||
                       !(ylow >= 0 && yhigh < 512) ||
                       !(zlow >= 0 && zhigh < 1536))
                    { throw new Exception("The requested region is out of bounds"); }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
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
        //Calculates the how large the file will be.
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
}