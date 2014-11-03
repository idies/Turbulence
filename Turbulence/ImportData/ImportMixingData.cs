using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Turbulence.TurbLib.DataTypes;

namespace ImportData
{
    class ImportMixingData
    {
        int time_start = 0;    // First time step to dataread
        int time_end = 99;      // Last time step to dataread [loaded up to and including 600]
        string data_dir = @"\\dss004\tdb_livescu\";
        //string data_dir = @"\\dss005.\tdb_livescu2\";
        //string data_dir = @"C:\data\ssd1\";
        string user = "kalin";
        string db_prefix = "mixingdb";
        string staging_db = "stagingdb";
        long headerSize = 192;
        int[] resolution = { 1024, 1024, 1024 };
        long[] range_start = { 0,         134217728, 268435456, 402653184, 536870912, 671088640, 805306368, 939524096 };
        long[] range_end =   { 134217216, 268434944, 402652672, 536870400, 671088128, 805305856, 939523584, 1073741312 };
        int timeinc = 1;
        int timeoff = 0;
        //int components = 3;
        int components = 1;
        //string tablePrefix = "vel_";
        string tablePrefix = "d_";
        string dataset = "density";
        //string tablePrefix = "p_";
        int atomSize = 8;
        string serverName = "dsp084";
        //string serverName = "gwwn1";
        bool isDataLittleEndian = true;
        int rounds = 1; //2;
        int numProcs = 24; //12;
        bool use_staging_db = false;
        bool use_staging_tables = false;

        static void Main(string[] args)
        {
            ImportMixingData import = new ImportMixingData();
            if (args.Length == 4)
            {
                import.data_dir = args[0];
                import.time_start = int.Parse(args[1]);
                import.time_end = int.Parse(args[2]);
                import.components = int.Parse(args[3]);
            }
            DateTime start = DateTime.Now;
            import.ParallelIngestSequentialRead();
            Console.WriteLine("Total running time {0} s.", (DateTime.Now - start).TotalSeconds);

            //Console.WriteLine("Press any key to continue!");
            //Console.ReadLine();
        }

        void ParallelIngestSequentialRead()
        {
            Console.WriteLine("Reading timestep {0} to {1} from {2}", time_start, time_end, data_dir);
            long inc = new Morton3D(0, 0, atomSize).Key;  // Address of 8 == "size" of a 8^3
            if (tablePrefix == "vel_")
            {
                headerSize += (long)resolution[2] * resolution[1] * resolution[0] * sizeof(double);
            }
            if (tablePrefix == "p_")
            {
                headerSize += 4L * resolution[2] * resolution[1] * resolution[0] * sizeof(double);
            }
            FileCacheMixing cache = new FileCacheMixing(data_dir, resolution, headerSize, components);

            int first_range = 4;
            int last_range = range_start.Length - 1;
            //int last_range = 7;

            Database[] DBs = new Database[numProcs];
            for (int i = 0; i < numProcs; i++)
            {
                string cString;
                if (use_staging_db)
                    cString = String.Format("server={0};database={1};Integrated Security=true;User ID={2};Connection Timeout=0", serverName, staging_db, user);
                else
                    cString = String.Format("server={0};Integrated Security=true;User ID={1};Connection Timeout=0", serverName, user);
                DBs[i] = new Database(cString);
                long fb = 0, lb = 0;
                DBs[i].GetPartLimits(1, 1, ref fb, ref lb);
            }

            for (int timestep = time_start; timestep <= time_end; timestep += timeinc)
            {
                DateTime timestepStart = DateTime.Now;
                for (int range = first_range; range <= last_range; range++)
                {
                    int sliceNum = range + 1;

                    string dbname = String.Format("{0}{1:00}", db_prefix, sliceNum);

                    Console.WriteLine("Reading from blob #{0} to {1} for time {2}", range_start[range], range_end[range], timestep);
                    DateTime startTime = DateTime.Now;
                    cache.readFilesIntoByteArray(timestep, range_start[range], range_end[range], atomSize);
                    TimeSpan span = DateTime.Now - startTime;
                    Console.WriteLine("Reading the file took: {0}s", span.TotalSeconds);

                    for (int round = 0; round < rounds; round++)
                    {
                        //TODO: SHOULD BE PARALLEL. USING LOOP FOR DEBUGGING ONLY.
                        Parallel.For(0, numProcs, proc =>
                        {
                            //for (int proc = 0; proc < numProcs; proc++)
                            //{
                            int partitionNum = round * numProcs + proc + 1;
                            //Database db = new Database(cString);
                            long firstbox = 0, lastbox = 0;
                            //db.GetPartLimits(sliceNum, partitionNum, ref firstbox, ref lastbox);
                            DBs[proc].GetPartLimits(sliceNum, partitionNum, ref firstbox, ref lastbox);
                            string tableName;
                            if (use_staging_db)
                                tableName = String.Format("{0}{1:00}", tablePrefix, partitionNum);
                            else if (use_staging_tables)
                                tableName = String.Format("{0}..{1}{2:00}", dbname, tablePrefix, partitionNum);
                            else
                                tableName = String.Format("{0}..{1}", dbname, dataset);

                            //DBs[proc].EnableMinimalLogging();
                            //DBs[proc].DisableLockEscalation();

                            DateTime startRunTime = DateTime.Now;
                            TurbDataReader.ExportDataFormat df = new TurbDataReader.ExportDataFormat(
                                firstbox,
                                lastbox,
                                inc,
                                atomSize,
                                0,
                                isDataLittleEndian);
                            TurbDataReader dataReader = (TurbDataReader)TurbDataReader.GetReader(timestep, timeoff, resolution, components, df, cache);

                            //db.BulkCopy(tableName, dataReader);
                            DBs[proc].BulkCopy(tableName, dataReader);
                            Console.WriteLine("Data ingest for round {2}, proc {1} took: {0}s", (DateTime.Now - startRunTime).TotalSeconds, proc, round);
                            //db.Close();
                            //}
                        });
                    }
                }
                Console.WriteLine("Data ingest for timestep {1} took: {0}s", (DateTime.Now - timestepStart).TotalSeconds, timestep);
            }

            for (int i = 0; i < numProcs; i++)
            {
                DBs[i].Close();
            }
        }
    }
}
