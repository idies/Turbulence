using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Turbulence.TurbLib.DataTypes;

namespace IsoImportData
{
    class IsoImportData
    {
        /// <summary>
        /// Crude program for loading data into the database.
        /// Once loaded into the database data can be copied between nodes.
        /// </summary>
        static void Main(string[] args)
        {
            int time_start = 15050;  // First time step to dataread
            int time_end = 15050;    // Last time step to dataread
            string data_dir = "z:\\";
            string prefix = "vel_vector";
            string user = "shamilto";
            string dbname = "";
            //string dbname = "turbload";
            string table = "vel";

            //int z_slices = 1024;
            long firstbox = 0;
            long lastbox = 0;
            int server = 8;
            int timeinc = 10;
            int timeoff = 0;
            firstbox = 0;   
            //lastbox = 1073741312;  
            server = 1;
            lastbox = 134217727;
            string serverName = "";
            if (args.Length == 10)
            {
                data_dir = args[0];
                table = args[1];
                time_start = int.Parse(args[2]);
                time_end = int.Parse(args[3]);
                timeinc = int.Parse(args[4]);
                timeoff = int.Parse(args[5]);
                firstbox = int.Parse(args[6]);
                lastbox = int.Parse(args[7]);
                dbname = args[8];
                serverName = args[9];
            
            }
              
            else // (args.Length == 0)
            {
                Console.WriteLine("Usage: ImportData <path> <table name> <time_start> <time_end> <timeinc> <timeoff> <firstbox> <lastbox> <dbname>");
                Console.WriteLine("Timesteps [time_start,time_end] (inclusive) are imported, with an increment of <timeinc>, loaded as time+timeoff.");
                Console.WriteLine("Blocks in the range [firstbox, lastbox) are imported.");
                Environment.Exit(1);
            }

            ParallelImport(data_dir, table, time_start, time_end, timeinc, timeoff, firstbox, lastbox, dbname, serverName); 
        }

        static void SequentialImport(string data_dir, string table, int time_start, int time_end, int timeinc, int timeoff, long firstbox, long lastbox, int server) 
        {
            int resolution = 1023;
            Console.WriteLine("Reading timestep {0} to {1} from {2}", time_start, time_end, data_dir);
            string dbname = "turbdb209";
            //String cString = "database=turbdata;server=10.0.0.150;User ID=turbadmin;Password=65ways";
            //String cString = "server=(local);database=turbdb;Integrated Security=true;User ID=eric";

            //string serverName = String.Format("dlmsdb{0:000}", server);
            //string serverName = "(local)";
            string serverName = "dsp012";
            string cString = String.Format("server={0};database={1};Integrated Security=true", serverName, dbname); //;User ID={2}, user
            IsoFileCache cache = new IsoFileCache(data_dir);
            IsoDatabase db = new IsoDatabase(cString);
            long inc = new Morton3D(0, 0, 8).Key;  // Address of 64 == "size" of a 64^3
            for (int timestep = time_start; timestep <= time_end; timestep += timeinc)
            {

                Console.WriteLine("Reading from blob #{0} to {1} for time {2}", firstbox, lastbox, timestep);
                IsoTurbDataReader.ExportDataFormat df = new IsoTurbDataReader.ExportDataFormat(
                     firstbox, //inc was multiplied here?
                     lastbox,
                    inc, 8, 0, true);
                Console.WriteLine("Initiating reader");
                /*GetReader(int timestep, int timeoff,
                int[] resolution, int components, ExportDataFormat dataFormat, IsoFileCache cache)*/
                int components = 1; //We are only doing velocity.
                /*IsoTurbDataReader turbDataReader = (IsoTurbDataReader)IsoTurbDataReader.GetReader(
                    data_dir, prefix, timestep, timeoff, z_slices, resolution, 4, df); */
                 
                int[] res = {resolution, resolution, resolution};
                IsoTurbDataReader turbDataReader = (IsoTurbDataReader)IsoTurbDataReader.GetReader(
                    timestep, timeoff, res, components, df, cache, data_dir);
                    
 
                Console.WriteLine("Initiating bulkcopy with reader {0} into table {1}", turbDataReader, table);
                db.BulkCopy(table, turbDataReader);
                Console.WriteLine("Bulcopy is over");
            }
            db.Close();
            Console.WriteLine("Hit enter to quit.");
            Console.ReadLine();
        }

        static void ParallelImport(string data_dir, string table, int time_start, int time_end, int timeinc, int timeoff, long firstbox, long lastbox, string dbname, string serverName)
        {
            //string serverName = "sciserver02";
            //string dbname = "turbdb201"; provided by command line parameter.
            string user = "shamilto";
            int atomSize = 8;
            int numProcs = 16;
            //int numProcs = 1;
            //int numProcs = 1;
            //int components = 3; //When changing this, you must change the components in IsoTurbDataReader!!!!! Also change tablename
            int components = 1;
            if (table == "vel")
            {
                components = 3;
            }
            int[] resolution = {1023,1023,1023};
            //int[] resolution = { 511, 511, 511 };
            Console.WriteLine("Reading timestep {0} to {1} from {2}", time_start, time_end, data_dir);

            string cString = String.Format("server={0};database={1};Integrated Security=true;User ID={2}", serverName, dbname, user);

            long inc = new Morton3D(0, 0, atomSize).Key;  // Address of 8 == "size" of a 8^3

            Morton3D start = new Morton3D(firstbox);
            Morton3D end = new Morton3D(lastbox);

            ServerBoundaries serverBoundaries = new ServerBoundaries(start.X, end.X + atomSize - 1, start.Y, end.Y + atomSize - 1, start.Z, end.Z + atomSize - 1, 0, 0);
            ServerBoundaries[] VirtualServerBoundaries = serverBoundaries.getVirtualServerBoundaries(numProcs);
            Console.WriteLine("{0} processors, startz = {1} endz = {2}, firstbox={3}, lastbox = {4}", numProcs, start.Z, end.Z, firstbox, lastbox);
            Parallel.For(0, numProcs, (i, loopState) =>
            {
                
                long dataSize = (long)(VirtualServerBoundaries[i].endx - VirtualServerBoundaries[i].startx + 1) *
                        (long)(VirtualServerBoundaries[i].endy - VirtualServerBoundaries[i].starty + 1) *
                        (long)(VirtualServerBoundaries[i].endz - VirtualServerBoundaries[i].startz + 1) *
                        components *
                        sizeof(float);

                int partitions = 1;
                while (dataSize > int.MaxValue)  
                {
                    partitions *= 2;
                    dataSize /= 2;
                }
                Console.WriteLine("Partitions: {0}", partitions);
                ServerBoundaries[] PartitionBoundaries = VirtualServerBoundaries[i].getVirtualServerBoundaries(partitions);

                long[] firstBoxes = new long[partitions];
                long[] lastBoxes = new long[partitions];
                for (int p = 0; p < partitions; p++)
                {
                    Console.WriteLine("{3}: sx={2}, sy={1}, sz={0}", PartitionBoundaries[p].startz, PartitionBoundaries[p].starty, PartitionBoundaries[p].startx, i);
                    Console.WriteLine("{3}: ex={2}, ey={1}, ez={0}", PartitionBoundaries[p].endz, PartitionBoundaries[p].endy, PartitionBoundaries[p].endx, i);


                    firstBoxes[p] = new Morton3D(PartitionBoundaries[p].startz, PartitionBoundaries[p].starty, PartitionBoundaries[p].startx);
                    lastBoxes[p] = new Morton3D(PartitionBoundaries[p].endz - atomSize + 1, PartitionBoundaries[p].endy - atomSize + 1, PartitionBoundaries[p].endx - atomSize + 1);
                    
                }

                //FileCache cache = new IsoFileCache(data_dir, resolution, headerSize, components, suffix[0]);
               
                int edge = 0;
                bool isDataLittleEndian = true;
                IsoDatabase db = new IsoDatabase(cString);
                db.EnableMinimalLoggin();
                db.DisableLockEscalation();
                DateTime startRunTime = DateTime.Now;
                //Console.WriteLine("Hit enter to begin.");
                //Console.ReadLine();
                IsoFileCache cache = new IsoFileCache(data_dir);
                for (int timestep = time_start; timestep <= time_end; timestep += timeinc) //Subtract the time offset.
                {
                    DateTime startTime = DateTime.Now;
                    TimeSpan span = DateTime.Now - startTime;
                    //Console.WriteLine("Creating the cache took: {0}s", span.TotalSeconds);
                    for (int p = 0; p < partitions; p++)
                    {

                        string tablename = String.Format("{1}_{0:00}", i+1, table); //Using table variable was shared and caused issues.  
                        //string tablename = "vel"; //use this for testing.
                        
                        //string tablename = String.Format("{1}", i + 1, table); //Using table variable was shared and caused issues.  
                        Console.WriteLine("Components = {0}", components);
                        Console.WriteLine("Inserting into {0} first={1} last={2} timestep {3}", tablename, firstBoxes[p], lastBoxes[p], timestep);
                        IsoTurbDataReader.ExportDataFormat df = new IsoTurbDataReader.ExportDataFormat(
                            firstBoxes[p],
                            lastBoxes[p],
                            inc, atomSize, edge,
                            isDataLittleEndian);
                        IsoTurbDataReader turbDataReader = (IsoTurbDataReader)IsoTurbDataReader.GetReader(timestep, timeoff, resolution, components, df, cache, data_dir);
                        //Console.WriteLine("Dataformat says {0}", df.start);
                        startTime = DateTime.Now;
                        db.BulkCopy(tablename, turbDataReader);
                        /*Stop looping if the file was missing */
                        if (cache.filenotfound)
                        {
                            Console.Write("Stopping Parallel loop.");
                            loopState.Stop(); //Exit since we are missing a file
                            return;
                        }
                        span = DateTime.Now - startTime;
                        Console.WriteLine("Components = {0}", components);
                        Console.WriteLine("Data ingest for table {1} took: {0}s", span.TotalSeconds, tablename);
                    }
                    cache.Close();
                }
                TimeSpan runningTime = DateTime.Now - startRunTime;
                Console.WriteLine("Total running time: {0}s", runningTime.TotalSeconds);
                
                db.Close();
            });
            //Console.WriteLine("Hit enter to quit.");
            //Console.ReadLine();
        }

    }
}
