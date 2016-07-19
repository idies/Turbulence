using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Turbulence.TurbLib.DataTypes;
using Turbulence.TurbLib;
using HDF5DotNet;

namespace ImportData
{
    class DatabaseSetup
    {
        static string user = "kalin";
        static string[] serverName = new string[] { "dsp085", "dsp086", "dsp087", "dsp088" };
        static string dbname = "turbdev";
        static long[] range_start = { 0,         536870912, 1073741824, 1610612736, 4294967296, 5368709120 };
        static long[] range_end   = { 268434944, 805305856, 1342176768, 1879047680, 4563402240, 5637144064 };
        static int numDBs = 12;
        static int numPartitions = 24;
        static int atomSize = 8;
        static int Ny = 512;

        static void Main(string[] args)
        {
            Console.WriteLine("Verify all input parameters! Press any key to continue with the database setup for channel flow DB!");
            Console.ReadLine();

            for (int i = 0; i < serverName.Length; i++)
            {
                string cString = String.Format("server={0};database={1};Integrated Security=true;User ID={2}", serverName[i], dbname, user);
                SqlConnection sqlcon = new SqlConnection(cString);
                sqlcon.Open();
                //DropCreateWeightsTables(sqlcon);
                //GenerateWeights(sqlcon);
                //GenerateWeightsY(sqlcon);
                //GetDx();
                //GetDz();
                //DropCreateGridPointsY(sqlcon);
                //GenerateGridPointsY(sqlcon);
                //DropCreatePartLimitsTable(sqlcon);
                //GeneratePartLimits2(sqlcon);
                sqlcon.Close();
            }
        }

        static void GenerateWeights(SqlConnection sqlcon)
        {
            double dx = 8 * Math.PI / 2048;
            double dz = 3 * Math.PI / 1536;

            int[] nOrder = new int[] { 4, 6, 8 };
            for (int n = 0; n < nOrder.Length; n++)
            {
                double[] grid_values_x = new double[nOrder[n]];
                double[] grid_values_z = new double[nOrder[n]];
                for (int i = 0; i < nOrder[n]; i++)
                {
                    grid_values_x[i] = i * dx;
                    grid_values_z[i] = i * dz;
                }

                double[] weights_x = new double[nOrder[n]];
                double[] weights_z = new double[nOrder[n]];
                ComputeWeights(grid_values_x, weights_x, nOrder[n]);
                ComputeWeights(grid_values_z, weights_z, nOrder[n]);
                string insert_x_sql = String.Format("INSERT barycentric_weights_x_{0}(", nOrder[n]);
                string insert_z_sql = String.Format("INSERT barycentric_weights_z_{0}(", nOrder[n]);
                string column_names = "";
                string values = " VALUES(";
                for (int i = 0; i < nOrder[n]; i++)
                {
                    if (i < nOrder[n] - 1)
                    {
                        column_names += String.Format("w{0}, ", i);
                        values += String.Format("@w{0}, ", i);
                    }
                    else
                    {
                        column_names += String.Format("w{0})", i);
                        values += String.Format("@w{0})\n", i);
                    }
                }
                insert_x_sql += column_names + values;
                SqlCommand cmd = new SqlCommand(insert_x_sql, sqlcon);
                for (int i = 0; i < nOrder[n]; i++)
                {
                    cmd.Parameters.AddWithValue(String.Format("@w{0}", i), weights_x[i]);
                }
                cmd.ExecuteNonQuery();
                insert_z_sql += column_names + values;
                cmd = new SqlCommand(insert_z_sql, sqlcon);
                for (int i = 0; i < nOrder[n]; i++)
                {
                    cmd.Parameters.AddWithValue(String.Format("@w{0}", i), weights_z[i]);
                }
                cmd.ExecuteNonQuery();
            }
        }

        static void GenerateWeightsY(SqlConnection sqlcon)
        {
            GridPoints grid_points_y = new GridPoints(Ny);
            grid_points_y.GetGridPointsFromDB(sqlcon);

            int[] nOrder = new int[] { 4, 6, 8 };
            for (int n = 0; n < nOrder.Length; n++)
            {
                for (int i = 0; i < Ny - 2; i++)
                {
                    int cell_index = i;
                    int offset_index;
                    if (cell_index <= Ny / 2 - 1)
                    {
                        offset_index = Math.Max((int)Math.Ceiling((double)nOrder[n] / 2) - cell_index - 1, 0);
                    }
                    else
                    {
                        offset_index = Math.Min(Ny - 1 - cell_index - (int)Math.Floor((double)nOrder[n] / 2), 0);
                    }
                    int stencil_start_index = cell_index - (int)Math.Ceiling((double)nOrder[n] / 2) + 1 + offset_index;

                    double[] grid_values_y = new double[nOrder[n]];
                    double[] weights_y = new double[nOrder[n]];
                    for (int j = 0; j < nOrder[n]; j++)
                    {
                        grid_values_y[j] = grid_points_y.GetGridValue(stencil_start_index + j);
                    }
                    ComputeWeights(grid_values_y, weights_y, nOrder[n]);

                    string insert_y_sql = String.Format("INSERT barycentric_weights_y_{0}([cell_index],[stencil_start_index],[offset_index], ", nOrder[n]);
                    string column_names = "";
                    string values = String.Format(" VALUES(@cell_index, @stencil_start_index, @offset_index, ", cell_index, stencil_start_index, offset_index);
                    for (int j = 0; j < nOrder[n]; j++)
                    {
                        if (j < nOrder[n] - 1)
                        {
                            column_names += String.Format("w{0}, ", j);
                            values += String.Format("@w{0}, ", j);
                        }
                        else
                        {
                            column_names += String.Format("w{0})", j);
                            values += String.Format("@w{0})\n", j);
                        }
                    }
                    insert_y_sql += column_names + values;
                    SqlCommand cmd = new SqlCommand(insert_y_sql, sqlcon);
                    cmd.Parameters.AddWithValue("@cell_index", cell_index);
                    cmd.Parameters.AddWithValue("@stencil_start_index", stencil_start_index);
                    cmd.Parameters.AddWithValue("@offset_index", offset_index);
                    for (int j = 0; j < nOrder[n]; j++)
                    {
                        cmd.Parameters.AddWithValue(String.Format("@w{0}", j), weights_y[j]);
                    }
                    cmd.ExecuteNonQuery();
                }
            }
        }

        static void DropCreateWeightsTables(SqlConnection sqlcon)
        {
            string[] coordinate = new string[] { "x", "z" };
            int[] nOrder = new int[] { 4, 6, 8 };
            for (int c = 0; c < coordinate.Length; c++)
            {
                for (int i = 0; i < nOrder.Length; i++)
                {
                    string sql = String.Format("IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[barycentric_weights_{0}_{1}]') AND type in (N'U'))\n" +
                        "DROP TABLE [dbo].[barycentric_weights_{0}_{1}]\n" +
                        "CREATE TABLE [dbo].[barycentric_weights_{0}_{1}](", coordinate[c], nOrder[i]);
                    for (int j = 0; j < nOrder[i]; j++)
                    {
                        if (j < nOrder[i] - 1)
                        {
                            sql += String.Format("[w{0}] float,", j);
                        }
                        else
                        {
                            sql += String.Format("[w{0}] float )\n", j);
                        }
                    }
                    SqlCommand cmd = new SqlCommand(sql, sqlcon);
                    cmd.ExecuteNonQuery();
                }
            }
            // The schema for the table containing the weights for y is different.

            for (int i = 0; i < nOrder.Length; i++)
            {
                string sql = String.Format("IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[barycentric_weights_y_{0}]') AND type in (N'U'))\n" +
                    "DROP TABLE [dbo].[barycentric_weights_y_{0}]\n" +
                    "CREATE TABLE [dbo].[barycentric_weights_y_{0}]( [cell_index] int, [stencil_start_index] int, [offset_index] int,", nOrder[i]);
                for (int j = 0; j < nOrder[i]; j++)
                {
                    if (j < nOrder[i] - 1)
                    {
                        sql += String.Format("[w{0}] float,", j);
                    }
                    else
                    {
                        sql += String.Format("[w{0}] float )\n", j);
                    }
                }
                SqlCommand cmd = new SqlCommand(sql, sqlcon);
                cmd.ExecuteNonQuery();
            }
        }

        public static void ComputeWeights(double[] grid_values, double[] weights, int nOrder)
        {
            for (int i = 0; i < nOrder; i++)
            {
                weights[i] = 1.0;
                for (int j = 0; j < nOrder; j++)
                {
                    if (i != j)
                    {
                        weights[i] /= grid_values[i] - grid_values[j];
                    }
                }
            }
        }

        static void GetDx()
        {
            double[] grid_data = null;
            long num_grid_points = 0;

            GetGridPoints(ref grid_data, ref num_grid_points, "x");
            double dx = grid_data[1] - grid_data[0];
            Console.WriteLine("dx = {0}", dx);
        }

        static void GetDz()
        {
            double[] grid_data = null;
            long num_grid_points = 0;

            GetGridPoints(ref grid_data, ref num_grid_points, "z");
            double dz = grid_data[1] - grid_data[0];
            Console.WriteLine("dz = {0}", dz);
        }

        static void DropCreateGridPointsY(SqlConnection sqlcon)
        {
            string sql = "IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[grid_points_y]') AND type in (N'U'))\n";
            sql += "DROP TABLE [dbo].[grid_points_y]\n";
            sql += "CREATE TABLE grid_points_y([cell_index] [int] NOT NULL, [value] [float] NOT NULL,\n";
            sql += "CONSTRAINT [grid_points_y_pk] PRIMARY KEY CLUSTERED ( [cell_index] ASC )\n";
            sql += "WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON))\n";
            SqlCommand cmd = new SqlCommand(sql, sqlcon);
            cmd.ExecuteNonQuery();
        }

        static void GenerateGridPointsY(SqlConnection sqlcon)
        {
            double[] grid_data = null;
            long num_grid_points = 0;

            GetGridPoints(ref grid_data, ref num_grid_points, "y");

            for (long i = 0; i < num_grid_points; i++)
            {
                string sql = String.Format("INSERT grid_points_y(cell_index, value) VALUES({0}, {1})", i, grid_data[i]);
                SqlCommand cmd = new SqlCommand(sql, sqlcon);
                cmd.ExecuteNonQuery();
            }
        }

        static void GetGridPoints(ref double[] grid_data, ref long num_grid_points, string dataset)
        {
            H5FileId file;
            H5DataSetId dataset_id;
            H5DataSpaceId dataspace;
            H5DataSpaceId memspace;
            H5DataTypeId datatype;
            H5T.H5TClass dataclass;
            H5T.Order order;

            string filename = @"\\dss005\tdbchannel\grid.h5";
            file = H5F.open(filename, H5F.OpenMode.ACC_RDONLY);
            dataset_id = H5D.open(file, dataset);
            dataspace = H5D.getSpace(dataset_id);

            datatype = H5D.getType(dataset_id);     /* datatype handle */
            dataclass = H5T.getClass(datatype);
            if (dataclass == H5T.H5TClass.FLOAT)
            {
                Console.WriteLine("Data set has FLOAT type.");
            }

            order = H5T.get_order(datatype);
            if (order == H5T.Order.LE)
            {
                Console.WriteLine("Little endian order.");
            }

            int size = H5T.getSize(datatype);
            Console.WriteLine("Data size is {0}.", size);

            int rank = H5S.getSimpleExtentNDims(dataspace);
            long[] dims_out = H5S.getSimpleExtentDims(dataspace);
            num_grid_points = dims_out[0];
            Console.WriteLine("Rank {0}, dimensions {1}.", rank, num_grid_points);

            grid_data = new double[num_grid_points];

            memspace = H5S.create_simple(rank, dims_out);

            H5S.selectHyperslab(dataspace, H5S.SelectOperator.SET, new long[] { 0 }, dims_out);
            H5D.read<double>(dataset_id, datatype, memspace, dataspace, new H5PropertyListId(H5P.Template.DEFAULT), new H5Array<double>(grid_data));
        }

        static void DropCreatePartLimitsTable(SqlConnection sqlcon)
        {
            string sql = "IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PartLimits08]') AND type in (N'U'))\n";
            sql += "DROP TABLE [dbo].[PartLimits08]\n";
            sql += "create table PartLimits08(sliceNum int, partitionNum int, minLim bigint, maxLim bigint, ordinal int identity(1,1))";
            SqlCommand cmd = new SqlCommand(sql, sqlcon);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// This method generates partition limits by simply dividing a given range of zindexes into
        /// the given number of partitions and thus should be suitable for any partition number, even those
        /// that are not a power of 2.
        /// </summary>
        /// <param name="sqlcon"></param>
        static void GeneratePartLimits2(SqlConnection sqlcon)
        {
            int numDBsPerRange = numDBs / range_start.Length;

            for (int range = 0; range < range_start.Length; range++)
            {
                long rangeSizePerDB = (long)Math.Round((range_end[range] + atomSize * atomSize * atomSize - range_start[range]) /
                    (double)(numDBsPerRange));
                long rangeSizePerPartition = (long)Math.Round(rangeSizePerDB / (double)(numPartitions));
                for (int i = 0; i < numDBsPerRange; i++)
                {
                    int sliceNum = range * numDBsPerRange + i + 1;
                    for (int partitionNum = 0; partitionNum < numPartitions; partitionNum++)
                    {
                        long minLim = range_start[range] + i * rangeSizePerDB + partitionNum * rangeSizePerPartition;
                        long maxLim;
                        if (partitionNum == numPartitions - 1)
                            if (i == numDBsPerRange - 1)
                                maxLim = range_end[range] + atomSize * atomSize * atomSize - 1;
                            else
                                maxLim = range_start[range] + (i + 1) * rangeSizePerDB - 1;
                        else
                            maxLim = minLim + rangeSizePerPartition - 1;
                        string sql = String.Format("INSERT PartLimits08(sliceNum, partitionNum, minLim, maxLim) VALUES({0:00}, {1}, {2}, {3})", sliceNum, partitionNum + 1, minLim, maxLim);
                        SqlCommand cmd = new SqlCommand(sql, sqlcon);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// This method generates partition limits that are based on a number of partitions that evenly
        /// divides the data volume (i.e. the number of partitions is a power of 2).
        /// </summary>
        /// <param name="sqlcon"></param>
        static void GeneratePartLimits(SqlConnection sqlcon)
        {
            int numDBsPerRange = numDBs / range_start.Length;

            for (int range = 0; range < range_start.Length; range++)
            {
                Morton3D start = new Morton3D(range_start[range]);
                Morton3D end = new Morton3D(range_end[range]);
                /* Note we put 0, 0 for the time boundaries.  Not sure if this is right, but it helps it compile for now */
                ServerBoundaries serverBoundaries = new ServerBoundaries(start.X, end.X + atomSize - 1, start.Y, end.Y + atomSize - 1, start.Z, end.Z + atomSize - 1, 0, 0);
                ServerBoundaries[] VirtualServerBoundaries = serverBoundaries.getVirtualServerBoundaries(numDBsPerRange);
                for (int i = 0; i < numDBsPerRange; i++)
                {
                    int sliceNum = range * numDBsPerRange + i + 1;
                    ServerBoundaries[] partitionBoundaries = VirtualServerBoundaries[i].getVirtualServerBoundaries(numPartitions);
                    for (int partitionNum = 0; partitionNum < numPartitions; partitionNum++)
                    {
                        long minLim = new Morton3D(partitionBoundaries[partitionNum].startz, partitionBoundaries[partitionNum].starty, partitionBoundaries[partitionNum].startx);
                        long maxLim = new Morton3D(partitionBoundaries[partitionNum].endz, partitionBoundaries[partitionNum].endy, partitionBoundaries[partitionNum].endx);
                        string sql = String.Format("INSERT PartLimits08(sliceNum, partitionNum, minLim, maxLim) VALUES({0:00}, {1}, {2}, {3})", sliceNum, partitionNum + 1, minLim, maxLim);
                        SqlCommand cmd = new SqlCommand(sql, sqlcon);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
