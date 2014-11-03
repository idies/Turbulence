using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Turbulence.TurbLib.DataTypes;
using Turbulence.TurbLib;

namespace ImportData
{
    class DatabaseSetup
    {
        static string user = "kalin";
        static string[] serverName = new string[] { "dsp048" };
        static string dbname = "turblib";
        static long[] range_start = { 0 };
        static long[] range_end   = { 1073741312 };
        static int numDBs = 8;
        static int numPartitions = 24;
        static int atomSize = 8;

        static void Main(string[] args)
        {
            Console.WriteLine("Verify all input parameters! Press any key to continue with the database setup for {0}!", dbname);
            Console.ReadLine();

            for (int i = 0; i < serverName.Length; i++)
            {
                string cString = String.Format("server={0};database={1};Integrated Security=true;User ID={2}", serverName[i], dbname, user);
                SqlConnection sqlcon = new SqlConnection(cString);
                sqlcon.Open();
                DropCreatePartLimitsTable(sqlcon);
                GeneratePartLimits2(sqlcon);
                sqlcon.Close();
            }
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

                ServerBoundaries serverBoundaries = new ServerBoundaries(start.X, end.X + atomSize - 1, start.Y, end.Y + atomSize - 1, start.Z, end.Z + atomSize - 1);
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
