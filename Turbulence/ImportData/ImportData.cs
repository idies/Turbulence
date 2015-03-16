using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Turbulence.TurbLib.DataTypes;

namespace ImportData
{
    public class ImportData
    {
        int time_start = 0;  // First time step to dataread
        int time_end = 0;    // Last time step to dataread
        string data_dir = @"H:\channel\";
        //string[] suffix = { "t04.s001.000" };
        int headerSize = 192;
        string[] suffix = { "u", "v", "w" };
        string user = "kalin";
        string dbname = "mixing";
        //string dbname = "turbload";
        string table = "velocity";
        int[] resolution = { 1024, 1024, 1024 };
        long firstbox = 0;
        long lastbox = new Morton3D(1020, 1020, 1020).Key;
        int timeinc = 1;
        int timeoff = 0;
        int components = -1;
        int atomSize = 4;
        int edge = 0;
        string serverName = "gw01";
        bool isDataLittleEndian = true;
        int numProcs = 2;

        ImportData(long firstbox, long lastbox, int numProcs, int timeoff)
        {
            this.time_start = 0;  // First time step to dataread
            this.time_end = 0;    // Last time step to dataread
            this.data_dir = @"\\dss004\tdb_livescu\";
            //string[] suffix = { "t04.s001.000" };
            this.headerSize = 192;
            this.suffix = new string[] { "u", "v", "w" };
            this.user = "kalin";
            this.dbname = "mixingdb01";
            //string dbname = "turbload";
            this.table = "vel";
            this.resolution = new int[] { 1023, 1023, 1023 };
            this.timeinc = 1;
            this.timeoff = timeoff;
            this.atomSize = 4;
            this.edge = 0;
            this.serverName = "dsp048";
            this.isDataLittleEndian = true;
            this.firstbox = firstbox;
            this.lastbox = lastbox;
            this.components = suffix.Length;
            this.numProcs = numProcs;
        }

        /// <summary>
        /// Crude program for loading data into the database.
        /// Once loaded into the database data can be copied between nodes.
        /// </summary>
        static void Main(string[] args)
        {
            long firstbox = -1, lastbox = -1;
            int numProcs = -1, timeoff = -1;

            if (args.Length == 4)
            {
                //data_dir = args[0];
                //table = args[1];
                //time_start = int.Parse(args[2]);
                //time_end = int.Parse(args[3]);
                //timeinc = int.Parse(args[4]);
                //timeoff = int.Parse(args[5]);
                firstbox = long.Parse(args[0]);
                lastbox = long.Parse(args[1]);
                numProcs = int.Parse(args[2]);
                timeoff = int.Parse(args[3]);
                //serverName = args[8];
            }
            else // (args.Length == 0)
            {
                Console.WriteLine("Usage: ImportData <firstbox> <lastbox> <number of processes> <timeoff>");
                //Console.WriteLine("Usage: ImportData <path> <table name> <time_start> <time_end> <timeinc> <timeoff> <firstbox> <lastbox> <server>");
                //Console.WriteLine("Timesteps [time_start,time_end] (inclusive) are imported, with an increment of <timeinc>, loaded as time+timeoff.");
                Console.WriteLine("Blocks in the range [firstbox, lastbox] are imported.");
                Environment.Exit(1);
            }

            ImportData importData = new ImportData(firstbox, lastbox, numProcs, timeoff);
            //importData.SequentialImport();
            //importData.ParallelImport();
            importData.ParallelImportUsingSharedMemory();
        }

        void SequentialImport()
        {
            Console.WriteLine("Reading timestep {0} to {1} from {2}", time_start, time_end, data_dir);

            string cString = String.Format("server={0};database={1};Integrated Security=true;User ID={2}", serverName, dbname, user);

            long inc = new Morton3D(0, 0, atomSize).Key;  // Address of 4 == "size" of a 4^3

            Morton3D start = new Morton3D(firstbox);
            Morton3D end = new Morton3D(lastbox);
            ServerBoundaries serverBoundaries = new ServerBoundaries(start.X, end.X + atomSize - 1, start.Y, end.Y + atomSize - 1, start.Z, end.Z + atomSize - 1);

            long dataSize = (long)(serverBoundaries.endx - serverBoundaries.startx + 1) *
                    (long)(serverBoundaries.endy - serverBoundaries.starty + 1) *
                    (long)(serverBoundaries.endz - serverBoundaries.startz + 1) *
                    sizeof(double);

            int partitions = 1;
            while (dataSize > int.MaxValue)
            {
                partitions *= 2;
                dataSize /= 2;
            }

            ServerBoundaries[] PartitionBoundaries = serverBoundaries.getVirtualServerBoundaries(partitions);

            long[] firstBoxes = new long[partitions];
            long[] lastBoxes = new long[partitions];
            for (int p = 0; p < partitions; p++)
            {
                firstBoxes[p] = new Morton3D(PartitionBoundaries[p].startz, PartitionBoundaries[p].starty, PartitionBoundaries[p].startx);
                lastBoxes[p] = new Morton3D(PartitionBoundaries[p].endz - atomSize + 1, PartitionBoundaries[p].endy - atomSize + 1, PartitionBoundaries[p].endx - atomSize + 1);
            }

            FileCache cache = new FileCache(data_dir, suffix, resolution, headerSize);
            
            Database db = new Database(cString);
            db.EnableMinimalLoggin();

            DateTime startRunTime = DateTime.Now;
            for (int timestep = time_start; timestep <= time_end; timestep += timeinc)
            {
                for (int p = 0; p < partitions; p++)
                {
                    // There is no data for z >= 512
                    if (new Morton3D(firstBoxes[p]).Z >= 512)
                        continue;

                    Console.WriteLine("Reading from blob #{0} to {1} for time {2}", new Morton3D(firstBoxes[p]).ToPrettyString(), new Morton3D(lastBoxes[p]).ToPrettyString(), timestep);
                    DateTime startTime = DateTime.Now;
                    //cache.readFiles(timestep, firstBoxes[p], lastBoxes[p], atomSize);
                    cache.readFilesIntoByteArray(timestep, firstBoxes[p], lastBoxes[p], atomSize);
                    TimeSpan span = DateTime.Now - startTime;
                    Console.WriteLine("Creating the cache took: {0}s", span.TotalSeconds);

                    TurbDataReader.ExportDataFormat df = new TurbDataReader.ExportDataFormat(
                        firstBoxes[p],
                        lastBoxes[p],
                        inc, atomSize, edge,
                        isDataLittleEndian);
                    TurbDataReader turbDataReader = (TurbDataReader)TurbDataReader.GetReader(timestep, timeoff, resolution, components, df, cache);

                    startTime = DateTime.Now;
                    db.BulkCopy(table, turbDataReader);
                    span = DateTime.Now - startTime;
                    Console.WriteLine("Date ingest took: {0}s", span.TotalSeconds);


                    #region Code to be moved into a test suite...
                    /*
            while (turbDataReader.Read())
            {
                int timestep = turbDataReader.GetInt32(0);
                Morton3D b = new Morton3D(turbDataReader.GetInt64(1));
                int[] v = b.GetValues();

                int z = v[0]; int y = v[1]; int x = v[2];
                int length = (int)turbDataReader.GetBytes(2, 0, null, 0, 0);
                Console.WriteLine("Output: 0: {0}, 1: {1}, 2: {2}", turbDataReader.GetInt32(0), turbDataReader.GetInt64(1), length);
                turbDataReader.GetBytes(2, 0, data, 0, length);

                unsafe
                {
                    fixed (byte* bdata = data)
                    {
                        int* idata = (int*)bdata;
                        int zero = 0;
                        for (int l = 0; l < 72 * 72 * 72 * 4; l++)
                        {
                            
                            if (*(idata + l) == 0)
                            {
                                zero++;
                            }
                        }

                        Console.WriteLine("Found 0s: {0}", zero);
                        float* fdata = (float *)bdata;
                        float[] result = turbDataReader.ReadDataPoint(1020, 1020, 1020);
                        
                        Console.WriteLine("{0},{1},{2},{3}", result[0], result[1], result[2], result[3]);
                        Console.WriteLine("{0},{1},{2},{3}", *fdata, *(fdata + 1), *(fdata + 2), *(fdata + 3));

                        fdata += (4 * 72 * 72 + 4 * 72 + 4) * 4;
                        result = turbDataReader.ReadDataPoint(0, 0, 0);
                        Console.WriteLine("{0},{1},{2},{3}", result[0], result[1], result[2], result[3]);
                        Console.WriteLine("{0},{1},{2},{3}", *fdata, *(fdata + 1), *(fdata + 2), *(fdata + 3));

                    }
                }
            }
             * */
                    #endregion


                }
            }
            TimeSpan runningTime = DateTime.Now - startRunTime;
            Console.WriteLine("Total running time: {0}s", runningTime.TotalSeconds);
            cache.Close();
            db.Close();
            Console.WriteLine("Hit enter to quit.");
            Console.ReadLine();
        }

        void ParallelImport()
        {
            //Parallel.For(

            Console.WriteLine("Reading timestep {0} to {1} from {2}", time_start, time_end, data_dir);

            string cString = String.Format("server={0};database={1};Integrated Security=true;User ID={2}", serverName, dbname, user);

            long inc = new Morton3D(0, 0, atomSize).Key;  // Address of 4 == "size" of a 4^3

            Morton3D start = new Morton3D(firstbox);
            Morton3D end = new Morton3D(lastbox);

            ServerBoundaries serverBoundaries = new ServerBoundaries(start.X, end.X + atomSize - 1, start.Y, end.Y + atomSize - 1, start.Z, end.Z + atomSize - 1);
            ServerBoundaries[] VirtualServerBoundaries = serverBoundaries.getVirtualServerBoundaries(numProcs);

            Parallel.For(0, numProcs, i =>
            {
                long dataSize = (long)(VirtualServerBoundaries[i].endx - VirtualServerBoundaries[i].startx + 1) *
                        (long)(VirtualServerBoundaries[i].endy - VirtualServerBoundaries[i].starty + 1) *
                        (long)(VirtualServerBoundaries[i].endz - VirtualServerBoundaries[i].startz + 1) *
                        sizeof(double);

                int partitions = 1;
                while (dataSize > int.MaxValue)
                {
                    partitions *= 2;
                    dataSize /= 2;
                }
                ServerBoundaries[] PartitionBoundaries = VirtualServerBoundaries[i].getVirtualServerBoundaries(partitions);

                long[] firstBoxes = new long[partitions];
                long[] lastBoxes = new long[partitions];
                for (int p = 0; p < partitions; p++)
                {
                    firstBoxes[p] = new Morton3D(PartitionBoundaries[p].startz, PartitionBoundaries[p].starty, PartitionBoundaries[p].startx);
                    lastBoxes[p] = new Morton3D(PartitionBoundaries[p].endz - atomSize + 1, PartitionBoundaries[p].endy - atomSize + 1, PartitionBoundaries[p].endx - atomSize + 1);
                }

                FileCache cache = new FileCache(data_dir, suffix, resolution, headerSize);

                Database db = new Database(cString);
                db.EnableMinimalLoggin();
                db.DisableLockEscalation();
                DateTime startRunTime = DateTime.Now;
                for (int timestep = time_start; timestep <= time_end; timestep += timeinc)
                {
                    for (int p = 0; p < partitions; p++)
                    {
                        // There is no data for z >= 512
                        if (new Morton3D(firstBoxes[p]).Z >= 512)
                            continue;

                        Console.WriteLine("Reading from blob #{0} to {1} for time {2}", new Morton3D(firstBoxes[p]).ToPrettyString(), new Morton3D(lastBoxes[p]).ToPrettyString(), timestep);
                        DateTime startTime = DateTime.Now;
                        //cache.readFiles(timestep, firstBoxes[p], lastBoxes[p], atomSize);
                        cache.readFilesIntoByteArray(timestep, firstBoxes[p], lastBoxes[p], atomSize);
                        TimeSpan span = DateTime.Now - startTime;
                        Console.WriteLine("Creating the cache took: {0}s", span.TotalSeconds);

                        TurbDataReader.ExportDataFormat df = new TurbDataReader.ExportDataFormat(
                            firstBoxes[p],
                            lastBoxes[p],
                            inc, atomSize, edge,
                            isDataLittleEndian);
                        TurbDataReader turbDataReader = (TurbDataReader)TurbDataReader.GetReader(timestep, timeoff, resolution, components, df, cache);

                        startTime = DateTime.Now;
                        db.BulkCopy(table, turbDataReader);
                        span = DateTime.Now - startTime;
                        Console.WriteLine("Date ingest took: {0}s", span.TotalSeconds);
                    }
                }
                TimeSpan runningTime = DateTime.Now - startRunTime;
                Console.WriteLine("Total running time: {0}s", runningTime.TotalSeconds);
                cache.Close();
                db.Close();
            });
            Console.WriteLine("Hit enter to quit.");
            Console.ReadLine();
        }

        void ParallelImportUsingSharedMemory()
        {
            //Parallel.For(

            Console.WriteLine("Reading timestep {0} to {1} from {2}", time_start, time_end, data_dir);

            string cString = String.Format("server={0};database={1};Integrated Security=true;User ID={2}", serverName, dbname, user);

            long inc = new Morton3D(0, 0, atomSize).Key;  // Address of 4 == "size" of a 4^3

            Morton3D start = new Morton3D(firstbox);
            Morton3D end = new Morton3D(lastbox);

            ServerBoundaries serverBoundaries = new ServerBoundaries(start.X, end.X + atomSize - 1, start.Y, end.Y + atomSize - 1, start.Z, end.Z + atomSize - 1);

            long dataSize = (long)(serverBoundaries.endx - serverBoundaries.startx + 1) *
                    (long)(serverBoundaries.endy - serverBoundaries.starty + 1) *
                    (long)(serverBoundaries.endz - serverBoundaries.startz + 1) *
                    sizeof(double);

            int partitions = 1;
            while (dataSize > int.MaxValue)
            {
                partitions *= 2;
                dataSize /= 2;
            }
            ServerBoundaries[] PartitionBoundaries = serverBoundaries.getVirtualServerBoundaries(partitions);
            long[] firstBoxes = new long[partitions];
            long[] lastBoxes = new long[partitions];
            for (int p = 0; p < partitions; p++)
            {
                firstBoxes[p] = new Morton3D(PartitionBoundaries[p].startz, PartitionBoundaries[p].starty, PartitionBoundaries[p].startx);
                lastBoxes[p] = new Morton3D(PartitionBoundaries[p].endz - atomSize + 1, PartitionBoundaries[p].endy - atomSize + 1, PartitionBoundaries[p].endx - atomSize + 1);
            }

            FileCache cache;
            if (dbname.Contains("mixing"))
            {
                cache = new FileCacheMixing(data_dir, resolution, headerSize, components);
            }
            else
            {
                cache = new FileCache(data_dir, suffix, resolution, headerSize);
            }

            Database[] db = new Database[numProcs];
            Parallel.For(0, numProcs, i =>
            {
                db[i] = new Database(cString);
                db[i].EnableMinimalLoggin();
                db[i].DisableLockEscalation();
            });

            DateTime startRunTime = DateTime.Now;
            for (int timestep = time_start; timestep <= time_end; timestep += timeinc)
            {
                for (int p = 0; p < partitions; p++)
                {
                    Console.WriteLine("Reading from blob #{0} to {1} for time {2}", new Morton3D(firstBoxes[p]).ToPrettyString(), new Morton3D(lastBoxes[p]).ToPrettyString(), timestep);
                    DateTime startTime = DateTime.Now;
                    //cache.readFiles(timestep, firstBoxes[p], lastBoxes[p], atomSize);
                    cache.readFilesIntoByteArray(timestep, firstBoxes[p], lastBoxes[p], atomSize);
                    TimeSpan span = DateTime.Now - startTime;
                    Console.WriteLine("Creating the cache took: {0}s", span.TotalSeconds);

                    ServerBoundaries[] VirtualServerBoundaries = PartitionBoundaries[p].getVirtualServerBoundaries(numProcs);

                    Parallel.For(0, numProcs, i =>
                    {
                        TurbDataReader.ExportDataFormat df = new TurbDataReader.ExportDataFormat(
                            new Morton3D(VirtualServerBoundaries[i].startz, VirtualServerBoundaries[i].starty, VirtualServerBoundaries[i].startx),
                            new Morton3D(VirtualServerBoundaries[i].endz - atomSize + 1, VirtualServerBoundaries[i].endy - atomSize + 1, VirtualServerBoundaries[i].endx - atomSize + 1),
                            inc, atomSize, edge,
                            isDataLittleEndian);
                        TurbDataReader turbDataReader = (TurbDataReader)TurbDataReader.GetReader(timestep, timeoff, resolution, components, df, cache);

                        startTime = DateTime.Now;
                        db[i].BulkCopy(table, turbDataReader);
                        span = DateTime.Now - startTime;
                        Console.WriteLine("Date ingest took: {0}s", span.TotalSeconds);
                    });
                }
            }
            TimeSpan runningTime = DateTime.Now - startRunTime;
            Console.WriteLine("Total running time: {0}s", runningTime.TotalSeconds);
            cache.Close();
            Parallel.For(0, numProcs, i =>
            {
                db[i].Close();
            });
            Console.WriteLine("Hit enter to quit.");
            Console.ReadLine();
        }
    }
}
