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
        public const bool DEVEL_MODE = true;
        public const string infodb_string = !DEVEL_MODE ? "turbinfo_conn" : "turbinfo_test_conn";
        public const string infodb_backup_string = !DEVEL_MODE ? "turbinfo_backup_conn" : "";
        //public const string infodb_string = "turbinfo_test_conn";
        public const string logdb_string = "turblog_conn";

        // batch scheduler queue
        public static BatchWorkerQueue batchQueue = null;

        Database database = new Database(infodb_string, DEVEL_MODE);
        //AuthInfo authInfo = new AuthInfo(infodb, "mydbsql", DEVEL_MODE);
        Log log = new Log(infodb_string, DEVEL_MODE);

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
                int pointsize = 1 * 1;
                Point3[] points = new Point3[pointsize];
                turbulence.Point3[] points1 = new turbulence.Point3[pointsize];
                //Point3[] positions = new Point3[pointsize];
                string authToken = "edu.jhu.pha.turbulence-dev";
                float dd = 0.0015339807878856412f;

                beginTime = DateTime.Now;
                for (int i = 0; i < 1; i++)
                {
                    for (int j = 0; j < 1; j++)
                    {
                        points[i * 100 + j].x = 0.5f;// dd * 2048;
                        points[i * 100 + j].y = 0f;// dd * 2048;
                        points[i * 100 + j].z = 0.5f;// dd * 2048;
                    }
                }

                float time = 0.001f;
                service.Timeout = -1;

                beginTime = DateTime.Now;
                Console.WriteLine("Calling GetVelocity");
                Vector3[] result = testp.GetVelocity(authToken, "bl_zaki", time,
                    TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, points);
                stopTime = DateTime.Now;
                Console.WriteLine("Execution time: {0}", stopTime - beginTime);
                //for (int i = 0; i < pointsize; i++)
                //{
                //    Console.WriteLine("U={0} V={1} W={2}", result[i].x, result[i].y, result[i].z);
                //}
                //Console.WriteLine("X={0} Y={1} Z={2}", result[0].x, result[0].y, result[0].z);

                beginTime = DateTime.Now;
                Console.WriteLine("Calling GetPressure");
                Pressure[] result_p = testp.GetPressure(authToken, "bl_zaki", time,
                    TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, points);
                stopTime = DateTime.Now;
                Console.WriteLine("Execution time: {0}", stopTime - beginTime);

                beginTime = DateTime.Now;
                Console.WriteLine("Calling GetTemperature");
                Pressure[] result_t = testp.GetTemperature(authToken, "strat4096", time,
                    TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, points);
                stopTime = DateTime.Now;
                Console.WriteLine("Execution time: {0}", stopTime - beginTime);
                //for (int i = 0; i < pointsize; i++)
                //{
                //    Console.WriteLine("P={0}", result_p[i].p);
                //}
                //Console.WriteLine("P={0}", result_p[0].p);

                beginTime = DateTime.Now;
                Console.WriteLine("Calling GetVelocityGradient");
                VelocityGradient[] result_vel_grad = testp.GetVelocityGradient(authToken, "bl_zaki", time,
                    TurbulenceOptions.SpatialInterpolation.Fd4Lag4, TurbulenceOptions.TemporalInterpolation.None, points);
                stopTime = DateTime.Now;
                Console.WriteLine("Execution time: {0}", stopTime - beginTime);
                //for (int i = 0; i < pointsize; i++)
                //{
                //    Console.WriteLine("duxdx={0} duydy={1} duzdz={2} sum={3}",
                //        result_vel_grad[i].duxdx, result_vel_grad[i].duydy, result_vel_grad[i].duzdz,
                //        result_vel_grad[i].duxdx + result_vel_grad[i].duydy + result_vel_grad[i].duzdz);
                //}
                //for (int i = 0; i < pointsize; i++)
                //{
                //    Console.WriteLine("duxdx={0} duxdy={1} duxdz={2}\nduydx={3} duydy={4} duydz={5}\nduzdx={6} duzdy={7} duzdz={8}",
                //        result_vel_grad[i].duxdx, result_vel_grad[i].duxdy, result_vel_grad[i].duxdz,
                //        result_vel_grad[i].duydx, result_vel_grad[i].duydy, result_vel_grad[i].duydz,
                //        result_vel_grad[i].duzdx, result_vel_grad[i].duzdy, result_vel_grad[i].duzdz);
                //}
                //Console.WriteLine("duxdx={0} duydy={1} duzdz={2} sum={3}",
                //result_vel_grad[0].duxdx, result_vel_grad[0].duydy, result_vel_grad[0].duzdz,
                //result_vel_grad[0].duxdx + result_vel_grad[0].duydy + result_vel_grad[0].duzdz);

                beginTime = DateTime.Now;
                Console.WriteLine("Calling GetPressureGradient");
                Vector3[] result_pr_grad = testp.GetPressureGradient(authToken, "bl_zaki", time,
                    TurbulenceOptions.SpatialInterpolation.Fd4Lag4, TurbulenceOptions.TemporalInterpolation.None, points);
                stopTime = DateTime.Now;
                Console.WriteLine("Execution time: {0}", stopTime - beginTime);

                beginTime = DateTime.Now;
                Console.WriteLine("Calling GetPressureHessian");
                PressureHessian[] result_pr_hes;
                result_pr_hes = testp.GetPressureHessian(authToken, "bl_zaki", time,
                    TurbulenceOptions.SpatialInterpolation.None_Fd4, TurbulenceOptions.TemporalInterpolation.None, points);
                stopTime = DateTime.Now;
                Console.WriteLine("Execution time: {0}", stopTime - beginTime);
                //for (int i = 0; i < pointsize; i++)
                //{
                //    Console.WriteLine("d2pdxdx={0} d2pdydy={1} d2pdzdz={2} sum={3}",
                //        result_pr_hes[i].d2pdxdx, result_pr_hes[i].d2pdydy, result_pr_hes[i].d2pdzdz,
                //        result_pr_hes[i].d2pdxdx + result_pr_hes[i].d2pdydy + result_pr_hes[i].d2pdzdz);
                //}

                //var csv1 = new StringBuilder();
                //var csv2 = new StringBuilder();
                //var csv3 = new StringBuilder();
                //for (int i = 0; i < pointsize; i++)
                //{
                //    //Suggestion made by KyleMit
                //    var newLine1 = string.Format("{0},{1},{2},{3}", result[i].x, result[i].y, result[i].z, result_p[i].p);
                //    csv1.AppendLine(newLine1);
                //    var newLine2 = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                //        result_vel_grad[i].duxdx, result_vel_grad[i].duxdy, result_vel_grad[i].duxdz,
                //        result_vel_grad[i].duydx, result_vel_grad[i].duydy, result_vel_grad[i].duydz,
                //        result_vel_grad[i].duzdx, result_vel_grad[i].duzdy, result_vel_grad[i].duzdz);
                //    csv2.AppendLine(newLine2);
                //    var newLine3 = string.Format("{0},{1},{2},{3},{4},{5}",
                //        result_pr_hes[i].d2pdxdx, result_pr_hes[i].d2pdxdy, result_pr_hes[i].d2pdxdz,
                //        result_pr_hes[i].d2pdydy, result_pr_hes[i].d2pdydz, result_pr_hes[i].d2pdzdz);
                //    csv3.AppendLine(newLine3);
                //}
                //File.WriteAllText("C:\\Users\\zwu27\\Documents\\vel_pr.txt", csv1.ToString());
                //File.WriteAllText("C:\\Users\\zwu27\\Documents\\vel_grad.txt", csv2.ToString());
                //File.WriteAllText("C:\\Users\\zwu27\\Documents\\pre_hes.txt", csv3.ToString());

                beginTime = DateTime.Now;
                Console.WriteLine("Calling GetThreshold");
                ThresholdInfo[] result_threshold = testp.GetThreshold(authToken, "bl_zaki", "q", time, -2000,
                    TurbulenceOptions.SpatialInterpolation.None_Fd4,
                    511, 5, 5, 1, 1, 1);
                stopTime = DateTime.Now;
                Console.WriteLine("Execution time: {0}", stopTime - beginTime);

                beginTime = DateTime.Now;
                Console.WriteLine("Calling GetBoxFilter");
                //Vector3[] result2 = testp.GetBoxFilter(authToken, "bl_zaki", "p", time,
                //     dd * 5, points);
                stopTime = DateTime.Now;
                Console.WriteLine("Execution time: {0}", stopTime - beginTime);

                beginTime = DateTime.Now;
                Console.WriteLine("Calling GetPosition");
                Point3[] resultp = testp.GetPosition(authToken, "channel", 0, 0.004f,
                    0.001f, TurbulenceOptions.SpatialInterpolation.None, points);
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

        private Vector3[] GetVelocity(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (auth.name == "dev")
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
                    GetMHDData(auth, dataset, dataset_enum, DataInfo.TableNames.isotropic1024fine_vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096: //check this
                    GetMHDData(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetMHDData(auth, dataset, dataset_enum, DataInfo.TableNames.velocity08, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.bl_zaki:
                    worker = (int)Worker.Workers.GetChannelVelocity;
                    GetMHDData(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
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
            Point3[] points)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (auth.name == "dev")
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

            bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
            int kernelSize = -1;
            int kernelSizeY = -1;

            DataInfo.TableNames tableName;
            int worker;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    tableName = DataInfo.TableNames.isotropic1024fine_pr;
                    worker = (int)Worker.Workers.GetMHDPressure;
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096: //check this
                    tableName = DataInfo.TableNames.pr;
                    worker = (int)Worker.Workers.GetMHDPressure;
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.bl_zaki:
                    tableName = DataInfo.TableNames.pr;
                    worker = (int)Worker.Workers.GetChannelPressure;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.pressure08;
                    worker = (int)Worker.Workers.GetMHDPressure;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            bool IsChannelGrid = dataset_enum == DataInfo.DataSets.channel ? true : false;
            IsChannelGrid = dataset_enum == DataInfo.DataSets.bl_zaki ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetMHDData(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        public VelocityGradient[] GetVelocityGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (auth.name == "dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
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
                    GetMHDGradient(auth, dataset, dataset_enum, DataInfo.TableNames.isotropic1024fine_vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096:
                    GetMHDGradient(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetMHDGradient(auth, dataset, dataset_enum, DataInfo.TableNames.velocity08, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.bl_zaki:
                    GetMHDGradient(auth, dataset, dataset_enum, DataInfo.TableNames.vel, (int)Worker.Workers.GetChannelVelocityGradient,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
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
            Point3[] points)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (auth.name == "dev")
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

            bool round = true;
            int kernelSize = -1;
            int kernelSizeY = -1;

            DataInfo.TableNames tableName;
            int worker;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    tableName = DataInfo.TableNames.isotropic1024fine_pr;
                    worker = (int)Worker.Workers.GetMHDPressureGradient;
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096: //check this
                    tableName = DataInfo.TableNames.pr;
                    worker = (int)Worker.Workers.GetMHDPressureGradient;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.pressure08;
                    worker = (int)Worker.Workers.GetMHDPressureGradient;
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.bl_zaki:
                    tableName = DataInfo.TableNames.pr;
                    worker = (int)Worker.Workers.GetChannelPressureGradient;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            bool IsChannelGrid = dataset_enum == DataInfo.DataSets.channel ? true : false;
            IsChannelGrid = dataset_enum == DataInfo.DataSets.bl_zaki ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetMHDData(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result, 1.0f);

            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        public PressureHessian[] GetPressureHessian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (auth.name == "dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            PressureHessian[] result = new PressureHessian[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_enum, time);

            object rowid = null;

            bool round = true;
            int kernelSize = -1;
            int kernelSizeY = -1;

            DataInfo.TableNames tableName;
            int worker;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    tableName = DataInfo.TableNames.isotropic1024fine_pr;
                    worker = (int)Worker.Workers.GetMHDPressureHessian;
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096: //check this
                    tableName = DataInfo.TableNames.pr;
                    worker = (int)Worker.Workers.GetMHDPressureHessian;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.pressure08;
                    worker = (int)Worker.Workers.GetMHDPressureHessian;
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.bl_zaki:
                    tableName = DataInfo.TableNames.pr;
                    worker = (int)Worker.Workers.GetChannelPressureHessian;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q4 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q6 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q8 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q10 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q12 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q14)
            {
                throw new Exception("This interpolation option does not support second order derivatives!");
            }

            bool IsChannelGrid = dataset_enum == DataInfo.DataSets.channel ? true : false;
            IsChannelGrid = dataset_enum == DataInfo.DataSets.bl_zaki ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetMHDPressureHessian(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        public Pressure[] GetTemperature(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (auth.name == "dev")
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

            bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
            int kernelSize = -1;
            int kernelSizeY = -1;

            DataInfo.TableNames tableName;
            int worker;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.strat4096:
                    tableName = DataInfo.TableNames.th;
                    worker = (int)Worker.Workers.GetMHDPressure;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            bool IsChannelGrid = dataset_enum == DataInfo.DataSets.channel ? true : false;
            IsChannelGrid = dataset_enum == DataInfo.DataSets.bl_zaki ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetMHDData(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        private void GetMHDData(AuthInfo.AuthToken auth, string dataset, DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, Vector3[] result, ref object rowid)
        {
            //Point3[] points1 = new Point3[points.Length];
            //points1 = points;
            bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
            int kernelSize = -1;
            int kernelSizeY = -1;

            bool IsChannelGrid = dataset_enum == DataInfo.DataSets.channel ? true : false;
            IsChannelGrid = dataset_enum == DataInfo.DataSets.bl_zaki ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);
            if (auth.name == "dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            //database.AddBulkParticles(points, round, spatialInterpolation, worker);
            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetMHDData(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result, 1.0f);
        }

        private void GetMHDGradient(AuthInfo.AuthToken auth, string dataset, DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, VelocityGradient[] result, ref object rowid)
        {
            bool round = true;
            int kernelSize = -1;
            int kernelSizeY = -1;

            bool IsChannelGrid = dataset_enum == DataInfo.DataSets.channel ? true : false;
            IsChannelGrid = dataset_enum == DataInfo.DataSets.bl_zaki ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);
            if (auth.name == "dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetMHDGradient(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
        }

        public ThresholdInfo[] GetThreshold(string authToken, string dataset, string field, float time, float threshold,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (auth.name == "dev")
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
                    else
                    {
                        throw new Exception("Invalid field specified");
                    }
                    break;
                case DataInfo.DataSets.channel:
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
                Xwidth * Ywidth * Zwidth, time, null, null);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

            List<ThresholdInfo> points_above_threshold = new List<ThresholdInfo>();
            database.ExecuteGetThreshold(dataset_enum, tableName, worker, time, spatialInterpolation, threshold,
                X, Y, Z, Xwidth, Ywidth, Zwidth, points_above_threshold);

            log.UpdateLogRecord(rowid, database.Bitfield);

            points_above_threshold.Sort((t1, t2) => -1 * t1.value.CompareTo(t2.value));

            return points_above_threshold.ToArray();
        }

        public Vector3[] GetBoxFilter(string authToken, string dataset, string field, float time,
            float filterwidth, Point3[] points)
        {
            int num_virtual_servers = 4;
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (auth.name == "dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            if (dataset_enum == DataInfo.DataSets.channel || dataset_enum == DataInfo.DataSets.bl_zaki)
            {
                throw new Exception(String.Format("GetBoxFilter is not available for the channel flow datasets!"));
            }
            DataInfo.verifyTimeInRange(dataset_enum, time);

            double dx = 0;
            if (dataset == "isotropic4096")
            {
                dx = (2.0 * Math.PI) / 4096;
            }
            else
            {
                dx = (2.0 * Math.PI) / (double)database.GridResolutionX;
            }
            int int_filterwidth = (int)Math.Round(filterwidth / dx);

            if (int_filterwidth % 2 == 0)
            {
                if (filterwidth <= dx * int_filterwidth)
                {
                    int_filterwidth--;
                    filterwidth = (float)dx * int_filterwidth;
                }
                else
                {
                    int_filterwidth++;
                    filterwidth = (float)dx * int_filterwidth;
                }
                //throw new Exception("Only filter widths that are an uneven multiple of the grid resolution are supported!");
            }

            bool round = true;
            if (num_virtual_servers == 1 && database.CheckInputForWrapAround(points, int_filterwidth, round))
                num_virtual_servers = 2;

            database.Initialize(dataset_enum, num_virtual_servers);
            //database.selectServers(dataset_enum, num_virtual_servers);

            Vector3[] result = new Vector3[points.Length];
            object rowid = null;

            DataInfo.TableNames tableName = DataInfo.getTableName(dataset_enum, field);

            int worker = (int)Worker.Workers.GetMHDBoxFilter;

            //database.AddBulkParticles(points, filter_width, round);
            worker = database.AddBulkParticlesFiltering(points, int_filterwidth, round, worker, time);

            rowid = log.CreateLog(auth.Id, dataset, worker, 0, 0,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            if (worker == (int)Worker.Workers.GetMHDBoxFilter)
            {
                database.ExecuteGetMHDData(tableName, worker, time,
                    TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);
            }
            else
            {
                database.ExecuteGetBoxFilter(tableName, worker, time,
                    TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        public Point3[] GetPosition(string authToken, string dataset, float StartTime, float EndTime,
            float dt, TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            Point3[] points)
        {
            //AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            AuthInfo.AuthToken auth = new AuthInfo.AuthToken("dev", -1, 0);
            if (auth.name == "dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, StartTime);
            DataInfo.verifyTimeInRange(dataset_enum, EndTime);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);

            if (Math.Abs(EndTime - StartTime) - Math.Abs(dt) < -0.000001)
                throw new Exception(String.Format("The time step dt cannot be greater than the StartTime : EndTime range!"));

            object rowid = null;

            TurbulenceOptions.TemporalInterpolation temporalInterpolation = TurbulenceOptions.TemporalInterpolation.PCHIP;

            bool round;
            int integralSteps;

            rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetPosition,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
                points.Length, StartTime, EndTime, dt);

            round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
            int kernelSize = -1;
            if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.Lag4)
                kernelSize = 4;
            else if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.Lag6)
                kernelSize = 6;
            else if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.Lag8)
                kernelSize = 8;
            else if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None)
                kernelSize = 0;
            else
                throw new Exception("Invalid interpolation option specified!");

            if ((StartTime > EndTime && dt > 0) || (StartTime < EndTime && dt < 0))
            {
                dt = -dt;
            }

            integralSteps = (int)Math.Abs((EndTime - StartTime) / dt);

            int numParticles = points.Length;
            // We query the database 2 * integralSteps number of times 
            //(the computation has 2 steps for the second order Runge Kutta)
            log.UpdateRecordCount(auth.Id, 2 * (integralSteps + 1) * numParticles);

            database.AddBulkParticlesSingleServer(points, kernelSize, kernelSize, kernelSize, round, StartTime);
            database.ExecuteGetPosition(dataset_enum, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP,
                DataInfo.getTableName(dataset_enum, "velocity").ToString(), StartTime, EndTime, dt, points);

            database.Close();

            log.UpdateLogRecord(rowid, database.Bitfield);

            return points;
        }
    }
}
