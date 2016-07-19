using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HDF5DotNet;

using Turbulence.TurbLib.DataTypes;

namespace ImportData
{
    class ImportChannelFlowData
    {
        //int time_start = 142005;    // First time step to dataread 142700
        //int time_end = 152015;      // Last time step to dataread  (db01 has pr up to 147000)
        int time_start = 135005;
        int time_end = 136000;
        string data_dir = @"\\dss005\tdbchannel\tdb-chunk-4\";
        //string data_dir = @"\\dss005\tdbchannel\tdb-channel-remaining\";
        //string data_dir = @"H:\channel\";
        string user = "shamilto";
        string db_prefix = "channeldb";
        string staging_db = "stagingdb";
        int[] resolution = { 1535, 511, 2047 };
        //long[] range_start = { 0,         536870912, 1073741824, 1610612736, 4294967296, 5368709120 };
        //long[] range_end   = { 268434944, 805305856, 1342176768, 1879047680, 4563402240, 5637144064 };
        //This is all of them.
        long[] range_start = { 0,         134217728, 536870912, 671088640, 1073741824, 1207959552, 1610612736, 1744830464, 4294967296, 4429185024, 5368709120, 5502926848 };
        long[] range_end   = { 134217216, 268434944, 671088128, 805305856, 1207959040, 1342176768, 1744829952, 1879047680, 4429184512, 4563402240, 5502926336, 5637144064 };
        
        int timeinc = 5;
        int timeoff = 0;
        int atomSize = 8;
        string dataset = "vel";
        string serverName = "dsp087";
        //string serverName = "gwwn1";
        bool isDataLittleEndian = true;
        int first_range = 9;
        int last_range = 9;
        //int last_range = range_start.Length - 1;
        int rounds = 1; //2;
        int numProcs = 24; //12;
        bool use_staging_db = false;
        bool use_staging_tables = false;

        static void Main(string[] args)
        {
            H5.Open();
            ImportChannelFlowData import = new ImportChannelFlowData();
            if (args.Length == 5)
            {
                //import.data_dir = args[0];
                import.dataset = args[0];
                import.time_start = int.Parse(args[1]);
                import.time_end = int.Parse(args[2]);
                import.first_range = int.Parse(args[3]);
                import.last_range = int.Parse(args[4]);
            }
            DateTime start = DateTime.Now;
            //import.TestHDF5Reader();
            //import.TestParallelReading();
            //import.TestSequentialReadParallelCopy();
            //import.TestAsyncReadParallelCopy();
            import.ParallelIngestSequentialRead();
            Console.WriteLine("Total running time {0} s. (Component: {1})", (DateTime.Now - start).TotalSeconds, import.dataset);
            H5.Close();

            //Console.WriteLine("Press any key to continue!");
            //Console.ReadLine();
        }

        void TestHDF5Reader()
        {
            DateTime start;
            TimeSpan elapsed;
            int components = GetComponents();
            HDF5Reader hdf5_reader = new HDF5Reader(data_dir, components);
            int dataOffset = 24;
            byte[] data = new byte[dataOffset + atomSize * atomSize * atomSize * sizeof(float) * components];
            //byte[] u = new byte[atomSize * atomSize * atomSize * sizeof(float)];
            //byte[] v = new byte[atomSize * atomSize * atomSize * sizeof(float)];
            //byte[] w = new byte[atomSize * atomSize * atomSize * sizeof(float)];
            //start = DateTime.Now;
            //hdf5_reader.GetAtom(new long[] { 0, 0, 0 }, new long[] { atomSize, atomSize, atomSize }, u, v, w);
            //elapsed = DateTime.Now - start;
            //long microseconds = elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
            //Console.WriteLine("GetAtom takes {0} microseconds", microseconds);
            //Console.WriteLine("GetAtom takes {0} milliseconds", elapsed.TotalMilliseconds);

            //start = DateTime.Now;
            //hdf5_reader.GetAtom(new long[] { 0, 0, 0 }, new long[] { atomSize, atomSize, atomSize }, data);
            //elapsed = DateTime.Now - start;
            //Console.WriteLine("GetAtom takes {0} milliseconds", elapsed.TotalMilliseconds);

            long inc = atomSize * atomSize * atomSize;

            for (int i = 0; i < range_start.Length; i++)
            {
                start = DateTime.Now;
                hdf5_reader.readFiles(time_start, range_start[i], range_end[i], atomSize);
                elapsed = DateTime.Now - start;
                Console.WriteLine("readFiles takes {0} milliseconds for range {1}", elapsed.TotalMilliseconds, i);
                start = DateTime.Now;
                for (long z = range_start[i]; z < range_end[i]; z += inc)
                {
                    Morton3D zindex = new Morton3D(z);
                    hdf5_reader.GetAtom(new int[] { zindex.Z - hdf5_reader.Base[0], zindex.Y - hdf5_reader.Base[1], zindex.X - hdf5_reader.Base[2] },
                        new int[] { atomSize, atomSize, atomSize }, data, dataOffset);
                    hdf5_reader.VerifyAtom(new long[] { zindex.Z, zindex.Y, zindex.X }, data, dataOffset);
                    //PrintAtom(data);
                }
                elapsed = DateTime.Now - start;
                Console.WriteLine("GetAtom takes {0} milliseconds for range {1}", elapsed.TotalMilliseconds, i);
            }

            hdf5_reader.Close();
        }

        void TestParallelReading()
        {
            Console.WriteLine("Reading timestep {0} to {1} from {2}", time_start, time_end, data_dir);
            int components = GetComponents();
            string cString = String.Format("server={0};database={1};Integrated Security=true;User ID={2}", serverName, db_prefix, user);
            long inc = new Morton3D(0, 0, atomSize).Key;  // Address of 8 == "size" of a 8^3

            for (int timestep = time_start; timestep <= time_end; timestep += timeinc)
            {
                for (int range = 0; range < range_start.Length; range++)
                {
                    Morton3D start = new Morton3D(range_start[range]);
                    Morton3D end = new Morton3D(range_end[range]);

                    ServerBoundaries serverBoundaries = new ServerBoundaries(start.X, end.X + atomSize - 1, start.Y, end.Y + atomSize - 1, start.Z, end.Z + atomSize - 1);
                    ServerBoundaries[] VirtualServerBoundaries = serverBoundaries.getVirtualServerBoundaries(numProcs);

                    Parallel.For(0, numProcs, proc =>
                    {
                        int dataOffset = 24;
                        byte[] data = new byte[dataOffset + atomSize * atomSize * atomSize * sizeof(float) * components];
                        HDF5Reader hdf5_reader = new HDF5Reader(data_dir, components);
                        Morton3D firstbox = new Morton3D(VirtualServerBoundaries[proc].startz, VirtualServerBoundaries[proc].starty, VirtualServerBoundaries[proc].startx);
                        Morton3D lastbox = new Morton3D(VirtualServerBoundaries[proc].endz - atomSize + 1,
                            VirtualServerBoundaries[proc].endy - atomSize + 1,
                            VirtualServerBoundaries[proc].endx - atomSize + 1);
                        Console.WriteLine("Reading from blob #{0} to {1} for time {2}", firstbox.ToPrettyString(), lastbox.ToPrettyString(), timestep);
                        DateTime startTime = DateTime.Now;
                        hdf5_reader.readFiles(timestep, firstbox, lastbox, atomSize);
                        TimeSpan span = DateTime.Now - startTime;
                        Console.WriteLine("Reading the file took: {0}s", span.TotalSeconds);

                        startTime = DateTime.Now;
                        for (long z = firstbox; z < lastbox; z += inc)
                        {
                            Morton3D zindex = new Morton3D(z);
                            hdf5_reader.GetAtom(new int[] { zindex.Z - hdf5_reader.Base[0], zindex.Y - hdf5_reader.Base[1], zindex.X - hdf5_reader.Base[2] },
                                new int[] { atomSize, atomSize, atomSize }, data, dataOffset);
                            //hdf5_reader.VerifyAtom(new long[] { zindex.Z, zindex.Y, zindex.X }, data, dataOffset);
                            //PrintAtom(data);
                        }
                        span = DateTime.Now - startTime;
                        Console.WriteLine("GetAtom takes {0} milliseconds for range {1} and process {2}", span.TotalMilliseconds, range, proc);
                        hdf5_reader.Close();
                    });
                }
            }
        }

        void TestAsyncReadParallelCopy()
        {
            Console.WriteLine("Reading timestep {0} to {1} from {2}", time_start, time_end, data_dir);
            int components = GetComponents();
            long inc = new Morton3D(0, 0, atomSize).Key;  // Address of 8 == "size" of a 8^3
            //TODO(kalin): Use the same hdf5 reader.
            HDF5Reader hdf5_reader1 = new HDF5Reader(data_dir, components);
            HDF5Reader hdf5_reader2 = new HDF5Reader(data_dir, components);

            for (int timestep = time_start; timestep <= time_end; timestep += timeinc)
            {
                Console.WriteLine("Reading from blob #{0} to {1} for time {2}", range_start[0], range_end[0], timestep);
                Task read1 = Task.Factory.StartNew(() =>
                    {
                        hdf5_reader1.readFiles(timestep, range_start[0], range_end[0], atomSize);
                    });

                for (int range = 0; range <= range_start.Length - 2; range += 2)
                {
                    read1.Wait();

                    Console.WriteLine("Reading from blob #{0} to {1} for time {2}", range_start[range + 1], range_end[range + 1], timestep);
                    // Start next reading task, while the ingest is going.
                    Task read2 = Task.Factory.StartNew(() =>
                    {
                        hdf5_reader2.readFiles(timestep, range_start[range + 1], range_end[range + 1], atomSize);
                    });

                    int sliceNum = range + 1;
                    string cString = String.Format("server={0};database={1}{2:00};Integrated Security=true;User ID={3};Connection Timeout=0", serverName, db_prefix, sliceNum, user);
                    ParallelCopy(hdf5_reader1, sliceNum, inc, cString);

                    // Wait for the second read task to complete.
                    read2.Wait();

                    // Start next reading task, while the ingest is going unless we are at the end.
                    if (range < range_start.Length - 2)
                    {
                        Console.WriteLine("Reading from blob #{0} to {1} for time {2}", range_start[range + 2], range_end[range + 2], timestep);
                        read1 = Task.Factory.StartNew(() =>
                        {
                            hdf5_reader1.readFiles(timestep, range_start[range + 2], range_end[range + 2], atomSize);
                        });
                    }
                    sliceNum = range + 2;
                    cString = String.Format("server={0};database={1}{2:00};Integrated Security=true;User ID={3};Connection Timeout=0", serverName, db_prefix, sliceNum, user);
                    ParallelCopy(hdf5_reader2, sliceNum, inc, cString);
                }
            }
            hdf5_reader1.Close();
            hdf5_reader2.Close();
        }

        void ParallelCopy(HDF5Reader hdf5_reader, int sliceNum, long increment, string cString)
        {
            Parallel.For(0, numProcs, proc =>
            {
                int dataOffset = 24;
                int components = GetComponents();
                byte[] data = new byte[dataOffset + atomSize * atomSize * atomSize * sizeof(float) * components];
                //for (int proc = 0; proc < numProcs; proc++)
                //{
                int partitionNum = proc + 1;
                Database db = new Database(cString);
                long firstbox = 0, lastbox = 0, firstbox_rounded;
                db.GetPartLimits(sliceNum, partitionNum, ref firstbox, ref lastbox);
                firstbox_rounded = firstbox / increment * increment;
                if (firstbox_rounded < firstbox)
                    firstbox_rounded += increment;

                DateTime startTime = DateTime.Now;
                for (long z = firstbox_rounded; z < lastbox; z += increment)
                {
                    Morton3D zindex = new Morton3D(z);
                    hdf5_reader.GetAtom(new int[] { zindex.Z - hdf5_reader.Base[0], zindex.Y - hdf5_reader.Base[1], zindex.X - hdf5_reader.Base[2] },
                        new int[] { atomSize, atomSize, atomSize }, data, dataOffset);
                    //hdf5_reader.VerifyAtom(new long[] { zindex.Z, zindex.Y, zindex.X }, data, dataOffset);
                    //if (flag)
                    //{
                    //    Print4x4x4Atom(data);
                    //    flag = false;
                    //}
                }
                TimeSpan span = DateTime.Now - startTime;
                Console.WriteLine("GetAtom takes {0} milliseconds for process {1}", span.TotalMilliseconds, proc);
                //}
            });
        }

        void TestSequentialReadParallelCopy()
        {
            Console.WriteLine("Reading timestep {0} to {1} from {2}", time_start, time_end, data_dir);
            int components = GetComponents();
            long inc = new Morton3D(0, 0, atomSize).Key;  // Address of 8 == "size" of a 8^3
            HDF5Reader hdf5_reader = new HDF5Reader(data_dir, components);

            for (int timestep = time_start; timestep <= time_end; timestep += timeinc)
            {
                for (int range = 0; range < range_start.Length; range++)
                {
                    Console.WriteLine("Reading from blob #{0} to {1} for time {2}", range_start[range], range_end[range], timestep);
                    DateTime startTime = DateTime.Now;
                    hdf5_reader.readFiles(timestep, range_start[range], range_end[range], atomSize);
                    TimeSpan span = DateTime.Now - startTime;
                    Console.WriteLine("Reading the file took: {0}s", span.TotalSeconds);

                    int sliceNum = range + 1;
                    string cString = String.Format("server={0};database={1}{2:00};Integrated Security=true;User ID={3};Connection Timeout=0", serverName, db_prefix, sliceNum, user);
                    ParallelCopy(hdf5_reader, sliceNum, inc, cString);
                }
            }
            hdf5_reader.Close();
        }

        void PrintAtom(byte[] data)
        {
            int components = GetComponents();
            int offset = 0;
            for (int i = 0; i < atomSize; i++)
                for (int j = 0; j < atomSize; j++)
                    for (int k = 0; k < atomSize; k++)
                    {
                        Console.WriteLine("[{3}, {4}, {5}]: u = {0}, v = {1}, w = {2}", BitConverter.ToSingle(data, offset * sizeof(float)),
                            BitConverter.ToSingle(data, (offset + 1) * sizeof(float)), BitConverter.ToSingle(data, (offset + 2) * sizeof(float)), i, j, k);
                        offset += components;
                    }
        }

        void Print4x4x4Atom(byte[] data)
        {
            int components = GetComponents();
            int offset = 0;
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    for (int k = 0; k < 4; k++)
                    {
                        Console.WriteLine("[{3}, {4}, {5}]: u = {0}, v = {1}, w = {2}", BitConverter.ToSingle(data, offset * sizeof(float)),
                            BitConverter.ToSingle(data, (offset + 1) * sizeof(float)), BitConverter.ToSingle(data, (offset + 2) * sizeof(float)), i, j, k);
                        offset += components;
                    }
        }

        void ParallelIngestSequentialRead()
        {
            Console.WriteLine("Reading timestep {0} to {1} from {2}", time_start, time_end, data_dir);
            long inc = new Morton3D(0, 0, atomSize).Key;  // Address of 8 == "size" of a 8^3

            string tablePrefix;
            int components = GetComponents();
            if (dataset.Equals("vel"))
            {
                tablePrefix = "vel_";
            }
            else if (dataset.Equals("pr"))
            {
                tablePrefix = "p_";
            }
            else
            {
                throw new Exception("Unknown dataset specified!");
            }

            HDF5Reader hdf5_reader = new HDF5Reader(data_dir, components);

            Database[] DBs = new Database[numProcs];
            for (int i = 0; i < numProcs; i++)
            {
                string cString;
                if (use_staging_db)
                    cString = String.Format("server={0};database={1};Integrated Security=true;User ID={2};Connection Timeout=0", serverName, staging_db, user);
                else
                    cString = String.Format("server={0};Integrated Security=true;User ID={1};Connection Timeout=0", serverName, user);
                DBs[i] = new Database(cString);
            }

            for (int timestep = time_start; timestep <= time_end; timestep += timeinc)
            {
                DateTime timestepStart = DateTime.Now;
                for (int range = first_range; range <= last_range; range++)
                {
                    int sliceNum = range + 1;

                    string channel_db = String.Format("{0}{1:00}", db_prefix, sliceNum);

                    //string cString;
                    //if (use_staging_db)
                    //    cString = String.Format("server={0};database={1};Integrated Security=true;User ID={2};Connection Timeout=0", serverName, staging_db, user);
                    //else
                    //    cString = String.Format("server={0};database={1};Integrated Security=true;User ID={2};Connection Timeout=0", serverName, channel_db, user);
                    
                    Console.WriteLine("Reading from blob #{0} to {1} for time {2}", range_start[range], range_end[range], timestep);
                    DateTime startTime = DateTime.Now;
                    hdf5_reader.readFiles(timestep, range_start[range], range_end[range], atomSize);
                    TimeSpan span = DateTime.Now - startTime;
                    Console.WriteLine("Reading the file took: {0}s", span.TotalSeconds);

                    for (int round = 0; round < rounds; round++)
                    {
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
                                tableName = String.Format("{0}..{1}{2:00}", channel_db, tablePrefix, partitionNum);
                            else
                                tableName = String.Format("{0}..{1}", channel_db, dataset);

                            //db.EnableMinimalLogging();
                            //db.DisableLockEscalation();

                            DateTime startRunTime = DateTime.Now;
                            ChannelDataReader.ExportDataFormat df = new ChannelDataReader.ExportDataFormat(
                                firstbox,
                                lastbox,
                                inc, atomSize,
                                isDataLittleEndian);
                            ChannelDataReader dataReader = (ChannelDataReader)ChannelDataReader.GetReader(timestep, timeoff, resolution, components, df, hdf5_reader);

                            //db.BulkCopy(tableName, dataReader);
                            DBs[proc].BulkCopy(tableName, dataReader);
                            if (use_staging_db)
                            {
                                //db.InsertIntoChannelDB(tableName, staging_db, channel_db);
                                //db.TruncateStagingTable(tableName, staging_db);
                                DBs[proc].InsertIntoChannelDB(tableName, staging_db, channel_db);
                                DBs[proc].TruncateStagingTable(tableName, staging_db);
                            }
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
            hdf5_reader.Close();
        }

        int GetComponents()
        {
            int components;
            if (dataset.Equals("vel"))
            {
                components = 3;
            }
            else if (dataset.Equals("pr"))
            {
                components = 1;
            }
            else
            {
                throw new Exception("Unknown dataset specified!");
            }

            return components;
        }
    }
}
