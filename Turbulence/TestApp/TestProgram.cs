using System;
using System.Collections.Generic;
using System.Text;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using TestApp;

using System.Data.Sql;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.IO;

namespace TestApp
{
    /// <summary>
    /// 
    /// </summary>
    class TestProgram
    {
        static Random random = new Random();
        static double EPSILON = 0.00002;

        /// <summary>
        /// A small application to test the particle tracking and
        /// turbulence code libraries.
        /// </summary>
        /// <remarks>
        /// Currently only used to paste & debug snipits of code...
        /// TODO: Make it a useful, complete, test suite...
        /// </remarks>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            
            //testMorton();

            //localWS.Service service = new localWS.Service();
          
            try
            {
                //TestGetForce();
                //TestMixingDataset();
                //AllTest();
                //TestGetBoxFilterGradient();
                //TestAllDisks();
                TestGetRawData();
                //TestIsotropicFine();
                //TestGetLaplacian();
                //TestChannelFlowInterpolation();
                //TestGetBoxFilter();
                //TestChannelFlowDB();
                //TestParticleTracking();
                //TestSplines();
                //ComputeSplinesGradient();
                //ComputeSplinesHessian();
                //TestDensityHessian();
                //ComputeDensityHessian();
                //TestGetThreshold();
                //ReadFileData();
                return;

                turbulence.TurbulenceService service = new turbulence.TurbulenceService();

                int pointsize = 1;
                turbulence.Point3[] points = new turbulence.Point3[pointsize];
                turbulence.Point3[] positions = new turbulence.Point3[pointsize];
                turbulence.Vector3[] result;
                //xp = new float[points];
                //yp = new float[points];
                //zp = new float[points];

                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new turbulence.Point3();
                    points[i].x = (float)(random.NextDouble() * 2.0 * 3.14);
                    points[i].y = (float)(random.NextDouble() * 2.0 * 3.14);
                    points[i].z = (float)(random.NextDouble() * 2.0 * 3.14);
                    //points[i].x = (float)(random.NextDouble() * 3.14);
                    //points[i].y = (float)(random.NextDouble() * 3.14 + Math.PI);
                    //points[i].z = (float)(random.NextDouble() * 3.14 + Math.PI);
                }
                //3.1,4.6,2
                points[0].x = 7.1f;
                points[0].y = 0.6f;
                points[0].z = 3.14f;
                service.Timeout = -1;
                Console.WriteLine("Calling service");
                result = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "channel", 24f,
                    turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
                Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
                float startTime = 0.175f;
                float endTime = 0.185f;
                positions = service.GetPosition("edu.jhu.pha.turbulence-monitor", "isotropic1024", startTime, endTime, 0.001f, turbulence.SpatialInterpolation.Lag8, points);
                Console.WriteLine("start time={3}, end time={4}, X={0} Y={1} Z={2}", positions[0].x, positions[0].y, positions[0].z, startTime, endTime);

                long[] range_start = { 0, 134217728, 536870912, 671088640, 1073741824, 1207959552, 1610612736, 1744830464, 4294967296, 4429185024, 5368709120, 5502926848 };
                points = new turbulence.Point3[48];
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new turbulence.Point3();
                    // This ensures that we hit all servers and all of their disks
                    // We have 4 servers with 12 logical data volumes per server
                    // Incrementing by 5592404 gives us the next partition
                    // There are 3 DBs per node, for now we just check the first one. Hence, (i / 12) * 3 to determine the range start.
                    Morton3D z = new Morton3D((i % 12) * 5592404 + range_start[(i / 12) * 3] + 5592404 / 2);
                    points[i].x = z.X * 8.0f * (float)Math.PI / 2048;
                    points[i].y = z.Y * 2.0f / 512 - 1.0f;
                    points[i].z = z.Z * 3.0f * (float)Math.PI / 1536;
                }
                result = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "channel", 0.0f,
                    turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);

                int num_servers = 4;
                int num_disks_per_server = 4;
                int server_size = 268435456;
                int partition_size = 16777216;
                string authToken = "edu.jhu.pha.turbulence.testing-201406";
                for (int i = 0; i < num_servers; i++)
                {
                    for (int j = 0; j < num_disks_per_server; j++)
                    {
                        points[i] = new turbulence.Point3();
                        Morton3D z = new Morton3D(i * server_size + j * partition_size + partition_size / 2);
                        Console.WriteLine(z);
                        points[i].x = z.X * 2 * (float)Math.PI / 1024;
                        points[i].y = z.Y * 2 * (float)Math.PI / 1024;
                        points[i].z = z.Z * 2 * (float)Math.PI / 1024;
                    }
                }
                result = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "mhd1024", 0.0f,
                    turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);

                DateTime beginTime, stopTime;
                beginTime = DateTime.Now;
                result = service.GetVelocity(authToken, "mhd1024", 1.432f,
                         turbulence.SpatialInterpolation.Lag6,
                         turbulence.TemporalInterpolation.None,
                         points);
                stopTime = DateTime.Now;
                Console.WriteLine("Execution time: {0}", stopTime - beginTime);
                //Console.WriteLine("{0} {1} {2}", result[0].duxdx, result[0].duxdy, result[0].duxdz);
                //Console.WriteLine("Point 234: {0},{1},{2} {3},{4},{5}", xp[234], yp[234], zp[234],
                //ux[234], uy[234], uz[234], up[234]);
                Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            }
            catch (Exception E)
            {
                Console.WriteLine(E);
            }
            Console.WriteLine("Called service");


            #region OldTests
            //Console.WriteLine("Running TurbLib tests...");
            //testBlob();
            /*for (int z = 0; z < 4; z++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        Morton3D m = new Morton3D(z, y, x);
                        Console.WriteLine("{0:X} = Z={1},Y={2},X={3}", m.ToBinaryString(), z, y, x);
                        Console.WriteLine("[or: {0},{1},{2}]", m.GetValues()[0], m.GetValues()[1], m.GetValues()[2]);
                    }
                }
            }*/
            /* for (int i = 0; i < 64; i++)
            {
                Morton3D m = new Morton3D(i);
                Console.WriteLine("{0} {1},{2},{3}]", i, m.GetValues()[0], m.GetValues()[1], m.GetValues()[2]);

            }

            float[] yx = { 0.1f, 0.1f, 1.5f, 0.9f, 3.0f};
            float[] yy = { 0.1f, 0.2f, 0.9f, 0.4f, 3.0f};
            float[] yz = { 0.1f, 0.2f, 1.4f, 2.4f, 3.0f};

            // float[] ux, uy, uz;

            localWS.Service service = new localWS.Service();
            float[] dxx, dxy, dxz, dyx, dyy, dyz, dzx, dzy, dzz;
            int count = service.GetVelocityGradient(33, 6, 5, yx, yy, yz,
                out dxx, out dxy, out dxz, out dyx, out dyy, out dyz, out dzx, out dzy, out dzz);
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine("DXX: {0}, DXY: {1}, DXZ: {2}", dxx[i], dxy[i], dxz[i]);
            }

            */
            /*  int count = service.GetVelocity(33, 6, yx.Length, yx, yy, yz, out ux, out uy, out uz);

           
              for (int q = 0; q < ux.Length; q++)
              {
                  Console.WriteLine("{0}, {1}, {2} [RC: {3}]", ux[q], uy[q], uz[q], count);
              }*/

            /*printPoint(service, 33, 2,2,2);
            printPoint(service, 33, 3,3,3);
            printPoint(service, 33, 4,4,4);
            printPoint(service, 33, 5,5,5);
            printPoint(service, 33, 6, 6, 6);*/


            /*
            for (int z = 0; z < 6; z++)
            {
                for (int y = 0; y < 6; y++)
                {
                    Console.Write("{0} ", y);
                    for (int x = 0; x < 6; x++)
                    {
                        float[] r = service.GetPoint(35, x, y, z);
                        Console.Write("{0} ", r[2]);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }*/
            /*  float [] data = new float[72 * 72 * 72 * 4];
              LoadDatabaseBlob(33, new Morton3D(0,0,0), data);

              int zz = 3, yy = 3, xx = 3;
              int off = (zz * 72 * 72 + yy * 72 + xx)*4;
              Console.WriteLine("{0},{1},{2}", data[off], data[off+1], data[off+2]);
              printPoint(service, 33, 1023, 1023, 1023);
              printPoint(service, 33, 0, 0, 0);*/
            
            #endregion
            
            Console.WriteLine("Hint enter to quit.");
            Console.ReadLine();

        }

        public static void TestGetThreshold()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();

            turbulence.ThresholdInfo[] result;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            service.Timeout = -1;
            Console.WriteLine("Calling service");

            float time = (float)(random.NextDouble() * 2.56);
            //time = 1.946407f;
            //time = 2.179058f;
            //time = 1.931042f;

            int num_runs = 6;
            double dt = 0.00025;
            int time_inc = 10;
            int time_off = 0;
            
            //dt = 0.0013;
            //time_inc = 5;
            //time_off = 132010;

            int timestep = (int)Math.Round(time / dt / (double)time_inc) * time_inc + time_off;

            //Console.WriteLine(String.Format("Timestep {0}:", timestep));
            //int[] bins = service.GetPDF(authToken, "mhd1024", "vorticity", time, 10, 10, turbulence.SpatialInterpolation.None_Fd4, 0, 0, 0, 1024, 1024, 1024);
            //for (int i = 0; i < bins.Length; i++)
            //{
            //    Console.WriteLine(String.Format("Bin {0}: count {1}", i, bins[i]));
            //}
            //return;

            float threshold = 1.42f;

            //time = 1.931042f;
            //threshold = 80.0f;
            //result = service.GetThreshold(authToken, "mhd1024", "magnetic", time, threshold,
            //    turbulence.SpatialInterpolation.None, 0, 0, 0, 1024, 1024, 1024);

            //time = 1.946407f;
            //threshold = 60.0f;
            //result = service.GetThreshold(authToken, "mhd1024", "vorticity", time, threshold,
            //    turbulence.SpatialInterpolation.None_Fd4, 0, 0, 0, 1024, 1024, 1024);

            //time = 2.179058f;
            //threshold = 44.0f;
            //result = service.GetThreshold(authToken, "mhd1024", "vorticity", time, threshold,
            //    turbulence.SpatialInterpolation.None_Fd4, 0, 0, 0, 1024, 1024, 1024);

            //time = 1.931042f;
            //threshold = 1.35f;
            //time = 1.315579f;
            //threshold = 1.35f;
            //threshold = 1.42f;
            //time = 0.1177055f;
            //threshold = 1.8f;
            //time = 1.915356f;
            //threshold = 1.82f;

            //time = 1.946407f;
            //threshold = 130.0f;
            //time = 1.946407f;
            //threshold = 260.0f;
            time = 1.946407f;
            threshold = 560.0f;
            timestep = (int)Math.Round(time / dt / (double)time_inc) * time_inc + time_off;
            for (int run = 0; run < num_runs; run++)
            {
                DateTime startTime, stopTime;
                startTime = DateTime.Now;
                //result = service.GetThreshold(authToken, "mhd1024", "vorticity", time, threshold,
                //    turbulence.SpatialInterpolation.None_Fd4, 0, 0, 0, 1024, 1024, 1024);
                //result = service.GetThreshold(authToken, "channel", "vorticity", time, threshold,
                //    turbulence.SpatialInterpolation.None_Fd4, 0, 0, 0, 2048, 512, 1536);
                //result = service.GetThreshold(authToken, "mhd1024", "magnetic", time, threshold,
                //    turbulence.SpatialInterpolation.None, 0, 0, 0, 1024, 1024, 1024);
                result = service.GetThreshold(authToken, "mhd1024", "q", time, threshold,
                    turbulence.SpatialInterpolation.None_Fd4, 0, 0, 0, 1024, 1024, 1024);
                stopTime = DateTime.Now;
                Console.Write("run {0}: ", run + 1);
                Console.WriteLine("Execution time: {0}", (stopTime - startTime).TotalSeconds);
                Console.WriteLine("Time queried: {4}, Num. points: {5}, X={0} Y={1} Z={2} value={3}", result[0].x, result[0].y, result[0].z, result[0].value, time, result.Length);
                Console.WriteLine("Time queried: {4}, Num. points: {5}, X={0} Y={1} Z={2} value={3}", result[1].x, result[1].y, result[1].z, result[1].value, time, result.Length);
            }
            //time = 2.179058f;
            //time = 0.0025f;
            //threshold = 44.0f;
            //timestep = (int)Math.Round(time / dt / (double)time_inc) * time_inc + time_off;
            //for (int run = 0; run < num_runs; run++)
            //{
            //    DateTime startTime, stopTime;
            //    startTime = DateTime.Now;
            //    //result = service.GetThreshold(authToken, "mhd1024", "vorticity", time, 60.0f,
            //    //    turbulence.SpatialInterpolation.None_Fd4, 0, 0, 0, 1024, 1024, 1024);
            //    result = service.GetThreshold(authToken, "mhd1024", "vorticity", time, threshold,
            //        turbulence.SpatialInterpolation.None_Fd4, 0, 0, 0, 1024, 1024, 1024);
            //    //result = service.GetThreshold(authToken, "channel", "vorticity", time, threshold,
            //    //    turbulence.SpatialInterpolation.None_Fd4, 0, 0, 0, 2048, 512, 1536);
            //    stopTime = DateTime.Now;
            //    Console.Write("run {0}: ", run + 1);
            //    Console.WriteLine("Execution time: {0}", (stopTime - startTime).TotalSeconds);
            //    Console.WriteLine("Time queried: {4}, Num. points: {5}, X={0} Y={1} Z={2} value={3}", result[0].x, result[0].y, result[0].z, result[0].value, time, result.Length);
            //    Console.WriteLine("Time queried: {4}, Num. points: {5}, X={0} Y={1} Z={2} value={3}", result[1].x, result[1].y, result[1].z, result[1].value, time, result.Length);
            //}
            //time = 1.946407f;
            //time = 0.0025f;
            //threshold = 60.0f;
            //timestep = (int)Math.Round(time / dt / (double)time_inc) * time_inc + time_off;
            //for (int run = 0; run < num_runs; run++)
            //{
            //    DateTime startTime, stopTime;
            //    startTime = DateTime.Now;
            //    //result = service.GetThreshold(authToken, "mhd1024", "vorticity", time, 60.0f,
            //    //    turbulence.SpatialInterpolation.None_Fd4, 0, 0, 0, 1024, 1024, 1024);
            //    result = service.GetThreshold(authToken, "mhd1024", "vorticity", time, threshold,
            //        turbulence.SpatialInterpolation.None_Fd4, 0, 0, 0, 1024, 1024, 1024);
            //    //result = service.GetThreshold(authToken, "channel", "vorticity", time, threshold,
            //    //    turbulence.SpatialInterpolation.None_Fd4, 0, 0, 0, 2048, 512, 1536);
            //    stopTime = DateTime.Now;
            //    Console.Write("run {0}: ", run + 1);
            //    Console.WriteLine("Execution time: {0}", (stopTime - startTime).TotalSeconds);
            //    Console.WriteLine("Time queried: {4}, Num. points: {5}, X={0} Y={1} Z={2} value={3}", result[0].x, result[0].y, result[0].z, result[0].value, time, result.Length);
            //    Console.WriteLine("Time queried: {4}, Num. points: {5}, X={0} Y={1} Z={2} value={3}", result[1].x, result[1].y, result[1].z, result[1].value, time, result.Length);
            //}

            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        public static void TestGetForce()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();

            int pointsize = 10;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.Vector3[] result;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            service.Timeout = -1;
            Console.WriteLine("Calling service"); 
            
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new turbulence.Point3();
                points[i].x = (float)(random.NextDouble() * 2.0 * 3.14);
                points[i].y = (float)(random.NextDouble() * 2.0 * 3.14);
                points[i].z = (float)(random.NextDouble() * 2.0 * 3.14);
            }

            DateTime startTime, stopTime;
            startTime = DateTime.Now;
            result = service.GetForce(authToken, "mhd1024", 0.75f,
                turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);

            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        public static void TestIsotropicFine()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();

            int pointsize = 1;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.Vector3[] result;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new turbulence.Point3();
                //points[i].x = (float)(random.NextDouble() * 2.0 * 3.14);
                //points[i].y = (float)(random.NextDouble() * 2.0 * 3.14);
                //points[i].z = (float)(random.NextDouble() * 2.0 * 3.14);
            }
            //points[0] = new turbulence.Point3();
            //points[0].x = 2.876447f;
            //points[0].y = 3.365972f;
            //points[0].z = 1.370830f;
            service.Timeout = -1;
            Console.WriteLine("Calling service");

            DateTime startTime, stopTime;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "isotropic1024fine", 0.0002f,
                turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);

            byte[] cutout = service.GetRawVelocity("", "isotropic1024fine", 0.0002f, 0, 0, 0, 1, 1, 1);
            Console.WriteLine("raw data = {0}", BitConverter.ToSingle(cutout, 0));

            float dx = 2.0f * (float)Math.PI / 1024;
            points[0].x = 10 * dx;
            points[0].y = 10 * dx;
            points[0].z = 10 * dx;
            result = service.GetBoxFilter(authToken, "isotropic1024fine", "velocity", 0.0002f, 7.0f * dx, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);

            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        public static void TestGetLaplacian()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();

            int pointsize = 1;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.Vector3[] result;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new turbulence.Point3();
                points[i].x = (float)(random.NextDouble() * 8.0 * 3.14);
                points[i].y = (float)(random.NextDouble() * 2.0 - 1.0);
                points[i].z = (float)(random.NextDouble() * 3.0 * 3.14);
            }
            //points[0] = new turbulence.Point3();
            //points[0].x = 2.876447f;
            //points[0].y = 3.365972f;
            //points[0].z = 1.370830f;
            service.Timeout = -1;
            Console.WriteLine("Calling service");

            DateTime startTime, stopTime;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "channel", 0.364f, 
                turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        public static void TestGetBoxFilter()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();

            int pointsize = 1;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.Vector3[] result;
            turbulence.SGSTensor[] sgs_tensor;
            turbulence.VelocityGradient[] tensor;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new turbulence.Point3();
                points[i].x = (float)(random.NextDouble() * 2.0 * 3.14);
                points[i].y = (float)(random.NextDouble() * 2.0 * 3.14);
                points[i].z = (float)(random.NextDouble() * 2.0 * 3.14);
            }
            points[0] = new turbulence.Point3();
            points[0].x = 2.876447f;
            points[0].y = 3.365972f;
            points[0].z = 1.370830f;
            service.Timeout = -1;
            Console.WriteLine("Calling service");

            DateTime startTime, stopTime;
            startTime = DateTime.Now;
            float dx = 2.0f * (float)Math.PI / 1024;
            result = service.GetBoxFilter(authToken, "isotropic1024", "velocity", 0.364f, 7.0f * dx, points);
            stopTime = DateTime.Now;
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);

            sgs_tensor = service.GetBoxFilterSGS(authToken, "mhd1024", "uu", 0.364f, 7.0f * dx, points);
            Console.WriteLine("tau11={0} tau12={1} tau13={2} tau22={3} tau23={4} tau33={5}", sgs_tensor[0].xx, sgs_tensor[0].xy, sgs_tensor[0].xz, sgs_tensor[0].yy, sgs_tensor[0].yz, sgs_tensor[0].zz);

            tensor = service.GetBoxFilterSGStensor(authToken, "mhd1024", "ub", 0.364f, 7.0f * dx, points);
            Console.WriteLine("tau11={0} tau21={1} tau23={2} tau12={3} tau22={4} tau23={5} tau31={6} tau32={7} tau33={8}",
                tensor[0].duxdx, tensor[0].duxdy, tensor[0].duxdz, tensor[0].duydx, tensor[0].duydy, tensor[0].duydz, tensor[0].duzdx, tensor[0].duzdy, tensor[0].duzdz);

            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        public static void TestGetBoxFilterGradient()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();

            int nx = 32;
            int ny = 32;
            int pointsize = nx * ny;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.VelocityGradient[] result;
            float xoff = 3.0f;
            float yoff = 1.0f;
            float zoff = (float)(random.NextDouble() * 2.0 * 3.14);
            float spacing = 2.0f * (float)Math.PI / 1023.0f;
            float dx = 2.0f * (float)Math.PI / 1024;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < nx; j++)
                {
                    points[i * ny + j] = new turbulence.Point3();
                    points[i * ny + j].x = xoff + i * spacing;
                    points[i * ny + j].y = yoff + j * spacing;
                    points[i * ny + j].z = zoff;
                }
            }
            service.Timeout = -1;
            Console.WriteLine("Calling service");

            DateTime startTime, stopTime;
            startTime = DateTime.Now;
            result = service.GetBoxFilterGradient(authToken, "isotropic1024", "velocity", 0.364f, 7.0f * dx, 4.0f * dx, points);
            stopTime = DateTime.Now;
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].duxdx, result[0].duydx, result[0].duzdx);
            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        public static void TestChannelFlowInterpolation()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();

            int pointsize = 1;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.Vector3[] result;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            float x = 2.1f, y = 0.1f, z = (float)Math.PI / 2;
            int nOrder = 4;
            double dx = 8 * Math.PI / 2048;
            GridPoints grid_points_y = new GridPoints(512);
            string connectionString = "Data Source=dsp085;Initial Catalog=turblib;Integrated Security=true;Pooling=false;";
            SqlConnection sqlConn = new SqlConnection(connectionString);
            sqlConn.Open();
            grid_points_y.GetGridPointsFromDB(sqlConn);
            sqlConn.Close();
            double dz = 3 * Math.PI / 1536;
            
            int m = (int)Math.Floor(x / dx);
            int n = grid_points_y.GetCellIndex(y, 0.0);
            if (n == 511)
                n--;
            int p = (int)Math.Floor(z / dz);
            int i_s = m - nOrder / 2 + 1, i_e = i_s + nOrder - 1;
            int j_s = n - nOrder / 2 + 1, j_e = j_s + nOrder - 1;
            int k_s = p - nOrder / 2 + 1, k_e = k_s + nOrder - 1;

            double[] grid_values_x = new double[nOrder];
            double[] grid_values_y = new double[nOrder];
            double[] grid_values_z = new double[nOrder];
            for (int i = 0; i < nOrder; i++)
            {
                grid_values_x[i] = (i_s + i) * dx;
                grid_values_y[i] = grid_points_y.GetGridValue(j_s + i);
                grid_values_z[i] = (k_s + i) * dz;
            }

            double[] weights_x = new double[nOrder];
            double[] weights_y = new double[nOrder];
            double[] weights_z = new double[nOrder];
            ComputeWeights(grid_values_x, weights_x, nOrder);
            ComputeWeights(grid_values_y, weights_y, nOrder);
            ComputeWeights(grid_values_z, weights_z, nOrder);

            double[] coeff_x = new double[nOrder];
            double[] coeff_y = new double[nOrder];
            double[] coeff_z = new double[nOrder];
            ComputeCoefficients(x, grid_values_x, weights_x, coeff_x, nOrder);
            ComputeCoefficients(y, grid_values_y, weights_y, coeff_y, nOrder);
            ComputeCoefficients(z, grid_values_z, weights_z, coeff_z, nOrder);

            float[] u = new float[3];
            for (int i = 0; i < nOrder; i++)
            {
                for (int j = 0; j < nOrder; j++)
                {
                    for (int k = 0; k < nOrder; k++)
                    {
                        points[0] = new turbulence.Point3();
                        points[0].x = (i_s + i) * (float)dx;
                        points[0].y = (float)grid_points_y.GetGridValue(j_s + j);
                        points[0].z = (k_s + k) * (float)dz;
                        result = service.GetVelocity(authToken, "channel", 0.35f, 
                            turbulence.SpatialInterpolation.None,
                            turbulence.TemporalInterpolation.None,
                            points);
                        u[0] += (float)(coeff_x[i]) * (float)coeff_y[j] * (float)coeff_z[k] * result[0].x;
                        u[1] += (float)(coeff_x[i]) * (float)coeff_y[j] * (float)coeff_z[k] * result[0].y;
                        u[2] += (float)(coeff_x[i]) * (float)coeff_y[j] * (float)coeff_z[k] * result[0].z; 
                    }
                }
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

        public static void ComputeCoefficients(double theta_prime, double[] grid_values, double[] weights, double[] coefficients, int nOrder)
        {
            double denom = 0.0;
            for (int i = 0; i < nOrder; i++)
            {
                denom += weights[i] / (theta_prime - grid_values[i]);
            }

            for (int i = 0; i < nOrder; i++)
            {
                coefficients[i] = (weights[i] / (theta_prime - grid_values[i])) / denom;
            }
        }

        public static void TestGetRawData()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();
            byte[] data = null;

            string authToken = "edu.jhu.pha.turbulence.testing-201406";
            DateTime startTime, stopTime;

            startTime = DateTime.Now;
            data = service.GetRawVelocity(authToken, "isotropic1024coarse", 2.06f, 0, 600, 600, 1, 1, 1);
            stopTime = DateTime.Now;
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("X={0} Y={1} Z={2}", BitConverter.ToSingle(data, 0), BitConverter.ToSingle(data, 4), BitConverter.ToSingle(data, 8));
            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();

            //startTime = DateTime.Now;
            //data = service.GetRawVelocity(authToken, "rmhd", 0.0f, 1016, 128, 128, 8, 8, 1);
            //stopTime = DateTime.Now;
            //for (int i = 0; i < data.Length; i += 2 * sizeof(float))
            //{
            //    Console.WriteLine("Execution time: {0}", stopTime - startTime);
            //    Console.WriteLine("X={0} Y={1}", BitConverter.ToSingle(data, i), BitConverter.ToSingle(data, i + sizeof(float)));
            //}
            //Console.WriteLine("Press any key to continue.");
            //Console.ReadLine();

            //startTime = DateTime.Now;
            //data = service.GetRawVelocity(authToken, "channel", 0.35f, 0, 0, 0, 2048, 10, 1536);
            //stopTime = DateTime.Now;
            //Console.WriteLine("Execution time: {0}", stopTime - startTime);
            //Console.WriteLine("X={0} Y={1} Z={2}", BitConverter.ToSingle(data, 0), BitConverter.ToSingle(data, 4), BitConverter.ToSingle(data, 8));
            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        public static void TestSplines()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();

            int pointsize = 1;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.Vector3[] result;
            turbulence.VelocityHessian[] hessian_result;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new turbulence.Point3();
                points[i].x = (float)(random.NextDouble() * 2.0 * 3.14);
                points[i].y = (float)(random.NextDouble() * 2.0 * 3.14);
                //points[i].z = (float)(random.NextDouble() * 2.0 * 3.14);
                points[i].z = (float)(random.NextDouble() * 1.0 * 3.14);
            }
            service.Timeout = -1;
            Console.WriteLine("Calling service");

            DateTime startTime, stopTime;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M1Q4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);

            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M1Q6, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M1Q8, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M1Q10, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M1Q12, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M1Q14, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);

            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M2Q4, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M2Q6, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M2Q8, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M2Q10, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M2Q12, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M2Q14, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);

            hessian_result = service.GetVelocityHessian(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M2Q4, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            hessian_result = service.GetVelocityHessian(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M2Q6, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            hessian_result = service.GetVelocityHessian(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M2Q8, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            hessian_result = service.GetVelocityHessian(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M2Q10, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            hessian_result = service.GetVelocityHessian(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M2Q12, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            hessian_result = service.GetVelocityHessian(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M2Q14, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);

            //result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
            //    turbulence.SpatialInterpolation.M3Q4, turbulence.TemporalInterpolation.None, points);
            //Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M3Q6, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M3Q8, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M3Q10, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M3Q12, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M3Q14, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);

            //result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
            //    turbulence.SpatialInterpolation.M4Q4, turbulence.TemporalInterpolation.None, points);
            //Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M4Q6, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M4Q8, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M4Q10, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M4Q12, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            result = service.GetVelocity(authToken, "mhd1024", 0.0002f,
                turbulence.SpatialInterpolation.M4Q14, turbulence.TemporalInterpolation.None, points);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);

            points[0].y = 0.0f;
            result = service.GetVelocity(authToken, "channel", 0.0002f,
                turbulence.SpatialInterpolation.M1Q4, turbulence.TemporalInterpolation.None, points);

            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }
        
        public static void TestChannelFlowDB()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();

            int pointsize = 10;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.Vector3[] result;
            turbulence.Pressure[] resultP;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new turbulence.Point3();
                points[i].x = (float)(random.NextDouble() * 8.0 * 3.14);
                points[i].y = (float)(random.NextDouble() * 2.0 - 1);
                points[i].z = (float)(random.NextDouble() * 6.0 * 3.14);
            }
            //points[0] = new turbulence.Point3();
            //points[0].x = 2.16f;
            //points[0].y = -0.83183f;
            //points[0].z = 0.59f;            
            //points[0].x = 2.1f;
            //points[0].y = 0.1f;
            //points[0].z = 3.14159f / 2;
            points[0].x = -(float)Math.PI / 2.0f;
            points[0].y = -0.998966449382745f;
            points[0].z = (float)Math.PI / 2.0f;
            service.Timeout = -1;
            Console.WriteLine("Calling service");

            DateTime startTime, stopTime;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "channel", 13.0325f,
                     turbulence.SpatialInterpolation.None,
                     turbulence.TemporalInterpolation.None,
                     points);
            resultP = service.GetPressure(authToken, "channel", 13.0325f,
                     turbulence.SpatialInterpolation.Lag6,
                     turbulence.TemporalInterpolation.None,
                     points);
            stopTime = DateTime.Now;
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);
            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        public static void TestParticleTracking()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();

            TimeSpan total = new TimeSpan(0);
            string dataset = "mhd1024";
            float dt = 0.001f;
            float max_time = 2.56f; 
            //string dataset = "mixing";
            //float dt = 0.016f;
            //float max_time = 40.44f;
            //string dataset = "isotropic1024";
            //float dt = 0.001f;
            //float max_time = 2.048f;
            float time_range = 100 * dt;
            turbulence.SpatialInterpolation spatial = turbulence.SpatialInterpolation.Lag4;

            //float startTime = (float)random.NextDouble() * (max_time - time_range);
            //float endTime = startTime + time_range;
            //long rowid = 0;
            //int pointsize = 100000;
            //turbulence.Point3[] points = new turbulence.Point3[pointsize];
            //turbulence.Point3[] positions = new turbulence.Point3[pointsize];
            //string authToken = "edu.jhu.pha.turbulence.testing-201406";
            //Int16 interp = 0;

            //points = GenerateWorkload(false, ref pointsize, ref rowid, ref interp, ref dt, ref startTime, ref endTime);
            //if (interp == 0)
            //{
            //    spatial = turbulence.SpatialInterpolation.None;
            //}
            //else if (interp == 4)
            //{
            //    spatial = turbulence.SpatialInterpolation.Lag4;
            //}
            //else if (interp == 6)
            //{
            //    spatial = turbulence.SpatialInterpolation.Lag6;
            //}
            //else if (interp == 8)
            //{
            //    spatial = turbulence.SpatialInterpolation.Lag6;
            //}
            //endTime = startTime + 0.05f;
            //dt = 0.001f;
            //Console.WriteLine("rowid = {0}, records = {1}, interpolation: {2}, dt = {3}, start = {4}, end = {5}", rowid, pointsize, spatial, dt, startTime, endTime);

            int num_runs = 1;
            for (int run = 0; run < num_runs; run++)
            {
                float startTime = (float)random.NextDouble() * (max_time - time_range);
                float endTime = startTime + time_range;
                int pointsize = 10000;
                turbulence.Point3[] points = new turbulence.Point3[pointsize];
                turbulence.Point3[] positions = new turbulence.Point3[pointsize];
                turbulence.Point3[] positions2 = new turbulence.Point3[pointsize];
                string authToken = "edu.jhu.pha.turbulence.testing-201406";

                DateTime runningTime, runningTimeEnd;

                float buffer_zone = 0.5f;

                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new turbulence.Point3();
                    points[i].x = (float)(random.NextDouble() * 2.0 * 3.14);
                    points[i].y = (float)(random.NextDouble() * 2.0 * 3.14);
                    points[i].z = (float)(random.NextDouble() * 2.0 * 3.14);
                    
                    //if (points[i].x - 0.0f < buffer_zone || (points[i].x - Math.PI < buffer_zone && points[i].x > Math.PI))
                    //{
                    //    points[i].x += buffer_zone;
                    //}
                    //if ((Math.PI - points[i].x < buffer_zone && points[i].x < Math.PI) || (2 * Math.PI - points[i].x < buffer_zone && points[i].x > Math.PI))
                    //{
                    //    points[i].x -= buffer_zone;
                    //}
                    
                    //if (points[i].y - 0.0f < buffer_zone || (points[i].y - Math.PI < buffer_zone && points[i].y > Math.PI))
                    //{
                    //    points[i].y += buffer_zone;
                    //}
                    //if ((Math.PI - points[i].y < buffer_zone && points[i].y < Math.PI) || (2 * Math.PI - points[i].y < buffer_zone && points[i].y > Math.PI))
                    //{
                    //    points[i].y -= buffer_zone;
                    //}
                    
                    //if (points[i].z - 0.0f < buffer_zone || (points[i].z - Math.PI < buffer_zone && points[i].z > Math.PI))
                    //{
                    //    points[i].z += buffer_zone;
                    //}
                    //if ((Math.PI - points[i].z < buffer_zone && points[i].z < Math.PI) || (2 * Math.PI - points[i].z < buffer_zone && points[i].z > Math.PI))
                    //{
                    //    points[i].z -= buffer_zone;
                    //}
                    //points[i].x = (float)(3.14 / 3 + random.NextDouble() * 3.14 / 3); // restrict points to middle section of the first server
                    //points[i].y = (float)(3.14 / 3 + random.NextDouble() * 3.14 / 3);
                    //points[i].z = (float)(3.14 / 3 + random.NextDouble() * 3.14 / 3);
                }

                service.Timeout = -1;
                Console.WriteLine("Calling service");
                
                //runningTime = DateTime.Now;
                //positions = service.GetPositionDBEvaluation(authToken, "isotropic1024", startTime, endTime, -0.001f, turbulence.SpatialInterpolation.Lag4, points);
                //Console.WriteLine("Execution time: {0}", (DateTime.Now - runningTime).TotalSeconds);
                //Console.WriteLine("start time={3}, end time={4}, X={0} Y={1} Z={2}", positions[0].x, positions[0].y, positions[0].z, startTime, endTime);

                //runningTime = DateTime.Now;
                //positions2 = service.GetPositionOld(authToken, dataset, startTime, endTime, dt, spatial, points);
                //runningTimeEnd = DateTime.Now;
                //Console.WriteLine("Run {1}: Execution time: {0}", (runningTimeEnd - runningTime).TotalSeconds, run);
                //Console.WriteLine("start time={3}, end time={4}, X={0} Y={1} Z={2}", positions2[0].x, positions2[0].y, positions2[0].z, startTime, endTime);
                //total += runningTimeEnd - runningTime;

                runningTime = DateTime.Now;
                positions = service.GetPosition(authToken, dataset, startTime, endTime, dt, spatial, points);
                runningTimeEnd = DateTime.Now;
                Console.WriteLine("Run {1}: Execution time: {0}", (runningTimeEnd - runningTime).TotalSeconds, run);
                Console.WriteLine("start time={3}, end time={4}, X={0} Y={1} Z={2}", positions[0].x, positions[0].y, positions[0].z, startTime, endTime);
                total += runningTimeEnd - runningTime;

                //for (int i = 0; i < points.Length; i++)
                //{
                //    if (Math.Abs(positions[i].x - positions2[i].x) > 0.0001f ||
                //        Math.Abs(positions[i].y - positions2[i].y) > 0.0001f ||
                //        Math.Abs(positions[i].z - positions2[i].z) > 0.0001f)
                //        Console.WriteLine("Positions don't match!");
                //}
            }
            Console.WriteLine("Avg. execution time: {0}", total.TotalSeconds / num_runs);
            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        static turbulence.Point3[] GenerateWorkload(bool use_rowid, ref int records, ref long rowid, ref Int16 spatial, ref float dt, ref float timestep, ref float endTimestep)
        {
            string connStringTurbinfo = String.Format("Data Source={0};Initial Catalog={1};Integrated Security=True", "gw01", "turbinfo_dev");
            SqlConnection TurbinfoConn;
            TurbinfoConn = new SqlConnection(connStringTurbinfo);
            TurbinfoConn.Open();

            String query;
            if (use_rowid)
            {
                query = String.Format(@"SELECT TOP 1 rowid, records, access, spatial, dt, timestep, endTimestep " +
                              "FROM {0} WHERE {0}.rowid = {1} " +
                              "ORDER BY NEWID() ",
                              "turbinfo_dev..particle_tracking_usage ", rowid);
            }
            else
            {
                query = String.Format(@"SELECT TOP 1 rowid, records, access, spatial, dt, timestep, endTimestep " +
                              "FROM {0} WHERE {0}.records = {1} " +
                              "ORDER BY NEWID() ",
                              "turbinfo_dev..particle_tracking_usage ", records);
            }
            SqlCommand cmd = new SqlCommand(query, TurbinfoConn);

            byte[] access = new byte[512];

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                do
                {
                    while (reader.Read())
                    {
                        rowid = reader.GetSqlInt64(0).Value;
                        records = reader.GetSqlInt32(1).Value;
                        int bytesread = 0;
                        while (bytesread < 512)
                        {
                            int bytes = (int)reader.GetBytes(2, 0, access, bytesread, 512 - bytesread);
                            bytesread += bytes;
                        }
                        spatial = reader.GetSqlInt16(3).Value;
                        dt = reader.GetSqlSingle(4).Value;
                        timestep = reader.GetSqlSingle(5).Value;
                        endTimestep = reader.GetSqlSingle(6).Value;
                    }
                }
                while (reader.NextResult());
            }

            List<long> indexes = GetCubesAccessed(access);

            if (indexes.Count == 0)
                throw new Exception("The access bitmap appears to be 0");

            float dx = (float)(2.0 * Math.PI / 1024.0);

            turbulence.Point3[] points = new turbulence.Point3[records];
            for (int i = 0; i < records; i++)
            {
                points[i] = new turbulence.Point3();
                int index = random.Next(indexes.Count);
                //long zindex = indexes[index];
                Morton3D zindex = new Morton3D(indexes[index]);
                points[i].x = (float)(zindex.X * dx + random.NextDouble() * 8.0 * dx);
                points[i].y = (float)(zindex.Y * dx + random.NextDouble() * 8.0 * dx);
                points[i].z = (float)(zindex.Z * dx + random.NextDouble() * 8.0 * dx);
            }

            return points;
        }

        static List<long> GetCubesAccessed(byte[] access)
        {
            List<long> ret = new List<long>();
            long zindex = 64 * 64 * 64;
            for (int i = 0; i < access.Length; i++)
            {
                byte value = access[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((value & 1) == 1)
                        ret.Add((i * 8 + j) * zindex);
                    value >>= 1;
                }
            }
            return ret;
        }

        public static bool testBlob() {
          TurbDataTable table = TurbDataTable.GetTableInfo("testing");
          TurbulenceBlob blob = new TurbulenceBlob(table);
          byte[] rawdata = new byte[table.BlobByteSize];
          float[] flat = new float[8 * 8 * 8 * 3];

          Morton3D[] baseids = { new Morton3D(0, 0, 0),
          new Morton3D(960, 960, 960) };

          for (int i = 0; i < baseids.Length; i++)
          {
            Morton3D baseid = baseids[i];
            Console.WriteLine("Using cube: {0} ({1},{2},{3})",
              baseid, baseid.X, baseid.Y, baseid.Z);
            blob.Setup(0, baseid, rawdata);
            // Try to fetch all possible 6*6*6 blocks
            double v;

            int x, y, z;
            Console.WriteLine("Retrieving all points in cube");
            for (x = baseid.X - 4; x < baseid.X + 64 + 4; x++)
            {
              for (y = baseid.Y - 4; y < baseid.Y + 64 + 4; y++)
              {
                for (z = baseid.Z - 4; z < baseid.Z + 64 + 4; z++)
                {
                  v = blob.GetDataValue(z, y, x, 0);
                  v = blob.GetDataValue(z, y, x, 3);
                }
              }
            }

            Console.WriteLine("Iterating through all 6^3 flat cubes");
            for (x = baseid.X - 1; x < baseid.X + 64 + 1; x++)
            {
              for (y = baseid.Y - 1; y < baseid.Y + 64 + 1; y++)
              {
                for (z = baseid.Z - 1; z < baseid.Z + 64 + 1; z++)
                {

                  blob.GetFlatDataCubeAroundPoint(z % 1024, y % 1024, x % 1024, 6, flat);
                  blob.GetFlatDataCubeAroundPoint(z - 1024, y - 1024, x - 1024, 6, flat);
                  blob.GetFlatDataCubeAroundPoint(z + 1024, y + 1024, x + 1024, 6, flat);
                }
              }
            }

            Console.WriteLine("Iterating through all flat 8^3 cubes");
            for (x = baseid.X; x < baseid.X + 64; x++)
            {
              for (y = baseid.Y; y < baseid.Y + 64; y++)
              {
                for (z = baseid.Z; z < baseid.Z + 64; z++)
                {
                  blob.GetFlatDataCubeAroundPoint(z % 1024 , y % 1024, x % 1024, 8, flat);
                }
              }
            }


          }
          return true;
        }

        public static bool testMorton() {

            for (int x = 0; x < 1024; x += 9)
            {
                for (int y = 0; y < 1024; y += 19)
                {
                    for (int z = 0; z < 1024; z += 4)
                    {
                        Morton3D a = new Morton3D(z, y, x);
                        Morton3D b = new Morton3D(0, 0, 0);
                        b.X = x;
                        b.Y = y;
                        b.Z = z;
                        if ( a != b || a.X != x || a.Y != y || a.Z != z) {
                            Console.WriteLine("Error: {0},{1},{2}", z, y, x);
                            Console.WriteLine("a.Z, a.Y, a.X: {0},{1},{2}", z, y, x);
                            Console.WriteLine("a,b:\n{0}\n{1})", a.ToBinaryString(), b.ToBinaryString());
                            return false;
                        }

                    }
                }
            }
            return true;
        }

        public static void TestAllDisks()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();
            turbulence.Vector3[] output;
            turbulence.Point3[] points = new turbulence.Point3[32];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new turbulence.Point3();
                //points[i] = new Point3();
                // This ensures that we hit all servers and all of their disks
                // We have 8 servers with 4 logical data volumes per server
                // Incrementing by 8388608 gives us the next partition (even though there are 16 partitions
                // we only need to hit the first 4 in order to hit each of the disks)
                // Incrementing by 134217728 gives us the next server
                // We add 8388608/2 so that we get a point in the middle of the partition
                Morton3D z = new Morton3D((i % 4) * 8388608 + (i / 4) * 134217728 + 8388608 / 2);
                points[i].x = z.X * 2 * (float)Math.PI / 1024;
                points[i].y = z.Y * 2 * (float)Math.PI / 1024;
                points[i].z = z.Z * 2 * (float)Math.PI / 1024;
                //Console.WriteLine(String.Format("({0}),", z));
            }
            output = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "isotropic1024", 0.0f,
                turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);

            for (int i = 0; i < points.Length; i++)
            {
                // This ensures that we hit all servers and all of their disks
                // We have 4 servers with 4 logical data volumes per server
                // Incrementing by 16777216 gives us the next partition (even though there are 16 partitions
                // we only need to hit the first 4 in order to hit each of the disks)
                // Incrementing by 268435456 gives us the next server
                Morton3D z = new Morton3D((i % 4) * 16777216 + (i / 4) * 268435456 + 16777216 / 2);
                points[i].x = z.X * 2 * (float)Math.PI / 1024;
                points[i].y = z.Y * 2 * (float)Math.PI / 1024;
                points[i].z = z.Z * 2 * (float)Math.PI / 1024;
                //Console.WriteLine(String.Format("({0}),", z));
            }
            output = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "mhd1024", 0.0f,
                turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);

            long[] range_start = { 0, 134217728, 536870912, 671088640, 1073741824, 1207959552, 1610612736, 1744830464, 4294967296, 4429185024, 5368709120, 5502926848 };
            points = new turbulence.Point3[48];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new turbulence.Point3();
                // This ensures that we hit all servers and all of their disks
                // We have 4 servers with 12 logical data volumes per server
                // Incrementing by 5592404 gives us the next partition
                // There are 3 DBs per node, for now we just check the first one. Hence, (i / 12) * 3 to determine the range start.
                Morton3D z = new Morton3D((i % 12) * 5592404 + range_start[(i / 12) * 3] + 5592404 / 2);
                points[i].x = z.X * 8 * (float)Math.PI / 2048;
                points[i].y = z.Y * 2 / 512 - 1.0f;
                points[i].z = z.Z * 3 * (float)Math.PI / 1536;
                Console.WriteLine(String.Format("({0}),", z));
            }

            output = service.GetVelocity("edu.jhu.pha.turbulence-monitor", "channel", 0.0f,
                turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
        }

        public static void ComputeSplinesGradient()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();

            int cube_width = 4;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            DateTime startTime, stopTime;
            startTime = DateTime.Now;
            byte[] data = service.GetRawVelocity(authToken, "mhd1024", 0.35f, 2, 2, 2, cube_width, cube_width, cube_width);
            stopTime = DateTime.Now;
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("X={0} Y={1} Z={2}", BitConverter.ToSingle(data, 0), BitConverter.ToSingle(data, 4), BitConverter.ToSingle(data, 8));
            
            int dimensions = 3;
            int kernelSize = 4;
            int derivative = 1;
            double Dx = Math.PI * 2.0 / 1024.0;
            //double Dy = Math.PI * 2.0 / 1024.0;
            //double Dz = Math.PI * 2.0 / 1024.0;
            double[] poly_val = new double[dimensions * kernelSize * (derivative + 1)];

            for (int i = 0; i <= derivative; i++)
            {
                ComputeBetas(TurbulenceOptions.SpatialInterpolation.M1Q4, i, 0.00001, poly_val, (i * dimensions) * kernelSize);
                ComputeBetas(TurbulenceOptions.SpatialInterpolation.M1Q4, i, 0.00001, poly_val, (i * dimensions + 1) * kernelSize);
                ComputeBetas(TurbulenceOptions.SpatialInterpolation.M1Q4, i, 0.00001, poly_val, (i * dimensions + 2) * kernelSize);
            }


            int off0 = 0;
            int x_coordinate = 0;
            int y_coordinate = 1;
            int z_coordinate = 2;
            int interpolant = 0;
            int first_derivative = 1;
            int components = 3;
            int startx = 0, starty = 0, startz = 0, endx = 3, endy = 3, endz = 3;
            int iLagIntx = 0, iLagInty = 0, iLagIntz = 0;

            double[] a = new double[dimensions * components];
            
            for (int iz = startz; iz <= endz; iz++)
            {
                double[] b = new double[dimensions * components];
                int off1 = off0 + iz * cube_width * cube_width * components;
                for (int iy = starty; iy <= endy; iy++)
                {
                    double[] c = new double[dimensions * components];
                    int off = off1 + iy * cube_width * components;
                    for (int ix = startx; ix <= endx; ix++)
                    {
                        double dudx_x_coeff = GetBeta(poly_val, kernelSize, first_derivative, x_coordinate, iLagIntx + ix - startx);
                        // the x beta coefficient for dudy and dudz is the same
                        double dudy_x_coeff = GetBeta(poly_val, kernelSize, interpolant, x_coordinate, iLagIntx + ix - startx);
                        for (int j = 0; j < components; j++)
                        {
                            // dudx computation
                            c[j] += dudx_x_coeff * BitConverter.ToSingle(data, sizeof(float) * (off + j));
                            // dudy computation
                            c[j + dimensions] += dudy_x_coeff * BitConverter.ToSingle(data, sizeof(float) * (off + j));
                            // dudz computation
                            c[j + 2 * dimensions] += dudy_x_coeff * BitConverter.ToSingle(data, sizeof(float) * (off + j));
                        }
                        off += components;
                    }
                    double dudy_y_coeff = GetBeta(poly_val, kernelSize, first_derivative, y_coordinate, iLagInty + iy - starty);
                    // the y beta coefficient for dudx and dudz is the same
                    double dudx_y_coeff = GetBeta(poly_val, kernelSize, interpolant, y_coordinate, iLagInty + iy - starty);
                    for (int j = 0; j < components; j++)
                    {
                        // dudx computation
                        b[j] += dudx_y_coeff * c[j];
                        // dudy computation
                        b[j + dimensions] += dudy_y_coeff * c[j + dimensions];
                        // dudz computation
                        b[j + 2 * dimensions] += dudx_y_coeff * c[j + 2 * dimensions];
                    }
                }
                double dudz_z_coeff = GetBeta(poly_val, kernelSize, first_derivative, z_coordinate, iLagIntz + iz - startz);
                // the z beta coefficient for dudx and dudy is the same
                double dudx_z_coeff = GetBeta(poly_val, kernelSize, interpolant, z_coordinate, iLagIntz + iz - startz);
                for (int j = 0; j < components; j++)
                {
                    // dudx computation
                    a[j] += dudx_z_coeff * b[j];
                    // dudy computation
                    a[j + dimensions] += dudx_z_coeff * b[j + dimensions];
                    // dudz computation
                    a[j + 2 * dimensions] += dudz_z_coeff * b[j + 2 * dimensions];
                }
            }

            for (int i = 0; i < a.Length; i++)
            {
                a[i] /= Dx;
            }

            Console.WriteLine("duxdx={0} duydx={1} duzdx={2}", a[0], a[1], a[2]);
            Console.WriteLine("duxdy={0} duydy={1} duzdy={2}", a[3], a[4], a[5]);
            Console.WriteLine("duxdz={0} duydz={1} duzdz={2}", a[6], a[7], a[8]);

            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        public static void ComputeSplinesHessian()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();

            int cube_width = 4;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            DateTime startTime, stopTime;
            startTime = DateTime.Now;
            byte[] data = service.GetRawVelocity(authToken, "mhd1024", 0.35f, 2, 2, 2, cube_width, cube_width, cube_width);
            stopTime = DateTime.Now;
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("X={0} Y={1} Z={2}", BitConverter.ToSingle(data, 0), BitConverter.ToSingle(data, 4), BitConverter.ToSingle(data, 8));

            int dimensions = 3;
            int kernelSize = 4;
            int derivative = 2;
            double Dx = Math.PI * 2.0 / 1024.0;
            //double Dy = Math.PI * 2.0 / 1024.0;
            //double Dz = Math.PI * 2.0 / 1024.0;
            double[] poly_val = new double[dimensions * kernelSize * (derivative + 1)];

            for (int i = 0; i <= derivative; i++)
            {
                ComputeBetas(TurbulenceOptions.SpatialInterpolation.M2Q4, i, 0.00001, poly_val, (i * dimensions) * kernelSize);
                ComputeBetas(TurbulenceOptions.SpatialInterpolation.M2Q4, i, 0.00001, poly_val, (i * dimensions + 1) * kernelSize);
                ComputeBetas(TurbulenceOptions.SpatialInterpolation.M2Q4, i, 0.00001, poly_val, (i * dimensions + 2) * kernelSize);
            }


            int off0 = 0;
            int x_coordinate = 0;
            int y_coordinate = 1;
            int z_coordinate = 2;
            int hessian_components = 6;
            int interpolant = 0;
            int first_derivative = 1;
            int second_derivative = 2;
            int components = 3;
            int startx = 0, starty = 0, startz = 0, endx = 3, endy = 3, endz = 3;
            int iLagIntx = 0, iLagInty = 0, iLagIntz = 0;

            double[] a = new double[hessian_components * components];

            for (int iz = startz; iz <= endz; iz++)
            {
                double[] b = new double[hessian_components * components];
                int off1 = off0 + iz * cube_width * cube_width * components;
                for (int iy = starty; iy <= endy; iy++)
                {
                    double[] c = new double[hessian_components * components];
                    int off = off1 + iy * cube_width * components;
                    for (int ix = startx; ix <= endx; ix++)
                    {
                        double d2udxdx_x_coeff = GetBeta(poly_val, kernelSize, second_derivative, x_coordinate, iLagIntx + ix - startx);
                        //double d2udxdy_x_coeff = GetBeta(poly_val, kernelSize, first_derivative, x_coordinate, iLagIntx + ix - startx);
                        //double d2udydz_x_coeff = GetBeta(poly_val, kernelSize, interpolant, x_coordinate, iLagIntx + ix - startx);
                        for (int j = 0; j < components; j++)
                        {
                            // d2udxdx computation
                            c[j * hessian_components] += d2udxdx_x_coeff * BitConverter.ToSingle(data, sizeof(float) * (off + j));
                        }
                        off += components;
                    }
                    //double d2udydy_y_coeff = GetBeta(poly_val, kernelSize, second_derivative, y_coordinate, iLagInty + iy - starty);
                    //double d2udxdy_y_coeff = GetBeta(poly_val, kernelSize, first_derivative, y_coordinate, iLagInty + iy - starty);
                    double d2udxdz_y_coeff = GetBeta(poly_val, kernelSize, interpolant, y_coordinate, iLagInty + iy - starty);
                    for (int j = 0; j < components; j++)
                    {
                        // d2udxdx computation
                        b[j * hessian_components] += d2udxdz_y_coeff * c[j * hessian_components];
                    }
                }
                //double d2udzdz_z_coeff = GetBeta(poly_val, kernelSize, second_derivative, z_coordinate, iLagIntz + iz - startz);
                //double d2udxdz_z_coeff = GetBeta(poly_val, kernelSize, first_derivative, z_coordinate, iLagIntz + iz - startz);
                double d2udxdy_z_coeff = GetBeta(poly_val, kernelSize, interpolant, z_coordinate, iLagIntz + iz - startz);
                for (int j = 0; j < components; j++)
                {
                    // d2udxdx computation
                    a[j * hessian_components] += d2udxdy_z_coeff * b[j * hessian_components];
                }
            }

            for (int i = 0; i < a.Length; i++)
            {
                a[i] /= Dx * Dx;
            }

            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={2} d2uxdydz={2} d2uxdzdz={2}", a[0], a[1], a[2], a[3], a[4], a[5]);
            Console.WriteLine("d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={2} d2uydydz={2} d2uydzdz={2}", a[6], a[7], a[8], a[9], a[10], a[11]);
            Console.WriteLine("d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={2} d2uzdydz={2} d2uzdzdz={2}", a[12], a[13], a[14], a[15], a[16], a[17]);

            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        private static void ComputeBetas(TurbulenceOptions.SpatialInterpolation spatialInterp, int deriv, double x, double[] poly_val, int offset)
        {
            switch (spatialInterp)
            {
                case TurbulenceOptions.SpatialInterpolation.M1Q4:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (-1.0 / 2.0 * x + 1) - 1.0 / 2.0);
                            poly_val[offset + 1] = Math.Pow(x, 2.0) * ((3.0 / 2.0) * x - 5.0 / 2.0) + 1;
                            poly_val[offset + 2] = x * (x * (-3.0 / 2.0 * x + 2) + 1.0 / 2.0);
                            poly_val[offset + 3] = Math.Pow(x, 2) * ((1.0 / 2.0) * x - 1.0 / 2.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (-3.0 / 2.0 * x + 2) - 1.0 / 2.0;
                            poly_val[offset + 1] = x * ((9.0 / 2.0) * x - 5);
                            poly_val[offset + 2] = x * (-9.0 / 2.0 * x + 4) + 1.0 / 2.0;
                            poly_val[offset + 3] = x * ((3.0 / 2.0) * x - 1);
                            break;
                        case 2:
                            poly_val[offset + 0] = -3 * x + 2;
                            poly_val[offset + 1] = 9 * x - 5;
                            poly_val[offset + 2] = -9 * x + 4;
                            poly_val[offset + 3] = 3 * x - 1;
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q6:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * ((1.0 / 12.0) * x - 1.0 / 6.0) + 1.0 / 12.0);
                            poly_val[offset + 1] = x * (x * (-7.0 / 12.0 * x + 5.0 / 4.0) - 2.0 / 3.0);
                            poly_val[offset + 2] = Math.Pow(x, 2) * ((4.0 / 3.0) * x - 7.0 / 3.0) + 1;
                            poly_val[offset + 3] = x * (x * (-4.0 / 3.0 * x + 5.0 / 3.0) + 2.0 / 3.0);
                            poly_val[offset + 4] = x * (x * ((7.0 / 12.0) * x - 1.0 / 2.0) - 1.0 / 12.0);
                            poly_val[offset + 5] = Math.Pow(x, 2) * (-1.0 / 12.0 * x + 1.0 / 12.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * ((1.0 / 4.0) * x - 1.0 / 3.0) + 1.0 / 12.0;
                            poly_val[offset + 1] = x * (-7.0 / 4.0 * x + 5.0 / 2.0) - 2.0 / 3.0;
                            poly_val[offset + 2] = x * (4 * x - 14.0 / 3.0);
                            poly_val[offset + 3] = x * (-4 * x + 10.0 / 3.0) + 2.0 / 3.0;
                            poly_val[offset + 4] = x * ((7.0 / 4.0) * x - 1) - 1.0 / 12.0;
                            poly_val[offset + 5] = x * (-1.0 / 4.0 * x + 1.0 / 6.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = (1.0 / 2.0) * x - 1.0 / 3.0;
                            poly_val[offset + 1] = -7.0 / 2.0 * x + 5.0 / 2.0;
                            poly_val[offset + 2] = 8 * x - 14.0 / 3.0;
                            poly_val[offset + 3] = -8 * x + 10.0 / 3.0;
                            poly_val[offset + 4] = (7.0 / 2.0) * x - 1;
                            poly_val[offset + 5] = -1.0 / 2.0 * x + 1.0 / 6.0;
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q8:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (-1.0 / 60.0 * x + 1.0 / 30.0) - 1.0 / 60.0);
                            poly_val[offset + 1] = x * (x * ((2.0 / 15.0) * x - 17.0 / 60.0) + 3.0 / 20.0);
                            poly_val[offset + 2] = x * (x * (-3.0 / 5.0 * x + 27.0 / 20.0) - 3.0 / 4.0);
                            poly_val[offset + 3] = Math.Pow(x, 2) * ((5.0 / 4.0) * x - 9.0 / 4.0) + 1;
                            poly_val[offset + 4] = x * (x * (-5.0 / 4.0 * x + 3.0 / 2.0) + 3.0 / 4.0);
                            poly_val[offset + 5] = x * (x * ((3.0 / 5.0) * x - 9.0 / 20.0) - 3.0 / 20.0);
                            poly_val[offset + 6] = x * (x * (-2.0 / 15.0 * x + 7.0 / 60.0) + 1.0 / 60.0);
                            poly_val[offset + 7] = Math.Pow(x, 2) * ((1.0 / 60.0) * x - 1.0 / 60.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (-1.0 / 20.0 * x + 1.0 / 15.0) - 1.0 / 60.0;
                            poly_val[offset + 1] = x * ((2.0 / 5.0) * x - 17.0 / 30.0) + 3.0 / 20.0;
                            poly_val[offset + 2] = x * (-9.0 / 5.0 * x + 27.0 / 10.0) - 3.0 / 4.0;
                            poly_val[offset + 3] = x * ((15.0 / 4.0) * x - 9.0 / 2.0);
                            poly_val[offset + 4] = x * (-15.0 / 4.0 * x + 3) + 3.0 / 4.0;
                            poly_val[offset + 5] = x * ((9.0 / 5.0) * x - 9.0 / 10.0) - 3.0 / 20.0;
                            poly_val[offset + 6] = x * (-2.0 / 5.0 * x + 7.0 / 30.0) + 1.0 / 60.0;
                            poly_val[offset + 7] = x * ((1.0 / 20.0) * x - 1.0 / 30.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = -1.0 / 10.0 * x + 1.0 / 15.0;
                            poly_val[offset + 1] = (4.0 / 5.0) * x - 17.0 / 30.0;
                            poly_val[offset + 2] = -18.0 / 5.0 * x + 27.0 / 10.0;
                            poly_val[offset + 3] = (15.0 / 2.0) * x - 9.0 / 2.0;
                            poly_val[offset + 4] = -15.0 / 2.0 * x + 3;
                            poly_val[offset + 5] = (18.0 / 5.0) * x - 9.0 / 10.0;
                            poly_val[offset + 6] = -4.0 / 5.0 * x + 7.0 / 30.0;
                            poly_val[offset + 7] = (1.0 / 10.0) * x - 1.0 / 30.0;
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M2Q4:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (x - 5.0 / 2.0) + 3.0 / 2.0) + 1.0 / 2.0) - 1.0 / 2.0);
                            poly_val[offset + 1] = Math.Pow(x, 2) * (x * (x * (-3 * x + 15.0 / 2.0) - 9.0 / 2.0) - 1) + 1;
                            poly_val[offset + 2] = x * (x * (x * (x * (3 * x - 15.0 / 2.0) + 9.0 / 2.0) + 1.0 / 2.0) + 1.0 / 2.0);
                            poly_val[offset + 3] = Math.Pow(x, 3) * (x * (-x + 5.0 / 2.0) - 3.0 / 2.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (5 * x - 10) + 9.0 / 2.0) + 1) - 1.0 / 2.0;
                            poly_val[offset + 1] = x * (x * (x * (-15 * x + 30) - 27.0 / 2.0) - 2);
                            poly_val[offset + 2] = x * (x * (x * (15 * x - 30) + 27.0 / 2.0) + 1) + 1.0 / 2.0;
                            poly_val[offset + 3] = Math.Pow(x, 2) * (x * (-5 * x + 10) - 9.0 / 2.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (20 * x - 30) + 9) + 1;
                            poly_val[offset + 1] = x * (x * (-60 * x + 90) - 27) - 2;
                            poly_val[offset + 2] = x * (x * (60 * x - 90) + 27) + 1;
                            poly_val[offset + 3] = x * (x * (-20 * x + 30) - 9);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M2Q6:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (-5.0 / 24.0 * x + 13.0 / 24.0) - 3.0 / 8.0) - 1.0 / 24.0) + 1.0 / 12.0);
                            poly_val[offset + 1] = x * (x * (x * (x * ((25.0 / 24.0) * x - 8.0 / 3.0) + 13.0 / 8.0) + 2.0 / 3.0) - 2.0 / 3.0);
                            poly_val[offset + 2] = Math.Pow(x, 2) * (x * (x * (-25.0 / 12.0 * x + 21.0 / 4.0) - 35.0 / 12.0) - 5.0 / 4.0) + 1;
                            poly_val[offset + 3] = x * (x * (x * (x * ((25.0 / 12.0) * x - 31.0 / 6.0) + 11.0 / 4.0) + 2.0 / 3.0) + 2.0 / 3.0);
                            poly_val[offset + 4] = x * (x * (x * (x * (-25.0 / 24.0 * x + 61.0 / 24.0) - 11.0 / 8.0) - 1.0 / 24.0) - 1.0 / 12.0);
                            poly_val[offset + 5] = Math.Pow(x, 3) * (x * ((5.0 / 24.0) * x - 1.0 / 2.0) + 7.0 / 24.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (-25.0 / 24.0 * x + 13.0 / 6.0) - 9.0 / 8.0) - 1.0 / 12.0) + 1.0 / 12.0;
                            poly_val[offset + 1] = x * (x * (x * ((125.0 / 24.0) * x - 32.0 / 3.0) + 39.0 / 8.0) + 4.0 / 3.0) - 2.0 / 3.0;
                            poly_val[offset + 2] = x * (x * (x * (-125.0 / 12.0 * x + 21) - 35.0 / 4.0) - 5.0 / 2.0);
                            poly_val[offset + 3] = x * (x * (x * ((125.0 / 12.0) * x - 62.0 / 3.0) + 33.0 / 4.0) + 4.0 / 3.0) + 2.0 / 3.0;
                            poly_val[offset + 4] = x * (x * (x * (-125.0 / 24.0 * x + 61.0 / 6.0) - 33.0 / 8.0) - 1.0 / 12.0) - 1.0 / 12.0;
                            poly_val[offset + 5] = Math.Pow(x, 2) * (x * ((25.0 / 24.0) * x - 2) + 7.0 / 8.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (-25.0 / 6.0 * x + 13.0 / 2.0) - 9.0 / 4.0) - 1.0 / 12.0;
                            poly_val[offset + 1] = x * (x * ((125.0 / 6.0) * x - 32) + 39.0 / 4.0) + 4.0 / 3.0;
                            poly_val[offset + 2] = x * (x * (-125.0 / 3.0 * x + 63) - 35.0 / 2.0) - 5.0 / 2.0;
                            poly_val[offset + 3] = x * (x * ((125.0 / 3.0) * x - 62) + 33.0 / 2.0) + 4.0 / 3.0;
                            poly_val[offset + 4] = x * (x * (-125.0 / 6.0 * x + 61.0 / 2.0) - 33.0 / 4.0) - 1.0 / 12.0;
                            poly_val[offset + 5] = x * (x * ((25.0 / 6.0) * x - 6) + 7.0 / 4.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M2Q8:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * ((2.0 / 45.0) * x - 7.0 / 60.0) + 1.0 / 12.0) + 1.0 / 180.0) - 1.0 / 60.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (-23.0 / 72.0 * x + 61.0 / 72.0) - 217.0 / 360.0) - 3.0 / 40.0) + 3.0 / 20.0);
                            poly_val[offset + 2] = x * (x * (x * (x * ((39.0 / 40.0) * x - 51.0 / 20.0) + 63.0 / 40.0) + 3.0 / 4.0) - 3.0 / 4.0);
                            poly_val[offset + 3] = Math.Pow(x, 2) * (x * (x * (-59.0 / 36.0 * x + 25.0 / 6.0) - 13.0 / 6.0) - 49.0 / 36.0) + 1;
                            poly_val[offset + 4] = x * (x * (x * (x * ((59.0 / 36.0) * x - 145.0 / 36.0) + 17.0 / 9.0) + 3.0 / 4.0) + 3.0 / 4.0);
                            poly_val[offset + 5] = x * (x * (x * (x * (-39.0 / 40.0 * x + 93.0 / 40.0) - 9.0 / 8.0) - 3.0 / 40.0) - 3.0 / 20.0);
                            poly_val[offset + 6] = x * (x * (x * (x * ((23.0 / 72.0) * x - 3.0 / 4.0) + 49.0 / 120.0) + 1.0 / 180.0) + 1.0 / 60.0);
                            poly_val[offset + 7] = Math.Pow(x, 3) * (x * (-2.0 / 45.0 * x + 19.0 / 180.0) - 11.0 / 180.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * ((2.0 / 9.0) * x - 7.0 / 15.0) + 1.0 / 4.0) + 1.0 / 90.0) - 1.0 / 60.0;
                            poly_val[offset + 1] = x * (x * (x * (-115.0 / 72.0 * x + 61.0 / 18.0) - 217.0 / 120.0) - 3.0 / 20.0) + 3.0 / 20.0;
                            poly_val[offset + 2] = x * (x * (x * ((39.0 / 8.0) * x - 51.0 / 5.0) + 189.0 / 40.0) + 3.0 / 2.0) - 3.0 / 4.0;
                            poly_val[offset + 3] = x * (x * (x * (-295.0 / 36.0 * x + 50.0 / 3.0) - 13.0 / 2.0) - 49.0 / 18.0);
                            poly_val[offset + 4] = x * (x * (x * ((295.0 / 36.0) * x - 145.0 / 9.0) + 17.0 / 3.0) + 3.0 / 2.0) + 3.0 / 4.0;
                            poly_val[offset + 5] = x * (x * (x * (-39.0 / 8.0 * x + 93.0 / 10.0) - 27.0 / 8.0) - 3.0 / 20.0) - 3.0 / 20.0;
                            poly_val[offset + 6] = x * (x * (x * ((115.0 / 72.0) * x - 3) + 49.0 / 40.0) + 1.0 / 90.0) + 1.0 / 60.0;
                            poly_val[offset + 7] = Math.Pow(x, 2) * (x * (-2.0 / 9.0 * x + 19.0 / 45.0) - 11.0 / 60.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * ((8.0 / 9.0) * x - 7.0 / 5.0) + 1.0 / 2.0) + 1.0 / 90.0;
                            poly_val[offset + 1] = x * (x * (-115.0 / 18.0 * x + 61.0 / 6.0) - 217.0 / 60.0) - 3.0 / 20.0;
                            poly_val[offset + 2] = x * (x * ((39.0 / 2.0) * x - 153.0 / 5.0) + 189.0 / 20.0) + 3.0 / 2.0;
                            poly_val[offset + 3] = x * (x * (-295.0 / 9.0 * x + 50) - 13) - 49.0 / 18.0;
                            poly_val[offset + 4] = x * (x * ((295.0 / 9.0) * x - 145.0 / 3.0) + 34.0 / 3.0) + 3.0 / 2.0;
                            poly_val[offset + 5] = x * (x * (-39.0 / 2.0 * x + 279.0 / 10.0) - 27.0 / 4.0) - 3.0 / 20.0;
                            poly_val[offset + 6] = x * (x * ((115.0 / 18.0) * x - 9) + 49.0 / 20.0) + 1.0 / 90.0;
                            poly_val[offset + 7] = x * (x * (-8.0 / 9.0 * x + 19.0 / 15.0) - 11.0 / 30.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
        }

        private static double GetBeta(double[] poly_val, int kernel_size, int derivative, int coordinate, int position)
        {
            int dimensions = 3;
            return poly_val[(dimensions * derivative + coordinate) * kernel_size + position];
        }

        public static void TestDensityHessian()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();
            service.Timeout = -1;

            int pointsize = 1;
            float time = 35.0f;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.PressureHessian[] density_hessian;
            turbulence.PressureHessian computed_hessian;
            points[0] = new turbulence.Point3();
            points[0].x = 10.0146792005238f - (float)(2.0 * Math.PI);
            points[0].y = 6.55945318720046f - (float)(2.0 * Math.PI);
            points[0].z = 5.02831048374490f;
            float dx = 2.0f * (float)Math.PI / 1024;
            float filterwidth = 7.0f * dx;
            float spacing = 4.0f * dx;
            turbulence.PressureHessian expected_d_hessian = new turbulence.PressureHessian();
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            DateTime startTime, stopTime;
            expected_d_hessian.d2pdxdx = 14.48022f;
            expected_d_hessian.d2pdxdy = 3.544189f;
            expected_d_hessian.d2pdxdz = 15.42188f;
            expected_d_hessian.d2pdydy = -0.6557617f;
            expected_d_hessian.d2pdydz = 3.514099f;
            expected_d_hessian.d2pdzdz = 19.10449f;
            startTime = DateTime.Now;
            density_hessian = service.GetDensityHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(density_hessian[0], expected_d_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                density_hessian[0].d2pdxdx, density_hessian[0].d2pdxdy, density_hessian[0].d2pdxdz,
                density_hessian[0].d2pdydy, density_hessian[0].d2pdydz, density_hessian[0].d2pdzdz);

            computed_hessian = ComputeScalarHessian(points[0].x, points[0].y, points[0].z, "density");
            CompareAndReport(density_hessian[0], computed_hessian);
        }

        public static turbulence.PressureHessian ComputeScalarHessian(float x_coord, float y_coord, float z_coord, string field)
        {
            SqlConnection conn = new SqlConnection("Server=dsp048;Database=turblib;Asynchronous Processing=true;Trusted_Connection=True;Pooling=false;");
            conn.Open();
            TurbDataTable turbTable = TurbDataTable.GetTableInfo("dsp048", "mixingdb02", field, 16, conn);
            Computations computations = new Computations(turbTable);
            float[] particle = new float[] { x_coord, y_coord, z_coord };
            int[] base_coordinates = new int[] { 808, 40, 600 };
            Morton3D key = new Morton3D(base_coordinates[0], base_coordinates[1], base_coordinates[2]);
            TurbulenceBlob blob = new TurbulenceBlob(turbTable);

            float time = 15.0f;
            int timestep = 251;
            int headerSize = 192;
            int start_component = 0;
            int pointDataSize = sizeof(double);
            long offset0 = (long)headerSize +
                          ((long)base_coordinates[2] +
                           (long)base_coordinates[1] * (long)turbTable.GridResolution[2] +
                           (long)base_coordinates[0] * (long)turbTable.GridResolution[1] * (long)turbTable.GridResolution[2]) *
                           (long)pointDataSize;
            byte[] rawdata;
            
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();
            service.Timeout = -1;
            if (field == "density")
            {
                rawdata = service.GetRawDensity("edu.jhu.pha.turbulence.testing-201406", "mixing", time, base_coordinates[2], base_coordinates[1], base_coordinates[0], 16, 16, 16);
            }
            else if (field.Contains("pr"))
            {
                rawdata = service.GetRawPressure("edu.jhu.pha.turbulence.testing-201406", "mixing", time, base_coordinates[2], base_coordinates[1], base_coordinates[0], 16, 16, 16);
            }
            else
            {
                throw new Exception("Invalid field specified!");
            }

            //byte[] data_value = new byte[pointDataSize];
            //try
            //{
            //    string filename = GetMixingFileName(timestep);
            //    FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            //    int bytesToRead = blobDim * pointDataSize;
            //    int destinationIndex = 0;

            //    if (File.Exists(filename))
            //    {
            //        for (int z = 0; z < blobDim; z++)
            //        {
            //            for (int y = 0; y < blobDim; y++)
            //            {
            //                for (int x = 0; x < blobDim; x++)
            //                {
            //                    for (int c = start_component; c < start_component + turbTable.Components; c++)
            //                    {
            //                        long offset = offset0 + (long)c * turbTable.GridResolution[2] * turbTable.GridResolution[1] * turbTable.GridResolution[0] * pointDataSize +
            //                            ((long)x + (long)y * turbTable.GridResolution[2] + (long)z * turbTable.GridResolution[2] * turbTable.GridResolution[1]) * pointDataSize;
            //                        fs.Seek(offset, SeekOrigin.Begin);
            //                        int n = fs.Read(data_value, 0, pointDataSize);
            //                        if (pointDataSize == sizeof(double))
            //                        {
            //                            float float_value = (float)BitConverter.ToDouble(data_value, 0);
            //                            Array.Copy(BitConverter.GetBytes(float_value), 0, rawdata, destinationIndex, sizeof(float));
            //                            destinationIndex += sizeof(float);
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    else
            //    {
            //        throw new System.IO.IOException(String.Format("File not found: {0}!", filename));
            //    }
            //    fs.Close();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Error: {0}\n Inner Exception: {1}", ex, ex.InnerException);
            //    int milliseconds = 2000;
            //    Console.WriteLine("Retring after {0} seconds", milliseconds / 1000);
            //    System.Threading.Thread.Sleep(milliseconds);
            //    return null;
            //}

            blob.Setup(251, key, rawdata);

            double[] result = computations.CalcPressureHessian(blob, particle, TurbulenceOptions.SpatialInterpolation.None_Fd4);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                result[0], result[1], result[2], result[3], result[4], result[5]);

            turbulence.PressureHessian p_hessian_result = new turbulence.PressureHessian();
            p_hessian_result.d2pdxdx = (float)result[0];
            p_hessian_result.d2pdxdy = (float)result[1];
            p_hessian_result.d2pdxdz = (float)result[2];
            p_hessian_result.d2pdydy = (float)result[3];
            p_hessian_result.d2pdydz = (float)result[4];
            p_hessian_result.d2pdzdz = (float)result[5];

            return p_hessian_result;
        }

        private static string GetMixingFileName(float time)
        {
            string data_dir = @"\\dss004\tdb_livescu\";
            return String.Format(@"{0}rstrt.{1:0000}.bin", data_dir, time);
        }

        private static string GetRMHDFileName(int time, long zindex, string field)
        {
            if (zindex < 0x400000 || (zindex >= 0x800000 && zindex < 0xc00000))
            {
                return String.Format(@"T:\RMHD_{2}_t{0:x4}_z{1:x7}", time, zindex, field);
            }
            else
            {
                return String.Format(@"S:\RMHD_{2}_t{0:x4}_z{1:x7}", time, zindex, field);
            }
        }

        public static void ReadFileData()
        {
            int blobDim = 8;
            int components = 2;
            int timestep = 0;
            string field = "u";
            int cubeSize = blobDim * blobDim * blobDim;
            long zindex = 467141632;
            long startOfFile = zindex / cubeSize & -262144;
            long offset = (zindex - startOfFile * cubeSize) * components * sizeof(float);

            byte[] atom = new byte[cubeSize * components * sizeof(float)];
            try
            {
                string filename = GetRMHDFileName(timestep, startOfFile, field);
                FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

                if (File.Exists(filename))
                {
                    fs.Seek(offset, SeekOrigin.Begin);
                    int n = fs.Read(atom, 0, cubeSize * components * sizeof(float));
                }
                else
                {
                    throw new System.IO.IOException(String.Format("File not found: {0}!", filename));
                }
                fs.Close();

                int startIndex = 0;
                for (int z = 0; z < blobDim; z++)
                {
                    for (int y = 0; y < blobDim; y++)
                    {
                        for (int x = 0; x < blobDim; x++)
                        {
                            for (int c = 0; c < components; c++)
                            {
                                Console.WriteLine("[{0}, {1}, {2}, {3}] = {4}", z, y, x, c, BitConverter.ToSingle(atom, startIndex));
                                startIndex += sizeof(float);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}\n Inner Exception: {1}", ex, ex.InnerException);
            }
        }

        public static void AllTest()
        {
            TestIsotropicDataset();
            TestMHDDataset();
            TestChannelDataset();
            TestMixingDataset();

            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        public static void TestIsotropicDataset()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();
            service.Timeout = -1;

            int pointsize = 1;
            float time = 0.364f;
            float time_for_fine = 0.0047f;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.Vector3[] result;
            turbulence.Point3[] position;
            turbulence.Pressure[] pressure;
            turbulence.VelocityGradient[] gradient;
            turbulence.VelocityHessian[] hessian;
            turbulence.PressureHessian[] pressure_hessian;
            turbulence.SGSTensor[] sgs_tensor;
            turbulence.ThresholdInfo[] points_above_threshold;
            points[0] = new turbulence.Point3();
            points[0].x = 2.876447f;
            points[0].y = 3.365972f;
            points[0].z = 1.370830f;
            float dx = 2.0f * (float)Math.PI / 1024;
            float filterwidth = 7.0f * dx;
            float spacing = 4.0f * dx;
            turbulence.Vector3 expected = new turbulence.Vector3();
            turbulence.VelocityGradient expected_gradient = new turbulence.VelocityGradient();
            turbulence.VelocityHessian expected_hessian = new turbulence.VelocityHessian();
            turbulence.SGSTensor expected_sgs_tensor = new turbulence.SGSTensor();
            turbulence.PressureHessian expected_p_hessian = new turbulence.PressureHessian();
            float expected_pressure, raw_pressure, expected_norm;
            byte[] data;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            DateTime startTime, stopTime;

            expected.x = -0.351615f;
            expected.y = 1.535706f;
            expected.z = -0.4778695f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.3578765f;
            expected.y = 1.547667f;
            expected.z = -0.4818715f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.3578992f;
            expected.y = 1.547627f;
            expected.z = -0.4818901f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Lag6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.3579338f;
            expected.y = 1.547621f;
            expected.z = -0.4818015f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.3579338f;
            expected.y = 1.547621f;
            expected.z = -0.4818015f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.6802751f;
            expected.y = 1.457929f;
            expected.z = -0.2215174f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "isotropic1024fine", time_for_fine, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.3674078f;
            expected.y = 1.555561f;
            expected.z = -0.4347775f;
            startTime = DateTime.Now;
            result = service.GetBoxFilter(authToken, "isotropic1024", "velocity", time, filterwidth, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected_gradient.duxdx = -0.0926466f;
            expected_gradient.duxdy = 3.746106f;
            expected_gradient.duxdz = 1.402954f;
            expected_gradient.duydx = -3.82403f;
            expected_gradient.duydy = 1.132124f;
            expected_gradient.duydz = 3.542008f;
            expected_gradient.duzdx = 1.254507f;
            expected_gradient.duzdy = 0.8053942f;
            expected_gradient.duzdz = -1.019077f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -0.09009999f;
            expected_gradient.duxdy = 3.741745f;
            expected_gradient.duxdz = 1.38176f;
            expected_gradient.duydx = -3.831586f;
            expected_gradient.duydy = 1.095115f;
            expected_gradient.duydz = 3.540314f;
            expected_gradient.duzdx = 1.319867f;
            expected_gradient.duzdy = 0.8034983f;
            expected_gradient.duzdz = -0.9737129f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -0.0891763f;
            expected_gradient.duxdy = 3.738734f;
            expected_gradient.duxdz = 1.369854f;
            expected_gradient.duydx = -3.838008f;
            expected_gradient.duydy = 1.072176f;
            expected_gradient.duydz = 3.537384f;
            expected_gradient.duzdx = 1.353137f;
            expected_gradient.duzdy = 0.7957377f;
            expected_gradient.duzdz = -0.9494324f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -0.1055225f;
            expected_gradient.duxdy = 4.276608f;
            expected_gradient.duxdz = 1.769272f;
            expected_gradient.duydx = -4.347949f;
            expected_gradient.duydy = 0.9394213f;
            expected_gradient.duydz = 3.761047f;
            expected_gradient.duzdx = 0.03522807f;
            expected_gradient.duzdy = -0.4218047f;
            expected_gradient.duzdz = -0.8208237f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -0.0926466f;
            expected_gradient.duxdy = 3.746106f;
            expected_gradient.duxdz = 1.402954f;
            expected_gradient.duydx = -3.82403f;
            expected_gradient.duydy = 1.132124f;
            expected_gradient.duydz = 3.542023f;
            expected_gradient.duzdx = 1.254507f;
            expected_gradient.duzdy = 0.8053942f;
            expected_gradient.duzdz = -1.019077f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -9.055465f;
            expected_gradient.duxdy = 31.79656f;
            expected_gradient.duxdz = -28.82988f;
            expected_gradient.duydx = -18.11995f;
            expected_gradient.duydy = 7.907279f;
            expected_gradient.duydz = 7.147011f;
            expected_gradient.duzdx = 18.52938f;
            expected_gradient.duzdy = -5.454676f;
            expected_gradient.duzdz = 0.8964107f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "isotropic1024fine", time_for_fine, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -0.04148462f;
            expected_gradient.duxdy = 1.542056f;
            expected_gradient.duxdz = 0.7975092f;
            expected_gradient.duydx = -1.829895f;
            expected_gradient.duydy = 0.4081805f;
            expected_gradient.duydz = 1.757755f;
            expected_gradient.duzdx = -0.02720547f;
            expected_gradient.duzdy = -0.4895201f;
            expected_gradient.duzdz = -0.01574463f;
            startTime = DateTime.Now;
            gradient = service.GetBoxFilterGradient(authToken, "isotropic1024", "velocity", time, filterwidth, spacing, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_hessian.d2uxdxdx = 51.68277f;
            expected_hessian.d2uxdxdy = -63.03896f;
            expected_hessian.d2uxdxdz = -50.65637f;
            expected_hessian.d2uxdydy = -274.330933f;
            expected_hessian.d2uxdydz = -57.20203f;
            expected_hessian.d2uxdzdz = 23.58963f;
            expected_hessian.d2uydxdx = 377.219818f;
            expected_hessian.d2uydxdy = 93.62816f;
            expected_hessian.d2uydxdz = -21.88168f;
            expected_hessian.d2uydydy = 27.22743f;
            expected_hessian.d2uydydz = -54.63118f;
            expected_hessian.d2uydzdz = 14.19235f;
            expected_hessian.d2uzdxdx = 164.412155f;
            expected_hessian.d2uzdxdy = 141.8988f;
            expected_hessian.d2uzdxdz = -131.560226f;
            expected_hessian.d2uzdydy = 409.319427f;
            expected_hessian.d2uzdydz = 50.621f;
            expected_hessian.d2uzdzdz = 71.897934f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz); 
            expected_hessian.d2uxdxdx = 52.6854324f;
            expected_hessian.d2uxdxdy = -61.1333f;
            expected_hessian.d2uxdxdz = -52.35629f;
            expected_hessian.d2uxdydy = -277.7843f;
            expected_hessian.d2uxdydz = -61.59891f;
            expected_hessian.d2uxdzdz = 27.1526356f;
            expected_hessian.d2uydxdx = 388.3439f;
            expected_hessian.d2uydxdy = 93.1895f;
            expected_hessian.d2uydxdz = -21.458992f;
            expected_hessian.d2uydydy = 29.0007267f;
            expected_hessian.d2uydydz = -52.23146f;
            expected_hessian.d2uydzdz = 12.7515526f;
            expected_hessian.d2uzdxdx = 156.926422f;
            expected_hessian.d2uzdxdy = 145.308212f;
            expected_hessian.d2uzdxdz = -131.9255f;
            expected_hessian.d2uzdydy = 407.608551f;
            expected_hessian.d2uzdydz = 48.6270676f;
            expected_hessian.d2uzdzdz = 68.24273f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = 51.5374222f;
            expected_hessian.d2uxdxdy = -59.9753f;
            expected_hessian.d2uxdxdz = -52.9094963f;
            expected_hessian.d2uxdydy = -274.793243f;
            expected_hessian.d2uxdydz = -63.89513f;
            expected_hessian.d2uxdzdz = 24.2341766f;
            expected_hessian.d2uydxdx = 378.365265f;
            expected_hessian.d2uydxdy = 92.43778f;
            expected_hessian.d2uydxdz = -21.1024876f;
            expected_hessian.d2uydydy = 26.5668087f;
            expected_hessian.d2uydydz = -50.7736053f;
            expected_hessian.d2uydzdz = 14.2829456f;
            expected_hessian.d2uzdxdx = 162.506241f;
            expected_hessian.d2uzdxdy = 146.645447f;
            expected_hessian.d2uzdxdz = -131.181412f;
            expected_hessian.d2uzdydy = 408.55835f;
            expected_hessian.d2uzdydz = 46.86449f;
            expected_hessian.d2uzdzdz = 70.83705f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = 65.9810944f;
            expected_hessian.d2uxdxdy = -59.8785973f;
            expected_hessian.d2uxdxdz = -29.1473351f;
            expected_hessian.d2uxdydy = -232.453537f;
            expected_hessian.d2uxdydz = -53.8638535f;
            expected_hessian.d2uxdzdz = 9.408873f;
            expected_hessian.d2uydxdx = 289.46286f;
            expected_hessian.d2uydxdy = 86.66852f;
            expected_hessian.d2uydxdz = 7.217052f;
            expected_hessian.d2uydydy = 1.86880577f;
            expected_hessian.d2uydydz = -96.79661f;
            expected_hessian.d2uydzdz = -4.15394449f;
            expected_hessian.d2uzdxdx = 227.732971f;
            expected_hessian.d2uzdxdy = 155.191055f;
            expected_hessian.d2uzdxdz = -135.825165f;
            expected_hessian.d2uzdydy = 468.435364f;
            expected_hessian.d2uzdydz = 47.7437668f;
            expected_hessian.d2uzdzdz = 97.36317f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = 51.6827736f;
            expected_hessian.d2uxdxdy = -63.03896f;
            expected_hessian.d2uxdxdz = -50.65637f;
            expected_hessian.d2uxdydy = -274.330933f;
            expected_hessian.d2uxdydz = -57.2020264f;
            expected_hessian.d2uxdzdz = 23.58963f;
            expected_hessian.d2uydxdx = 377.219818f;
            expected_hessian.d2uydxdy = 93.62816f;
            expected_hessian.d2uydxdz = -21.8816814f;
            expected_hessian.d2uydydy = 27.22743f;
            expected_hessian.d2uydydz = -54.63118f;
            expected_hessian.d2uydzdz = 14.1923532f;
            expected_hessian.d2uzdxdx = 164.412155f;
            expected_hessian.d2uzdxdy = 141.898819f;
            expected_hessian.d2uzdxdz = -131.560226f;
            expected_hessian.d2uzdydy = 409.319427f;
            expected_hessian.d2uzdydz = 50.621f;
            expected_hessian.d2uzdzdz = 71.897934f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = -174.133575f;
            expected_hessian.d2uxdxdy = 573.811157f;
            expected_hessian.d2uxdxdz = -736.6959f;
            expected_hessian.d2uxdydy = 809.631042f;
            expected_hessian.d2uxdydz = -900.308167f;
            expected_hessian.d2uxdzdz = 378.2306f;
            expected_hessian.d2uydxdx = -2095.62817f;
            expected_hessian.d2uydxdy = 98.9859543f;
            expected_hessian.d2uydxdz = 418.5114f;
            expected_hessian.d2uydydy = -844.2249f;
            expected_hessian.d2uydydz = 719.766846f;
            expected_hessian.d2uydzdz = -1063.25049f;
            expected_hessian.d2uzdxdx = 246.870682f;
            expected_hessian.d2uzdxdy = 776.1137f;
            expected_hessian.d2uzdxdz = -11.8820257f;
            expected_hessian.d2uzdydy = -437.0833f;
            expected_hessian.d2uzdydz = 180.993271f;
            expected_hessian.d2uzdzdz = 23.2964287f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "isotropic1024fine", time_for_fine, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected.x = -199.058533f;
            expected.y = 418.639618f;
            expected.z = 645.6295f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z); 
            expected.x = -197.946243f;
            expected.y = 430.0962f;
            expected.z = 632.7777f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -199.021652f;
            expected.y = 419.215027f;
            expected.z = 641.9016f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -157.063568f;
            expected.y = 287.1777f;
            expected.z = 793.5315f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -199.058533f;
            expected.y = 418.639618f;
            expected.z = 645.6295f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -3235.17969f;
            expected.y = -4795.217f;
            expected.z = -1547.31384f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "isotropic1024fine", time_for_fine, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected_pressure = 0.2283736f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = 0.2280325f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = 0.2280379f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Lag6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = 0.2280431f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = 0.2280431f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.09444662f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "isotropic1024fine", time_for_fine, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected.x = 0.225501537f;
            expected.y = 0.175536871f;
            expected.z = 0.171236038f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.225099146f;
            expected.y = 0.1774736f;
            expected.z = 0.172369f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.224178433f;
            expected.y = 0.177845f;
            expected.z = 0.173278809f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.187506527f;
            expected.y = 0.185370639f;
            expected.z = 0.173995972f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.187506527f;
            expected.y = 0.185370639f;
            expected.z = 0.173995972f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -4.23830652f;
            expected.y = -5.33472967f;
            expected.z = 3.86238956f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "isotropic1024fine", time_for_fine, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            startTime = DateTime.Now;
            expected_p_hessian.d2pdxdx = 3.74637723f;
            expected_p_hessian.d2pdxdy = -6.42473459f;
            expected_p_hessian.d2pdxdz = -10.1420794f;
            expected_p_hessian.d2pdydy = 4.09902239f;
            expected_p_hessian.d2pdydz = 10.1303205f;
            expected_p_hessian.d2pdzdz = 5.10379076f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = 3.350587f;
            expected_p_hessian.d2pdxdy = -6.79500961f;
            expected_p_hessian.d2pdxdz = -10.123291f;
            expected_p_hessian.d2pdydy = 4.131257f;
            expected_p_hessian.d2pdydz = 10.1883345f;
            expected_p_hessian.d2pdzdz = 5.19432f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = 3.676668f;
            expected_p_hessian.d2pdxdy = -6.94950771f;
            expected_p_hessian.d2pdxdz = -10.0737085f;
            expected_p_hessian.d2pdydy = 3.98353267f;
            expected_p_hessian.d2pdydz = 10.1902695f;
            expected_p_hessian.d2pdzdz = 5.09998846f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = 16.2644958f;
            expected_p_hessian.d2pdxdy = 2.00632739f;
            expected_p_hessian.d2pdxdz = -15.9824228f;
            expected_p_hessian.d2pdydy = 10.8579092f;
            expected_p_hessian.d2pdydz = 10.7292318f;
            expected_p_hessian.d2pdzdz = 9.045428f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = 16.2644958f;
            expected_p_hessian.d2pdxdy = 2.00632739f;
            expected_p_hessian.d2pdxdz = -15.9824228f;
            expected_p_hessian.d2pdydy = 10.8579092f;
            expected_p_hessian.d2pdydz = 10.7292318f;
            expected_p_hessian.d2pdzdz = 9.045428f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "isotropic1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = 944.2842f;
            expected_p_hessian.d2pdxdy = -238.276352f;
            expected_p_hessian.d2pdxdz = -198.241074f;
            expected_p_hessian.d2pdydy = 574.0822f;
            expected_p_hessian.d2pdydz = -485.0709f;
            expected_p_hessian.d2pdzdz = 666.0767f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "isotropic1024fine", time_for_fine, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_pressure = 0.2296961f;
            startTime = DateTime.Now;
            result = service.GetBoxFilter(authToken, "isotropic1024", "pressure", time, filterwidth, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0].x, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", result[0].x);
            expected_sgs_tensor.xx = 0.001444887f;
            expected_sgs_tensor.xy = 0.0004910775f;
            expected_sgs_tensor.xz = -0.0004631896f;
            expected_sgs_tensor.yy = 0.002217596f;
            expected_sgs_tensor.yz = -3.107103E-05f;
            expected_sgs_tensor.zz = 0.0007305829f;
            startTime = DateTime.Now;
            sgs_tensor = service.GetBoxFilterSGS(authToken, "isotropic1024", "velocity", time, filterwidth, points);
            stopTime = DateTime.Now;
            CompareAndReport(sgs_tensor[0], expected_sgs_tensor);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("xx={0} xy={1} xz={2} yy={3} yz={4} zz={5}",
                sgs_tensor[0].xx, sgs_tensor[0].xy, sgs_tensor[0].xz, sgs_tensor[0].yy, sgs_tensor[0].yz, sgs_tensor[0].zz);
            expected_sgs_tensor.xx = 0.0620666f;
            expected_sgs_tensor.xy = -0.004500785f;
            expected_sgs_tensor.xz = -0.02472843f;
            expected_sgs_tensor.yy = 0.03162256f;
            expected_sgs_tensor.yz = -0.009145633f;
            expected_sgs_tensor.zz = 0.0217363f;
            startTime = DateTime.Now;
            sgs_tensor = service.GetBoxFilterSGS(authToken, "isotropic1024fine", "velocity", time_for_fine, filterwidth, points);
            stopTime = DateTime.Now;
            CompareAndReport(sgs_tensor[0], expected_sgs_tensor);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("xx={0} xy={1} xz={2} yy={3} yz={4} zz={5}",
                sgs_tensor[0].xx, sgs_tensor[0].xy, sgs_tensor[0].xz, sgs_tensor[0].yy, sgs_tensor[0].yz, sgs_tensor[0].zz);

            expected.x = -0.3606773f;
            expected.y = 0.750819564f;
            expected.z = -0.49764055f;
            startTime = DateTime.Now;
            data = service.GetRawVelocity(authToken, "isotropic1024", time, 256, 256, 256, 16, 16, 16);
            stopTime = DateTime.Now;
            result[0].x = BitConverter.ToSingle(data, 0);
            result[0].y = BitConverter.ToSingle(data, sizeof(float));
            result[0].z = BitConverter.ToSingle(data, 2 * sizeof(float));
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);

            expected_pressure = 0.272334f;
            startTime = DateTime.Now;
            data = service.GetRawPressure(authToken, "isotropic1024", time, 256, 256, 256, 16, 16, 16);
            stopTime = DateTime.Now;
            raw_pressure = BitConverter.ToSingle(data, 0);
            CompareAndReport(raw_pressure, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", raw_pressure);

            expected_norm = 23.26728f;
            startTime = DateTime.Now;
            points_above_threshold = service.GetThreshold(authToken, "isotropic1024", "vorticity", time, 0.0f, turbulence.SpatialInterpolation.None_Fd4, 510, 510, 510, 4, 4, 4);
            stopTime = DateTime.Now;
            CompareAndReport(points_above_threshold[0].value, expected_norm);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("vort={0}", points_above_threshold[0].value);

            expected_norm = 1.040131f;
            startTime = DateTime.Now;
            points_above_threshold = service.GetThreshold(authToken, "isotropic1024", "velocity", time, 0.0f, turbulence.SpatialInterpolation.None, 510, 510, 510, 4, 4, 4);
            stopTime = DateTime.Now;
            CompareAndReport(points_above_threshold[0].value, expected_norm);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("vel={0}", points_above_threshold[0].value);

            expected_norm = 0.2996746f;
            startTime = DateTime.Now;
            points_above_threshold = service.GetThreshold(authToken, "isotropic1024", "pressure", time, 0.0f, turbulence.SpatialInterpolation.None, 510, 510, 510, 4, 4, 4);
            stopTime = DateTime.Now;
            CompareAndReport(points_above_threshold[0].value, expected_norm);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", points_above_threshold[0].value);

            expected.x = 2.85833573f;
            expected.y = 3.443399f;
            expected.z = 1.34671152f;
            startTime = DateTime.Now;
            position = service.GetPosition(authToken, "isotropic1024", time, time + 0.05f, 0.001f, turbulence.SpatialInterpolation.Lag6, points);
            stopTime = DateTime.Now;
            result[0].x = position[0].x;
            result[0].y = position[0].y;
            result[0].z = position[0].z;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
        }

        public static void TestMHDDataset()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();
            service.Timeout = -1;

            int pointsize = 1;
            float time = 0.364f;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.Vector3[] result;
            turbulence.Point3[] position;
            turbulence.Pressure[] pressure;
            turbulence.VelocityGradient[] gradient;
            turbulence.VelocityHessian[] hessian;
            turbulence.PressureHessian[] pressure_hessian;
            turbulence.SGSTensor[] sgs_tensor;
            turbulence.ThresholdInfo[] points_above_threshold;
            points[0] = new turbulence.Point3();
            points[0].x = 2.876447f;
            points[0].y = 3.365972f;
            points[0].z = 1.370830f;
            float dx = 2.0f * (float)Math.PI / 1024;
            float filterwidth = 7.0f * dx;
            float spacing = 4.0f * dx;
            turbulence.Vector3 expected = new turbulence.Vector3();
            turbulence.VelocityGradient expected_gradient = new turbulence.VelocityGradient();
            turbulence.VelocityHessian expected_hessian = new turbulence.VelocityHessian();
            turbulence.SGSTensor expected_sgs_tensor = new turbulence.SGSTensor();
            turbulence.PressureHessian expected_p_hessian = new turbulence.PressureHessian();
            float expected_pressure, raw_pressure, expected_norm;
            byte[] data;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            DateTime startTime, stopTime;

            expected.x = 0.4454932f;
            expected.y = 0.1584287f;
            expected.z = -0.1781128f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.4528341f;
            expected.y = 0.1583061f;
            expected.z = -0.179286f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.452803f;
            expected.y = 0.1583463f;
            expected.z = -0.1792495f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Lag6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.4527864f;
            expected.y = 0.1583594f;
            expected.z = -0.1792264f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.4526956f;
            expected.y = 0.1583185f;
            expected.z = -0.1792588f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.2916398f;
            expected.y = 0.1067021f;
            expected.z = -0.1434947f;
            startTime = DateTime.Now;
            result = service.GetMagneticField(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.4479939f;
            expected.y = 0.1554907f;
            expected.z = -0.1769155f;
            startTime = DateTime.Now;
            result = service.GetBoxFilter(authToken, "mhd1024", "velocity", time, filterwidth, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("z={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.2891023f;
            expected.y = 0.1072278f;
            expected.z = -0.1468005f;
            startTime = DateTime.Now;
            result = service.GetBoxFilter(authToken, "mhd1024", "magnetic", time, filterwidth, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected_pressure = -0.04308273f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.04337484f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.04338907f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Lag6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.04339586f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.04329401f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.04418224f;
            startTime = DateTime.Now;
            result = service.GetBoxFilter(authToken, "mhd1024", "pressure", time, filterwidth, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0].x, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", result[0].x);
            expected_gradient.duxdx = -0.2622404f;
            expected_gradient.duxdy = -1.328682f;
            expected_gradient.duxdz = 1.36747f;
            expected_gradient.duydx = 0.7959509f;
            expected_gradient.duydy = 0.3345242f;
            expected_gradient.duydz = 0.8077517f;
            expected_gradient.duzdx = -0.315711f;
            expected_gradient.duzdy = 0.5271616f;
            expected_gradient.duzdz = -0.1237411f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -0.2485868f;
            expected_gradient.duxdy = -1.319479f;
            expected_gradient.duxdz = 1.379833f;
            expected_gradient.duydx = 0.8241483f;
            expected_gradient.duydy = 0.3386661f;
            expected_gradient.duydz = 0.8315134f;
            expected_gradient.duzdx = -0.3324273f;
            expected_gradient.duzdy = 0.5118256f;
            expected_gradient.duzdz = -0.1328392f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -0.2378314f;
            expected_gradient.duxdy = -1.314397f;
            expected_gradient.duxdz = 1.386547f;
            expected_gradient.duydx = 0.8457574f;
            expected_gradient.duydy = 0.3422009f;
            expected_gradient.duydz = 0.8435631f;
            expected_gradient.duzdx = -0.3450596f;
            expected_gradient.duzdy = 0.5029044f;
            expected_gradient.duzdz = -0.1379013f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -0.296266f;
            expected_gradient.duxdy = -1.361775f;
            expected_gradient.duxdz = 1.396885f;
            expected_gradient.duydx = 0.7512492f;
            expected_gradient.duydy = 0.3013022f;
            expected_gradient.duydz = 0.6965961f;
            expected_gradient.duzdx = -0.2379359f;
            expected_gradient.duzdy = 0.5477031f;
            expected_gradient.duzdz = -0.0286026f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -0.2663527f;
            expected_gradient.duxdy = -1.330206f;
            expected_gradient.duxdz = 1.360882f;
            expected_gradient.duydx = 0.8011234f;
            expected_gradient.duydy = 0.3336264f;
            expected_gradient.duydz = 0.8087796f;
            expected_gradient.duzdx = -0.3166841f;
            expected_gradient.duzdy = 0.5249838f;
            expected_gradient.duzdz = -0.1168841f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -0.5791067f;
            expected_gradient.duxdy = -0.8986934f;
            expected_gradient.duxdz = 0.9503888f;
            expected_gradient.duydx = 0.4227619f;
            expected_gradient.duydy = 0.5158283f;
            expected_gradient.duydz = 0.1011568f;
            expected_gradient.duzdx = -0.2397825f;
            expected_gradient.duzdy = 0.4471085f;
            expected_gradient.duzdz = 0.01971613f;
            startTime = DateTime.Now;
            gradient = service.GetBoxFilterGradient(authToken, "mhd1024", "velocity", time, filterwidth, spacing, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected.x = 0.546433449f;
            expected.y = -0.5777724f;
            expected.z = -0.477152824f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.5424781f;
            expected.y = -0.5780709f;
            expected.z = -0.476046562f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.539458156f;
            expected.y = -0.5795156f;
            expected.z = -0.475112915f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.5695668f;
            expected.y = -0.6062902f;
            expected.z = -0.467104435f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.5679792f;
            expected.y = -0.6055997f;
            expected.z = -0.4639184f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected_hessian.d2uxdxdx = -44.677166f;
            expected_hessian.d2uxdxdy = 11.5633459f;
            expected_hessian.d2uxdxdz = 1.20323849f;
            expected_hessian.d2uxdydy = 8.553724f;
            expected_hessian.d2uxdydz = 11.28877f;
            expected_hessian.d2uxdzdz = 66.26432f;
            expected_hessian.d2uydxdx = -50.93659f;
            expected_hessian.d2uydxdy = -7.323927f;
            expected_hessian.d2uydxdz = -3.45185471f;
            expected_hessian.d2uydydy = 1.80010009f;
            expected_hessian.d2uydydz = -31.0068722f;
            expected_hessian.d2uydzdz = -29.4793186f;
            expected_hessian.d2uzdxdx = -40.9854736f;
            expected_hessian.d2uzdxdy = -4.612142f;
            expected_hessian.d2uzdxdz = 19.3350544f;
            expected_hessian.d2uzdydy = 13.83331f;
            expected_hessian.d2uzdydz = -15.3442717f;
            expected_hessian.d2uzdzdz = 35.8056259f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = -49.0762558f;
            expected_hessian.d2uxdxdy = 12.2969656f;
            expected_hessian.d2uxdxdz = -0.518849969f;
            expected_hessian.d2uxdydy = 7.671755f;
            expected_hessian.d2uxdydz = 12.9268837f;
            expected_hessian.d2uxdzdz = 68.4574661f;
            expected_hessian.d2uydxdx = -58.38407f;
            expected_hessian.d2uydxdy = -5.668314f;
            expected_hessian.d2uydxdz = -3.63823152f;
            expected_hessian.d2uydydy = 2.70378923f;
            expected_hessian.d2uydydz = -31.0065289f;
            expected_hessian.d2uydzdz = -29.5709438f;
            expected_hessian.d2uzdxdx = -46.9576378f;
            expected_hessian.d2uzdxdy = -6.45625067f;
            expected_hessian.d2uzdxdz = 20.7023487f;
            expected_hessian.d2uzdydy = 16.527195f;
            expected_hessian.d2uzdydz = -16.739315f;
            expected_hessian.d2uzdzdz = 36.77799f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = -44.92622f;
            expected_hessian.d2uxdxdy = 12.7197266f;
            expected_hessian.d2uxdxdz = -1.84167171f;
            expected_hessian.d2uxdydy = 8.875896f;
            expected_hessian.d2uxdydz = 14.0421152f;
            expected_hessian.d2uxdzdz = 66.78616f;
            expected_hessian.d2uydxdx = -50.786087f;
            expected_hessian.d2uydxdy = -4.4523983f;
            expected_hessian.d2uydxdz = -3.99515486f;
            expected_hessian.d2uydydy = 1.790426f;
            expected_hessian.d2uydydz = -30.9745178f;
            expected_hessian.d2uydzdz = -29.2550812f;
            expected_hessian.d2uzdxdx = -40.6106949f;
            expected_hessian.d2uzdxdy = -7.76955748f;
            expected_hessian.d2uzdxdz = 21.8492088f;
            expected_hessian.d2uzdydy = 13.8255014f;
            expected_hessian.d2uzdydz = -17.6455822f;
            expected_hessian.d2uzdzdz = 35.86832f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = -42.1914558f;
            expected_hessian.d2uxdxdy = 12.95092f;
            expected_hessian.d2uxdxdz = 2.454124f;
            expected_hessian.d2uxdydy = 14.85135f;
            expected_hessian.d2uxdydz = 13.1540661f;
            expected_hessian.d2uxdzdz = 41.848465f;
            expected_hessian.d2uydxdx = -77.73848f;
            expected_hessian.d2uydxdy = -5.04651356f;
            expected_hessian.d2uydxdz = -13.9839067f;
            expected_hessian.d2uydydy = -10.7930431f;
            expected_hessian.d2uydydz = -24.6846447f;
            expected_hessian.d2uydzdz = -54.16968f;
            expected_hessian.d2uzdxdx = -23.718277f;
            expected_hessian.d2uzdxdy = -1.85485971f;
            expected_hessian.d2uzdxdz = 21.1801033f;
            expected_hessian.d2uzdydy = -14.4949045f;
            expected_hessian.d2uzdydz = -12.6380854f;
            expected_hessian.d2uzdzdz = 26.3866425f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = -42.14976f;
            expected_hessian.d2uxdxdy = 10.9638529f;
            expected_hessian.d2uxdxdz = 0.800689161f;
            expected_hessian.d2uxdydy = 14.3514223f;
            expected_hessian.d2uxdydz = 11.1107454f;
            expected_hessian.d2uxdzdz = 67.86502f;
            expected_hessian.d2uydxdx = -49.44199f;
            expected_hessian.d2uydxdy = -8.281412f;
            expected_hessian.d2uydxdz = -4.107229f;
            expected_hessian.d2uydydy = 3.71401978f;
            expected_hessian.d2uydydz = -31.0426273f;
            expected_hessian.d2uydzdz = -28.7415619f;
            expected_hessian.d2uzdxdx = -38.727314f;
            expected_hessian.d2uzdxdy = -3.45726824f;
            expected_hessian.d2uzdxdz = 19.488287f;
            expected_hessian.d2uzdydy = 13.6233816f;
            expected_hessian.d2uzdydz = -15.2374239f;
            expected_hessian.d2uzdzdz = 35.1489563f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = -0.3058927f;
            expected_hessian.d2uxdxdy = -0.09953931f;
            expected_hessian.d2uxdxdz = -0.0112728635f;
            expected_hessian.d2uxdydy = 0.217306182f;
            expected_hessian.d2uxdydz = 0.1272088f;
            expected_hessian.d2uxdzdz = 0.317908585f;
            expected_hessian.d2uydxdx = -0.241190165f;
            expected_hessian.d2uydxdy = 0.3223036f;
            expected_hessian.d2uydxdz = 0.05526491f;
            expected_hessian.d2uydydy = -0.50518024f;
            expected_hessian.d2uydydz = 0.443567067f;
            expected_hessian.d2uydzdz = -0.9271873f;
            expected_hessian.d2uzdxdx = 0.5176989f;
            expected_hessian.d2uzdxdy = -1.10514009f;
            expected_hessian.d2uzdxdz = 0.0103572588f;
            expected_hessian.d2uzdydy = 0.9148197f;
            expected_hessian.d2uzdydz = 0.6205987f;
            expected_hessian.d2uzdzdz = -0.4771409f;
            startTime = DateTime.Now;
            hessian = service.GetVectorPotentialHessian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_p_hessian.d2pdxdx = -27.4014435f;
            expected_p_hessian.d2pdxdy = -2.29550028f;
            expected_p_hessian.d2pdxdz = 3.71739f;
            expected_p_hessian.d2pdydy = 26.8128777f;
            expected_p_hessian.d2pdydz = -6.94272757f;
            expected_p_hessian.d2pdzdz = -5.3681426f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = -31.4843769f;
            expected_p_hessian.d2pdxdy = -2.4920547f;
            expected_p_hessian.d2pdxdz = 4.429066f;
            expected_p_hessian.d2pdydy = 30.009409f;
            expected_p_hessian.d2pdydz = -7.450863f;
            expected_p_hessian.d2pdzdz = -5.93279266f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = -27.1461926f;
            expected_p_hessian.d2pdxdy = -2.47616124f;
            expected_p_hessian.d2pdxdz = 4.92325926f;
            expected_p_hessian.d2pdydy = 27.1683044f;
            expected_p_hessian.d2pdydz = -7.78332f;
            expected_p_hessian.d2pdzdz = -5.20868f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = -15.0153418f;
            expected_p_hessian.d2pdxdy = -2.91878414f;
            expected_p_hessian.d2pdxdz = 4.9671464f;
            expected_p_hessian.d2pdydy = 4.23196268f;
            expected_p_hessian.d2pdydz = -8.915736f;
            expected_p_hessian.d2pdzdz = -0.6917453f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = -16.8966923f;
            expected_p_hessian.d2pdxdy = -2.89115953f;
            expected_p_hessian.d2pdxdz = 4.757846f;
            expected_p_hessian.d2pdydy = 4.8945446f;
            expected_p_hessian.d2pdydz = -8.854527f;
            expected_p_hessian.d2pdzdz = -0.631526351f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_sgs_tensor.xx = 0.000384612f;
            expected_sgs_tensor.xy = -6.796083E-05f;
            expected_sgs_tensor.xz = -5.410858E-05f;
            expected_sgs_tensor.yy = 0.0001107258f;
            expected_sgs_tensor.yz = 1.338594E-05f;
            expected_sgs_tensor.zz = 5.035833E-05f;
            startTime = DateTime.Now;
            sgs_tensor = service.GetBoxFilterSGS(authToken, "mhd1024", "velocity", time, filterwidth, points);
            stopTime = DateTime.Now;
            CompareAndReport(sgs_tensor[0], expected_sgs_tensor);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("xx={0} xy={1} xz={2} yy={3} yz={4} zz={5}",
                sgs_tensor[0].xx, sgs_tensor[0].xy, sgs_tensor[0].xz, sgs_tensor[0].yy, sgs_tensor[0].yz, sgs_tensor[0].zz);
            expected.x = 30.1408749f;
            expected.y = -78.61581f;
            expected.z = 8.653462f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 27.0529633f;
            expected.y = -85.25123f;
            expected.z = 6.347548f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 30.7358379f;
            expected.y = -78.25074f;
            expected.z = 9.083125f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 14.50836f;
            expected.y = -142.7012f;
            expected.z = -11.826539f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 40.06668f;
            expected.y = -74.4695358f;
            expected.z = 10.045023f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "mhd1024", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);

            expected.x = 0.17097038f;
            expected.y = -0.0423543677f;
            expected.z = 0.0915776938f;
            startTime = DateTime.Now;
            data = service.GetRawVelocity(authToken, "mhd1024", time, 256, 256, 256, 16, 16, 16);
            stopTime = DateTime.Now;
            result[0].x = BitConverter.ToSingle(data, 0);
            result[0].y = BitConverter.ToSingle(data, sizeof(float));
            result[0].z = BitConverter.ToSingle(data, 2 * sizeof(float));
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);

            expected_pressure = 0.08253646f;
            startTime = DateTime.Now;
            data = service.GetRawPressure(authToken, "mhd1024", time, 256, 256, 256, 16, 16, 16);
            stopTime = DateTime.Now;
            raw_pressure = BitConverter.ToSingle(data, 0);
            CompareAndReport(raw_pressure, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", raw_pressure);

            expected_norm = 14.51427f;
            startTime = DateTime.Now;
            points_above_threshold = service.GetThreshold(authToken, "mhd1024", "vorticity", time, 0.0f, turbulence.SpatialInterpolation.None_Fd4, 510, 510, 510, 4, 4, 4);
            stopTime = DateTime.Now;
            CompareAndReport(points_above_threshold[0].value, expected_norm);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("vort. norm={0}", points_above_threshold[0].value);

            expected_norm = 0.344052f;
            startTime = DateTime.Now;
            points_above_threshold = service.GetThreshold(authToken, "mhd1024", "velocity", time, 0.0f, turbulence.SpatialInterpolation.None, 510, 510, 510, 4, 4, 4);
            stopTime = DateTime.Now;
            CompareAndReport(points_above_threshold[0].value, expected_norm);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("vel. norm={0}", points_above_threshold[0].value);

            expected.x = 2.89843631f;
            expected.y = 3.37422538f;
            expected.z = 1.36187077f;
            startTime = DateTime.Now;
            position = service.GetPosition(authToken, "mhd1024", time, time + 0.05f, 0.001f, turbulence.SpatialInterpolation.Lag6, points);
            stopTime = DateTime.Now;
            result[0].x = position[0].x;
            result[0].y = position[0].y;
            result[0].z = position[0].z;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
        }

        public static void TestChannelDataset()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();
            service.Timeout = -1;

            int pointsize = 1;
            float time = 0.364f;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.Vector3[] result;
            turbulence.Pressure[] pressure;
            turbulence.VelocityGradient[] gradient;
            turbulence.VelocityHessian[] hessian;
            turbulence.PressureHessian[] pressure_hessian;
            turbulence.ThresholdInfo[] points_above_threshold;
            points[0] = new turbulence.Point3();
            points[0].x = 2.876447f;
            points[0].y = 3.365972f;
            points[0].z = 1.370830f;
            float dx = 2.0f * (float)Math.PI / 1024;
            float filterwidth = 7.0f * dx;
            float spacing = 4.0f * dx;
            turbulence.Vector3 expected = new turbulence.Vector3();
            turbulence.VelocityGradient expected_gradient = new turbulence.VelocityGradient();
            turbulence.VelocityHessian expected_hessian = new turbulence.VelocityHessian();
            turbulence.SGSTensor expected_sgs_tensor = new turbulence.SGSTensor();
            turbulence.PressureHessian expected_p_hessian = new turbulence.PressureHessian();
            float expected_pressure, raw_pressure, expected_norm;
            byte[] data;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            DateTime startTime, stopTime;

            // Test channel flow DB
            // restrict y to be within (-1,1)
            points[0].y = 0.5674932f;

            expected.x = 1.11659193f;
            expected.y = -0.00607302366f;
            expected.z = 0.00907494f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "channel", time, turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 1.10486817f;
            expected.y = -0.007616419f;
            expected.z = 0.01053513f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "channel", time, turbulence.SpatialInterpolation.Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 1.10477233f;
            expected.y = -0.007558492f;
            expected.z = 0.0106105385f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "channel", time, turbulence.SpatialInterpolation.Lag6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 1.10474551f;
            expected.y = -0.00754111027f;
            expected.z = 0.0106375162f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "channel", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 1.10474551f;
            expected.y = -0.00754111027f;
            expected.z = 0.0106375162f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "channel", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected_pressure = -0.003846144f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "channel", time, turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.004080374f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "channel", time, turbulence.SpatialInterpolation.Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.004078661f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "channel", time, turbulence.SpatialInterpolation.Lag6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.004078317f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "channel", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.004078317f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "channel", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_gradient.duxdx = -1.47910607f;
            expected_gradient.duxdy = -1.81606138f;
            expected_gradient.duxdz = -2.60047913f;
            expected_gradient.duydx = -0.825545967f;
            expected_gradient.duydy = 0.7082105f;
            expected_gradient.duydz = -1.08823657f;
            expected_gradient.duzdx = 0.8275332f;
            expected_gradient.duzdy = -0.3693893f;
            expected_gradient.duzdz = 0.7465825f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -1.50077379f;
            expected_gradient.duxdy = -1.8148632f;
            expected_gradient.duxdz = -2.6129303f;
            expected_gradient.duydx = -0.8508426f;
            expected_gradient.duydy = 0.7067135f;
            expected_gradient.duydz = -1.09769416f;
            expected_gradient.duzdx = 0.8798238f;
            expected_gradient.duzdy = -0.368008167f;
            expected_gradient.duzdz = 0.7548044f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -1.50507951f;
            expected_gradient.duxdy = -1.81456947f;
            expected_gradient.duxdz = -2.61602783f;
            expected_gradient.duydx = -0.852867961f;
            expected_gradient.duydy = 0.7063894f;
            expected_gradient.duydz = -1.10048389f;
            expected_gradient.duzdx = 0.8992971f;
            expected_gradient.duzdy = -0.367808878f;
            expected_gradient.duzdz = 0.757274747f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -1.3730793f;
            expected_gradient.duxdy = -1.99472046f;
            expected_gradient.duxdz = -2.37017822f;
            expected_gradient.duydx = -0.9476658f;
            expected_gradient.duydy = 0.6238997f;
            expected_gradient.duydz = -1.18844032f;
            expected_gradient.duzdx = 0.762110949f;
            expected_gradient.duzdy = -0.3212621f;
            expected_gradient.duzdz = 0.724520445f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "channel", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = -1.47910607f;
            expected_gradient.duxdy = -1.81606138f;
            expected_gradient.duxdz = -2.60047913f;
            expected_gradient.duydx = -0.825545967f;
            expected_gradient.duydy = 0.7082105f;
            expected_gradient.duydz = -1.08823657f;
            expected_gradient.duzdx = 0.8275332f;
            expected_gradient.duzdy = -0.3693893f;
            expected_gradient.duzdz = 0.7465825f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected.x = -0.0569342263f;
            expected.y = -0.0202015582f;
            expected.z = -0.05250874f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.05520568f;
            expected.y = -0.0201912038f;
            expected.z = -0.0523878932f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.0546042323f;
            expected.y = -0.020191351f;
            expected.z = -0.05236411f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.07002482f;
            expected.y = -0.0163656473f;
            expected.z = -0.06172341f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "channel", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.07002482f;
            expected.y = -0.0163656473f;
            expected.z = -0.06172341f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "channel", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected_hessian.d2uxdxdx = 24.1496658f;
            expected_hessian.d2uxdxdy = 6.615334f;
            expected_hessian.d2uxdxdz = 25.0959911f;
            expected_hessian.d2uxdydy = 41.72812f;
            expected_hessian.d2uxdydz = -18.6678848f;
            expected_hessian.d2uxdzdz = 50.2600555f;
            expected_hessian.d2uydxdx = -92.22493f;
            expected_hessian.d2uydxdy = -18.1944523f;
            expected_hessian.d2uydxdz = -71.02826f;
            expected_hessian.d2uydydy = -15.7653723f;
            expected_hessian.d2uydydz = -48.35003f;
            expected_hessian.d2uydzdz = -98.3544f;
            expected_hessian.d2uzdxdx = -34.77077f;
            expected_hessian.d2uzdxdy = 7.6409564f;
            expected_hessian.d2uzdxdz = -4.500085f;
            expected_hessian.d2uzdydy = -19.8390026f;
            expected_hessian.d2uzdydz = 4.79925346f;
            expected_hessian.d2uzdzdz = 5.842309f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = 24.4463043f;
            expected_hessian.d2uxdxdy = 10.7253542f;
            expected_hessian.d2uxdxdz = 28.0788975f;
            expected_hessian.d2uxdydy = 41.7324181f;
            expected_hessian.d2uxdydz = -17.14355f;
            expected_hessian.d2uxdzdz = 52.1957855f;
            expected_hessian.d2uydxdx = -94.66425f;
            expected_hessian.d2uydxdy = -18.0908585f;
            expected_hessian.d2uydxdz = -77.85528f;
            expected_hessian.d2uydydy = -15.7936583f;
            expected_hessian.d2uydydz = -49.4997025f;
            expected_hessian.d2uydzdz = -98.91268f;
            expected_hessian.d2uzdxdx = -36.5817223f;
            expected_hessian.d2uzdxdy = 5.29627943f;
            expected_hessian.d2uzdxdz = -5.746588f;
            expected_hessian.d2uzdydy = -20.0061073f;
            expected_hessian.d2uzdydz = 4.80815744f;
            expected_hessian.d2uzdzdz = 5.184058f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = 24.5591564f;
            expected_hessian.d2uxdxdy = 12.9619274f;
            expected_hessian.d2uxdxdz = 29.7732449f;
            expected_hessian.d2uxdydy = 41.730732f;
            expected_hessian.d2uxdydz = -16.5664444f;
            expected_hessian.d2uxdzdz = 52.9455f;
            expected_hessian.d2uydxdx = -95.17201f;
            expected_hessian.d2uydxdy = -17.8665428f;
            expected_hessian.d2uydxdz = -80.72761f;
            expected_hessian.d2uydydy = -15.7952118f;
            expected_hessian.d2uydydz = -49.843792f;
            expected_hessian.d2uydzdz = -98.98832f;
            expected_hessian.d2uzdxdx = -37.6246071f;
            expected_hessian.d2uzdxdy = 3.41854382f;
            expected_hessian.d2uzdxdz = -6.541169f;
            expected_hessian.d2uzdydy = -20.04674f;
            expected_hessian.d2uzdydz = 4.76680374f;
            expected_hessian.d2uzdzdz = 4.89751148f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = 42.85462f;
            expected_hessian.d2uxdxdy = -2.97424555f;
            expected_hessian.d2uxdxdz = 41.3553238f;
            expected_hessian.d2uxdydy = 55.609272f;
            expected_hessian.d2uxdydz = -25.5733986f;
            expected_hessian.d2uxdzdz = 74.13193f;
            expected_hessian.d2uydxdx = -75.38661f;
            expected_hessian.d2uydxdy = -25.3493538f;
            expected_hessian.d2uydxdz = -48.3515968f;
            expected_hessian.d2uydydy = -10.0782175f;
            expected_hessian.d2uydydz = -46.7858238f;
            expected_hessian.d2uydzdz = -66.1833f;
            expected_hessian.d2uzdxdx = -42.0982056f;
            expected_hessian.d2uzdxdy = 3.53085423f;
            expected_hessian.d2uzdxdz = -13.511035f;
            expected_hessian.d2uzdydy = -15.2089739f;
            expected_hessian.d2uzdydz = 2.44263268f;
            expected_hessian.d2uzdzdz = -5.207473f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "channel", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = 24.1496658f;
            expected_hessian.d2uxdxdy = 6.615334f;
            expected_hessian.d2uxdxdz = 25.0959911f;
            expected_hessian.d2uxdydy = 41.72812f;
            expected_hessian.d2uxdydz = -18.6678848f;
            expected_hessian.d2uxdzdz = 50.2600555f;
            expected_hessian.d2uydxdx = -92.22493f;
            expected_hessian.d2uydxdy = -18.1944523f;
            expected_hessian.d2uydxdz = -71.02826f;
            expected_hessian.d2uydydy = -15.7653723f;
            expected_hessian.d2uydydz = -48.35003f;
            expected_hessian.d2uydzdz = -98.3544f;
            expected_hessian.d2uzdxdx = -34.77077f;
            expected_hessian.d2uzdxdy = 7.6409564f;
            expected_hessian.d2uzdxdz = -4.500085f;
            expected_hessian.d2uzdydy = -19.8390026f;
            expected_hessian.d2uzdydz = 4.79925346f;
            expected_hessian.d2uzdzdz = 5.842309f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_p_hessian.d2pdxdx = -1.166303f;
            expected_p_hessian.d2pdxdy = 1.41206217f;
            expected_p_hessian.d2pdxdz = -2.73382282f;
            expected_p_hessian.d2pdydy = -0.00773827f;
            expected_p_hessian.d2pdydz = 1.39248574f;
            expected_p_hessian.d2pdzdz = -1.063858f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = -1.0861243f;
            expected_p_hessian.d2pdxdy = 1.37939084f;
            expected_p_hessian.d2pdxdz = -2.673571f;
            expected_p_hessian.d2pdydy = -0.00576043129f;
            expected_p_hessian.d2pdydz = 1.396564f;
            expected_p_hessian.d2pdzdz = -1.059494f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = -1.0405786f;
            expected_p_hessian.d2pdxdy = 1.34628069f;
            expected_p_hessian.d2pdxdz = -2.63033843f;
            expected_p_hessian.d2pdydy = -0.0054872036f;
            expected_p_hessian.d2pdydz = 1.39786673f;
            expected_p_hessian.d2pdzdz = -1.05863953f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = -2.16746831f;
            expected_p_hessian.d2pdxdy = 1.394025f;
            expected_p_hessian.d2pdxdz = -3.39462137f;
            expected_p_hessian.d2pdydy = 0.378553867f;
            expected_p_hessian.d2pdydz = 1.33229065f;
            expected_p_hessian.d2pdzdz = -1.7441597f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "channel", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = -2.16746831f;
            expected_p_hessian.d2pdxdy = 1.394025f;
            expected_p_hessian.d2pdxdz = -3.39462137f;
            expected_p_hessian.d2pdydy = 0.378553867f;
            expected_p_hessian.d2pdydz = 1.33229065f;
            expected_p_hessian.d2pdzdz = -1.7441597f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "channel", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected.x = 116.13784f;
            expected.y = -206.3447f;
            expected.z = -48.7674637f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 118.374504f;
            expected.y = -209.37059f;
            expected.z = -51.4037743f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 119.235382f;
            expected.y = -209.955551f;
            expected.z = -52.7738342f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 172.595825f;
            expected.y = -151.648132f;
            expected.z = -62.5146523f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "channel", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 116.13784f;
            expected.y = -206.3447f;
            expected.z = -48.7674637f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "channel", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);

            expected.x = 1.1781112f;
            expected.y = -0.005176977f;
            expected.z = 0.000154264519f;
            startTime = DateTime.Now;
            data = service.GetRawVelocity(authToken, "channel", time, 256, 256, 256, 16, 16, 16);
            stopTime = DateTime.Now;
            result[0].x = BitConverter.ToSingle(data, 0);
            result[0].y = BitConverter.ToSingle(data, sizeof(float));
            result[0].z = BitConverter.ToSingle(data, 2 * sizeof(float));
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);

            expected_pressure = -0.0008468386f;
            startTime = DateTime.Now;
            data = service.GetRawPressure(authToken, "channel", time, 256, 256, 256, 16, 16, 16);
            stopTime = DateTime.Now;
            raw_pressure = BitConverter.ToSingle(data, 0);
            CompareAndReport(raw_pressure, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", raw_pressure);

            expected_norm = 61.0588951f;
            startTime = DateTime.Now;
            points_above_threshold = service.GetThreshold(authToken, "channel", "vorticity", time, 0.0f, turbulence.SpatialInterpolation.None_Fd4, 510, 0, 510, 4, 4, 4);
            stopTime = DateTime.Now;
            CompareAndReport(points_above_threshold[0].value, expected_norm);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("vort. norm={0}", points_above_threshold[0].value);

            expected_norm = 0.006632454f;
            startTime = DateTime.Now;
            points_above_threshold = service.GetThreshold(authToken, "channel", "velocity", time, 0.0f, turbulence.SpatialInterpolation.None, 510, 508, 510, 4, 4, 4);
            stopTime = DateTime.Now;
            CompareAndReport(points_above_threshold[0].value, expected_norm);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("vel. norm={0}", points_above_threshold[0].value);

            expected_norm = 0.009846616f;
            startTime = DateTime.Now;
            points_above_threshold = service.GetThreshold(authToken, "channel", "pressure", time, 0.0f, turbulence.SpatialInterpolation.None, 510, 508, 510, 4, 4, 4);
            stopTime = DateTime.Now;
            CompareAndReport(points_above_threshold[0].value, expected_norm);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("pr. norm={0}", points_above_threshold[0].value);
        }

        public static void TestMixingDataset()
        {
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();
            service.Timeout = -1;

            int pointsize = 1;
            float time = 5.0f;
            turbulence.Point3[] points = new turbulence.Point3[pointsize];
            turbulence.Vector3[] result;
            turbulence.Point3[] position;
            turbulence.Pressure[] pressure;
            turbulence.Pressure[] density;
            turbulence.VelocityGradient[] gradient;
            turbulence.VelocityHessian[] hessian;
            turbulence.PressureHessian[] pressure_hessian;
            turbulence.PressureHessian[] density_hessian;
            turbulence.SGSTensor[] sgs_tensor;
            turbulence.ThresholdInfo[] points_above_threshold;
            points[0] = new turbulence.Point3();
            points[0].x = 2.876447f;
            points[0].y = 3.365972f;
            points[0].z = 1.370830f;
            float dx = 2.0f * (float)Math.PI / 1024;
            float filterwidth = 7.0f * dx;
            float spacing = 4.0f * dx;
            turbulence.Vector3 expected = new turbulence.Vector3();
            turbulence.VelocityGradient expected_gradient = new turbulence.VelocityGradient();
            turbulence.VelocityHessian expected_hessian = new turbulence.VelocityHessian();
            turbulence.SGSTensor expected_sgs_tensor = new turbulence.SGSTensor();
            turbulence.PressureHessian expected_p_hessian = new turbulence.PressureHessian();
            turbulence.PressureHessian expected_d_hessian = new turbulence.PressureHessian();
            byte[] data;
            float expected_pressure, expected_density, raw_density, raw_pressure, expected_norm;
            string authToken = "edu.jhu.pha.turbulence.testing-201406";

            DateTime startTime, stopTime;
            
            expected_density = 1.001256f;
            startTime = DateTime.Now;
            density = service.GetDensity(authToken, "mixing", time, turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(density[0].p, expected_density);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", density[0].p);
            expected_density = 1.001212f;
            startTime = DateTime.Now;
            density = service.GetDensity(authToken, "mixing", time, turbulence.SpatialInterpolation.Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(density[0].p, expected_density);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", density[0].p);
            expected_density = 1.001212f;
            startTime = DateTime.Now;
            density = service.GetDensity(authToken, "mixing", time, turbulence.SpatialInterpolation.Lag6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(density[0].p, expected_density);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", density[0].p);
            expected_density = 1.001212f;
            startTime = DateTime.Now;
            density = service.GetDensity(authToken, "mixing", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(density[0].p, expected_density);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", density[0].p);
            expected_density = 1.001212f;
            startTime = DateTime.Now;
            density = service.GetDensity(authToken, "mixing", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(density[0].p, expected_density);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", density[0].p);
            expected.x = 0.0420122147f;
            expected.y = 0.01044178f;
            expected.z = 0.0147628784f;
            startTime = DateTime.Now;
            result = service.GetDensityGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.0420165062f;
            expected.y = 0.0104539394f;
            expected.z = 0.0147628784f;
            startTime = DateTime.Now;
            result = service.GetDensityGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.0419955254f;
            expected.y = 0.0104556084f;
            expected.z = 0.014755249f;
            startTime = DateTime.Now;
            result = service.GetDensityGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.04147637f;
            expected.y = 0.00990811f;
            expected.z = 0.0145339966f;
            startTime = DateTime.Now;
            result = service.GetDensityGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.04147637f;
            expected.y = 0.00990811f;
            expected.z = 0.0145339966f;
            startTime = DateTime.Now;
            result = service.GetDensityGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected_d_hessian.d2pdxdx = 1.40240133f;
            expected_d_hessian.d2pdxdy = -0.127311125f;
            expected_d_hessian.d2pdxdz = 0.384703875f;
            expected_d_hessian.d2pdydy = 0.2894514f;
            expected_d_hessian.d2pdydz = 0.007981682f;
            expected_d_hessian.d2pdzdz = 0.113722473f;
            startTime = DateTime.Now;
            density_hessian = service.GetDensityHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(density_hessian[0], expected_d_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                density_hessian[0].d2pdxdx, density_hessian[0].d2pdxdy, density_hessian[0].d2pdxdz,
                density_hessian[0].d2pdydy, density_hessian[0].d2pdydz, density_hessian[0].d2pdzdz);
            expected_d_hessian.d2pdxdx = 1.40208471f;
            expected_d_hessian.d2pdxdy = -0.127319917f;
            expected_d_hessian.d2pdxdz = 0.38462472f;
            expected_d_hessian.d2pdydy = 0.2898384f;
            expected_d_hessian.d2pdydz = 0.007876139f;
            expected_d_hessian.d2pdzdz = 0.113616928f;
            startTime = DateTime.Now;
            density_hessian = service.GetDensityHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(density_hessian[0], expected_d_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                density_hessian[0].d2pdxdx, density_hessian[0].d2pdxdy, density_hessian[0].d2pdxdz,
                density_hessian[0].d2pdydy, density_hessian[0].d2pdydz, density_hessian[0].d2pdzdz);
            expected_d_hessian.d2pdxdx = 1.40233779f;
            expected_d_hessian.d2pdxdy = -0.1273185f;
            expected_d_hessian.d2pdxdz = 0.384600669f;
            expected_d_hessian.d2pdydy = 0.2894858f;
            expected_d_hessian.d2pdydz = 0.007771538f;
            expected_d_hessian.d2pdzdz = 0.113689259f;
            startTime = DateTime.Now;
            density_hessian = service.GetDensityHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(density_hessian[0], expected_d_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                density_hessian[0].d2pdxdx, density_hessian[0].d2pdxdy, density_hessian[0].d2pdxdz,
                density_hessian[0].d2pdydy, density_hessian[0].d2pdydz, density_hessian[0].d2pdzdz);
            expected_d_hessian.d2pdxdx = 1.41577709f;
            expected_d_hessian.d2pdxdy = -0.119023658f;
            expected_d_hessian.d2pdxdz = 0.39259845f;
            expected_d_hessian.d2pdydy = 0.256476969f;
            expected_d_hessian.d2pdydz = 0.00592391053f;
            expected_d_hessian.d2pdzdz = 0.114701174f;
            startTime = DateTime.Now;
            density_hessian = service.GetDensityHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(density_hessian[0], expected_d_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                density_hessian[0].d2pdxdx, density_hessian[0].d2pdxdy, density_hessian[0].d2pdxdz,
                density_hessian[0].d2pdydy, density_hessian[0].d2pdydz, density_hessian[0].d2pdzdz);
            expected_d_hessian.d2pdxdx = 1.41577709f;
            expected_d_hessian.d2pdxdy = -0.119023658f;
            expected_d_hessian.d2pdxdz = 0.39259845f;
            expected_d_hessian.d2pdydy = 0.256476969f;
            expected_d_hessian.d2pdydz = 0.00592391053f;
            expected_d_hessian.d2pdzdz = 0.114701174f;
            startTime = DateTime.Now;
            density_hessian = service.GetDensityHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(density_hessian[0], expected_d_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                density_hessian[0].d2pdxdx, density_hessian[0].d2pdxdy, density_hessian[0].d2pdxdz,
                density_hessian[0].d2pdydy, density_hessian[0].d2pdydz, density_hessian[0].d2pdzdz);
            expected_density = 1.001395f;
            startTime = DateTime.Now;
            result = service.GetBoxFilter(authToken, "mixing", "density", time, filterwidth, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0].x, expected_density);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", result[0].x);

            expected_density = 1.10377121f;
            startTime = DateTime.Now;
            data = service.GetRawDensity(authToken, "mixing", time, 0, 0, 0, 16, 16, 16);
            stopTime = DateTime.Now;
            raw_density = BitConverter.ToSingle(data, 0);
            CompareAndReport(raw_density, expected_density);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", raw_density);

            expected.x = 0.008379767f;
            expected.y = 0.06922164f;
            expected.z = 0.0615928769f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "mixing", time, turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.0067241434f;
            expected.y = 0.07075314f;
            expected.z = 0.06159591f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "mixing", time, turbulence.SpatialInterpolation.Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.0067242505f;
            expected.y = 0.07075299f;
            expected.z = 0.06159584f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "mixing", time, turbulence.SpatialInterpolation.Lag6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.00672424957f;
            expected.y = 0.07075299f;
            expected.z = 0.0615958348f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "mixing", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.00672424957f;
            expected.y = 0.07075299f;
            expected.z = 0.0615958348f;
            startTime = DateTime.Now;
            result = service.GetVelocity(authToken, "mixing", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 0.008642722f;
            expected.y = 0.06848274f;
            expected.z = 0.0613147f;
            startTime = DateTime.Now;
            result = service.GetBoxFilter(authToken, "mixing", "velocity", time, filterwidth, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected_gradient.duxdx = 0.8444475f;
            expected_gradient.duxdy = 0.595726132f;
            expected_gradient.duxdz = 0.4068811f;
            expected_gradient.duydx = 0.2864297f;
            expected_gradient.duydy = -0.890828848f;
            expected_gradient.duydz = -0.176894188f;
            expected_gradient.duzdx = 0.284538627f;
            expected_gradient.duzdy = -0.0988164544f;
            expected_gradient.duzdz = 0.0462441444f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = 0.844440639f;
            expected_gradient.duxdy = 0.595734537f;
            expected_gradient.duxdz = 0.4068812f;
            expected_gradient.duydx = 0.2864306f;
            expected_gradient.duydy = -0.8908282f;
            expected_gradient.duydz = -0.176894188f;
            expected_gradient.duzdx = 0.2845363f;
            expected_gradient.duzdy = -0.09881808f;
            expected_gradient.duzdz = 0.0462436676f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = 0.8444407f;
            expected_gradient.duxdy = 0.595733941f;
            expected_gradient.duxdz = 0.4068812f;
            expected_gradient.duydx = 0.286430776f;
            expected_gradient.duydy = -0.8908285f;
            expected_gradient.duydz = -0.176894188f;
            expected_gradient.duzdx = 0.284537047f;
            expected_gradient.duzdy = -0.0988177359f;
            expected_gradient.duzdz = 0.04624462f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = 0.8376642f;
            expected_gradient.duxdy = 0.600680649f;
            expected_gradient.duxdz = 0.40591085f;
            expected_gradient.duydx = 0.300485522f;
            expected_gradient.duydy = -0.884536147f;
            expected_gradient.duydz = -0.171685219f;
            expected_gradient.duzdx = 0.286245316f;
            expected_gradient.duzdy = -0.09526346f;
            expected_gradient.duzdz = 0.0467357635f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = 0.8444475f;
            expected_gradient.duxdy = 0.595726132f;
            expected_gradient.duxdz = 0.4068811f;
            expected_gradient.duydx = 0.2864297f;
            expected_gradient.duydy = -0.890828848f;
            expected_gradient.duydz = -0.176894188f;
            expected_gradient.duzdx = 0.284538627f;
            expected_gradient.duzdy = -0.0988164544f;
            expected_gradient.duzdz = 0.0462441444f;
            startTime = DateTime.Now;
            gradient = service.GetVelocityGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_gradient.duxdx = 0.8454851f;
            expected_gradient.duxdy = 0.61337924f;
            expected_gradient.duxdz = 0.406784952f;
            expected_gradient.duydx = 0.24110502f;
            expected_gradient.duydy = -0.8804205f;
            expected_gradient.duydz = -0.182371065f;
            expected_gradient.duzdx = 0.264947683f;
            expected_gradient.duzdy = -0.09539308f;
            expected_gradient.duzdz = 0.0426006168f;
            startTime = DateTime.Now;
            gradient = service.GetBoxFilterGradient(authToken, "mixing", "velocity", time, filterwidth, spacing, points);
            stopTime = DateTime.Now;
            CompareAndReport(gradient[0], expected_gradient);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("duxdx={0} duxdy={1} duxdz={2} duydx={3} duydy={4} duydz={5} duzdx={6} duzdy={7} duzdz={8}",
                gradient[0].duxdx, gradient[0].duxdy, gradient[0].duxdz,
                gradient[0].duydx, gradient[0].duydy, gradient[0].duydz,
                gradient[0].duzdx, gradient[0].duzdy, gradient[0].duzdz);
            expected_hessian.d2uxdxdx = 5.50019f;
            expected_hessian.d2uxdxdy = 2.2155745f;
            expected_hessian.d2uxdxdz = 2.53060532f;
            expected_hessian.d2uxdydy = -2.913894f;
            expected_hessian.d2uxdydz = -0.00371126551f;
            expected_hessian.d2uxdzdz = 0.924738765f;
            expected_hessian.d2uydxdx = -6.61124039f;
            expected_hessian.d2uydxdy = -5.11312675f;
            expected_hessian.d2uydxdz = -3.208805f;
            expected_hessian.d2uydydy = -1.57382607f;
            expected_hessian.d2uydydz = -1.8560276f;
            expected_hessian.d2uydzdz = -1.54653335f;
            expected_hessian.d2uzdxdx = -1.18817389f;
            expected_hessian.d2uzdxdy = -0.44712466f;
            expected_hessian.d2uzdxdz = -0.3905108f;
            expected_hessian.d2uzdydy = -1.73381424f;
            expected_hessian.d2uzdydz = -0.6409608f;
            expected_hessian.d2uzdzdz = -0.6753426f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = 5.50013161f;
            expected_hessian.d2uxdxdy = 2.21563864f;
            expected_hessian.d2uxdxdz = 2.53056622f;
            expected_hessian.d2uxdydy = -2.914175f;
            expected_hessian.d2uxdydz = -0.00356368767f;
            expected_hessian.d2uxdzdz = 0.9247409f;
            expected_hessian.d2uydxdx = -6.611084f;
            expected_hessian.d2uydxdy = -5.11309767f;
            expected_hessian.d2uydxdz = -3.20855451f;
            expected_hessian.d2uydydy = -1.57372046f;
            expected_hessian.d2uydydz = -1.85601544f;
            expected_hessian.d2uydzdz = -1.546542f;
            expected_hessian.d2uzdxdx = -1.18811345f;
            expected_hessian.d2uzdxdy = -0.447104871f;
            expected_hessian.d2uzdxdz = -0.39040634f;
            expected_hessian.d2uzdydy = -1.7337296f;
            expected_hessian.d2uzdydz = -0.6410083f;
            expected_hessian.d2uzdzdz = -0.675341547f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = 5.500128f;
            expected_hessian.d2uxdxdy = 2.21565747f;
            expected_hessian.d2uxdxdz = 2.53056145f;
            expected_hessian.d2uxdydy = -2.913683f;
            expected_hessian.d2uxdydz = -0.00357911759f;
            expected_hessian.d2uxdzdz = 0.9247385f;
            expected_hessian.d2uydxdx = -6.61108446f;
            expected_hessian.d2uydxdy = -5.11308861f;
            expected_hessian.d2uydxdz = -3.20854926f;
            expected_hessian.d2uydydy = -1.57378888f;
            expected_hessian.d2uydydz = -1.85602f;
            expected_hessian.d2uydzdz = -1.54653358f;
            expected_hessian.d2uzdxdx = -1.18812191f;
            expected_hessian.d2uzdxdy = -0.447108448f;
            expected_hessian.d2uzdxdz = -0.390406251f;
            expected_hessian.d2uzdydy = -1.733849f;
            expected_hessian.d2uzdydz = -0.641005158f;
            expected_hessian.d2uzdzdz = -0.675340652f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = 5.52098846f;
            expected_hessian.d2uxdxdy = 2.3513608f;
            expected_hessian.d2uxdxdz = 2.57713175f;
            expected_hessian.d2uxdydy = -3.071605f;
            expected_hessian.d2uxdydz = 0.00401467877f;
            expected_hessian.d2uxdzdz = 0.943004549f;
            expected_hessian.d2uydxdx = -6.449467f;
            expected_hessian.d2uydxdy = -5.148258f;
            expected_hessian.d2uydxdz = -3.172966f;
            expected_hessian.d2uydydy = -1.70157814f;
            expected_hessian.d2uydydz = -1.89071751f;
            expected_hessian.d2uydzdz = -1.55446517f;
            expected_hessian.d2uzdxdx = -1.15444815f;
            expected_hessian.d2uzdxdy = -0.418987274f;
            expected_hessian.d2uzdxdz = -0.3763408f;
            expected_hessian.d2uzdydy = -1.748846f;
            expected_hessian.d2uzdydz = -0.6493197f;
            expected_hessian.d2uzdzdz = -0.6872665f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected_hessian.d2uxdxdx = 5.50019f;
            expected_hessian.d2uxdxdy = 2.2155745f;
            expected_hessian.d2uxdxdz = 2.53060532f;
            expected_hessian.d2uxdydy = -2.913894f;
            expected_hessian.d2uxdydz = -0.00371126551f;
            expected_hessian.d2uxdzdz = 0.924738765f;
            expected_hessian.d2uydxdx = -6.61124039f;
            expected_hessian.d2uydxdy = -5.11312675f;
            expected_hessian.d2uydxdz = -3.208805f;
            expected_hessian.d2uydydy = -1.57382607f;
            expected_hessian.d2uydydz = -1.8560276f;
            expected_hessian.d2uydzdz = -1.54653335f;
            expected_hessian.d2uzdxdx = -1.18817389f;
            expected_hessian.d2uzdxdy = -0.44712466f;
            expected_hessian.d2uzdxdz = -0.3905108f;
            expected_hessian.d2uzdydy = -1.73381424f;
            expected_hessian.d2uzdydz = -0.6409608f;
            expected_hessian.d2uzdzdz = -0.6753426f;
            startTime = DateTime.Now;
            hessian = service.GetVelocityHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(hessian[0], expected_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2uxdxdx={0} d2uxdxdy={1} d2uxdxdz={2} d2uxdydy={3} d2uxdydz={4} d2uxdzdz={5} " +
                "d2uydxdx={0} d2uydxdy={1} d2uydxdz={2} d2uydydy={3} d2uydydz={4} d2uydzdz={5} " +
                "d2uzdxdx={0} d2uzdxdy={1} d2uzdxdz={2} d2uzdydy={3} d2uzdydz={4} d2uzdzdz={5} ",
                hessian[0].d2uxdxdx, hessian[0].d2uxdxdy, hessian[0].d2uxdxdz, hessian[0].d2uxdydy, hessian[0].d2uxdydz, hessian[0].d2uxdzdz,
                hessian[0].d2uydxdx, hessian[0].d2uydxdy, hessian[0].d2uydxdz, hessian[0].d2uydydy, hessian[0].d2uydydz, hessian[0].d2uydzdz,
                hessian[0].d2uzdxdx, hessian[0].d2uzdxdy, hessian[0].d2uzdxdz, hessian[0].d2uzdydy, hessian[0].d2uzdydz, hessian[0].d2uzdzdz);
            expected.x = 3.51103473f;
            expected.y = -9.7316f;
            expected.z = -3.59733081f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 3.5106976f;
            expected.y = -9.731347f;
            expected.z = -3.59718466f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 3.5111835f;
            expected.y = -9.731407f;
            expected.z = -3.59731174f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 3.39238787f;
            expected.y = -9.705511f;
            expected.z = -3.59056067f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "mixing", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = 3.51103473f;
            expected.y = -9.7316f;
            expected.z = -3.59733081f;
            startTime = DateTime.Now;
            result = service.GetVelocityLaplacian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);

            expected_pressure = -0.00900476f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "mixing", time, turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.009071511f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "mixing", time, turbulence.SpatialInterpolation.Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.009071501f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "mixing", time, turbulence.SpatialInterpolation.Lag6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.009071501f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "mixing", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected_pressure = -0.009071501f;
            startTime = DateTime.Now;
            pressure = service.GetPressure(authToken, "mixing", time, turbulence.SpatialInterpolation.Lag8, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure[0].p, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", pressure[0].p);
            expected.x = -0.168484241f;
            expected.y = 0.08220611f;
            expected.z = -0.0270295739f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.168483213f;
            expected.y = 0.08220607f;
            expected.z = -0.0270295143f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.168483242f;
            expected.y = 0.08220595f;
            expected.z = -0.0270295143f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.169334427f;
            expected.y = 0.08275392f;
            expected.z = -0.02689761f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected.x = -0.169334427f;
            expected.y = 0.08275392f;
            expected.z = -0.02689761f;
            startTime = DateTime.Now;
            result = service.GetPressureGradient(authToken, "mixing", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
            expected_p_hessian.d2pdxdx = -1.49132431f;
            expected_p_hessian.d2pdxdy = 0.747192442f;
            expected_p_hessian.d2pdxdz = -0.323572218f;
            expected_p_hessian.d2pdydy = -0.532309353f;
            expected_p_hessian.d2pdydz = 0.05387429f;
            expected_p_hessian.d2pdzdz = -0.0579723231f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = -1.49131191f;
            expected_p_hessian.d2pdxdy = 0.747171044f;
            expected_p_hessian.d2pdxdz = -0.323562652f;
            expected_p_hessian.d2pdydy = -0.5323118f;
            expected_p_hessian.d2pdydz = 0.053874515f;
            expected_p_hessian.d2pdzdz = -0.0579727031f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd6, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = -1.49131191f;
            expected_p_hessian.d2pdxdy = 0.747171044f;
            expected_p_hessian.d2pdxdz = -0.323562652f;
            expected_p_hessian.d2pdydy = -0.5323118f;
            expected_p_hessian.d2pdydz = 0.053874515f;
            expected_p_hessian.d2pdzdz = -0.0579727031f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.None_Fd8, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_p_hessian.d2pdxdx = -1.51510429f;
            expected_p_hessian.d2pdxdy = 0.7495857f;
            expected_p_hessian.d2pdxdz = -0.329003125f;
            expected_p_hessian.d2pdydy = -0.508072138f;
            expected_p_hessian.d2pdydz = 0.05974006f;
            expected_p_hessian.d2pdzdz = -0.0545916371f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.None, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz); 
            expected_p_hessian.d2pdxdx = -1.51510429f;
            expected_p_hessian.d2pdxdy = 0.7495857f;
            expected_p_hessian.d2pdxdz = -0.329003125f;
            expected_p_hessian.d2pdydy = -0.508072138f;
            expected_p_hessian.d2pdydz = 0.05974006f;
            expected_p_hessian.d2pdzdz = -0.0545916371f;
            startTime = DateTime.Now;
            pressure_hessian = service.GetPressureHessian(authToken, "mixing", time, turbulence.SpatialInterpolation.Fd4Lag4, turbulence.TemporalInterpolation.PCHIP, points);
            stopTime = DateTime.Now;
            CompareAndReport(pressure_hessian[0], expected_p_hessian);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("d2pdxdx={0} d2pdxdy={1} d2pdxdz={2} d2pdydy={3} d2pdydz={4} d2pdzdz={5} ",
                pressure_hessian[0].d2pdxdx, pressure_hessian[0].d2pdxdy, pressure_hessian[0].d2pdxdz,
                pressure_hessian[0].d2pdydy, pressure_hessian[0].d2pdydz, pressure_hessian[0].d2pdzdz);
            expected_pressure = -0.009160909f;
            startTime = DateTime.Now;
            result = service.GetBoxFilter(authToken, "mixing", "pressure", time, filterwidth, points);
            stopTime = DateTime.Now;
            CompareAndReport(result[0].x, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", result[0].x);
            expected_sgs_tensor.xx = 0.000185829937f;
            expected_sgs_tensor.xy = -5.68665819E-05f;
            expected_sgs_tensor.xz = 2.91096858E-05f;
            expected_sgs_tensor.yy = 0.00013621882f;
            expected_sgs_tensor.yz = 2.34716881E-05f;
            expected_sgs_tensor.zz = 1.33988979E-05f;
            startTime = DateTime.Now;
            sgs_tensor = service.GetBoxFilterSGS(authToken, "mixing", "velocity", time, filterwidth, points);
            stopTime = DateTime.Now;
            CompareAndReport(sgs_tensor[0], expected_sgs_tensor);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("xx={0} xy={1} xz={2} yy={3} yz={4} zz={5}",
                sgs_tensor[0].xx, sgs_tensor[0].xy, sgs_tensor[0].xz, sgs_tensor[0].yy, sgs_tensor[0].yz, sgs_tensor[0].zz);
            
            expected.x = 0.0853985f;
            expected.y = -0.0169677641f;
            expected.z = -0.0003303562f;
            startTime = DateTime.Now;
            data = service.GetRawVelocity(authToken, "mixing", time, 256, 256, 256, 16, 16, 16);
            stopTime = DateTime.Now;
            result[0].x = BitConverter.ToSingle(data, 0);
            result[0].y = BitConverter.ToSingle(data, sizeof(float));
            result[0].z = BitConverter.ToSingle(data, 2 * sizeof(float));
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);

            expected_pressure = 0.01801266f;
            startTime = DateTime.Now;
            data = service.GetRawPressure(authToken, "mixing", time, 256, 256, 256, 16, 16, 16);
            stopTime = DateTime.Now;
            raw_pressure = BitConverter.ToSingle(data, 0);
            CompareAndReport(raw_pressure, expected_pressure);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("p={0}", raw_pressure);

            expected_norm = 6.872591f;
            startTime = DateTime.Now;
            points_above_threshold = service.GetThreshold(authToken, "mixing", "vorticity", time, 0.0f, turbulence.SpatialInterpolation.None_Fd4, 510, 510, 510, 4, 4, 4);
            stopTime = DateTime.Now;
            CompareAndReport(points_above_threshold[0].value, expected_norm);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("vort. norm={0}", points_above_threshold[0].value);

            expected_norm = 0.3000101f;
            startTime = DateTime.Now;
            points_above_threshold = service.GetThreshold(authToken, "mixing", "velocity", time, 0.0f, turbulence.SpatialInterpolation.None, 510, 510, 510, 4, 4, 4);
            stopTime = DateTime.Now;
            CompareAndReport(points_above_threshold[0].value, expected_norm);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("vel. norm={0}", points_above_threshold[0].value);

            expected_norm = 1.085284f;
            startTime = DateTime.Now;
            points_above_threshold = service.GetThreshold(authToken, "mixing", "density", time, 0.0f, turbulence.SpatialInterpolation.None, 510, 510, 510, 4, 4, 4);
            stopTime = DateTime.Now;
            CompareAndReport(points_above_threshold[0].value, expected_norm);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("density norm={0}", points_above_threshold[0].value);

            expected.x = 2.87693024f;
            expected.y = 3.36940575f;
            expected.z = 1.37394285f;
            startTime = DateTime.Now;
            position = service.GetPosition(authToken, "mixing", time, time + 0.05f, 0.001f, turbulence.SpatialInterpolation.Lag6, points);
            stopTime = DateTime.Now;
            result[0].x = position[0].x;
            result[0].y = position[0].y;
            result[0].z = position[0].z;
            CompareAndReport(result[0], expected);
            Console.WriteLine("Execution time: {0}", stopTime - startTime);
            Console.WriteLine("x={0} y={1} z={2}", result[0].x, result[0].y, result[0].z);
        }

        private static void CompareAndReport(turbulence.Vector3 result, turbulence.Vector3 expected)
        {
            if (!NearEqual(expected.x, result.x) || !NearEqual(expected.y, result.y) || !NearEqual(expected.z, result.z))
            {
                throw new Exception(String.Format("Values don't match!\n" +
                    "Expected:\nexpected.x = {0:R}f;\nexpected.y = {1:R}f;\nexpected.z = {2:R}f;\n" +
                    "Result: x={3:R} y={4:R} z={5:R}",
                    expected.x, expected.y, expected.z,
                    result.x, result.y, result.z));
            }
        }

        private static void CompareAndReport(float result, float expected)
        {
            if (!NearEqual(expected, result))
            {
                throw new Exception(String.Format("Values don't match!\nExpected: p={0} \nResult: p={1}", expected, result));
            }
        }
                    
        private static void CompareAndReport(turbulence.SGSTensor result, turbulence.SGSTensor expected)
        {
            if (!NearEqual(expected.xx, result.xx) || 
                !NearEqual(expected.xy, result.xy) ||
                !NearEqual(expected.xz, result.xz) ||
                !NearEqual(expected.yy, result.yy) ||
                !NearEqual(expected.yz, result.yz) ||
                !NearEqual(expected.zz, result.zz))
            {
                throw new Exception(String.Format("Values don't match!\n"+
                    "Expected:\nexpected_sgs_tensor.xx = {0:R}f;\nexpected_sgs_tensor.xy = {1:R}f;\nexpected_sgs_tensor.xz = {2:R}f;"+
                    "\nexpected_sgs_tensor.yy = {3:R}f;\nexpected_sgs_tensor.yz = {4:R}f;\nexpected_sgs_tensor.zz = {5:R}f;\n"+
                    "Result: xx={6:R} xy={7:R} xz={8:R} yy={9:R} yz={10:R} zz={11:R}",
                    expected.xx, expected.xy, expected.xz, expected.yy, expected.yz, expected.zz,
                    result.xx, result.xy, result.xz, result.yy, result.yz, result.zz));
            }
        }

        private static void CompareAndReport(turbulence.VelocityGradient result, turbulence.VelocityGradient expected)
        {
            if (!NearEqual(expected.duxdx, result.duxdx) || 
                !NearEqual(expected.duxdy, result.duxdy) ||
                !NearEqual(expected.duxdz, result.duxdz) ||
                !NearEqual(expected.duydx, result.duydx) ||
                !NearEqual(expected.duydy, result.duydy) ||
                !NearEqual(expected.duydz, result.duydz) ||
                !NearEqual(expected.duzdx, result.duzdx) ||
                !NearEqual(expected.duzdy, result.duzdy) ||
                !NearEqual(expected.duzdz, result.duzdz))
            {
                throw new Exception(String.Format("Values don't match!\n" +
                    "Expected:\nexpected_gradient.duxdx = {0:R}f;\nexpected_gradient.duxdy = {1:R}f;\nexpected_gradient.duxdz = {2:R}f;" +
                    "\nexpected_gradient.duydx = {3:R}f;\nexpected_gradient.duydy = {4:R}f;\nexpected_gradient.duydz = {5:R}f;" +
                    "\nexpected_gradient.duzdx = {6:R}f;\nexpected_gradient.duzdy = {7:R}f;\nexpected_gradient.duzdz = {8:R}f;\n" +
                    "Result: duxdx={9:R} duxdy={10:R} duxdz={11:R} duydx={12:R} duydy={13:R} duydz={14:R} duzdx={15:R} duzdy={16:R} duzdz={17:R}",
                    expected.duxdx, expected.duxdy, expected.duxdz, expected.duydx, expected.duydy, expected.duydz, expected.duzdx, expected.duzdy, expected.duzdz,
                    result.duxdx, result.duxdy, result.duxdz, result.duydx, result.duydy, result.duydz, result.duzdx, result.duzdy, result.duzdz));
            }
        }

        private static void CompareAndReport(turbulence.VelocityHessian result, turbulence.VelocityHessian expected)
        {
            if (!NearEqual(expected.d2uxdxdx, result.d2uxdxdx) ||
                !NearEqual(expected.d2uxdxdy, result.d2uxdxdy) ||
                !NearEqual(expected.d2uxdxdz, result.d2uxdxdz) ||
                !NearEqual(expected.d2uxdydy, result.d2uxdydy) ||
                !NearEqual(expected.d2uxdydz, result.d2uxdydz) ||
                !NearEqual(expected.d2uxdzdz, result.d2uxdzdz) ||
                !NearEqual(expected.d2uydxdx, result.d2uydxdx) ||
                !NearEqual(expected.d2uydxdy, result.d2uydxdy) ||
                !NearEqual(expected.d2uydxdz, result.d2uydxdz) ||
                !NearEqual(expected.d2uydydy, result.d2uydydy) ||
                !NearEqual(expected.d2uydydz, result.d2uydydz) ||
                !NearEqual(expected.d2uydzdz, result.d2uydzdz) ||
                !NearEqual(expected.d2uzdxdx, result.d2uzdxdx) ||
                !NearEqual(expected.d2uzdxdy, result.d2uzdxdy) ||
                !NearEqual(expected.d2uzdxdz, result.d2uzdxdz) ||
                !NearEqual(expected.d2uzdydy, result.d2uzdydy) ||
                !NearEqual(expected.d2uzdydz, result.d2uzdydz) ||
                !NearEqual(expected.d2uzdzdz, result.d2uzdzdz))
            {
                throw new Exception(String.Format("Values don't match!\n" +
                    "Expected:\nexpected_hessian.d2uxdxdx = {0:R}f;\nexpected_hessian.d2uxdxdy = {1:R}f;\nexpected_hessian.d2uxdxdz = {2:R}f;" +
                    "\nexpected_hessian.d2uxdydy = {3:R}f;\nexpected_hessian.d2uxdydz = {4:R}f;\nexpected_hessian.d2uxdzdz = {5:R}f;" +
                    "\nexpected_hessian.d2uydxdx = {6:R}f;\nexpected_hessian.d2uydxdy = {7:R}f;\nexpected_hessian.d2uydxdz = {8:R}f;" +
                    "\nexpected_hessian.d2uydydy = {9:R}f;\nexpected_hessian.d2uydydz = {10:R}f;\nexpected_hessian.d2uydzdz = {11:R}f;" +
                    "\nexpected_hessian.d2uzdxdx = {12:R}f;\nexpected_hessian.d2uzdxdy = {13:R}f;\nexpected_hessian.d2uzdxdz = {14:R}f;" +
                    "\nexpected_hessian.d2uzdydy = {15:R}f;\nexpected_hessian.d2uzdydz = {16:R}f;\nexpected_hessian.d2uzdzdz = {17:R}f;\n" +
                    "Result: d2uxdxdx={18} d2uxdxdy={19} d2uxdxdz={20} d2uxdydy={21} d2uxdydz={22} d2uxdzdz={23} " +
                    "d2uydxdx={24} d2uydxdy={25} d2uydxdz={26} d2uydydy={27} d2uydydz={28} d2uydzdz={29} " +
                    "d2uzdxdx={30} d2uzdxdy={31} d2uzdxdz={32} d2uzdydy={33} d2uzdydz={34} d2uzdzdz={35}",
                    expected.d2uxdxdx, expected.d2uxdxdy, expected.d2uxdxdz, expected.d2uxdydy, expected.d2uxdydz, expected.d2uxdzdz,
                    expected.d2uydxdx, expected.d2uydxdy, expected.d2uydxdz, expected.d2uydydy, expected.d2uydydz, expected.d2uydzdz,
                    expected.d2uzdxdx, expected.d2uzdxdy, expected.d2uzdxdz, expected.d2uzdydy, expected.d2uzdydz, expected.d2uzdzdz,
                    result.d2uxdxdx, result.d2uxdxdy, result.d2uxdxdz, result.d2uxdydy, result.d2uxdydz, result.d2uxdzdz,
                    result.d2uydxdx, result.d2uydxdy, result.d2uydxdz, result.d2uydydy, result.d2uydydz, result.d2uydzdz,
                    result.d2uzdxdx, result.d2uzdxdy, result.d2uzdxdz, result.d2uzdydy, result.d2uzdydz, result.d2uzdzdz));
            }
        }

        private static void CompareAndReport(turbulence.PressureHessian result, turbulence.PressureHessian expected)
        {
            if (!NearEqual(expected.d2pdxdx, result.d2pdxdx) ||
                !NearEqual(expected.d2pdxdy, result.d2pdxdy) ||
                !NearEqual(expected.d2pdxdz, result.d2pdxdz) ||
                !NearEqual(expected.d2pdydy, result.d2pdydy) ||
                !NearEqual(expected.d2pdydz, result.d2pdydz) ||
                !NearEqual(expected.d2pdzdz, result.d2pdzdz))
            {
                throw new Exception(String.Format("Values don't match!\n" +
                    "Expected:\nexpected_p_hessian.d2pdxdx = {0:R}f;\nexpected_p_hessian.d2pdxdy = {1:R}f;\nexpected_p_hessian.d2pdxdz = {2:R}f;" +
                    "\nexpected_p_hessian.d2pdydy = {3:R}f;\nexpected_p_hessian.d2pdydz = {4:R}f;\nexpected_p_hessian.d2pdzdz = {5:R}f;\n" +
                    "Result: d2pdxdx={6} d2pdxdy={7} d2pdxdz={8} d2pdydy={9} d2pdydz={10} d2pdzdz={11} ",
                    expected.d2pdxdx, expected.d2pdxdy, expected.d2pdxdz, expected.d2pdydy, expected.d2pdydz, expected.d2pdzdz,
                    result.d2pdxdx, result.d2pdxdy, result.d2pdxdz, result.d2pdydy, result.d2pdydz, result.d2pdzdz));
            }
        }

        private static bool NearEqual(float value1, float value2)
        {
            if (Math.Abs(value1 - value2) <= EPSILON)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
