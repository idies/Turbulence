using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.IO;

namespace ImportData
{
    class ImportData
    {
        static string data_dir = "";
        static string prefix = "V";
        static string tableName = "data";
        static SqlConnection connection;
        static int resolution = 1024;
        static int cube_resolution = 64;
        static int slices = 128;
        static int components = 3;
        static int edge = 4;

        static void Main(string[] args)
        {
            string server = "(local)";
            string dbname = "mhddb";
            string username = "kalin";
            int time_start = 1;
            int time_end = 1;
            int time_inc = 1;
            int timeoff = 0;
            long firstBox = 0;
            long lastBox = 1073479680;

            if (args.Length == 11)
            {
                data_dir = args[0];
                prefix = args[1];
                tableName = args[2];
                components = int.Parse(args[3]);
                time_start = int.Parse(args[4]);
                time_end = int.Parse(args[5]);
                time_inc = int.Parse(args[6]);
                timeoff = int.Parse(args[7]);
                firstBox = long.Parse(args[8]);
                lastBox = long.Parse(args[9]);
                server = args[10];
            }
            else // (args.Length == 0)
            {
                Console.WriteLine("Usage: ImportData <path> <prefix> <table name> <components> <time_start> <time_end> <timeinc> <timeoff> <firstbox> <lastbox> <server>");
                Console.WriteLine("Timesteps [time_start,time_end] (inclusive) are imported, with an increment of <timeinc>, loaded as time+timeoff.");
                Console.WriteLine("Blocks in the range [firstbox, lastbox] are imported, where firstbox and lastbox are specified by their Morton keys.");
                Environment.Exit(1);
            }

            string connectionString = GetConnectionString(server, dbname, username, sqlPassword);
            // Open a connection to the database.
            OpenConnection(connectionString);

            DateTime startTime = DateTime.Now;

            for (int timeStep = time_start; timeStep <= time_end; timeStep += time_inc)
            {
                BulkCopy(timeStep, new Morton3D(firstBox), new Morton3D(lastBox));
            }

            DateTime endTime = DateTime.Now;
            TimeSpan executionTime = endTime - startTime;
            Console.Write("Execution Time: ");
            Console.WriteLine(executionTime);

            CloseConnection();

            Console.WriteLine("Press Enter to finish.");
            Console.ReadLine();
        }

        private static void BulkCopy(int timeStep, Morton3D firstBox, Morton3D lastBox)
        {

            // Create a DataReader
            DataReader newData = (DataReader)DataReader.GetReader(data_dir, prefix, timeStep, slices, resolution, cube_resolution, edge, components, firstBox, lastBox);

            // Create the SqlBulkCopy object. 
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
            {
                bulkCopy.DestinationTableName = tableName;
                bulkCopy.BulkCopyTimeout = 0;
                bulkCopy.BatchSize = 16;
                bulkCopy.SqlRowsCopied += new SqlRowsCopiedEventHandler(OnSqlRowsCopied);
                bulkCopy.NotifyAfter = 128;

                try
                {
                    // Write from the source to the destination.
                    bulkCopy.WriteToServer(newData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void OnSqlRowsCopied(object sender, SqlRowsCopiedEventArgs e)
        {
            Console.WriteLine("Copied {0} so far...", e.RowsCopied);
        }

        private static string GetConnectionString(string server, string dbname, string username, string sqlPassword)
        // To avoid storing the connection string in your code, 
        // you can retrieve it from a configuration file. 
        {
            return String.Format("Server={0}; Database={1}; User ID={2}; Integrated security=true;", server, dbname, username);
        }

        private static void OpenConnection(string connectionString)
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
        }

        private static void CloseConnection()
        {
            connection.Close();
        }
    }
}
