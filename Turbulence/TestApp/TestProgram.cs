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
            TestProgram testp = new TestProgram();
            turbulence.TurbulenceService service = new turbulence.TurbulenceService();
            //testMorton();

            //localWS.Service service = new localWS.Service();

            try
            {
                DateTime beginTime, stopTime;
                int nx = 1, ny = 1;
                int pointsize = nx * ny * 1;
                Point3[] points = new Point3[pointsize];
                turbulence.Point3[] points1 = new turbulence.Point3[pointsize];
                //Point3[] positions = new Point3[pointsize];
                string authToken = "edu.jhu.pha.turbulence-dev";
                float dd = (float)(3.0 * Math.PI / 7680.0);
                float dx = (float)(8 * Math.PI / nx);
                float dy = (float)(3 * Math.PI / ny);

                beginTime = DateTime.Now;
                float time = 14.864656326581096f;
                service.Timeout = -1;

                points[0].x = 21.936892f;// dd * 2048;
                points[0].y = -0.9920484f;// dd * 2048;
                points[0].z = 0.7583949f;// dd * 2048;
                //points[1].x = dd * 1023;// dd * 2048;
                //points[1].y = dd * 1000; ;// dd * 2048;
                //points[1].z = dd * 1000;// dd * 2048;
                //beginTime = DateTime.Now;
                //Console.WriteLine("Calling GetVelocity");
                //Vector3[] result = testp.GetVelocity(authToken, "channel", time,
                //    TurbulenceOptions.SpatialInterpolation.Lag6, TurbulenceOptions.TemporalInterpolation.None, points);
                //stopTime = DateTime.Now;
                //Console.WriteLine("Execution time: {0}", stopTime - beginTime);

                //for (int i = 0; i < 1; i++)
                //{
                //    for (int j = 0; j < 1; j++)
                //    {
                //        points[i * 100 + j].x = 30.218496172581567f;// dd * 2048;
                //        points[i * 100 + j].y = 0.00357889929984090f;// dd * 2048;
                //        points[i * 100 + j].z = 9.84855886663f;// dd * 2048;
                //    }
                //}

                //beginTime = DateTime.Now;
                //Console.WriteLine("Calling GetVelocity");
                //Pressure[] result_pr = testp.GetPressure(authToken, "bl_zaki", time,
                //    TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, points);
                //stopTime = DateTime.Now;
                //Console.WriteLine("Execution time: {0}", stopTime - beginTime);

                //beginTime = DateTime.Now;
                //Console.WriteLine("Calling GetVelocityGradient");
                //Vector3[] result_vel_grad = testp.GetPressureGradient(authToken, "channel", time,
                //    TurbulenceOptions.SpatialInterpolation.None_Fd4, TurbulenceOptions.TemporalInterpolation.None, points);
                //stopTime = DateTime.Now;
                //Console.WriteLine("Execution time: {0}", stopTime - beginTime);


                //beginTime = DateTime.Now;
                //Console.WriteLine("Calling GetVelocityGradient");
                //VelocityGradient[] result_vel_hess = testp.GetVelocityGradient(authToken, "channel", time,
                //    TurbulenceOptions.SpatialInterpolation.Fd4Lag4, TurbulenceOptions.TemporalInterpolation.None, points);
                //stopTime = DateTime.Now;
                //Console.WriteLine("Execution time: {0}", stopTime - beginTime);

                beginTime = DateTime.Now;
                Console.WriteLine("Calling GetAnyCutoutWeb");
                DateTime beginTime1 = DateTime.Now;
                //byte[] result_raw = testp.GetRawVelocity(authToken, "isotropic1024coarse", 1,
                //    0, 0, 0, 64, 1024, 1024);
                //public byte[] GetAnyCutoutWeb(string authToken, string dataset, string field, int T,
                //    int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, int x_step, int y_step, int z_step,
                //    int filter_width, string addr = null)
                float[] result_raw = testp.GetAnyCutoutWeb(authToken, "channel", "u", 2, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, null);
                Console.WriteLine("   {0}", DateTime.Now - beginTime1);
                stopTime = DateTime.Now;
                Console.WriteLine("Execution time: {0}", stopTime - beginTime);

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

        public float[] GetAnyCutoutWeb(string authToken, string dataset, string field, int T,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, int x_step, int y_step, int z_step,
            int filter_width, string addr = null)
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
            DataInfo.verifyTimeInRange(dataset_enum, T * database.Dt * database.TimeInc);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the scalar pressure field
            int components = new int();

            T = T * database.TimeInc+database.TimeOff;
            DataInfo.TableNames tableName = DataInfo.getTableName(dataset_enum, field[0].ToString());
            int worker = new int();
            switch (field[0])
            {
                case 'u':
                    worker = (int)Worker.Workers.GetFilteredVelocity;
                    components = 3;
                    break;
                case 'a':
                    worker = (int)Worker.Workers.GetFilteredMagnetic;
                    components = 3;
                    break;
                case 'b':
                    worker = (int)Worker.Workers.GetFilteredPotential;
                    components = 3;
                    break;
                case 'p':
                    worker = (int)Worker.Workers.GetFilteredPressure;
                    components = 1;
                    break;
                case 'd':
                    worker = (int)Worker.Workers.GetFilteredDensity;
                    components = 1;
                    break;
                case 't':
                    worker = (int)Worker.Workers.GetFilteredTemperature;
                    components = 1;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)TurbulenceOptions.SpatialInterpolation.None,
                (int)TurbulenceOptions.TemporalInterpolation.None,
               Xwidth * Ywidth * Zwidth, T * database.Dt * database.TimeInc, null, null, addr);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

            byte[] result = database.GetCutoutData(dataset_enum, tableName, T, components, X, Y, Z, Xwidth, Ywidth, Zwidth,
                x_step, y_step, z_step, filter_width);
            float[] data = new float[(long)components * (long)((Xwidth + x_step - 1) / x_step) * (long)((Ywidth + y_step - 1) / y_step) * (long)((Zwidth + z_step - 1) / z_step)];

            unsafe
            {
                // TODO: This code is still far from optimal...
                //       Why can't we pass a reference to the float array directly?
                fixed (byte* brawdata = result)
                {
                    fixed (float* fdata = data)
                    {
                        float* frawdata = (float*)brawdata;
                        for (int i = 0; i < data.Length; i++)
                        {
                            data[i] = frawdata[i];
                        }
                    }
                }
            }
            log.UpdateLogRecord(rowid, database.Bitfield);
            return data;

        }
    }
}
