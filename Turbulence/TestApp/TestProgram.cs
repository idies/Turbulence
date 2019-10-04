using System;
using System.Collections.Generic;
using System.Text;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using Turbulence.SQLInterface;
using Turbulence.TurbBatch;
using Turbulence.SciLib;
using TestApp;
using TurbulenceService;

using System.Data.Sql;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.IO;

using HDF.PInvoke;
using hid_t = System.Int64;

namespace TestApp
{
    /// <summary>
    /// 
    /// </summary>
    class TestProgram
    {

        static Random random = new Random();
        static double EPSILON = 0.00002;
        public const bool DEVEL_MODE = false;
        //public const string infodb_string = !DEVEL_MODE ? "turbinfo_conn" : "turbinfo_test_conn";
        public const string infodb_backup_string = !DEVEL_MODE ? "turbinfo_backup_conn" : "";
        public const string infodb_string = "turbinfo_conn";
        public const string logdb_string = "turblog_conn";

        // batch scheduler queue
        public static BatchWorkerQueue batchQueue = null;

        Database database = new Database(infodb_string, DEVEL_MODE);
        AuthInfo authInfo;
        Log log = new Log(logdb_string, DEVEL_MODE);

        /// <summary>
        /// A small application to test the particle tracking and
        /// turbulence code libraries.
        /// </summary>
        /// <remarks>
        /// Currently only used to paste & debug snipits of code...
        /// TODO: Make it a useful, complete, test suite...
        /// </remarks>
        /// <param name="args">Command line arguments</param>
        public static void Main()
        {
            string fname = Path.GetTempFileName();
            hid_t file = H5F.create(fname, H5F.ACC_EXCL);
            // this is expected, because Path.GetTempFileName() creates
            // an empty file
            //Assert.IsFalse(file >= 0);

            file = H5F.create(fname, H5F.ACC_TRUNC);
            //Assert.IsTrue(file >= 0);
            H5F.close(file);
            File.Delete(fname);

            TestProgram testp = new TestProgram();
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();
            //testMorton();

            //localWS.Service service = new localWS.Service();

            try
            {
                DateTime beginTime, stopTime;
                int nx = 1, ny = 2;
                int pointsize = nx * ny * 1;
                Point3[] points = new Point3[pointsize];
                turbulence.Point3[] points1 = new turbulence.Point3[pointsize];
                //Point3[] positions = new Point3[pointsize];
                string authToken = "edu.jhu.pha.turbulence-dev";
                float dd = (float)(3.0 * Math.PI / 7680.0);
                float dx = (float)(8 * Math.PI / nx);
                float dy = (float)(3 * Math.PI / ny);

                beginTime = DateTime.Now;
                float time = 0f;
                service.Timeout = -1;

                points[0].x = 0.3f;// dd * 2048;
                points[0].y = 0.4f;// dd * 2048;
                points[0].z = 0.5f;// dd * 2048;
                points[1].x = 4.0f;// dd * 2048;
                points[1].y = 5.0f; ;// dd * 2048;
                points[1].z = 6.0f;// dd * 2048;
                beginTime = DateTime.Now;
                Console.WriteLine("Calling GetVelocity");
                Vector3[] result = testp.GetVelocity(authToken, "isotropic1024coarse", time,
                    TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, points);
                stopTime = DateTime.Now;
                Console.WriteLine("Execution time: {0}", stopTime - beginTime);

                //for (int i = 0; i < nx; i++)
                //{
                //    for (int j = 0; j < ny; j++)
                //    {
                //        points[i * ny + j].x = (float)0.0;
                //        points[i * ny + j].y = (float)(2.0f * Math.PI / 4096) * j;
                //        points[i * ny + j].z = (float)0.0;
                //    }
                //}

                //beginTime = DateTime.Now;
                //Console.WriteLine("Calling Pressure");
                //for (int i = 0; i < 110; i++)
                //{
                //    if (i==10)
                //    {
                //        beginTime = DateTime.Now;
                //    }
                //    Pressure[] result_pr = testp.GetPressure(authToken, "isotropic4096", time,
                //    TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, points);
                //}
                //stopTime = DateTime.Now;
                //Console.WriteLine("Execution time: {0}", stopTime - beginTime);

                //beginTime = DateTime.Now;
                //Console.WriteLine("Calling GetVelocityGradient");
                //Vector3[] result_vel_grad = testp.GetPressureGradient(authToken, "isotropic1024coarse", time,
                //    TurbulenceOptions.SpatialInterpolation.None_Fd4, TurbulenceOptions.TemporalInterpolation.None, points);
                //stopTime = DateTime.Now;
                //Console.WriteLine("Execution time: {0}", stopTime - beginTime);


                //beginTime = DateTime.Now;
                //Console.WriteLine("Calling GetVelocityGradient");
                //VelocityGradient[] result_vel_hess = testp.GetVelocityGradient(authToken, "channel", time,
                //    TurbulenceOptions.SpatialInterpolation.Fd4Lag4, TurbulenceOptions.TemporalInterpolation.None, points);
                //stopTime = DateTime.Now;
                //Console.WriteLine("Execution time: {0}", stopTime - beginTime);

                //beginTime = DateTime.Now;
                //Console.WriteLine("Calling GetAnyCutoutWeb");
                //DateTime beginTime1 = DateTime.Now;

                //ThresholdInfo[] GetThreshold(string authToken, string dataset, string field, float time, float threshold,
                //    TurbulenceOptions.SpatialInterpolation spatialInterpolation,
                //    int x_start, int y_start, int z_start, int x_end, int y_end, int z_end, string addr = null)
                //ThresholdInfo[] result_raw = testp.GetThreshold(authToken, "isotropic1024coarse", "vorticity", 0.3f, 30, TurbulenceOptions.SpatialInterpolation.None_Fd4, 1, 1, 1, 4, 4, 4);

                //byte[] result_raw = testp.GetRawVelocity(authToken, "isotropic1024coarse", 0,
                //    0, 0, 0, 2, 2, 2);
                //public byte[] GetAnyCutoutWeb(string authToken, string dataset, string field, int T,
                //    int x_start, int y_start, int z_start, int x_end, int y_end, int z_end, int x_step, int y_step, int z_step,
                //    int filter_width, string addr = null)
                //byte[] result_raw = testp.GetAnyCutoutWeb(authToken, "isotropic4096", "u", 1, 1, 1, 1, 10, 10, 10, 1, 1, 1, 1, null);


                //Console.WriteLine("   {0}", DateTime.Now - beginTime1);
                //stopTime = DateTime.Now;
                //Console.WriteLine("Execution time: {0}", stopTime - beginTime);

            }
            catch (Exception E)
            {
                Console.WriteLine(E);
            }
            Console.WriteLine("Called service");

            Console.WriteLine("Hint enter to quit.");
            Console.ReadLine();

        }

        public static void testMorton()
        {
            long zindex = 234815487;
            int[] my = new Morton3D((long)zindex).GetValues();
            Console.WriteLine("zindx={0} X={1} Y={2} Z={3}", zindex, my[2], my[1], my[0]);

            zindex = 536870911;
            my = new Morton3D((long)zindex).GetValues();
            Console.WriteLine("zindx={0} X={1} Y={2} Z={3}", zindex, my[2], my[1], my[0]);

            my[0] = 1; my[1] = 2; my[2] = 1;
            zindex = new Morton3D(my[2], my[1], my[0]).Key;
            Console.WriteLine("X={0} Y={1} Z={2} zindx={3} ", my[0], my[1], my[2], zindex);

            for (int j = 0; j < 1; j++)
            {
                my = new Morton3D((long)j).GetValues();
                Console.WriteLine("zindx={0} X={1} Y={2} Z={3}", j, my[2], my[1], my[0]);
            }
        }

        public Vector3[] GetVelocity(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            object rowid = null;

            int worker = (int)Worker.Workers.GetMHDVelocity;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    GetVectorData(auth, dataset, dataset_enum, DataInfo.TableNames.isotropic1024fine_vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096: //check this
                case DataInfo.DataSets.strat4096:
                    GetVectorData(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetVectorData(auth, dataset, dataset_enum, DataInfo.TableNames.velocity08, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                    worker = (int)Worker.Workers.GetChannelVelocity;
                    GetVectorData(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.bl_zaki:
                    worker = (int)Worker.Workers.GetChannelVelocity;

                    List<int> idx = new List<int>();
                    List<int> idx_not0 = new List<int>();
                    for (int i = 0; i < points.Length; i++)
                    {
                        if (points[i].y < 0.00178944959)
                            idx.Add(i);
                        else
                            idx_not0.Add(i);
                    }

                    if (idx_not0.Count > 0)
                    {
                        Point3[] points1 = new Point3[idx_not0.Count];
                        Vector3[] result1 = new Vector3[idx_not0.Count];
                        for (int i = 0; i < idx_not0.Count; i++)
                        {
                            points1[i].x = points[idx_not0[i]].x;
                            points1[i].y = points[idx_not0[i]].y;
                            points1[i].z = points[idx_not0[i]].z;
                        }
                        GetVectorData(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                            time, spatialInterpolation, temporalInterpolation, points1, result1, ref rowid, addr);
                        for (int i = 0; i < idx_not0.Count; i++)
                        {
                            result[idx_not0[i]] = result1[i];
                        }
                    }

                    if (idx.Count > 0)
                    {
                        database.Initialize(dataset_enum, num_virtual_servers);
                        object rowid1 = null;
                        Point3[] points1 = new Point3[idx.Count];
                        Vector3[] result1 = new Vector3[idx.Count];
                        for (int i = 0; i < idx.Count; i++)
                        {
                            points1[i].x = (float)(Math.Round((points[idx[i]].x - 30.218496172581567) / 0.292210466240511) * 0.292210466240511 + 30.218496172581567);
                            points1[i].y = 0.0f;
                            points1[i].z = (float)(Math.Round(points[idx[i]].z / 0.117244748412311) * 0.117244748412311);
                        }
                        spatialInterpolation = TurbulenceOptions.SpatialInterpolation.Lag4;
                        GetVectorData(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                            time, spatialInterpolation, temporalInterpolation, points1, result1, ref rowid1, addr);
                        for (int i = 0; i < idx.Count; i++)
                        {
                            result[idx[i]].x = 0.0f;
                            result[idx[i]].y = 0.0f;
                            result[idx[i]].z = 0.0f;
                        }
                        log.UpdateLogRecord(rowid1, database.Bitfield);
                    }


                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        public VelocityGradient[] GetVelocityGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            if (authToken == "uk.ac.manchester.zhao.wu-ea658424")
            {
                auth = new AuthInfo.AuthToken("uk.ac.manchester.zhao.wu-ea658424", 10165, 0);
            }
            VelocityGradient[] result = new VelocityGradient[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            object rowid = null;

            int worker = (int)Worker.Workers.GetMHDVelocityGradient;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.isotropic1024fine_vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.strat4096:
                    GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.velocity08, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                    worker = (int)Worker.Workers.GetChannelVelocityGradient;
                    GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.bl_zaki:
                    worker = (int)Worker.Workers.GetChannelVelocityGradient;
                    if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None_Fd4)
                    {
                        List<int> idx = new List<int>();
                        List<int> idx_not0 = new List<int>();
                        for (int i = 0; i < points.Length; i++)
                        {
                            if (points[i].y < 0.00178944959)
                                idx.Add(i);
                            else
                                idx_not0.Add(i);
                        }

                        if (idx_not0.Count > 0)
                        {
                            Point3[] points1 = new Point3[idx_not0.Count];
                            VelocityGradient[] result1 = new VelocityGradient[idx_not0.Count];
                            for (int i = 0; i < idx_not0.Count; i++)
                            {
                                points1[i].x = points[idx_not0[i]].x;
                                points1[i].y = points[idx_not0[i]].y;
                                points1[i].z = points[idx_not0[i]].z;
                            }
                            GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                                time, spatialInterpolation, temporalInterpolation, points1, result1, ref rowid, addr);
                            for (int i = 0; i < idx_not0.Count; i++)
                            {
                                result[idx_not0[i]] = result1[i];
                            }
                        }

                        if (idx.Count > 0)
                        {
                            database.Initialize(dataset_enum, num_virtual_servers);
                            object rowid1 = null;
                            Point3[] points1 = new Point3[idx.Count];
                            VelocityGradient[] result1 = new VelocityGradient[idx.Count];
                            for (int i = 0; i < idx.Count; i++)
                            {
                                points1[i].x = (float)(Math.Round((points[idx[i]].x - 30.218496172581567) / 0.292210466240511) * 0.292210466240511 + 30.218496172581567);
                                points1[i].y = 0.0f;
                                points1[i].z = (float)(Math.Round(points[idx[i]].z / 0.117244748412311) * 0.117244748412311);
                            }
                            spatialInterpolation = TurbulenceOptions.SpatialInterpolation.Fd4Lag4;
                            GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                                time, spatialInterpolation, temporalInterpolation, points1, result1, ref rowid1, addr);
                            for (int i = 0; i < idx.Count; i++)
                            {
                                result[idx[i]] = result1[i];
                            }
                            log.UpdateLogRecord(rowid1, database.Bitfield);
                        }
                    }
                    else
                    {
                        GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                            time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        public Pressure[] GetPressure(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            Pressure[] result = new Pressure[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            object rowid = null;
            DataInfo.verifyTimeInRange(dataset_enum, time);

            int worker = (int)Worker.Workers.GetMHDPressure;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    GetScalarData(auth, dataset, dataset_enum, DataInfo.TableNames.isotropic1024fine_pr, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096: //check this                  
                    GetScalarData(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetScalarData(auth, dataset, dataset_enum, DataInfo.TableNames.pressure08, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                    worker = (int)Worker.Workers.GetChannelPressure;
                    GetScalarData(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.bl_zaki:
                    worker = (int)Worker.Workers.GetChannelPressure;

                    if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None)
                    {
                        List<int> idx = new List<int>();
                        List<int> idx_not0 = new List<int>();
                        for (int i = 0; i < points.Length; i++)
                        {
                            if (points[i].y < 0.00178944959)
                                idx.Add(i);
                            else
                                idx_not0.Add(i);
                        }

                        if (idx_not0.Count > 0)
                        {
                            Point3[] points1 = new Point3[idx_not0.Count];
                            Pressure[] result1 = new Pressure[idx_not0.Count];
                            for (int i = 0; i < idx_not0.Count; i++)
                            {
                                points1[i].x = points[idx_not0[i]].x;
                                points1[i].y = points[idx_not0[i]].y;
                                points1[i].z = points[idx_not0[i]].z;
                            }
                            GetScalarData(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                                time, spatialInterpolation, temporalInterpolation, points1, result1, ref rowid, addr);
                            for (int i = 0; i < idx_not0.Count; i++)
                            {
                                result[idx_not0[i]] = result1[i];
                            }
                        }

                        if (idx.Count > 0)
                        {
                            database.Initialize(dataset_enum, num_virtual_servers);
                            object rowid1 = null;
                            Point3[] points1 = new Point3[idx.Count];
                            Pressure[] result1 = new Pressure[idx.Count];
                            for (int i = 0; i < idx.Count; i++)
                            {
                                points1[i].x = (float)(Math.Round((points[idx[i]].x - 30.218496172581567) / 0.292210466240511) * 0.292210466240511 + 30.218496172581567);
                                points1[i].y = 0.0f;
                                points1[i].z = (float)(Math.Round(points[idx[i]].z / 0.117244748412311) * 0.117244748412311);
                            }
                            spatialInterpolation = TurbulenceOptions.SpatialInterpolation.Lag4;
                            GetScalarData(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                                time, spatialInterpolation, temporalInterpolation, points1, result1, ref rowid1, addr);
                            for (int i = 0; i < idx.Count; i++)
                            {
                                result[idx[i]] = result1[i];
                            }
                            log.UpdateLogRecord(rowid1, database.Bitfield);
                        }
                    }
                    else
                    {
                        GetScalarData(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                            time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        public Vector3[] GetPressureGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            object rowid = null;

            int worker = (int)Worker.Workers.GetMHDPressureGradient;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    GetScalarGradient(auth, dataset, dataset_enum, DataInfo.TableNames.isotropic1024fine_pr, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096: //check this                  
                    GetScalarGradient(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetScalarGradient(auth, dataset, dataset_enum, DataInfo.TableNames.pressure08, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.channel:
                    worker = (int)Worker.Workers.GetChannelPressureGradient;
                    GetScalarGradient(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.bl_zaki:
                    worker = (int)Worker.Workers.GetChannelPressureGradient;

                    if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None_Fd4)
                    {
                        List<int> idx = new List<int>();
                        List<int> idx_not0 = new List<int>();
                        for (int i = 0; i < points.Length; i++)
                        {
                            if (points[i].y < 0.00178944959)
                                idx.Add(i);
                            else
                                idx_not0.Add(i);
                        }
                        Point3[] points1 = new Point3[idx_not0.Count];
                        Vector3[] result1 = new Vector3[idx_not0.Count];
                        for (int i = 0; i < idx_not0.Count; i++)
                        {
                            points1[i].x = points[idx_not0[i]].x;
                            points1[i].y = points[idx_not0[i]].y;
                            points1[i].z = points[idx_not0[i]].z;
                        }
                        GetScalarGradient(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                            time, spatialInterpolation, temporalInterpolation, points1, result1, ref rowid, addr);
                        for (int i = 0; i < idx_not0.Count; i++)
                        {
                            result[idx_not0[i]] = result1[i];
                        }

                        points1 = new Point3[idx.Count];
                        result1 = new Vector3[idx.Count];
                        for (int i = 0; i < idx.Count; i++)
                        {
                            points1[i].x = (float)(Math.Round((points[idx[i]].x - 30.218496172581567) / 0.292210466240511) * 0.292210466240511 + 30.218496172581567);
                            points1[i].y = 0.0f;
                            points1[i].z = (float)(Math.Round(points[idx[i]].z / 0.117244748412311) * 0.117244748412311);
                        }
                        spatialInterpolation = TurbulenceOptions.SpatialInterpolation.Fd4Lag4;
                        GetScalarGradient(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                            time, spatialInterpolation, temporalInterpolation, points1, result1, ref rowid, addr);
                        for (int i = 0; i < idx.Count; i++)
                        {
                            result[idx[i]] = result1[i];
                        }
                    }
                    else
                    {
                        worker = (int)Worker.Workers.GetChannelVelocity;
                        GetScalarGradient(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                            time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        private void GetVectorData(AuthInfo.AuthToken auth, string dataset, DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, Vector3[] result, ref object rowid, string addr = null)
        {
            bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
            int kernelSize = -1;
            int kernelSizeY = -1;

            bool IsChannelGrid = (dataset_enum == DataInfo.DataSets.channel || dataset_enum == DataInfo.DataSets.bl_zaki) ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);
            if (auth.name == "edu.jhu.pha.turbulence-monitor" || auth.name == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null, addr);
            log.UpdateRecordCount(auth.Id, points.Length);

            //database.AddBulkParticles(points, round, spatialInterpolation, worker);
            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetVectorData(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
        }

        private void GetScalarData(AuthInfo.AuthToken auth, string dataset, DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, Pressure[] result, ref object rowid, string addr = null)
        {
            bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
            int kernelSize = -1;
            int kernelSizeY = -1;

            bool IsChannelGrid = (dataset_enum == DataInfo.DataSets.channel || dataset_enum == DataInfo.DataSets.bl_zaki) ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);
            if (auth.name == "edu.jhu.pha.turbulence-monitor" || auth.name == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null, addr);
            log.UpdateRecordCount(auth.Id, points.Length);

            //database.AddBulkParticles(points, round, spatialInterpolation, worker);
            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetScalarData(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
        }

        private void GetVectorGradient(AuthInfo.AuthToken auth, string dataset, DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, VelocityGradient[] result, ref object rowid, string addr = null)
        {
            bool round = true;
            int kernelSize = -1;
            int kernelSizeY = -1;

            bool IsChannelGrid = (dataset_enum == DataInfo.DataSets.channel || dataset_enum == DataInfo.DataSets.bl_zaki) ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);
            if (auth.name == "edu.jhu.pha.turbulence-monitor" || auth.name == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null, addr);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetVectorGradient(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
        }

        private void GetScalarGradient(AuthInfo.AuthToken auth, string dataset, DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, Vector3[] result, ref object rowid, string addr = null)
        {
            bool round = true;
            int kernelSize = -1;
            int kernelSizeY = -1;

            bool IsChannelGrid = (dataset_enum == DataInfo.DataSets.channel || dataset_enum == DataInfo.DataSets.bl_zaki) ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);
            if (auth.name == "edu.jhu.pha.turbulence-monitor" || auth.name == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null, addr);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetScalarGradient(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
        }

        public byte[] GetRawVelocity(string authToken, string dataset, int T,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, string addr = null)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            float time = T * database.Dt * database.TimeInc;
            DataInfo.verifyTimeInRange(dataset_enum, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the 3 components of the velocity field
            int components = 3;
            byte[] result = null;
            DataInfo.TableNames tableName;

            if (dataset_enum == DataInfo.DataSets.channel)
            {
                T = T + 132005;
            }

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    tableName = DataInfo.TableNames.isotropic1024fine_vel;
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096: //This is not really used in filedb, but we don't want to get an invalid dataset.
                case DataInfo.DataSets.strat4096:
                    tableName = DataInfo.TableNames.vel;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.velocity08;
                    break;
                //case DataInfo.DataSets.rmhd:
                //    tableName = DataInfo.TableNames.vel;
                //    components = 2;
                //    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    tableName = DataInfo.TableNames.vel;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawVelocity,
                (int)TurbulenceOptions.SpatialInterpolation.None,
                (int)TurbulenceOptions.TemporalInterpolation.None,
                Xwidth * Ywidth * Zwidth, time, null, null, addr);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

            result = database.GetRawData(dataset_enum, tableName, T, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        public byte[] GetAnyCutoutWeb(string authToken, string dataset, string field, int T,
            int x_start, int y_start, int z_start, int x_end, int y_end, int z_end, int x_step, int y_step, int z_step,
            int filter_width, string addr = null)
        {
            if (x_start > x_end || y_start > y_end || z_start > z_end)
            {
                throw new Exception(String.Format("Ending index must be larger or equal to the starting index!"));
            }

            T = T - 1;
            int X = x_start - 1;
            int Y = y_start - 1;
            int Z = z_start - 1;
            int Xwidth = x_end - x_start + 1;
            int Ywidth = y_end - y_start + 1;
            int Zwidth = z_end - z_start + 1;

            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            float time = T * database.Dt * database.TimeInc;
            DataInfo.verifyTimeInRange(dataset_enum, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the scalar pressure field
            byte[] result = null;
            int components = new int();

            T = T * database.TimeInc + database.TimeOff;
            DataInfo.TableNames tableName = DataInfo.getTableName(dataset_enum, field[0].ToString());
            int worker = new int();
            switch (field[0])
            {
                case 'u':
                    worker = (int)Worker.Workers.GetCutoutVelocity;
                    components = 3;
                    break;
                case 'a':
                    worker = (int)Worker.Workers.GetCutoutMagnetic;
                    components = 3;
                    break;
                case 'b':
                    worker = (int)Worker.Workers.GetCutoutPotential;
                    components = 3;
                    break;
                case 'p':
                    worker = (int)Worker.Workers.GetCutoutPressure;
                    components = 1;
                    break;
                case 'd':
                    worker = (int)Worker.Workers.GetCutoutDensity;
                    components = 1;
                    break;
                case 't':
                    worker = (int)Worker.Workers.GetCutoutTemperature;
                    components = 1;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)TurbulenceOptions.SpatialInterpolation.None,
                (int)TurbulenceOptions.TemporalInterpolation.None,
               Xwidth * Ywidth * Zwidth, time, null, null, addr);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

            if ((long)components * (long)((Xwidth + x_step - 1) / x_step) * (long)((Ywidth + y_step - 1) / y_step) * (long)((Zwidth + z_step - 1) / z_step) > 192000000)
            {
                throw new Exception(String.Format("The getCutout query should be less than 64000000 points for vector fields or 192000000 points for scalar fields!"));
            }

            //DateTime beginTime = DateTime.Now;
            result = database.GetCutoutData(dataset_enum, tableName, T, components, X, Y, Z, Xwidth, Ywidth, Zwidth,
                x_step, y_step, z_step, filter_width);
            //DateTime stopTime = DateTime.Now;
            //string elapsedTime = String.Format("GetCutoutData done: {0}", stopTime - beginTime);
            //string[] tester = { elapsedTime };
            //System.IO.File.WriteAllLines(@"C:\www\JHTDB.txt", tester);

            //beginTime = DateTime.Now;
            //float[] data = new float[(long)components * (long)((Xwidth + x_step - 1) / x_step) * (long)((Ywidth + y_step - 1) / y_step) * (long)((Zwidth + z_step - 1) / z_step)];
            //unsafe
            //{
            //    // TODO: This code is still far from optimal...
            //    //       Why can't we pass a reference to the float array directly?
            //    fixed (byte* brawdata = result)
            //    {
            //        fixed (float* fdata = data)
            //        {
            //            float* frawdata = (float*)brawdata;
            //            for (int i = 0; i < data.Length; i++)
            //            {
            //                data[i] = frawdata[i];
            //            }
            //        }
            //    }
            //}
            //stopTime = DateTime.Now;
            //elapsedTime = String.Format("To float done: {0}", stopTime - beginTime);
            //System.IO.File.AppendAllText(@"C:\www\JHTDB.txt", elapsedTime);

            log.UpdateLogRecord(rowid, database.Bitfield);
            //elapsedTime = String.Format("Transferring data, size: {0}", result.Length);
            //System.IO.File.AppendAllText(@"C:\www\JHTDB.txt", elapsedTime + System.Environment.NewLine);
            return result;

        }

        public ThresholdInfo[] GetThreshold(string authToken, string dataset, string field, float time, float threshold,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            int x_start, int y_start, int z_start, int x_end, int y_end, int z_end, string addr = null)
        {
            if (x_start > x_end || y_start > y_end || z_start > z_end)
            {
                throw new Exception(String.Format("Ending index must be larger or equal to the starting index!"));
            }

            int X = x_start - 1;
            int Y = y_start - 1;
            int Z = z_start - 1;
            int Xwidth = x_end - x_start + 1;
            int Ywidth = y_end - y_start + 1;
            int Zwidth = z_end - z_start + 1;

            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 4;
            database.Initialize(dataset_enum, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            object rowid = null;

            DataInfo.TableNames tableName = DataInfo.getTableName(dataset_enum, field);

            int worker;
            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.mhd1024:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.strat4096:
                    if (field.Contains("vorticity"))
                    {
                        worker = (int)Worker.Workers.GetCurlThreshold;
                    }
                    else if (field.Contains("q"))
                    {
                        worker = (int)Worker.Workers.GetQThreshold;
                    }
                    else if (field.Equals("u") || field.Contains("vel") || field.Contains("Vel"))
                    {
                        worker = (int)Worker.Workers.GetVelocityThreshold;
                    }
                    else if (field.Equals("b") || field.Contains("mag") || field.Contains("Mag"))
                    {
                        worker = (int)Worker.Workers.GetMagneticThreshold;
                    }
                    else if (field.Equals("a") || field.Contains("vec") || field.Contains("pot") || field.Contains("Vec"))
                    {
                        worker = (int)Worker.Workers.GetPotentialThreshold;
                    }
                    else if (field.Equals("p") || field.Contains("pr") || field.Contains("Pr"))
                    {
                        worker = (int)Worker.Workers.GetPressureThreshold;
                    }
                    else if (field.Equals("d") || field.Contains("density") || field.Contains("Density"))
                    {
                        worker = (int)Worker.Workers.GetDensityThreshold;
                    }
                    else if (field.Equals("t") || field.Contains("th") || field.Contains("Th") || field.Contains("tem") || field.Contains("Tem") || field.Contains("phi") || field.Contains("Phi"))
                    {
                        worker = (int)Worker.Workers.GetPressureThreshold;
                    }
                    else
                    {
                        throw new Exception("Invalid field specified");
                    }
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    switch (field)
                    {
                        case "vorticity":
                            worker = (int)Worker.Workers.GetChannelCurlThreshold;
                            break;
                        case "q":
                            worker = (int)Worker.Workers.GetChannelQThreshold;
                            break;
                        case "velocity":
                            worker = (int)Worker.Workers.GetChannelVelocityThreshold;
                            break;
                        case "pressure":
                            worker = (int)Worker.Workers.GetChannelPressureThreshold;
                            break;
                        default:
                            throw new Exception("Invalid field specified");
                    }
                    break;
                default:
                    throw new Exception("Invalid dataset specified");
            }

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)TurbulenceOptions.TemporalInterpolation.None,
                Xwidth * Ywidth * Zwidth, time, null, null, addr);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

            List<ThresholdInfo> points_above_threshold = new List<ThresholdInfo>();
            database.ExecuteGetThreshold(dataset_enum, tableName, worker, time, spatialInterpolation, threshold,
                X, Y, Z, Xwidth, Ywidth, Zwidth, points_above_threshold);

            log.UpdateLogRecord(rowid, database.Bitfield);


            points_above_threshold.Sort((t1, t2) => -1 * t1.value.CompareTo(t2.value));

            ThresholdInfo[] result = points_above_threshold.ToArray();
            for (int i = 0; i < result.Length; i++)
            {
                result[i].x = result[i].x + 1;
                result[i].y = result[i].y + 1;
                result[i].z = result[i].z + 1;
            }

            return result;
        }
    }
}
