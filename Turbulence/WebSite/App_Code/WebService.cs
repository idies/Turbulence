using System;
using System.Web;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Collections.Generic;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using Turbulence.SQLInterface;
using Turbulence.TurbBatch;

namespace TurbulenceService
{
    /// <summary>
    /// Public WebService interface to the turublence database.
    /// </summary>
    [WebService(Namespace = "http://turbulence.pha.jhu.edu/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class TurbulenceService : System.Web.Services.WebService
    {

        /*
         * Set to TRUE for development mode (uses the test database)
         * Set to FALSE before production deployment
         * TODO: Automatic selection based on host name.
         */

        public const bool DEVEL_MODE = false;
        public const string infodb_string = !DEVEL_MODE ? "turbinfo_conn" : "turbinfo_test_conn";
        public const string infodb_backup_string = !DEVEL_MODE ? "turbinfo_backup_conn" : "";
        //public const string infodb_string = "turbinfo_test_conn";
        public const string logdb_string = (infodb_string == "turbinfo_conn") ? "turblog_conn" : "turbinfo_test_conn";

        // batch scheduler queue
        public static BatchWorkerQueue batchQueue = null;

        Database database;
        AuthInfo authInfo;
        Log log;

        public TurbulenceService()
        {
            database = new Database(infodb_string, DEVEL_MODE);
            authInfo = new AuthInfo(database.infodb, database.infodb_server, DEVEL_MODE);
            log = new Log(logdb_string, DEVEL_MODE);
        }

        ~TurbulenceService()
        {
            if (database != null)
            {
                database.Close();
                database = null;
            }
        }

        [WebMethod(Description = "Perform a null operation -- for testing throughput")]
        public Vector3[] NullOp(string authToken, Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3[] result = new Vector3[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                result[i].x = points[i].x;
                result[i].y = points[i].y;
                result[i].z = points[i].z;
            }
            return result;
        }

        #region SimulationField
        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetVelocity",
        Description = @"Spatially interpolate the velocity at a number of points for a given time.")]
        public Vector3[] GetVelocity(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
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
                    }
                    else
                    {
                        GetVectorData(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                                time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetMagneticField",
        Description = @"Spatially interpolate the magnetic field at a number of points for a given time.")]
        public Vector3[] GetMagneticField(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
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

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    throw new Exception(String.Format("GetMagneticField is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    DataInfo.TableNames tableName = DataInfo.TableNames.magnetic08;
                    GetVectorData(auth, dataset, dataset_enum, tableName, (int)Worker.Workers.GetMHDMagnetic,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetVectorPotential",
        Description = @"Spatially interpolate the magnetic field at a number of points for a given time.")]
        public Vector3[] GetVectorPotential(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
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

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    throw new Exception(String.Format("GetVectorPotential is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    DataInfo.TableNames tableName = DataInfo.TableNames.potential08;
                    GetVectorData(auth, dataset, dataset_enum, tableName, (int)Worker.Workers.GetMHDPotential,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetPressure",
        Description = @"Spatially interpolate the pressure field at a number of points for a given time.")]
        public Pressure[] GetPressure(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
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

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetDensity",
        Description = @"Spatially interpolate the density field at a number of points for a given time.")]
        public Pressure[] GetDensity(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
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

            int worker = (int)Worker.Workers.GetDensity; ;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.mixing:
                    GetScalarData(auth, dataset, dataset_enum, DataInfo.TableNames.density, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetTemperature",
        Description = @"Spatially interpolate the temperature field at a number of points for a given time.")]
        public Pressure[] GetTemperature(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
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
                case DataInfo.DataSets.strat4096:
                    GetScalarData(auth, dataset, dataset_enum, DataInfo.TableNames.th, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Spatially interpolate the velocity and pressure for an array of points")]
        public Vector3P[] GetVelocityAndPressure(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            Vector3P[] result = new Vector3P[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            //int num_virtual_servers = 1;
            //database.Initialize(dataset_enum, num_virtual_servers);  //todo: the pressure and velocity at the same server? nessessary?
            DataInfo.verifyTimeInRange(dataset_enum, time);

            Point3[] points1 = new Point3[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points1[i].x = points[i].x;
                points1[i].y = points[i].y;
                points1[i].z = points[i].z;
            }

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mhd1024:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.isotropic4096: //check this
                case DataInfo.DataSets.bl_zaki:
                    Vector3[] velocities = GetVelocity(authToken, dataset, time, spatialInterpolation, temporalInterpolation, points1, addr);
                    Pressure[] pressures = GetPressure(authToken, dataset, time, spatialInterpolation, temporalInterpolation, points, addr);
                    for (int i = 0; i < points.Length; i++)
                    {
                        result[i].x = velocities[i].x;
                        result[i].y = velocities[i].y;
                        result[i].z = velocities[i].z;
                        result[i].p = pressures[i].p;
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Spatially interpolate the velocity and temperature for an array of points")]
        public Vector3P[] GetVelocityAndTemperature(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            Vector3P[] result = new Vector3P[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            //int num_virtual_servers = 1;
            //database.Initialize(dataset_enum, num_virtual_servers);  //todo: the pressure and velocity at the same server? nessessary?
            DataInfo.verifyTimeInRange(dataset_enum, time);

            switch (dataset_enum)
            {
                case DataInfo.DataSets.strat4096:
                    Vector3[] velocities = GetVelocity(authToken, dataset, time, spatialInterpolation, temporalInterpolation, points, addr);
                    Pressure[] temperature = GetTemperature(authToken, dataset, time, spatialInterpolation, temporalInterpolation, points, addr);
                    for (int i = 0; i < points.Length; i++)
                    {
                        result[i].x = velocities[i].x;
                        result[i].y = velocities[i].y;
                        result[i].z = velocities[i].z;
                        result[i].p = temperature[i].p;
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

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

        #endregion

        #region Gradient
        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"Retrieve the velocity gradient at a fixed location")]
        public VelocityGradient[] GetVelocityGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
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

                        if (idx_not0.Count>0)
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

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetMagneticFieldGradient",
        Description = @"Retrieve the magnetic field gradient at a number of points for a given time.")]
        public VelocityGradient[] GetMagneticFieldGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
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

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    throw new Exception(String.Format("GetMagneticField is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.magnetic08, (int)Worker.Workers.GetMHDMagneticGradient,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetVectorPotentialGradient",
        Description = @"Retrieve the vector potential gradient at a number of points for a given time.")]
        public VelocityGradient[] GetVectorPotentialGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
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

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    throw new Exception(String.Format("GetVectorPotential is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.potential08, (int)Worker.Workers.GetMHDPotentialGradient,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"Retrieve the pressure gradient at a fixed location")]
        public Vector3[] GetPressureGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
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
                case DataInfo.DataSets.channel5200:
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
                            GetScalarGradient(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                                time, spatialInterpolation, temporalInterpolation, points1, result1, ref rowid, addr);
                            for (int i = 0; i < idx_not0.Count; i++)
                            {
                                result[idx_not0[i]] = result1[i];
                            }
                        }

                        if(idx.Count>0)
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
                            spatialInterpolation = TurbulenceOptions.SpatialInterpolation.Fd4Lag4;
                            GetScalarGradient(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
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

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"Retrieve the density gradient at a fixed location")]
        public Vector3[] GetDensityGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
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

            int worker = (int)Worker.Workers.GetDensityGradient;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.mixing:
                    GetScalarGradient(auth, dataset, dataset_enum, DataInfo.TableNames.density, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Retrieve the temperature gradient at a fixed location")]
        public Vector3[] GetTemperatureGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
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
                case DataInfo.DataSets.strat4096:
                    GetScalarGradient(auth, dataset, dataset_enum, DataInfo.TableNames.th, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            return result;
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
        #endregion

        #region Hessien
        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Retrieve the velocity hessian at a fixed location")]
        public VelocityHessian[] GetVelocityHessian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            VelocityHessian[] result = new VelocityHessian[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            object rowid = null;

            int worker = (int)Worker.Workers.GetMHDVelocityHessian;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    GetVectorHessian(auth, dataset, dataset_enum, DataInfo.TableNames.isotropic1024fine_vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.strat4096:
                    GetVectorHessian(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetVectorHessian(auth, dataset, dataset_enum, DataInfo.TableNames.velocity08, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                    worker = (int)Worker.Workers.GetChannelVelocityHessian;
                    GetVectorHessian(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.bl_zaki:
                    worker = (int)Worker.Workers.GetChannelVelocityHessian;
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
                            VelocityHessian[] result1 = new VelocityHessian[idx_not0.Count];
                            for (int i = 0; i < idx_not0.Count; i++)
                            {
                                points1[i].x = points[idx_not0[i]].x;
                                points1[i].y = points[idx_not0[i]].y;
                                points1[i].z = points[idx_not0[i]].z;
                            }
                            GetVectorHessian(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
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
                            VelocityHessian[] result1 = new VelocityHessian[idx.Count];
                            for (int i = 0; i < idx.Count; i++)
                            {
                                points1[i].x = (float)(Math.Round((points[idx[i]].x - 30.218496172581567) / 0.292210466240511) * 0.292210466240511 + 30.218496172581567);
                                points1[i].y = 0.0f;
                                points1[i].z = (float)(Math.Round(points[idx[i]].z / 0.117244748412311) * 0.117244748412311);
                            }
                            spatialInterpolation = TurbulenceOptions.SpatialInterpolation.Fd4Lag4;
                            GetVectorHessian(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
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
                        GetVectorHessian(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                            time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetMagneticHessian",
        Description = @"Retrieve the magnetic field hessian at a number of points for a given time.")]
        public VelocityHessian[] GetMagneticHessian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            VelocityHessian[] result = new VelocityHessian[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            object rowid = null;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    throw new Exception(String.Format("GetMagnetic is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    GetVectorHessian(auth, dataset, dataset_enum, DataInfo.TableNames.magnetic08, (int)Worker.Workers.GetMHDMagneticHessian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetVectorPotentialHessian",
        Description = @"Retrieve the vector potential hessian at a number of points for a given time.")]
        public VelocityHessian[] GetVectorPotentialHessian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            VelocityHessian[] result = new VelocityHessian[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            object rowid = null;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    throw new Exception(String.Format("GetVectorPotential is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    GetVectorHessian(auth, dataset, dataset_enum, DataInfo.TableNames.potential08, (int)Worker.Workers.GetMHDPotentialHessian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Retrieve the pressure hessian at a fixed location")]
        public PressureHessian[] GetPressureHessian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
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

            int worker = (int)Worker.Workers.GetMHDPressureHessian;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    GetScalarHessian(auth, dataset, dataset_enum, DataInfo.TableNames.isotropic1024fine_pr, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096: //check this                  
                    GetScalarHessian(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetScalarHessian(auth, dataset, dataset_enum, DataInfo.TableNames.pressure08, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                    worker = (int)Worker.Workers.GetChannelPressureHessian;
                    GetScalarHessian(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.bl_zaki:
                    worker = (int)Worker.Workers.GetChannelPressureHessian;
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
                            PressureHessian[] result1 = new PressureHessian[idx_not0.Count];
                            for (int i = 0; i < idx_not0.Count; i++)
                            {
                                points1[i].x = points[idx_not0[i]].x;
                                points1[i].y = points[idx_not0[i]].y;
                                points1[i].z = points[idx_not0[i]].z;
                            }
                            GetScalarHessian(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
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
                            PressureHessian[] result1 = new PressureHessian[idx.Count];
                            for (int i = 0; i < idx.Count; i++)
                            {
                                points1[i].x = (float)(Math.Round((points[idx[i]].x - 30.218496172581567) / 0.292210466240511) * 0.292210466240511 + 30.218496172581567);
                                points1[i].y = 0.0f;
                                points1[i].z = (float)(Math.Round(points[idx[i]].z / 0.117244748412311) * 0.117244748412311);
                            }
                            spatialInterpolation = TurbulenceOptions.SpatialInterpolation.Fd4Lag4;
                            GetScalarHessian(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
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
                        GetScalarHessian(auth, dataset, dataset_enum, DataInfo.TableNames.pr, worker,
                            time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Retrieve the density hessian at a fixed location")]
        public PressureHessian[] GetDensityHessian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
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

            int worker = (int)Worker.Workers.GetDensityHessian;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.mixing:
                    GetScalarHessian(auth, dataset, dataset_enum, DataInfo.TableNames.density, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Retrieve the temperature hessian at a fixed location")]
        public PressureHessian[] GetTemperatureHessian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
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

            int worker = (int)Worker.Workers.GetMHDPressureHessian;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.strat4096:
                    GetScalarHessian(auth, dataset, dataset_enum, DataInfo.TableNames.th, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        private void GetVectorHessian(AuthInfo.AuthToken auth, string dataset, DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, VelocityHessian[] result, ref object rowid, string addr = null)
        {
            if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q4 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q6 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q8 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q10 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q12 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q14)
            {
                throw new Exception("This interpolation option does not support second order derivatives!");
            }

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

            database.ExecuteGetVectorHessian(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
        }

        private void GetScalarHessian(AuthInfo.AuthToken auth, string dataset, DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, PressureHessian[] result, ref object rowid, string addr = null)
        {
            if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q4 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q6 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q8 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q10 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q12 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q14)
            {
                throw new Exception("This interpolation option does not support second order derivatives!");
            }

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

            database.ExecuteGetScalarHessian(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
        }
        #endregion

        #region Laplacian
        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Retrieve the velocity laplacian at a fixed location")]
        public Vector3[] GetVelocityLaplacian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            object rowid = null;

            int worker = (int)Worker.Workers.GetMHDVelocityLaplacian;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    GetVectorLaplacian(auth, dataset, dataset_enum, DataInfo.TableNames.isotropic1024fine_vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.strat4096:
                case DataInfo.DataSets.mixing:
                    GetVectorLaplacian(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetVectorLaplacian(auth, dataset, dataset_enum, DataInfo.TableNames.velocity08, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                    worker = (int)Worker.Workers.GetChannelVelocityLaplacian;
                    GetVectorLaplacian(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                case DataInfo.DataSets.bl_zaki:
                    worker = (int)Worker.Workers.GetChannelVelocityLaplacian;
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

                        if (idx_not0.Count>0)
                        {
                            Point3[] points1 = new Point3[idx_not0.Count];
                            Vector3[] result1 = new Vector3[idx_not0.Count];
                            for (int i = 0; i < idx_not0.Count; i++)
                            {
                                points1[i].x = points[idx_not0[i]].x;
                                points1[i].y = points[idx_not0[i]].y;
                                points1[i].z = points[idx_not0[i]].z;
                            }
                            GetVectorLaplacian(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                                time, spatialInterpolation, temporalInterpolation, points1, result1, ref rowid, addr);
                            for (int i = 0; i < idx_not0.Count; i++)
                            {
                                result[idx_not0[i]] = result1[i];
                            }
                        }

                        if (idx.Count>0)
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
                            spatialInterpolation = TurbulenceOptions.SpatialInterpolation.Fd4Lag4;
                            GetVectorLaplacian(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
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
                        GetVectorLaplacian(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                            time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetMagneticFieldLaplacian",
        Description = @"Retrieve the magnetic field Laplacian at a number of points for a given time.")]
        public Vector3[] GetMagneticFieldLaplacian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            object rowid = null;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    throw new Exception(String.Format("GetMagneticField is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    GetVectorLaplacian(auth, dataset, dataset_enum, DataInfo.TableNames.magnetic08, (int)Worker.Workers.GetMHDMagneticLaplacian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetVectorPotentialLaplacian",
        Description = @"Retrieve the vector potential Laplacian at a number of points for a given time.")]
        public Vector3[] GetVectorPotentialLaplacian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            object rowid = null;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    throw new Exception(String.Format("GetVectorPotential is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    GetVectorLaplacian(auth, dataset, dataset_enum, DataInfo.TableNames.potential08, (int)Worker.Workers.GetMHDPotentialLaplacian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid, addr);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        private void GetVectorLaplacian(AuthInfo.AuthToken auth, string dataset, DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, Vector3[] result, ref object rowid, string addr = null)
        {
            if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q4 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q6 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q8 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q10 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q12 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q14)
            {
                throw new Exception("This interpolation option does not support second order derivatives!");
            }

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

            database.ExecuteGetVectorLaplacian(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
        }
        #endregion

        #region Position_Threshold_Force_Invariant
        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetPosition",
        Description = @"Fluid particle tracking using a task parallel evaluation approach inside the database engine.")]
        public Point3[] GetPosition(string authToken, string dataset, float StartTime, float EndTime,
            float dt, TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
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
                points.Length, StartTime, EndTime, dt, addr);

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

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"GetThreshold of the specified field.")]
        public ThresholdInfo[] GetThreshold(string authToken, string dataset, string field, float time, float threshold,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
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

            return points_above_threshold.ToArray();
        }

        /// <summary>
        /// TODO:
        ///  * Get complete dataset (Minping)
        ///  * Move forcing data into a database (currently resides in files on web server) (Kalin)
        ///    - Cache forcing data into IIS memory cache
        ///  * Test for data accuracy (Huidan)
        /// </summary>
        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetForce",
        Description = @"Retrieve the force for a number of points for a given time.")]
        public Vector3[] GetForce(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);

            object rowid = null;
            rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetForce,
            (int)spatialInterpolation,
            (int)temporalInterpolation,
            points.Length, time, null, null, addr);

            const int time_offset = 4800;  // integral time offset to match forcing files
            Vector3[] result = new Vector3[points.Length];
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);

            log.UpdateRecordCount(auth.Id, points.Length);

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                    TurbDataTable table = TurbDataTable.GetTableInfo(dataset);

                    string directory = System.Web.HttpContext.Current.Request.MapPath("/")
                        + @"data\forcing\";

                    ReadForceData forceDataReader = new ReadForceData(directory);

                    if (temporalInterpolation == TurbulenceOptions.TemporalInterpolation.None)
                    {

                        int timestep = SQLUtility.GetNearestTimestep(time, table);

                        FourierInfo[] forceInfo = forceDataReader.getForceDataForTimestep(timestep + time_offset);

                        GetForce gf = new GetForce();
                        for (int i = 0; i < points.Length; i++)
                        {
                            result[i] = gf.getForceByTime(forceInfo, points[i]);
                        }

                    }
                    else if (temporalInterpolation == TurbulenceOptions.TemporalInterpolation.PCHIP)
                    {
                        // PCHIP code ripped from ExecuteTurbulenceWorker.cs
                        // TODO: Make this code more generic for use elsewhere (eric)
                        int basetime = SQLUtility.GetFlooredTimestep(time, table);
                        int[] timesteps = { basetime - table.TimeInc,
                        basetime,
                        basetime + table.TimeInc,
                        basetime + table.TimeInc * 2 };
                        float[] times = { timesteps[0] * table.Dt,
                                            timesteps[1] * table.Dt,
                                            timesteps[2] * table.Dt,
                                            timesteps[3] * table.Dt };

                        FourierInfo[][] forceInfo = new FourierInfo[4][];
                        for (int i = 0; i < 4; i++)
                        {
                            forceInfo[i] = forceDataReader.getForceDataForTimestep(timesteps[i] + time_offset);
                        }

                        GetForce gf = new GetForce();
                        for (int i = 0; i < points.Length; i++)
                        {
                            Vector3[] forces = { gf.getForceByTime(forceInfo[0], points[i]),
                                          gf.getForceByTime(forceInfo[1], points[i]),
                                          gf.getForceByTime(forceInfo[2], points[i]),
                                          gf.getForceByTime(forceInfo[3], points[i]) };
                            result[i].x = Turbulence.SciLib.TemporalInterpolation.PCHIP(time,
                                times[0], times[1], times[2], times[3],
                                forces[0].x, forces[1].x, forces[2].x, forces[3].x);
                            result[i].y = Turbulence.SciLib.TemporalInterpolation.PCHIP(time,
                                times[0], times[1], times[2], times[3],
                                forces[0].y, forces[1].y, forces[2].y, forces[3].y);
                            result[i].z = Turbulence.SciLib.TemporalInterpolation.PCHIP(time,
                                times[0], times[1], times[2], times[3],
                                forces[0].z, forces[1].z, forces[2].z, forces[3].z);
                        }
                    }
                    else
                    {
                        throw new Exception("Unsupported TemporalInterpolation Type");
                    }
                    break;

                case DataInfo.DataSets.mhd1024:
                    for (int i = 0; i < points.Length; i++)
                    {
                        result[i].x = 0.25f * (float)(Math.Sin(2 * points[i].x) * Math.Cos(2 * points[i].y) * Math.Cos(2 * points[i].z));
                        result[i].y = -0.25f * (float)(Math.Cos(2 * points[i].x) * Math.Sin(2 * points[i].y) * Math.Cos(2 * points[i].z));
                        result[i].z = 0.0f;
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            //log.WriteLog(auth.Id, dataset, (int)Worker.Workers.GetForce,
            //    (int)spatialInterpolation, (int)temporalInterpolation,
            //    points.Length, time, null, null, database.Bitfield);
            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Retrieve the invariants of velocity gradient at a fixed location")]
        public Vector3[] GetInvariant(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            VelocityGradient[] vel_grad = new VelocityGradient[points.Length];
            Vector3[] result = new Vector3[points.Length];
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
                        time, spatialInterpolation, temporalInterpolation, points, vel_grad, ref rowid, addr);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.strat4096:
                    GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, vel_grad, ref rowid, addr);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.velocity08, worker,
                        time, spatialInterpolation, temporalInterpolation, points, vel_grad, ref rowid, addr);
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                    worker = (int)Worker.Workers.GetChannelVelocityGradient;
                    GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, vel_grad, ref rowid, addr);
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
                                vel_grad[idx_not0[i]] = result1[i];
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
                                vel_grad[idx[i]] = result1[i];
                            }
                            log.UpdateLogRecord(rowid1, database.Bitfield);
                        }
                    }
                    else
                    {
                        GetVectorGradient(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                            time, spatialInterpolation, temporalInterpolation, points, vel_grad, ref rowid, addr);
                    }

                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            for (int k = 0; k < points.Length; k++)
            {
                float[,] S = new float[3, 3];
                S[0, 0] = vel_grad[k].duxdx; S[0, 1] = 0.5f * (vel_grad[k].duxdy + vel_grad[k].duydx); S[0, 2] = 0.5f * (vel_grad[k].duxdz + vel_grad[k].duzdx);
                S[1, 0] = S[0, 1]; S[1, 1] = vel_grad[k].duydy; S[1, 2] = 0.5f * (vel_grad[k].duydz + vel_grad[k].duzdy);
                S[2, 0] = S[0, 2]; S[2, 1] = S[1, 2]; S[2, 2] = vel_grad[k].duzdz;
                float[,] O = new float[3, 3];
                O[0, 0] = 0; O[0, 1] = 0.5f * (vel_grad[k].duxdy - vel_grad[k].duydx); O[0, 2] = 0.5f * (vel_grad[k].duxdz - vel_grad[k].duzdx);
                O[1, 0] = -O[0, 1]; O[1, 1] = 0; O[1, 2] = 0.5f * (vel_grad[k].duydz - vel_grad[k].duzdy);
                O[2, 0] = -O[0, 2]; O[2, 1] = -O[1, 2]; O[2, 2] = 0;

                float S2 = 0f;
                float O2 = 0f;

                for (int i = 0; i <= 2; i++)
                {
                    for (int j = 0; j <= 2; j++)
                    {
                        S2 += S[i, j] * S[i, j];
                        O2 += O[i, j] * O[i, j];
                    }
                }
                result[k].x = S2;
                result[k].y = O2;
                result[k].z = 0.5f * (result[k].y - result[k].x);
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }
        #endregion

        #region Filter
        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"GetBoxFilter of the specified field; uses workload density to decide how to evaluate.")]
        public Vector3[] GetBoxFilter(string authToken, string dataset, string field, float time,
            float filterwidth, Point3[] points, string addr = null)
        {
            int num_virtual_servers = 4;
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
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
            if ((dataset == "isotropic4096") || (dataset == "strat4096"))
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
               points.Length, time, null, null, addr);
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

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetBoxFilterSGS",
            Description = @"Retrieve the SGS symmetric tensor for a single vector field. Also, used
                            when two identical fields are specified (e.g. uu or bb).")]
        public SGSTensor[] GetBoxFilterSGS(string authToken, string dataset, string field, float time,
            float filterwidth, Point3[] points, string addr = null)
        {
            return GetBoxFilterSGSsymtensor(authToken, dataset, field, time, filterwidth, points, addr);
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetBoxFilterSGSsymtensor",
            Description = @"Retrieve the SGS symmetric tensor for a single vector field. Also, used
                            when two identical fields are specified (e.g. uu or bb).")]
        public SGSTensor[] GetBoxFilterSGSsymtensor(string authToken, string dataset, string field, float time,
            float filterwidth, Point3[] points, string addr = null)
        {
            SGSTensor[] result = new SGSTensor[points.Length];
            int worker = (int)Worker.Workers.GetMHDBoxFilterSGS; ;
            DataInfo.TableNames tableName1;
            DataInfo.TableNames tableName2;
            object rowid;

            InitializeSGSMethod(authToken, time, points, field, ref worker, ref dataset, ref filterwidth, out tableName1, out tableName2, out rowid, addr);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }

            if ((tableName1 == tableName2) && (DataInfo.getNumberComponents(tableName1) == 3))
            {
                if (worker == (int)Worker.Workers.GetMHDBoxFilterSGS)
                {
                    database.ExecuteGetMHDData(tableName1, worker, time,
                        TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);
                }
                else
                {
                    database.ExecuteGetBoxFilter(tableName1, worker, time,
                        TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);
                }
            }
            else
            {
                throw new Exception("The GetBoxFilterSGSsymtensor method should be called either with a single field or with two identical " +
                                     "vector fields (e.g. \"velocity\", \"uu\" or \"bb\")");
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetBoxFilterSGStensor",
            Description = @"Retrieve the SGS tensor for a combination of two vector fields(e.g. ub or ba).")]
        public VelocityGradient[] GetBoxFilterSGStensor(string authToken, string dataset, string field, float time,
            float filterwidth, Point3[] points, string addr = null)
        {
            VelocityGradient[] result = new VelocityGradient[points.Length];
            int worker = (int)Worker.Workers.GetMHDBoxFilterSGS; ;
            DataInfo.TableNames tableName1;
            DataInfo.TableNames tableName2;
            object rowid;
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            InitializeSGSMethod(authToken, time, points, field, ref worker, ref dataset, ref filterwidth, out tableName1, out tableName2, out rowid, addr);

            if ((tableName1 != tableName2) && (DataInfo.getNumberComponents(tableName1) == 3) && (DataInfo.getNumberComponents(tableName2) == 3))
            {
                if (worker == (int)Worker.Workers.GetMHDBoxFilterSGS)
                {
                    database.ExecuteGetMHDData(tableName1, tableName2, worker, time,
                        TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);
                }
                else
                {
                    database.ExecuteGetBoxFilter(tableName1, tableName2, worker, time,
                        TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);
                }
            }
            else
            {
                throw new Exception("The GetBoxFilterSGStensor method should be called with two distinct " +
                                     "vector fields (e.g. \"ub\" or \"ba\")");
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetBoxFilterSGSvector",
            Description = @"Retrieve the SGS vector for a combination of a vector and a scalar field(e.g. up or bp).")]
        public Vector3[] GetBoxFilterSGSvector(string authToken, string dataset, string field, float time,
            float filterwidth, Point3[] points, string addr = null)
        {
            Vector3[] result = new Vector3[points.Length];
            int worker = (int)Worker.Workers.GetMHDBoxFilterSGS; ;
            DataInfo.TableNames tableName1;
            DataInfo.TableNames tableName2;
            object rowid;
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            InitializeSGSMethod(authToken, time, points, field, ref worker, ref dataset, ref filterwidth, out tableName1, out tableName2, out rowid, addr);

            if (DataInfo.getNumberComponents(tableName1) == DataInfo.getNumberComponents(tableName2))
            {
                throw new Exception("The GetBoxFilterSGSvector method should be called with a vector and a scalar (e.g. \"up\" or \"bp\")");
            }
            else
            {
                // switch the table names if the first one is for a scalar field
                if (DataInfo.getNumberComponents(tableName1) == 1)
                {
                    DataInfo.TableNames temp = tableName1;
                    tableName1 = tableName2;
                    tableName2 = temp;
                }
                if (worker == (int)Worker.Workers.GetMHDBoxFilterSGS)
                {
                    database.ExecuteGetMHDData(tableName1, tableName2, worker, time,
                        TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);
                }
                else
                {
                    database.ExecuteGetBoxFilter(tableName1, tableName2, worker, time,
                        TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);
                }
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetBoxFilterSGSscalar",
            Description = @"Retrieve the SGS scalar for a combination of two scalar fields(e.g. pp or pd).")]
        public float[] GetBoxFilterSGSscalar(string authToken, string dataset, string field, float time,
            float filterwidth, Point3[] points, string addr = null)
        {
            float[] result = new float[points.Length];
            int worker = (int)Worker.Workers.GetMHDBoxFilterSGS; ;
            DataInfo.TableNames tableName1;
            DataInfo.TableNames tableName2;
            object rowid;
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            InitializeSGSMethod(authToken, time, points, field, ref worker, ref dataset, ref filterwidth, out tableName1, out tableName2, out rowid, addr);

            if ((DataInfo.getNumberComponents(tableName1) == DataInfo.getNumberComponents(tableName2)) && (DataInfo.getNumberComponents(tableName1) == 1))
            {
                if (worker == (int)Worker.Workers.GetMHDBoxFilterSGS)
                {
                    database.ExecuteGetMHDData(tableName1, tableName2, worker, time,
                        TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);
                }
                else
                {
                    database.ExecuteGetBoxFilter(tableName1, tableName2, worker, time,
                        TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);
                }
            }
            else
            {
                throw new Exception("The GetBoxFilterSGSscalar method should be called with two scalar fields (e.g. \"pp\" or \"pd\")");
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        private void InitializeSGSMethod(string authToken, float time, Point3[] points, string field,
            ref int worker,
            ref string dataset, ref float filterwidth, out DataInfo.TableNames tableName1, out DataInfo.TableNames tableName2,
            out object rowid, string addr = null)
        {
            int num_virtual_servers = 4;
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            if (dataset_enum == DataInfo.DataSets.channel || dataset_enum == DataInfo.DataSets.bl_zaki)
            {
                throw new Exception(String.Format("Box filter methods are not available for the channel flow datasets!"));
            }
            DataInfo.verifyTimeInRange(dataset_enum, time);

            double dx = 0;
            if ((dataset == "isotropic4096") || (dataset == "strat4096"))
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
            }

            bool round = true;
            if (num_virtual_servers == 1 && database.CheckInputForWrapAround(points, int_filterwidth, round))
                num_virtual_servers = 2;

            database.Initialize(dataset_enum, num_virtual_servers);

            worker = database.AddBulkParticlesFiltering(points, int_filterwidth, round, worker, time);

            rowid = log.CreateLog(auth.Id, dataset, worker, 0, 0,
               points.Length, time, null, null, addr);
            log.UpdateRecordCount(auth.Id, points.Length);

            // The user can specify either 2 fields (e.g. "uu" or "ub")
            // or a single field (e.g. "velocity", "magnetic", "potential").
            // We determine the appropriate table name for each of these cases below.
            if (field.Length == 2)
            {
                tableName1 = DataInfo.getTableName(dataset_enum, field.Substring(0, 1));
                tableName2 = DataInfo.getTableName(dataset_enum, field.Substring(1, 1));
            }
            else
            {
                tableName1 = DataInfo.getTableName(dataset_enum, field);
                tableName2 = tableName1;
            }
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"GetBoxFilter of the specified field; uses workload density to decide how to evaluate.")]
        public VelocityGradient[] GetBoxFilterGradient(string authToken, string dataset, string field, float time,
            float filterwidth, float spacing, Point3[] points, string addr = null)
        {
            int num_virtual_servers = 4;
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
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
            //database.Initialize(dataset_enum);

            double dx = 0;
            if ((dataset == "isotropic4096") || (dataset == "strat4096"))
            {
                dx = (2.0 * Math.PI) / 4096;
            }
            else
            {
                dx = (2.0 * Math.PI) / (double)database.GridResolutionX;
            }
            int int_filterwidth = (int)Math.Round(filterwidth / dx);
            int FDgrid_spacing = (int)Math.Round(spacing / dx);

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

            int kernelSize = int_filterwidth + 2 * FDgrid_spacing;

            bool round = true;
            if (num_virtual_servers == 1 && database.CheckInputForWrapAround(points, int_filterwidth, round))
                num_virtual_servers = 2;

            database.Initialize(dataset_enum, num_virtual_servers);
            //database.selectServers(dataset_enum, num_virtual_servers);

            VelocityGradient[] result = new VelocityGradient[points.Length];
            object rowid = null;

            DataInfo.TableNames tableName = DataInfo.getTableName(dataset_enum, field);

            int worker = (int)Worker.Workers.GetMHDBoxFilterGradient;

            rowid = log.CreateLog(auth.Id, dataset, worker, 0, 0,
               points.Length, time, null, null, addr);
            log.UpdateRecordCount(auth.Id, points.Length);

            // TODO: Summed Volumes technique is not yet implemented for the computation of the filtered gradient
            //worker = database.AddBulkParticlesFiltering(points, filter_width, round, worker);
            database.AddBulkParticlesFiltering(points, kernelSize, round, worker, time);

            if (worker == (int)Worker.Workers.GetMHDBoxFilterGradient)
            {
                database.ExecuteGetMHDFilterGradient(tableName, worker, time, FDgrid_spacing, filterwidth, result);
            }
            else
            {
                //database.ExecuteGetBoxFilter(tableName, worker, time,
                //    TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }
        #endregion

        //////////////////////////////////////////////////////////////////////
        //
        // The methods below this comment are NOT intended for production use.
        //
        //////////////////////////////////////////////////////////////////////
        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetLaplacianOfGradient",
        Description = @"Retrieve the laplacian of the gradient of the specified field at a number of points for a given time. Development version, not intended for production use!")]
        public VelocityGradient[] GetLaplacianOfGradient(string authToken, string dataset, string field, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            VelocityGradient[] result = new VelocityGradient[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            object rowid = null;

            DataInfo.TableNames tableName = DataInfo.getTableName(dataset_enum, field);
            int worker = (int)Worker.Workers.GetLaplacianOfGradient;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mhd1024:
                    bool round = true;
                    int kernelSize = -1;
                    if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None_Fd4)
                        kernelSize = 4;
                    else if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None_Fd6)
                        kernelSize = 6;
                    else if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None_Fd8)
                        kernelSize = 8;
                    else
                        throw new Exception("Invalid spatial interpolation option specified!\n");

                    rowid = log.CreateLog(auth.Id, dataset, worker,
                        (int)spatialInterpolation,
                        (int)temporalInterpolation,
                       points.Length, time, null, null, addr);
                    log.UpdateRecordCount(auth.Id, points.Length);

                    //database.AddBulkParticles(points, round, spatialInterpolation, worker);
                    database.AddBulkParticles(points, kernelSize, kernelSize, kernelSize, round, time);

                    database.ExecuteGetVectorGradient(tableName, worker, time,
                        spatialInterpolation, temporalInterpolation, result);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

#if DEV

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetPositionDBEvaluation",
        Description = @"FluidParticleTracking UNDER DEVELOPMENT")]
        public Point3[] GetPositionDBEvaluation(string authToken, string dataset, float StartTime, float EndTime,
            float dt, TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, StartTime);
            DataInfo.verifyTimeInRange(dataset_enum, EndTime);
            int worker = (int)Worker.Workers.GetPositionDBEvaluation;
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            database.selectServers(dataset_enum, num_virtual_servers, worker);

            if (Math.Abs(EndTime - StartTime) - Math.Abs(dt) < -0.000001)
                throw new Exception(String.Format("The time step dt cannot be greater than the StartTime : EndTime range!"));

            object rowid = null;

            TurbulenceOptions.TemporalInterpolation temporalInterpolation = TurbulenceOptions.TemporalInterpolation.PCHIP;

            bool round;
            double time;
            int integralSteps;

            rowid = log.CreateLog(auth.Id, dataset, worker,
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

            TrackingInfo[] tInfo = new TrackingInfo[numParticles];

            time = StartTime;
            string tableName = DataInfo.getTableName(dataset_enum, "velocity").ToString();
            int timeStep = (int)Math.Floor((StartTime / database.Dt) / database.TimeInc) * database.TimeInc + database.TimeOff;
            for (int i = 0; i < numParticles; i++)
            {
                tInfo[i] = new TrackingInfo(points[i], new Point3(0.0f, 0.0f, 0.0f), timeStep, StartTime, EndTime, dt, true, false);
            }

            int num_crossings = 0;
            bool all_done = false;
            while (!all_done)
            {
                database.AddBulkTrackingParticles(tInfo, round, kernelSize);

                database.ExecuteGetPosition(tableName, spatialInterpolation, temporalInterpolation, tInfo);

                all_done = true;
                for (int i = 0; i < numParticles; i++)
                    if (!tInfo[i].done)
                    {
                        all_done = false;
                        num_crossings++;
                        //throw new Exception(String.Format("There was a point that crossed a server boundary and needs to be reassigned!"));
                        break;
                    }
            }

            System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\Users\\kalin\\Documents\\output\\GetPositionDB_output.txt", true);
            file.WriteLine("number of crossings = {0}", num_crossings);
            file.Close();

            database.Close();

            Point3[] result = new Point3[numParticles];

            for (int i = 0; i < numParticles; i++)
            {
                result[i].x = tInfo[i].position.x;
                result[i].y = tInfo[i].position.y;
                result[i].z = tInfo[i].position.z;
            }

            database.Close();

            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"Calls DBCC DROPCLEANBUFFERS on each server")]
        public void ClearDBCache(string dataset)
        {
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            database.Initialize(dataset_enum);
            database.ClearDBCache();
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"BoxFilterForVelocity; uses I/O streaming method to perform evaluation")]
        public Vector3[] GetBoxFilterVelocityIOS(string authToken, string dataset, float time,
            float filterwidth, Point3[] points, int num_virtual_servers)
        {
            //double x = 0.375;
            //double y = 0.375;
            //double z = 0.375;

            //float ddx = 2.0f * (float)Math.PI / 1024.0f;
            //ddx *= 4;

            //int cubewidth = 64;
            //points = new Point3[cubewidth * cubewidth * cubewidth];
            //for (int i = 0; i < cubewidth; i++)
            //    for (int ii = 0; ii < cubewidth; ii++)
            //        for (int iii = 0; iii < cubewidth; iii++)
            //        {
            //            points[iii + ii * cubewidth + i * cubewidth * cubewidth] = new Point3();
            //            //points[iii + ii * cubewidth + i * cubewidth * cubewidth].x = (float)((float)(iii * filterwidth / 2) * ddx);
            //            //points[iii + ii * cubewidth + i * cubewidth * cubewidth].y = (float)((float)(ii * filterwidth / 2) * ddx);
            //            //points[iii + ii * cubewidth + i * cubewidth * cubewidth].z = (float)((float)(i * filterwidth / 2) * ddx);
            //            points[iii + ii * cubewidth + i * cubewidth * cubewidth].x = (float)(x * 2.0 * 3.14 + (float)iii * ddx);
            //            points[iii + ii * cubewidth + i * cubewidth * cubewidth].y = (float)(y * 2.0 * 3.14 + (float)ii * ddx);
            //            points[iii + ii * cubewidth + i * cubewidth * cubewidth].z = (float)(z * 2.0 * 3.14 + (float)i * ddx);
            //        }

            //System.IO.StreamReader file =
            //    new System.IO.StreamReader(@"C:\Users\kalin\Documents\turbulence\Turbulence\TestApp\bin\Release\Points.txt");
            //string line;
            //file.BaseStream.Position = 0;
            //file.DiscardBufferedData();

            //char[] delimiterChars = { ',' };
            //for (int i = 0; i < points.Length; i++)
            //{
            //    line = file.ReadLine();
            //    string[] words = line.Split(delimiterChars);
            //    points[i] = new Point3();
            //    points[i].x = float.Parse(words[0]);
            //    points[i].y = float.Parse(words[1]);
            //    points[i].z = float.Parse(words[2]);
            //}

            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            database.setBlobDim(dataset_enum);
            database.selectServers(dataset_enum, num_virtual_servers);

            Vector3[] result = new Vector3[points.Length];
            object rowid = null;

            DataInfo.TableNames tableName;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                    tableName = DataInfo.TableNames.vel;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.velocity08;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            int worker = (int)Worker.Workers.GetMHDBoxFilter;

            rowid = log.CreateLog(auth.Id, dataset, worker, 0, 0,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            double dx = (2.0 * Math.PI) / (double)database.gridDIM;
            int filter_width = (int)Math.Round(filterwidth / dx);
            database.AddBulkParticles(points, filter_width, true);

            database.ExecuteGetMHDData(tableName, worker, time,
                TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"BoxFilterForVelocity; uses summed volumes for evaluation.")]
        public Vector3[] GetBoxFilterVelocity3(string authToken, string dataset, float time,
            float filterwidth, Point3[] points, int num_virtual_servers)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            database.setBlobDim(dataset_enum);
            double dx = (2.0 * Math.PI) / (double)database.gridDIM;
            int filter_width = (int)Math.Round(filterwidth / dx);

            bool round = true;
            //if (num_virtual_servers == 1 && database.CheckInputForWrapAround(points, filter_width, round))
            //    num_virtual_servers = 2;

            database.selectServers(dataset_enum, num_virtual_servers);

            Vector3[] result = new Vector3[points.Length];
            object rowid = null;

            DataInfo.TableNames tableName;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                    tableName = DataInfo.TableNames.vel;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.velocity08;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            int worker = (int)Worker.Workers.GetMHDBoxFilterSV;

            rowid = log.CreateLog(auth.Id, dataset, worker, 0, 0,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticles(points, filter_width, round);

            database.ExecuteGetBoxFilter(tableName, worker, time,
                TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"BoxFilterForVelocity3; uses summed volumes for evaluation.")]
        public SGSTensor[] GetBoxFilterSGS_SV(string authToken, string dataset, float time,
            float filterwidth, Point3[] points, int num_virtual_servers)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            database.setBlobDim(dataset_enum);
            double dx = (2.0 * Math.PI) / (double)database.gridDIM;
            int filter_width = (int)Math.Round(filterwidth / dx);

            bool round = true;
            //if (num_virtual_servers == 1 && database.CheckInputForWrapAround(points, filter_width, round))
            //    num_virtual_servers = 2;

            database.selectServers(dataset_enum, num_virtual_servers);

            SGSTensor[] result = new SGSTensor[points.Length];
            object rowid = null;

            DataInfo.TableNames tableName;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                    tableName = DataInfo.TableNames.vel;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.velocity08;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            int worker = (int)Worker.Workers.GetMHDBoxFilterSGS_SV;

            rowid = log.CreateLog(auth.Id, dataset, worker, 0, 0,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticles(points, filter_width, round);

            database.ExecuteGetBoxFilter(tableName, worker, time,
                TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"BoxFilterForVelocity; uses naive method to perform evaluation")]
        public Vector3[] GetBoxFilterVelocityNaive(string authToken, string dataset, float time,
            float filterwidth, Point3[] points, int num_virtual_servers)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            database.setBlobDim(dataset_enum);
            database.selectServers(dataset_enum, num_virtual_servers);

            Vector3[] result = new Vector3[points.Length];
            object rowid = null;

            DataInfo.TableNames tableName;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                    tableName = DataInfo.TableNames.vel;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.velocity08;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            int worker = (int)Worker.Workers.GetBoxFilterWorkerDirect;

            rowid = log.CreateLog(auth.Id, dataset, worker, 0, 0,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            double dx = (2.0 * Math.PI) / (double)database.gridDIM;
            int filter_width = (int)Math.Round(filterwidth / dx);
            database.AddBulkParticles(points, filter_width, true);

            database.ExecuteGetMHDDataNaive(tableName, worker, time,
                TurbulenceOptions.SpatialInterpolation.None, TurbulenceOptions.TemporalInterpolation.None, result, filterwidth);

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, Description = @"BoxFilterForVelocity2")]
        public Vector3[] GetBoxFilterVelocity2(string authToken, string dataset, float time,
            float filterwidth,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation, Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            database.setBlobDim(dataset_enum);
            database.selectServers(dataset_enum);

            Vector3[] result = new Vector3[points.Length];
            object rowid = null;

            TurbulenceOptions.SpatialInterpolation spatialInterpolation = TurbulenceOptions.SpatialInterpolation.None;

            DataInfo.TableNames tableName;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                    tableName = DataInfo.TableNames.vel;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.velocity08;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;

            int worker = (int)Worker.Workers.GetMHDBoxFilter2;

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticlesFiltering2(points, filterwidth);

            database.ExecuteGetMHDData(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result, filterwidth);

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, Description = @"BoxFilterForVelocityOld")]
        public Vector3[] GetBoxFilterVelocityOld(string authToken, string dataset, float time,
            float filterlength, int nlayers,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation, Point3[] points)
         {
            //throw new Exception("GetBoxFilterVelocity is an experimental function and currently disabled.  Please e-mail turbulence@jhu.edu if you need to use it.");
            
            if (nlayers <= 0)
            {
                throw new Exception(String.Format("Invalid input of nlayers: {0}", nlayers));
            }
            float Dx = 2.0f * (float)(Math.PI) / 1024;
            Vector3[] result;
            if (filterlength < Dx)
            {
                /*if (nlayers > 1)
                {
                    throw new Exception("The input for nlayers exceeds the number of total layers");
                }                
                result = new Vector3[points.Length];
                Vector3[] bottomvel;
                bottomvel = GetVelocity(authToken, dataset, time, TurbulenceOptions.SpatialInterpolation.Lag4, temporalInterpolation, points);
                for (int i = 0; i < points.Length; i++)
                {
                    result[i].x = bottomvel[i].x;
                    result[i].y = bottomvel[i].y;
                    result[i].z = bottomvel[i].z;
                }*/
                throw new Exception("Filter length is smaller than grid size. Filter calculation not operated.");
            }
            else
            {
                int totlayers = (int)(Math.Ceiling(Math.Log(filterlength / Dx) / Math.Log(2.0f))); //original point not counted
                if (nlayers > totlayers)
                {
                    throw new Exception("The input for nlayers exceeds the number of total layers");
                }
                int totfilteredpts = 0;
                int totoutputpts = 0;
                for (int k = 0; k < totlayers; k++)
                {
                    totfilteredpts = totfilteredpts + (int)(Math.Pow(8, k));
                }

                for (int k = 0; k < nlayers; k++)
                {
                    totoutputpts = totoutputpts + (int)(Math.Pow(8, k));
                }                

                result = new Vector3[points.Length * totoutputpts];
                Vector3[,] filteredvalue = new Vector3[points.Length, totfilteredpts];
                int nbpnt = (int)(Math.Pow(8, totlayers));
                //Vector3[,] filteredvalue = new Vector3[totlayers + 1, nbpnt];
                Point3[] bottompts = new Point3[nbpnt];
                Vector3[] bottomvel;
                int[,] idx = new int[nbpnt, totlayers];
                float[] subFL = new float[totlayers];
                int stindex, edindex;

                for (int k = 0; k < totlayers; k++)
                {
                    subFL[k] = filterlength / (int)(Math.Pow(2, k + 1));
                }

                for (int i = 0; i < points.Length; i++)
                {
                    for (int j = 0; j < nbpnt; j++)
                    {
                        bottompts[j] = new Point3();
                        idx[j, 0] = (int)(j / (int)(Math.Pow(8, totlayers - 1)));
                        int clk = j;
                        for (int k = 1; k < totlayers; k++)
                        {
                            clk = clk - idx[j, k - 1] * (int)(Math.Pow(8, totlayers - k));
                            idx[j, k] = (int)(clk / (int)(Math.Pow(8, totlayers - k - 1)));

                        }
                        bottompts[j].x = points[i].x;
                        bottompts[j].y = points[i].y;
                        bottompts[j].z = points[i].z;
                        for (int k = 0; k < totlayers; k++)
                        {
                            if (idx[j, k] == 0 || idx[j, k] == 2 || idx[j, k] == 4 || idx[j, k] == 6)
                                bottompts[j].x = bottompts[j].x - subFL[k] / 2;
                            else
                                bottompts[j].x = bottompts[j].x + subFL[k] / 2;

                            if (idx[j, k] == 0 || idx[j, k] == 1 || idx[j, k] == 4 || idx[j, k] == 5)
                                bottompts[j].y = bottompts[j].y - subFL[k] / 2;
                            else
                                bottompts[j].y = bottompts[j].y + subFL[k] / 2;

                            if (idx[j, k] == 0 || idx[j, k] == 1 || idx[j, k] == 2 || idx[j, k] == 3)
                                bottompts[j].z = bottompts[j].z - subFL[k] / 2;
                            else
                                bottompts[j].z = bottompts[j].z + subFL[k] / 2;
                        }
                    }
                    bottomvel = GetVelocity(authToken, dataset, time, TurbulenceOptions.SpatialInterpolation.Lag4, temporalInterpolation, bottompts);

                    stindex = totfilteredpts - (int)(Math.Pow(8, totlayers - 1));
                    for (int j = 0; j < (int)(Math.Pow(8, totlayers - 1)); j++)
                    {
                        filteredvalue[i, stindex + j].x = (bottomvel[8 * j].x + bottomvel[8 * j + 1].x + bottomvel[8 * j + 2].x 
                            + bottomvel[8 * j + 3].x + bottomvel[8 * j + 4].x + bottomvel[8 * j + 5].x + bottomvel[8 * j + 6].x 
                            + bottomvel[8 * j + 7].x) / 8.0f;
                        filteredvalue[i, stindex + j].y = (bottomvel[8 * j].y + bottomvel[8 * j + 1].y + bottomvel[8 * j + 2].y
                            + bottomvel[8 * j + 3].y + bottomvel[8 * j + 4].y + bottomvel[8 * j + 5].y + bottomvel[8 * j + 6].y
                            + bottomvel[8 * j + 7].y) / 8.0f;
                        filteredvalue[i, stindex + j].z = (bottomvel[8 * j].z + bottomvel[8 * j + 1].z + bottomvel[8 * j + 2].z
                            + bottomvel[8 * j + 3].z + bottomvel[8 * j + 4].z + bottomvel[8 * j + 5].z + bottomvel[8 * j + 6].z
                            + bottomvel[8 * j + 7].z) / 8.0f;
                    }

                    for (int k = 1; k < totlayers; k++)
                    {
                        edindex = stindex - 1;
                        stindex = stindex - (int)(Math.Pow(8, totlayers - k - 1));
                        for (int kk = 0; kk < (int)(Math.Pow(8, totlayers - k - 1)); kk++)
                        {

                            filteredvalue[i, stindex + kk].x = (filteredvalue[i, edindex + 8 * kk + 1].x + filteredvalue[i, edindex + 8 * kk + 2].x
                            + filteredvalue[i, edindex + 8 * kk + 3].x + filteredvalue[i, edindex + 8 * kk + 4].x
                            + filteredvalue[i, edindex + 8 * kk + 5].x + filteredvalue[i, edindex + 8 * kk + 6].x
                            + filteredvalue[i, edindex + 8 * kk + 7].x + filteredvalue[i, edindex + 8 * kk + 8].x) / 8.0f;
                            filteredvalue[i, stindex + kk].y = (filteredvalue[i, edindex + 8 * kk + 1].y + filteredvalue[i, edindex + 8 * kk + 2].y
                            + filteredvalue[i, edindex + 8 * kk + 3].y + filteredvalue[i, edindex + 8 * kk + 4].y
                            + filteredvalue[i, edindex + 8 * kk + 5].y + filteredvalue[i, edindex + 8 * kk + 6].y
                            + filteredvalue[i, edindex + 8 * kk + 7].y + filteredvalue[i, edindex + 8 * kk + 8].y) / 8.0f;
                            filteredvalue[i, stindex + kk].z = (filteredvalue[i, edindex + 8 * kk + 1].z + filteredvalue[i, edindex + 8 * kk + 2].z
                            + filteredvalue[i, edindex + 8 * kk + 3].z + filteredvalue[i, edindex + 8 * kk + 4].z
                            + filteredvalue[i, edindex + 8 * kk + 5].z + filteredvalue[i, edindex + 8 * kk + 6].z
                            + filteredvalue[i, edindex + 8 * kk + 7].z + filteredvalue[i, edindex + 8 * kk + 8].z) / 8.0f;
                        }
                    }

                    for (int j = 0; j < totoutputpts; j++)
                    {
                        result[i * totoutputpts + j].x = filteredvalue[i, j].x;
                        result[i * totoutputpts + j].y = filteredvalue[i, j].y;
                        result[i * totoutputpts + j].z = filteredvalue[i, j].z;
                    }
                    
                    /*
                    for (int j = 0; j < nbpnt; j++)
                    {
                        filteredvalue[0, j].x = bottomvel[j].x;
                        filteredvalue[0, j].y = bottomvel[j].y;
                        filteredvalue[0, j].z = bottomvel[j].z;
                    }
                    for (int j = 1; j < totlayers + 1; j++)
                    {
                        for (int k = 0; k < (int)(Math.Pow(8, totlayers - j)); k++)
                        {
                            filteredvalue[j, k].x = (filteredvalue[j - 1, 8 * k].x + filteredvalue[j - 1, 8 * k + 1].x
                                + filteredvalue[j - 1, 8 * k + 2].x + filteredvalue[j - 1, 8 * k + 3].x
                                + filteredvalue[j - 1, 8 * k + 4].x + filteredvalue[j - 1, 8 * k + 5].x
                                + filteredvalue[j - 1, 8 * k + 6].x + filteredvalue[j - 1, 8 * k + 7].x) / 8.0f;
                            filteredvalue[j, k].y = (filteredvalue[j - 1, 8 * k].y + filteredvalue[j - 1, 8 * k + 1].y
                                + filteredvalue[j - 1, 8 * k + 2].y + filteredvalue[j - 1, 8 * k + 3].y
                                + filteredvalue[j - 1, 8 * k + 4].y + filteredvalue[j - 1, 8 * k + 5].y
                                + filteredvalue[j - 1, 8 * k + 6].y + filteredvalue[j - 1, 8 * k + 7].y) / 8.0f;
                            filteredvalue[j, k].z = (filteredvalue[j - 1, 8 * k].z + filteredvalue[j - 1, 8 * k + 1].z
                                + filteredvalue[j - 1, 8 * k + 2].z + filteredvalue[j - 1, 8 * k + 3].z
                                + filteredvalue[j - 1, 8 * k + 4].z + filteredvalue[j - 1, 8 * k + 5].z
                                + filteredvalue[j - 1, 8 * k + 6].z + filteredvalue[j - 1, 8 * k + 7].z) / 8.0f;
                        }
                    }

                    for (int j = 1; j < totlayers + 1; j++)
                    {
                        for (int k = 0; k < (int)(Math.Pow(8, totlayers - j)); k++)
                        {
                            int index = 0;
                            for (int jj = 0; jj < totlayers - j; jj++)
                            {
                                index = index + (int)(Math.Pow(8, jj));
                            }
                            index = index + k;
                            result[i, index].x = filteredvalue[j, k].x;
                            result[i, index].y = filteredvalue[j, k].y;
                            result[i, index].z = filteredvalue[j, k].z;
                        }
                    }
                    */
                }
            }
            return result;
         }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"BoxFilterForPressure")]
        public float[] GetBoxFilterPressure(string authToken, string dataset, float time,
            float filterlength, int nlayers,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation, Point3[] points)
        {
            throw new Exception("BoxFilterForPressure is an experimental function and currently disabled.  Please e-mail turbulence@jhu.edu if you need to use it.");
            /*
            if (nlayers <= 0)
            {
                throw new Exception(String.Format("Invalid input of nlayers: {0}", nlayers));
            }
            float Dx = 2.0f * (float)(Math.PI) / 1024;
            float[] result;
            if (filterlength < Dx)
            {
                /--*if (nlayers > 1)
                {
                    throw new Exception("The input for nlayers exceeds the number of total layers");
                }
                result = new float[points.Length];
                Vector3P[] bottomvelandp;
                bottomvelandp = GetVelocityAndPressure(authToken, dataset, time, TurbulenceOptions.SpatialInterpolation.Lag4, temporalInterpolation, points);
                for (int i = 0; i < points.Length; i++)
                {
                    result[i] = bottomvelandp[i].p;
                }*--/
                throw new Exception("Filter length is smaller than grid size. Filter calculation not operated.");
            }
            else
            {
                int totlayers = (int)(Math.Ceiling(Math.Log(filterlength / Dx) / Math.Log(2.0f))); //original point not counted
                if (nlayers > totlayers)
                {
                    throw new Exception("The input for nlayers exceeds the number of total layers");
                }
                int totfilteredpts = 0;
                int totoutputpts = 0;
                for (int k = 0; k < totlayers; k++)
                {
                    totfilteredpts = totfilteredpts + (int)(Math.Pow(8, k));
                }

                for (int k = 0; k < nlayers; k++)
                {
                    totoutputpts = totoutputpts + (int)(Math.Pow(8, k));
                }

                result = new float[points.Length * totoutputpts];
                float[,] filteredvalue = new float[points.Length, totfilteredpts];
                int nbpnt = (int)(Math.Pow(8, totlayers));
                //Vector3[,] filteredvalue = new Vector3[totlayers + 1, nbpnt];
                Point3[] bottompts = new Point3[nbpnt];
                Vector3P[] bottomvelandp;
                int[,] idx = new int[nbpnt, totlayers];
                float[] subFL = new float[totlayers];
                int stindex, edindex;

                for (int k = 0; k < totlayers; k++)
                {
                    subFL[k] = filterlength / (int)(Math.Pow(2, k + 1));
                }

                for (int i = 0; i < points.Length; i++)
                {
                    for (int j = 0; j < nbpnt; j++)
                    {
                        bottompts[j] = new Point3();
                        idx[j, 0] = (int)(j / (int)(Math.Pow(8, totlayers - 1)));
                        int clk = j;
                        for (int k = 1; k < totlayers; k++)
                        {
                            clk = clk - idx[j, k - 1] * (int)(Math.Pow(8, totlayers - k));
                            idx[j, k] = (int)(clk / (int)(Math.Pow(8, totlayers - k - 1)));

                        }
                        bottompts[j].x = points[i].x;
                        bottompts[j].y = points[i].y;
                        bottompts[j].z = points[i].z;
                        for (int k = 0; k < totlayers; k++)
                        {
                            if (idx[j, k] == 0 || idx[j, k] == 2 || idx[j, k] == 4 || idx[j, k] == 6)
                                bottompts[j].x = bottompts[j].x - subFL[k] / 2;
                            else
                                bottompts[j].x = bottompts[j].x + subFL[k] / 2;

                            if (idx[j, k] == 0 || idx[j, k] == 1 || idx[j, k] == 4 || idx[j, k] == 5)
                                bottompts[j].y = bottompts[j].y - subFL[k] / 2;
                            else
                                bottompts[j].y = bottompts[j].y + subFL[k] / 2;

                            if (idx[j, k] == 0 || idx[j, k] == 1 || idx[j, k] == 2 || idx[j, k] == 3)
                                bottompts[j].z = bottompts[j].z - subFL[k] / 2;
                            else
                                bottompts[j].z = bottompts[j].z + subFL[k] / 2;
                        }
                    }
                    bottomvelandp = GetVelocityAndPressure(authToken, dataset, time, TurbulenceOptions.SpatialInterpolation.Lag4, temporalInterpolation, bottompts);

                    stindex = totfilteredpts - (int)(Math.Pow(8, totlayers - 1));
                    for (int j = 0; j < (int)(Math.Pow(8, totlayers - 1)); j++)
                    {
                        filteredvalue[i, stindex + j] = (bottomvelandp[8 * j].p + bottomvelandp[8 * j + 1].p + bottomvelandp[8 * j + 2].p
                            + bottomvelandp[8 * j + 3].p + bottomvelandp[8 * j + 4].p + bottomvelandp[8 * j + 5].p + bottomvelandp[8 * j + 6].p
                            + bottomvelandp[8 * j + 7].p) / 8.0f;
                    }

                    for (int k = 1; k < totlayers; k++)
                    {
                        edindex = stindex - 1;
                        stindex = stindex - (int)(Math.Pow(8, totlayers - k - 1));
                        for (int kk = 0; kk < (int)(Math.Pow(8, totlayers - k - 1)); kk++)
                        {

                            filteredvalue[i, stindex + kk] = (filteredvalue[i, edindex + 8 * kk + 1] + filteredvalue[i, edindex + 8 * kk + 2]
                            + filteredvalue[i, edindex + 8 * kk + 3] + filteredvalue[i, edindex + 8 * kk + 4]
                            + filteredvalue[i, edindex + 8 * kk + 5] + filteredvalue[i, edindex + 8 * kk + 6]
                            + filteredvalue[i, edindex + 8 * kk + 7] + filteredvalue[i, edindex + 8 * kk + 8]) / 8.0f;
                        }
                    }

                    for (int j = 0; j < totoutputpts; j++)
                    {
                        result[i * totoutputpts + j] = filteredvalue[i, j];
                    }
                }
            }
            return result; */
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"BoxFilterForVelocityGradient")]
        public VelocityGradient[] GetBoxFilterVelocityGradient(string authToken, string dataset, float time,
            float filterlength, int nlayers,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation, Point3[] points)
        {
            throw new Exception("GetBoxFilterVelocityGradient is an experimental function and currently disabled.  Please e-mail turbulence@jhu.edu if you need to use it.");
            /* if (nlayers <= 0)
            {
                throw new Exception(String.Format("Invalid input of nlayers: {0}", nlayers));
            }
            float Dx = 2.0f * (float)(Math.PI) / 1024;
            VelocityGradient[] result;
            if (filterlength < Dx)
            {
                /*--if (nlayers > 1)
                {
                    throw new Exception("The input for nlayers exceeds the number of total layers");
                }
                result = new VelocityGradient[points.Length];
                VelocityGradient[] bottomvelgrad;
                bottomvelgrad = GetVelocityGradient(authToken, dataset, time, TurbulenceOptions.SpatialInterpolation.Fd4Lag4, temporalInterpolation, points);
                for (int i = 0; i < points.Length; i++)
                {
                    result[i].duxdx = bottomvelgrad[i].duxdx;
                    result[i].duxdy = bottomvelgrad[i].duxdy;
                    result[i].duxdz = bottomvelgrad[i].duxdz;
                    result[i].duydx = bottomvelgrad[i].duydx;
                    result[i].duydy = bottomvelgrad[i].duydy;
                    result[i].duydz = bottomvelgrad[i].duydz;
                    result[i].duzdx = bottomvelgrad[i].duzdx;
                    result[i].duzdy = bottomvelgrad[i].duzdy;
                    result[i].duzdz = bottomvelgrad[i].duzdz;
                }*--/
                throw new Exception("Filter length is smaller than grid size. Filter calculation not operated.");
            }
            else
            {
                int totlayers = (int)(Math.Ceiling(Math.Log(filterlength / Dx) / Math.Log(2.0f))); //original point not counted
                if (nlayers > totlayers)
                {
                    throw new Exception("The input for nlayers exceeds the number of total layers");
                }
                int totfilteredpts = 0;
                int totoutputpts = 0;
                for (int k = 0; k < totlayers; k++)
                {
                    totfilteredpts = totfilteredpts + (int)(Math.Pow(8, k));
                }

                for (int k = 0; k < nlayers; k++)
                {
                    totoutputpts = totoutputpts + (int)(Math.Pow(8, k));
                }

                result = new VelocityGradient[points.Length * totoutputpts];
                VelocityGradient[,] filteredvalue = new VelocityGradient[points.Length, totfilteredpts];
                int nbpnt = (int)(Math.Pow(8, totlayers));
                Point3[] bottompts = new Point3[nbpnt];
                VelocityGradient[] bottomvelgrad;
                int[,] idx = new int[nbpnt, totlayers];
                float[] subFL = new float[totlayers];
                int stindex, edindex;

                for (int k = 0; k < totlayers; k++)
                {
                    subFL[k] = filterlength / (int)(Math.Pow(2, k + 1));
                }

                for (int i = 0; i < points.Length; i++)
                {
                    for (int j = 0; j < nbpnt; j++)
                    {
                        bottompts[j] = new Point3();
                        idx[j, 0] = (int)(j / (int)(Math.Pow(8, totlayers - 1)));
                        int clk = j;
                        for (int k = 1; k < totlayers; k++)
                        {
                            clk = clk - idx[j, k - 1] * (int)(Math.Pow(8, totlayers - k));
                            idx[j, k] = (int)(clk / (int)(Math.Pow(8, totlayers - k - 1)));

                        }
                        bottompts[j].x = points[i].x;
                        bottompts[j].y = points[i].y;
                        bottompts[j].z = points[i].z;
                        for (int k = 0; k < totlayers; k++)
                        {
                            if (idx[j, k] == 0 || idx[j, k] == 2 || idx[j, k] == 4 || idx[j, k] == 6)
                                bottompts[j].x = bottompts[j].x - subFL[k] / 2;
                            else
                                bottompts[j].x = bottompts[j].x + subFL[k] / 2;

                            if (idx[j, k] == 0 || idx[j, k] == 1 || idx[j, k] == 4 || idx[j, k] == 5)
                                bottompts[j].y = bottompts[j].y - subFL[k] / 2;
                            else
                                bottompts[j].y = bottompts[j].y + subFL[k] / 2;

                            if (idx[j, k] == 0 || idx[j, k] == 1 || idx[j, k] == 2 || idx[j, k] == 3)
                                bottompts[j].z = bottompts[j].z - subFL[k] / 2;
                            else
                                bottompts[j].z = bottompts[j].z + subFL[k] / 2;
                        }
                    }
                    bottomvelgrad = GetVelocityGradient(authToken, dataset, time, TurbulenceOptions.SpatialInterpolation.Fd4Lag4, temporalInterpolation, bottompts);

                    stindex = totfilteredpts - (int)(Math.Pow(8, totlayers - 1));
                    for (int j = 0; j < (int)(Math.Pow(8, totlayers - 1)); j++)
                    {
                        filteredvalue[i, stindex + j].duxdx = (bottomvelgrad[8 * j].duxdx + bottomvelgrad[8 * j + 1].duxdx + bottomvelgrad[8 * j + 2].duxdx
                            + bottomvelgrad[8 * j + 3].duxdx + bottomvelgrad[8 * j + 4].duxdx + bottomvelgrad[8 * j + 5].duxdx + bottomvelgrad[8 * j + 6].duxdx
                            + bottomvelgrad[8 * j + 7].duxdx) / 8.0f;
                        filteredvalue[i, stindex + j].duxdy = (bottomvelgrad[8 * j].duxdy + bottomvelgrad[8 * j + 1].duxdy + bottomvelgrad[8 * j + 2].duxdy
                            + bottomvelgrad[8 * j + 3].duxdy + bottomvelgrad[8 * j + 4].duxdy + bottomvelgrad[8 * j + 5].duxdy + bottomvelgrad[8 * j + 6].duxdy
                            + bottomvelgrad[8 * j + 7].duxdy) / 8.0f;
                        filteredvalue[i, stindex + j].duxdz = (bottomvelgrad[8 * j].duxdz + bottomvelgrad[8 * j + 1].duxdz + bottomvelgrad[8 * j + 2].duxdz
                            + bottomvelgrad[8 * j + 3].duxdz + bottomvelgrad[8 * j + 4].duxdz + bottomvelgrad[8 * j + 5].duxdz + bottomvelgrad[8 * j + 6].duxdz
                            + bottomvelgrad[8 * j + 7].duxdz) / 8.0f;
                        filteredvalue[i, stindex + j].duydx = (bottomvelgrad[8 * j].duydx + bottomvelgrad[8 * j + 1].duydx + bottomvelgrad[8 * j + 2].duydx
                            + bottomvelgrad[8 * j + 3].duydx + bottomvelgrad[8 * j + 4].duydx + bottomvelgrad[8 * j + 5].duydx + bottomvelgrad[8 * j + 6].duydx
                            + bottomvelgrad[8 * j + 7].duydx) / 8.0f;
                        filteredvalue[i, stindex + j].duydy = (bottomvelgrad[8 * j].duydy + bottomvelgrad[8 * j + 1].duydy + bottomvelgrad[8 * j + 2].duydy
                            + bottomvelgrad[8 * j + 3].duydy + bottomvelgrad[8 * j + 4].duydy + bottomvelgrad[8 * j + 5].duydy + bottomvelgrad[8 * j + 6].duydy
                            + bottomvelgrad[8 * j + 7].duydy) / 8.0f;
                        filteredvalue[i, stindex + j].duydz = (bottomvelgrad[8 * j].duydz + bottomvelgrad[8 * j + 1].duydz + bottomvelgrad[8 * j + 2].duydz
                            + bottomvelgrad[8 * j + 3].duydz + bottomvelgrad[8 * j + 4].duydz + bottomvelgrad[8 * j + 5].duydz + bottomvelgrad[8 * j + 6].duydz
                            + bottomvelgrad[8 * j + 7].duydz) / 8.0f;
                        filteredvalue[i, stindex + j].duzdx = (bottomvelgrad[8 * j].duzdx + bottomvelgrad[8 * j + 1].duzdx + bottomvelgrad[8 * j + 2].duzdx
                            + bottomvelgrad[8 * j + 3].duzdx + bottomvelgrad[8 * j + 4].duzdx + bottomvelgrad[8 * j + 5].duzdx + bottomvelgrad[8 * j + 6].duzdx
                            + bottomvelgrad[8 * j + 7].duzdx) / 8.0f;
                        filteredvalue[i, stindex + j].duzdy = (bottomvelgrad[8 * j].duzdy + bottomvelgrad[8 * j + 1].duzdy + bottomvelgrad[8 * j + 2].duzdy
                            + bottomvelgrad[8 * j + 3].duzdy + bottomvelgrad[8 * j + 4].duzdy + bottomvelgrad[8 * j + 5].duzdy + bottomvelgrad[8 * j + 6].duzdy
                            + bottomvelgrad[8 * j + 7].duzdy) / 8.0f;
                        filteredvalue[i, stindex + j].duzdz = (bottomvelgrad[8 * j].duzdz + bottomvelgrad[8 * j + 1].duzdz + bottomvelgrad[8 * j + 2].duzdz
                            + bottomvelgrad[8 * j + 3].duzdz + bottomvelgrad[8 * j + 4].duzdz + bottomvelgrad[8 * j + 5].duzdz + bottomvelgrad[8 * j + 6].duzdz
                            + bottomvelgrad[8 * j + 7].duzdz) / 8.0f;
                    }

                    for (int k = 1; k < totlayers; k++)
                    {
                        edindex = stindex - 1;
                        stindex = stindex - (int)(Math.Pow(8, totlayers - k - 1));
                        for (int kk = 0; kk < (int)(Math.Pow(8, totlayers - k - 1)); kk++)
                        {

                            filteredvalue[i, stindex + kk].duxdx =
                                (filteredvalue[i, edindex + 8 * kk + 1].duxdx + filteredvalue[i, edindex + 8 * kk + 2].duxdx
                            + filteredvalue[i, edindex + 8 * kk + 3].duxdx + filteredvalue[i, edindex + 8 * kk + 4].duxdx
                            + filteredvalue[i, edindex + 8 * kk + 5].duxdx + filteredvalue[i, edindex + 8 * kk + 6].duxdx
                            + filteredvalue[i, edindex + 8 * kk + 7].duxdx + filteredvalue[i, edindex + 8 * kk + 8].duxdx) / 8.0f;
                            filteredvalue[i, stindex + kk].duxdy =
                                (filteredvalue[i, edindex + 8 * kk + 1].duxdy + filteredvalue[i, edindex + 8 * kk + 2].duxdy
                            + filteredvalue[i, edindex + 8 * kk + 3].duxdy + filteredvalue[i, edindex + 8 * kk + 4].duxdy
                            + filteredvalue[i, edindex + 8 * kk + 5].duxdy + filteredvalue[i, edindex + 8 * kk + 6].duxdy
                            + filteredvalue[i, edindex + 8 * kk + 7].duxdy + filteredvalue[i, edindex + 8 * kk + 8].duxdy) / 8.0f;
                            filteredvalue[i, stindex + kk].duxdz =
                                (filteredvalue[i, edindex + 8 * kk + 1].duxdz + filteredvalue[i, edindex + 8 * kk + 2].duxdz
                            + filteredvalue[i, edindex + 8 * kk + 3].duxdz + filteredvalue[i, edindex + 8 * kk + 4].duxdz
                            + filteredvalue[i, edindex + 8 * kk + 5].duxdz + filteredvalue[i, edindex + 8 * kk + 6].duxdz
                            + filteredvalue[i, edindex + 8 * kk + 7].duxdz + filteredvalue[i, edindex + 8 * kk + 8].duxdz) / 8.0f;
                            filteredvalue[i, stindex + kk].duydx =
                                (filteredvalue[i, edindex + 8 * kk + 1].duydx + filteredvalue[i, edindex + 8 * kk + 2].duydx
                            + filteredvalue[i, edindex + 8 * kk + 3].duydx + filteredvalue[i, edindex + 8 * kk + 4].duydx
                            + filteredvalue[i, edindex + 8 * kk + 5].duydx + filteredvalue[i, edindex + 8 * kk + 6].duydx
                            + filteredvalue[i, edindex + 8 * kk + 7].duydx + filteredvalue[i, edindex + 8 * kk + 8].duydx) / 8.0f;
                            filteredvalue[i, stindex + kk].duydy =
                                (filteredvalue[i, edindex + 8 * kk + 1].duydy + filteredvalue[i, edindex + 8 * kk + 2].duydy
                            + filteredvalue[i, edindex + 8 * kk + 3].duydy + filteredvalue[i, edindex + 8 * kk + 4].duydy
                            + filteredvalue[i, edindex + 8 * kk + 5].duydy + filteredvalue[i, edindex + 8 * kk + 6].duydy
                            + filteredvalue[i, edindex + 8 * kk + 7].duydy + filteredvalue[i, edindex + 8 * kk + 8].duydy) / 8.0f;
                            filteredvalue[i, stindex + kk].duydz =
                                (filteredvalue[i, edindex + 8 * kk + 1].duydz + filteredvalue[i, edindex + 8 * kk + 2].duydz
                            + filteredvalue[i, edindex + 8 * kk + 3].duydz + filteredvalue[i, edindex + 8 * kk + 4].duydz
                            + filteredvalue[i, edindex + 8 * kk + 5].duydz + filteredvalue[i, edindex + 8 * kk + 6].duydz
                            + filteredvalue[i, edindex + 8 * kk + 7].duydz + filteredvalue[i, edindex + 8 * kk + 8].duydz) / 8.0f;
                            filteredvalue[i, stindex + kk].duzdx =
                                (filteredvalue[i, edindex + 8 * kk + 1].duzdx + filteredvalue[i, edindex + 8 * kk + 2].duzdx
                            + filteredvalue[i, edindex + 8 * kk + 3].duzdx + filteredvalue[i, edindex + 8 * kk + 4].duzdx
                            + filteredvalue[i, edindex + 8 * kk + 5].duzdx + filteredvalue[i, edindex + 8 * kk + 6].duzdx
                            + filteredvalue[i, edindex + 8 * kk + 7].duzdx + filteredvalue[i, edindex + 8 * kk + 8].duzdx) / 8.0f;
                            filteredvalue[i, stindex + kk].duzdy =
                                (filteredvalue[i, edindex + 8 * kk + 1].duzdy + filteredvalue[i, edindex + 8 * kk + 2].duzdy
                            + filteredvalue[i, edindex + 8 * kk + 3].duzdy + filteredvalue[i, edindex + 8 * kk + 4].duzdy
                            + filteredvalue[i, edindex + 8 * kk + 5].duzdy + filteredvalue[i, edindex + 8 * kk + 6].duzdy
                            + filteredvalue[i, edindex + 8 * kk + 7].duzdy + filteredvalue[i, edindex + 8 * kk + 8].duzdy) / 8.0f;
                            filteredvalue[i, stindex + kk].duzdz =
                                (filteredvalue[i, edindex + 8 * kk + 1].duzdz + filteredvalue[i, edindex + 8 * kk + 2].duzdz
                            + filteredvalue[i, edindex + 8 * kk + 3].duzdz + filteredvalue[i, edindex + 8 * kk + 4].duzdz
                            + filteredvalue[i, edindex + 8 * kk + 5].duzdz + filteredvalue[i, edindex + 8 * kk + 6].duzdz
                            + filteredvalue[i, edindex + 8 * kk + 7].duzdz + filteredvalue[i, edindex + 8 * kk + 8].duzdz) / 8.0f;
                        }
                    }

                    for (int j = 0; j < totoutputpts; j++)
                    {
                        result[i * totoutputpts + j].duxdx = filteredvalue[i, j].duxdx;
                        result[i * totoutputpts + j].duxdy = filteredvalue[i, j].duxdy;
                        result[i * totoutputpts + j].duxdz = filteredvalue[i, j].duxdz;
                        result[i * totoutputpts + j].duydx = filteredvalue[i, j].duydx;
                        result[i * totoutputpts + j].duydy = filteredvalue[i, j].duydy;
                        result[i * totoutputpts + j].duydz = filteredvalue[i, j].duydz;
                        result[i * totoutputpts + j].duzdx = filteredvalue[i, j].duzdx;
                        result[i * totoutputpts + j].duzdy = filteredvalue[i, j].duzdy;
                        result[i * totoutputpts + j].duzdz = filteredvalue[i, j].duzdz;
                    }
                }
            }
            return result; */
        }


        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"BoxFilterForSubGridScaleStress")]
        public SGSStress[] GetBoxFilterSGSStress(string authToken, string dataset, float time,
            float filterlength, int nlayers,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation, Point3[] points)
        {
            throw new Exception("GetBoxFilterSGSStress is an experimental function and currently disabled.  Please e-mail turbulence@jhu.edu if you need to use it.");
            /*if (nlayers <= 0)
            {
                throw new Exception(String.Format("Invalid input of nlayers: {0}", nlayers));
            }
            float Dx = 2.0f * (float)(Math.PI) / 1024;
            SGSStress[] result;
            if (filterlength < Dx)
            {
                /--*if (nlayers > 1)
                {
                    throw new Exception("The input for nlayers exceeds the number of total layers");
                }
                result = new SGSStress[points.Length];
                Vector3[] bottomvel;
                bottomvel = GetVelocity(authToken, dataset, time, TurbulenceOptions.SpatialInterpolation.Lag4, temporalInterpolation, points);
                for (int i = 0; i < points.Length; i++)
                {
                    result[i].xx = 0;
                    result[i].yy = 0;
                    result[i].zz = 0;
                    result[i].xy = 0;
                    result[i].xz = 0;
                    result[i].yz = 0;
                }*--/
                throw new Exception("Filter length is smaller than grid size. Filter calculation not operated.");
            }
            else
            {
                int totlayers = (int)(Math.Ceiling(Math.Log(filterlength / Dx) / Math.Log(2.0f))); //original point not counted
                if (nlayers > totlayers)
                {
                    throw new Exception("The input for nlayers exceeds the number of total layers");
                }
                int totfilteredpts = 0;
                int totoutputpts = 0;
                for (int k = 0; k < totlayers; k++)
                {
                    totfilteredpts = totfilteredpts + (int)(Math.Pow(8, k));
                }

                for (int k = 0; k < nlayers; k++)
                {
                    totoutputpts = totoutputpts + (int)(Math.Pow(8, k));
                }

                result = new SGSStress[points.Length * totoutputpts];
                SGSStress[,] filteredvalue = new SGSStress[points.Length, totfilteredpts];
                SGSStress[,] filteredvalue1 = new SGSStress[points.Length, totfilteredpts];
                Vector3[,] filteredvalue2 = new Vector3[points.Length, totfilteredpts];
                int nbpnt = (int)(Math.Pow(8, totlayers));
                //Vector3[,] filteredvalue = new Vector3[totlayers + 1, nbpnt];
                Point3[] bottompts = new Point3[nbpnt];
                Vector3[] bottomvel;
                int[,] idx = new int[nbpnt, totlayers];
                float[] subFL = new float[totlayers];
                int stindex, edindex;

                for (int k = 0; k < totlayers; k++)
                {
                    subFL[k] = filterlength / (int)(Math.Pow(2, k + 1));
                }

                for (int i = 0; i < points.Length; i++)
                {
                    for (int j = 0; j < nbpnt; j++)
                    {
                        bottompts[j] = new Point3();
                        idx[j, 0] = (int)(j / (int)(Math.Pow(8, totlayers - 1)));
                        int clk = j;
                        for (int k = 1; k < totlayers; k++)
                        {
                            clk = clk - idx[j, k - 1] * (int)(Math.Pow(8, totlayers - k));
                            idx[j, k] = (int)(clk / (int)(Math.Pow(8, totlayers - k - 1)));

                        }
                        bottompts[j].x = points[i].x;
                        bottompts[j].y = points[i].y;
                        bottompts[j].z = points[i].z;
                        for (int k = 0; k < totlayers; k++)
                        {
                            if (idx[j, k] == 0 || idx[j, k] == 2 || idx[j, k] == 4 || idx[j, k] == 6)
                                bottompts[j].x = bottompts[j].x - subFL[k] / 2;
                            else
                                bottompts[j].x = bottompts[j].x + subFL[k] / 2;

                            if (idx[j, k] == 0 || idx[j, k] == 1 || idx[j, k] == 4 || idx[j, k] == 5)
                                bottompts[j].y = bottompts[j].y - subFL[k] / 2;
                            else
                                bottompts[j].y = bottompts[j].y + subFL[k] / 2;

                            if (idx[j, k] == 0 || idx[j, k] == 1 || idx[j, k] == 2 || idx[j, k] == 3)
                                bottompts[j].z = bottompts[j].z - subFL[k] / 2;
                            else
                                bottompts[j].z = bottompts[j].z + subFL[k] / 2;
                        }
                    }
                    bottomvel = GetVelocity(authToken, dataset, time, TurbulenceOptions.SpatialInterpolation.Lag4, temporalInterpolation, bottompts);

                    stindex = totfilteredpts - (int)(Math.Pow(8, totlayers - 1));
                    for (int j = 0; j < (int)(Math.Pow(8, totlayers - 1)); j++)
                    {
                        filteredvalue1[i, stindex + j].xx = (bottomvel[8 * j].x * bottomvel[8 * j].x + bottomvel[8 * j + 1].x * bottomvel[8 * j + 1].x
                            + bottomvel[8 * j + 2].x * bottomvel[8 * j + 2].x + bottomvel[8 * j + 3].x * bottomvel[8 * j + 3].x
                            + bottomvel[8 * j + 4].x * bottomvel[8 * j + 4].x + bottomvel[8 * j + 5].x * bottomvel[8 * j + 5].x
                            + bottomvel[8 * j + 6].x * bottomvel[8 * j + 6].x + bottomvel[8 * j + 7].x * bottomvel[8 * j + 7].x) / 8.0f;
                        filteredvalue1[i, stindex + j].yy = (bottomvel[8 * j].y * bottomvel[8 * j].y + bottomvel[8 * j + 1].y * bottomvel[8 * j + 1].y
                            + bottomvel[8 * j + 2].y * bottomvel[8 * j + 2].y + bottomvel[8 * j + 3].y * bottomvel[8 * j + 3].y
                            + bottomvel[8 * j + 4].y * bottomvel[8 * j + 4].y + bottomvel[8 * j + 5].y * bottomvel[8 * j + 5].y
                            + bottomvel[8 * j + 6].y * bottomvel[8 * j + 6].y + bottomvel[8 * j + 7].y * bottomvel[8 * j + 7].y) / 8.0f;
                        filteredvalue1[i, stindex + j].zz = (bottomvel[8 * j].z * bottomvel[8 * j].z + bottomvel[8 * j + 1].z * bottomvel[8 * j + 1].z
                            + bottomvel[8 * j + 2].z * bottomvel[8 * j + 2].z + bottomvel[8 * j + 3].z * bottomvel[8 * j + 3].z
                            + bottomvel[8 * j + 4].z * bottomvel[8 * j + 4].z + bottomvel[8 * j + 5].z * bottomvel[8 * j + 5].z
                            + bottomvel[8 * j + 6].z * bottomvel[8 * j + 6].z + bottomvel[8 * j + 7].z * bottomvel[8 * j + 7].z) / 8.0f;
                        filteredvalue1[i, stindex + j].xy = (bottomvel[8 * j].x * bottomvel[8 * j].y + bottomvel[8 * j + 1].x * bottomvel[8 * j + 1].y
                            + bottomvel[8 * j + 2].x * bottomvel[8 * j + 2].y + bottomvel[8 * j + 3].x * bottomvel[8 * j + 3].y
                            + bottomvel[8 * j + 4].x * bottomvel[8 * j + 4].y + bottomvel[8 * j + 5].x * bottomvel[8 * j + 5].y
                            + bottomvel[8 * j + 6].x * bottomvel[8 * j + 6].y + bottomvel[8 * j + 7].x * bottomvel[8 * j + 7].y) / 8.0f;
                        filteredvalue1[i, stindex + j].xz = (bottomvel[8 * j].x * bottomvel[8 * j].z + bottomvel[8 * j + 1].x * bottomvel[8 * j + 1].z
                            + bottomvel[8 * j + 2].x * bottomvel[8 * j + 2].z + bottomvel[8 * j + 3].x * bottomvel[8 * j + 3].z
                            + bottomvel[8 * j + 4].x * bottomvel[8 * j + 4].z + bottomvel[8 * j + 5].x * bottomvel[8 * j + 5].z
                            + bottomvel[8 * j + 6].x * bottomvel[8 * j + 6].z + bottomvel[8 * j + 7].x * bottomvel[8 * j + 7].z) / 8.0f;
                        filteredvalue1[i, stindex + j].yz = (bottomvel[8 * j].y * bottomvel[8 * j].z + bottomvel[8 * j + 1].y * bottomvel[8 * j + 1].z
                            + bottomvel[8 * j + 2].y * bottomvel[8 * j + 2].z + bottomvel[8 * j + 3].y * bottomvel[8 * j + 3].z
                            + bottomvel[8 * j + 4].y * bottomvel[8 * j + 4].z + bottomvel[8 * j + 5].y * bottomvel[8 * j + 5].z
                            + bottomvel[8 * j + 6].y * bottomvel[8 * j + 6].z + bottomvel[8 * j + 7].y * bottomvel[8 * j + 7].z) / 8.0f;

                        filteredvalue2[i, stindex + j].x = (bottomvel[8 * j].x + bottomvel[8 * j + 1].x + bottomvel[8 * j + 2].x
                            + bottomvel[8 * j + 3].x + bottomvel[8 * j + 4].x + bottomvel[8 * j + 5].x + bottomvel[8 * j + 6].x
                            + bottomvel[8 * j + 7].x) / 8.0f;
                        filteredvalue2[i, stindex + j].y = (bottomvel[8 * j].y + bottomvel[8 * j + 1].y + bottomvel[8 * j + 2].y
                            + bottomvel[8 * j + 3].y + bottomvel[8 * j + 4].y + bottomvel[8 * j + 5].y + bottomvel[8 * j + 6].y
                            + bottomvel[8 * j + 7].y) / 8.0f;
                        filteredvalue2[i, stindex + j].z = (bottomvel[8 * j].z + bottomvel[8 * j + 1].z + bottomvel[8 * j + 2].z
                            + bottomvel[8 * j + 3].z + bottomvel[8 * j + 4].z + bottomvel[8 * j + 5].z + bottomvel[8 * j + 6].z
                            + bottomvel[8 * j + 7].z) / 8.0f;

                        filteredvalue[i, stindex + j].xx = filteredvalue1[i, stindex + j].xx
                            - filteredvalue2[i, stindex + j].x * filteredvalue2[i, stindex + j].x;
                        filteredvalue[i, stindex + j].yy = filteredvalue1[i, stindex + j].yy
                            - filteredvalue2[i, stindex + j].y * filteredvalue2[i, stindex + j].y;
                        filteredvalue[i, stindex + j].zz = filteredvalue1[i, stindex + j].zz
                            - filteredvalue2[i, stindex + j].z * filteredvalue2[i, stindex + j].z;
                        filteredvalue[i, stindex + j].xy = filteredvalue1[i, stindex + j].xy
                            - filteredvalue2[i, stindex + j].x * filteredvalue2[i, stindex + j].y;
                        filteredvalue[i, stindex + j].xz = filteredvalue1[i, stindex + j].xz
                            - filteredvalue2[i, stindex + j].x * filteredvalue2[i, stindex + j].z;
                        filteredvalue[i, stindex + j].yz = filteredvalue1[i, stindex + j].yz
                            - filteredvalue2[i, stindex + j].y * filteredvalue2[i, stindex + j].z;
                    }

                    for (int k = 1; k < totlayers; k++)
                    {
                        edindex = stindex - 1;
                        stindex = stindex - (int)(Math.Pow(8, totlayers - k - 1));
                        for (int kk = 0; kk < (int)(Math.Pow(8, totlayers - k - 1)); kk++)
                        {
                            filteredvalue1[i, stindex + kk].xx = (filteredvalue1[i, edindex + 8 * kk + 1].xx + filteredvalue1[i, edindex + 8 * kk + 2].xx
                            + filteredvalue1[i, edindex + 8 * kk + 3].xx + filteredvalue1[i, edindex + 8 * kk + 4].xx
                            + filteredvalue1[i, edindex + 8 * kk + 5].xx + filteredvalue1[i, edindex + 8 * kk + 6].xx
                            + filteredvalue1[i, edindex + 8 * kk + 7].xx + filteredvalue1[i, edindex + 8 * kk + 8].xx) / 8.0f;
                            filteredvalue1[i, stindex + kk].yy = (filteredvalue1[i, edindex + 8 * kk + 1].yy + filteredvalue1[i, edindex + 8 * kk + 2].yy
                            + filteredvalue1[i, edindex + 8 * kk + 3].yy + filteredvalue1[i, edindex + 8 * kk + 4].yy
                            + filteredvalue1[i, edindex + 8 * kk + 5].yy + filteredvalue1[i, edindex + 8 * kk + 6].yy
                            + filteredvalue1[i, edindex + 8 * kk + 7].yy + filteredvalue1[i, edindex + 8 * kk + 8].yy) / 8.0f;
                            filteredvalue1[i, stindex + kk].zz = (filteredvalue1[i, edindex + 8 * kk + 1].zz + filteredvalue1[i, edindex + 8 * kk + 2].zz
                            + filteredvalue1[i, edindex + 8 * kk + 3].zz + filteredvalue1[i, edindex + 8 * kk + 4].zz
                            + filteredvalue1[i, edindex + 8 * kk + 5].zz + filteredvalue1[i, edindex + 8 * kk + 6].zz
                            + filteredvalue1[i, edindex + 8 * kk + 7].zz + filteredvalue1[i, edindex + 8 * kk + 8].zz) / 8.0f;
                            filteredvalue1[i, stindex + kk].xy = (filteredvalue1[i, edindex + 8 * kk + 1].xy + filteredvalue1[i, edindex + 8 * kk + 2].xy
                            + filteredvalue1[i, edindex + 8 * kk + 3].xy + filteredvalue1[i, edindex + 8 * kk + 4].xy
                            + filteredvalue1[i, edindex + 8 * kk + 5].xy + filteredvalue1[i, edindex + 8 * kk + 6].xy
                            + filteredvalue1[i, edindex + 8 * kk + 7].xy + filteredvalue1[i, edindex + 8 * kk + 8].xy) / 8.0f;
                            filteredvalue1[i, stindex + kk].xz = (filteredvalue1[i, edindex + 8 * kk + 1].xz + filteredvalue1[i, edindex + 8 * kk + 2].xz
                            + filteredvalue1[i, edindex + 8 * kk + 3].xz + filteredvalue1[i, edindex + 8 * kk + 4].xz
                            + filteredvalue1[i, edindex + 8 * kk + 5].xz + filteredvalue1[i, edindex + 8 * kk + 6].xz
                            + filteredvalue1[i, edindex + 8 * kk + 7].xz + filteredvalue1[i, edindex + 8 * kk + 8].xz) / 8.0f;
                            filteredvalue1[i, stindex + kk].yz = (filteredvalue1[i, edindex + 8 * kk + 1].yz + filteredvalue1[i, edindex + 8 * kk + 2].yz
                            + filteredvalue1[i, edindex + 8 * kk + 3].yz + filteredvalue1[i, edindex + 8 * kk + 4].yz
                            + filteredvalue1[i, edindex + 8 * kk + 5].yz + filteredvalue1[i, edindex + 8 * kk + 6].yz
                            + filteredvalue1[i, edindex + 8 * kk + 7].yz + filteredvalue1[i, edindex + 8 * kk + 8].yz) / 8.0f;
                            
                            filteredvalue2[i, stindex + kk].x = (filteredvalue2[i, edindex + 8 * kk + 1].x + filteredvalue2[i, edindex + 8 * kk + 2].x
                            + filteredvalue2[i, edindex + 8 * kk + 3].x + filteredvalue2[i, edindex + 8 * kk + 4].x
                            + filteredvalue2[i, edindex + 8 * kk + 5].x + filteredvalue2[i, edindex + 8 * kk + 6].x
                            + filteredvalue2[i, edindex + 8 * kk + 7].x + filteredvalue2[i, edindex + 8 * kk + 8].x) / 8.0f;
                            filteredvalue2[i, stindex + kk].y = (filteredvalue2[i, edindex + 8 * kk + 1].y + filteredvalue2[i, edindex + 8 * kk + 2].y
                            + filteredvalue2[i, edindex + 8 * kk + 3].y + filteredvalue2[i, edindex + 8 * kk + 4].y
                            + filteredvalue2[i, edindex + 8 * kk + 5].y + filteredvalue2[i, edindex + 8 * kk + 6].y
                            + filteredvalue2[i, edindex + 8 * kk + 7].y + filteredvalue2[i, edindex + 8 * kk + 8].y) / 8.0f;
                            filteredvalue2[i, stindex + kk].z = (filteredvalue2[i, edindex + 8 * kk + 1].z + filteredvalue2[i, edindex + 8 * kk + 2].z
                            + filteredvalue2[i, edindex + 8 * kk + 3].z + filteredvalue2[i, edindex + 8 * kk + 4].z
                            + filteredvalue2[i, edindex + 8 * kk + 5].z + filteredvalue2[i, edindex + 8 * kk + 6].z
                            + filteredvalue2[i, edindex + 8 * kk + 7].z + filteredvalue2[i, edindex + 8 * kk + 8].z) / 8.0f;

                            filteredvalue[i, stindex + kk].xx = filteredvalue1[i, stindex + kk].xx
                                - filteredvalue2[i, stindex + kk].x * filteredvalue2[i, stindex + kk].x;
                            filteredvalue[i, stindex + kk].yy = filteredvalue1[i, stindex + kk].yy
                                - filteredvalue2[i, stindex + kk].y * filteredvalue2[i, stindex + kk].y;
                            filteredvalue[i, stindex + kk].zz = filteredvalue1[i, stindex + kk].zz
                                - filteredvalue2[i, stindex + kk].z * filteredvalue2[i, stindex + kk].z;
                            filteredvalue[i, stindex + kk].xy = filteredvalue1[i, stindex + kk].xy
                                - filteredvalue2[i, stindex + kk].x * filteredvalue2[i, stindex + kk].y;
                            filteredvalue[i, stindex + kk].xz = filteredvalue1[i, stindex + kk].xz
                                - filteredvalue2[i, stindex + kk].x * filteredvalue2[i, stindex + kk].z;
                            filteredvalue[i, stindex + kk].yz = filteredvalue1[i, stindex + kk].yz
                                - filteredvalue2[i, stindex + kk].y * filteredvalue2[i, stindex + kk].z;
                        }
                    }

                    for (int j = 0; j < totoutputpts; j++)
                    {
                        result[i * totoutputpts + j].xx = filteredvalue[i, j].xx;
                        result[i * totoutputpts + j].yy = filteredvalue[i, j].yy;
                        result[i * totoutputpts + j].zz = filteredvalue[i, j].zz;
                        result[i * totoutputpts + j].xy = filteredvalue[i, j].xy;
                        result[i * totoutputpts + j].xz = filteredvalue[i, j].xz;
                        result[i * totoutputpts + j].yz = filteredvalue[i, j].yz;
                    }
                }
            }
            return result; */
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"[UNDER DEVELOPMENT] GetPositionInertialParticle")]
        public Point3[] GetPositionInertialParticle(string authToken, string dataset, float StartTime,
            float EndTime, float Tp, TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            Point3[] points, Vector3[] InitialV)
        {
            throw new Exception("GetPositionInertialParticle is an experimental function and currently disabled.  Please e-mail turbulence@jhu.edu if you need to use it.");
            /*
            int npnt = points.Length;
            Vector3[] vf1, vf2, vf3;
            Vector3[] vp1 = new Vector3[npnt];
            Vector3[] vp2 = new Vector3[npnt];
            Vector3[] vp3 = new Vector3[npnt];
            Vector3[] vtmp = new Vector3[npnt];
            Vector3[] Q1 = new Vector3[npnt];
            Vector3[] Q2 = new Vector3[npnt];
            Point3[] xs = new Point3[npnt];

            float DNSdt = 0.0002f;
            int nt = (int)(Math.Ceiling((EndTime - StartTime) / DNSdt));
            float dt = (EndTime - StartTime) / nt;
            for (int i = 0; i < npnt; i++)
            {
                vp1[i] = new Vector3();
                vp2[i] = new Vector3();
                vp3[i] = new Vector3();
                vtmp[i] = new Vector3();
                Q1[i] = new Vector3();
                Q2[i] = new Vector3();
                xs[i] = new Point3();
                vp1[i].x = InitialV[i].x;
                vp1[i].y = InitialV[i].y;
                vp1[i].z = InitialV[i].z;
            }

            vf1 = GetVelocity(authToken, dataset, StartTime, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, points);
            for (int i = 0; i < npnt; i++)
            {
                Q1[i].x = 1.0f / Tp * (vf1[i].x - vp1[i].x);
                Q1[i].y = 1.0f / Tp * (vf1[i].y - vp1[i].y);
                Q1[i].z = 1.0f / Tp * (vf1[i].z - vp1[i].z);
                xs[i].x = points[i].x + vp1[i].x * dt;
                xs[i].y = points[i].y + vp1[i].y * dt;
                xs[i].z = points[i].z + vp1[i].z * dt;
            }
            vf2 = GetVelocity(authToken, dataset, StartTime + dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, xs);
            for (int i = 0; i < npnt; i++)
            {
                Q2[i].x = 1.0f / Tp * (vf2[i].x - vp1[i].x - dt * Q1[i].x);
                Q2[i].y = 1.0f / Tp * (vf2[i].y - vp1[i].y - dt * Q1[i].y);
                Q2[i].z = 1.0f / Tp * (vf2[i].z - vp1[i].z - dt * Q1[i].z);
                vp2[i].x = vp1[i].x + dt / 2.0f * (Q1[i].x + Q2[i].x);
                vp2[i].y = vp1[i].y + dt / 2.0f * (Q1[i].y + Q2[i].y);
                vp2[i].z = vp1[i].z + dt / 2.0f * (Q1[i].z + Q2[i].z);
                points[i].x = points[i].x + dt / 2.0f * (vp1[i].x + vp2[i].x);
                points[i].y = points[i].y + dt / 2.0f * (vp1[i].y + vp2[i].y);
                points[i].z = points[i].z + dt / 2.0f * (vp1[i].z + vp2[i].z);
            }

            vf2 = GetVelocity(authToken, dataset, StartTime + dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, points);
            for (int i = 0; i < npnt; i++)
            {
                Q1[i].x = 1.0f / Tp * (vf2[i].x - vp2[i].x);
                Q1[i].y = 1.0f / Tp * (vf2[i].y - vp2[i].y);
                Q1[i].z = 1.0f / Tp * (vf2[i].z - vp2[i].z);
                xs[i].x = points[i].x + vp2[i].x * dt;
                xs[i].y = points[i].y + vp2[i].y * dt;
                xs[i].z = points[i].z + vp2[i].z * dt;
            }
            vf3 = GetVelocity(authToken, dataset, StartTime + 2.0f * dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, xs);
            for (int i = 0; i < npnt; i++)
            {
                Q2[i].x = 1.0f / Tp * (vf3[i].x - vp2[i].x - dt * Q1[i].x);
                Q2[i].y = 1.0f / Tp * (vf3[i].y - vp2[i].y - dt * Q1[i].y);
                Q2[i].z = 1.0f / Tp * (vf3[i].z - vp2[i].z - dt * Q1[i].z);
                vp3[i].x = vp2[i].x + dt / 2.0f * (Q1[i].x + Q2[i].x);
                vp3[i].y = vp2[i].y + dt / 2.0f * (Q1[i].y + Q2[i].y);
                vp3[i].z = vp2[i].z + dt / 2.0f * (Q1[i].z + Q2[i].z);
                points[i].x = points[i].x + dt / 2.0f * (vp2[i].x + vp3[i].x);
                points[i].y = points[i].y + dt / 2.0f * (vp2[i].y + vp3[i].y);
                points[i].z = points[i].z + dt / 2.0f * (vp2[i].z + vp3[i].z);
            }

            for (int j = 2; j < nt; j++)
            {
                vf3 = GetVelocity(authToken, dataset, StartTime + j * dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, points);
                for (int i = 0; i < npnt; i++)
                {
                    points[i].x = points[i].x + dt * (23.0f / 12 * vp3[i].x - 4.0f / 3 * vp2[i].x + 5.0f / 12 * vp1[i].x);
                    points[i].y = points[i].y + dt * (23.0f / 12 * vp3[i].y - 4.0f / 3 * vp2[i].y + 5.0f / 12 * vp1[i].y);
                    points[i].z = points[i].z + dt * (23.0f / 12 * vp3[i].z - 4.0f / 3 * vp2[i].z + 5.0f / 12 * vp1[i].z);
                    vtmp[i].x = vp3[i].x + dt / Tp * (23.0f / 12 * (vf3[i].x - vp3[i].x) - 4.0f / 3 * (vf2[i].x - vp2[i].x)
                        + 5.0f / 12 * (vf1[i].x - vp1[i].x));
                    vtmp[i].y = vp3[i].y + dt / Tp * (23.0f / 12 * (vf3[i].y - vp3[i].y) - 4.0f / 3 * (vf2[i].y - vp2[i].y)
                        + 5.0f / 12 * (vf1[i].y - vp1[i].y));
                    vtmp[i].z = vp3[i].z + dt / Tp * (23.0f / 12 * (vf3[i].z - vp3[i].z) - 4.0f / 3 * (vf2[i].z - vp2[i].z)
                        + 5.0f / 12 * (vf1[i].z - vp1[i].z));
                    vp1[i].x = vp2[i].x;
                    vp1[i].y = vp2[i].y;
                    vp1[i].z = vp2[i].z;
                    vp2[i].x = vp3[i].x;
                    vp2[i].y = vp3[i].y;
                    vp2[i].z = vp3[i].z;
                    vp3[i].x = vtmp[i].x;
                    vp3[i].y = vtmp[i].y;
                    vp3[i].z = vtmp[i].z;
                    vf1[i].x = vf2[i].x;
                    vf1[i].y = vf2[i].y;
                    vf1[i].z = vf2[i].z;
                    vf2[i].x = vf3[i].x;
                    vf2[i].y = vf3[i].y;
                    vf2[i].z = vf3[i].z;
                }
            }
            return points; */
        }

        /*
        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetPosition_new",
        Description = @"FluidParticleTracking")]
        public Point3[] GetPosition_new(string authToken, string dataset, float StartTime, float EndTime,
            int nt, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            float dt,dt1,time;
            int integralSteps,integralNumber;

            if (nt <= 0 || temporalInterpolation == TurbulenceOptions.TemporalInterpolation.None)
                integralNumber = 1;
            else
                integralNumber = nt;
            
            if (StartTime < EndTime)
            {
                dt = (float)0.002 / integralNumber;
            }
            else
            {
                dt = (float)-0.002 / integralNumber;
            }

          //  System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\Users\\h0y5840\\JHU_turbulencedatabase\\output\\GetPosition_new_output.txt");
          //  file.WriteLine("dt = {0}",dt);
          //  file.WriteLine("StartTime={0}, EndTime={1}", StartTime, EndTime);
          
            integralSteps = (int) Math.Abs((EndTime - StartTime)/dt);
         //   file.WriteLine("integralSteps = {0}", integralSteps);

            int ParticleNumber = points.Length;
            Point3[] result = new Point3[ParticleNumber];
            Point3[]  predictor = new Point3[ParticleNumber];
            Vector3[] velocities = new Vector3[ParticleNumber];
            Vector3[] predicting_velocities = new Vector3[ParticleNumber];

            for (int i = 0; i < ParticleNumber; i++)
            {
                result[i] = new Point3(points[i].x, points[i].y, points[i].z);
                predictor[i] = new Point3();
                velocities[i] = new Vector3();
                predicting_velocities[i] = new Vector3();
            }
            
            time = StartTime;

            for (int i = 0; i < integralSteps; i++)
            {
                //file.WriteLine("step={0}", i);
               // file.WriteLine("time={0}, px={1},py={2},pz={3}", time, points[0].x, points[0].y, points[0].z);
                velocities = GetVelocity(authToken, dataset, time, 
                    spatialInterpolation, temporalInterpolation, result);

               // file.WriteLine("vx={0},vy={1},vz={2}", velocities[0].x, velocities[0].y, velocities[0].z);

                for (int j = 0; j < predictor.Length; j++)
                {
                    predictor[j].x = result[j].x + velocities[j].x * dt;
                    predictor[j].y = result[j].y + velocities[j].y * dt;
                    predictor[j].z = result[j].z + velocities[j].z * dt;
                }
              // file.WriteLine("pre px={0},py={1},pz={2}", predictor[0].x, predictor[0].y, predictor[0].z);

                time = time + dt;
                predicting_velocities = GetVelocity(authToken, dataset, time,
                    spatialInterpolation, temporalInterpolation, predictor);

               // file.WriteLine("pre vx={0},vy={1},vz={2}", predicting_velocities[0].x, predicting_velocities[0].y, predicting_velocities[0].z);
                for (int j = 0; j < result.Length; j++)
                {
                    result[j].x = result[j].x + (float)0.5 * (predicting_velocities[j].x + velocities[j].x) * dt;
                    result[j].y = result[j].y + (float)0.5 * (predicting_velocities[j].y + velocities[j].y) * dt;
                    result[j].z = result[j].z + (float)0.5 * (predicting_velocities[j].z + velocities[j].z) * dt;
                }
               // file.WriteLine("px={0},py={1},pz={2}", result[0].x, result[0].y, result[0].z);

            }

            dt1 = (float)(EndTime - (StartTime + integralSteps*dt));

            if (dt1 >= 0.00001 )
            {
                //file.WriteLine("last step dt={0}, time={1}", dt1,time);
                velocities = GetVelocity(authToken, dataset, time,
                     spatialInterpolation, temporalInterpolation, result);
               // file.WriteLine("vx={0},vy={1},vz={2}", velocities[0].x, velocities[0].y, velocities[0].z);

                for (int j = 0; j < predictor.Length; j++)
                {
                    predictor[j].x = result[j].x + velocities[j].x * dt1;
                    predictor[j].y = result[j].y + velocities[j].y * dt1;
                    predictor[j].z = result[j].z + velocities[j].z * dt1;
                }
                //file.WriteLine("pre px={0},py={1},pz={2}", predictor[0].x, predictor[0].y, predictor[0].z);
                predicting_velocities = GetVelocity(authToken, dataset, EndTime,
               spatialInterpolation, temporalInterpolation, predictor);
               // file.WriteLine("pre vx={0},vy={1},vz={2}", predicting_velocities[0].x, predicting_velocities[0].y, predicting_velocities[0].z);
                for (int j = 0; j < result.Length; j++)
                {
                    result[j].x = result[j].x + (float)0.5 * (predicting_velocities[j].x + velocities[j].x) * dt1;
                    result[j].y = result[j].y + (float)0.5 * (predicting_velocities[j].y + velocities[j].y) * dt1;
                    result[j].z = result[j].z + (float)0.5 * (predicting_velocities[j].z + velocities[j].z) * dt1;
                }
               // file.WriteLine("result px={0},py={1},pz={2}", result[0].x, result[0].y, result[0].z);
            }

            //file.Close();

            return result;
        }
        */

        //[WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetPosition_new2",
        //Description = @"FluidParticleTracking")]
        //public Point3[] GetPosition_new2(string authToken, string dataset, float StartTime, float EndTime,
        //    int nt, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
        //    Point3[] points)
        //{
        //    float dt;
        //    int integralNumber;

        //    if (nt <= 0 || temporalInterpolation == TurbulenceOptions.TemporalInterpolation.None) 
        //        integralNumber = 1;
        //    else 
        //        integralNumber = nt;

        //    if (StartTime < EndTime)
        //    {
        //        dt = (float)0.002 / integralNumber;
        //    }
        //    else
        //    {
        //        dt = (float)-0.002 / integralNumber;
        //    }

        //    //  System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\Users\\h0y5840\\JHU_turbulencedatabase\\output\\GetPosition_new_output.txt");
        //    //  file.WriteLine("dt = {0}",dt);
        //    //  file.WriteLine("StartTime={0}, EndTime={1}", StartTime, EndTime);

        //    //   file.WriteLine("integralSteps = {0}", integralSteps);

        //    AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            
        //    //This does not make sense to me... -- Eric
        //    database.selectServers();
        //    //database.selectServers(0);
            
        //    dataset = DataInfo.findDataSet(dataset);
        //    DataInfo.verifyTimeInRange(dataset, StartTime);
        //    DataInfo.verifyTimeInRange(dataset, EndTime);

        //    bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
        //    bool time_round = temporalInterpolation == TurbulenceOptions.TemporalInterpolation.None ? true : false;
            
        //    int ParticleNumber = points.Length;
        //    TrackingInfo[] tInfo = new TrackingInfo[ParticleNumber];
        //    int TimeInc = 10;
        //    if (dataset.Equals("isotropic1024fine"))
        //    {
        //        TimeInc = 1;
        //    }
        //    for (int i = 0; i < ParticleNumber; i++)
        //    {
        //        tInfo[i].pos = points[i];
        //        tInfo[i].pre_pos = new Point3(0.0f,0.0f,0.0f);
        //        tInfo[i].vel = new Vector3(0.0f, 0.0f, 0.0f);
        //        if (time_round)
        //        {
        //            tInfo[i].timeStep = (int)Math.Round((StartTime / 0.0002f) / TimeInc) * TimeInc;
        //        }
        //        else
        //        {
        //            tInfo[i].timeStep = (int)Math.Floor((StartTime / 0.0002f) / TimeInc) * TimeInc;
        //        }
        //        tInfo[i].time = StartTime;
        //        tInfo[i].endTime = EndTime;
        //        tInfo[i].flag = true;
        //        tInfo[i].done = false;
        //    }

        //    bool cumulative_done = false;
        //    while (!cumulative_done)
        //    {
        //        database.AddBulkTrackingParticles(tInfo, round, time_round, dataset);

        //        database.ExecuteGetPosition(dataset, spatialInterpolation, temporalInterpolation, dt, tInfo);

        //        cumulative_done = true;
        //        for (int i = 0; i < ParticleNumber; i++)
        //            if (!tInfo[i].done)
        //            {
        //                cumulative_done = false;
        //                //throw new Exception(String.Format("There was a point that crossed a server boundary and needs to be reassigned!"));
        //                break;
        //            }
        //    }

        //    database.Close();

        //    Point3[] result = new Point3[ParticleNumber];

        //    for (int i = 0; i < ParticleNumber; i++)
        //    {
        //        result[i].x = tInfo[i].pos.x;
        //        result[i].y = tInfo[i].pos.y;
        //        result[i].z = tInfo[i].pos.z;
        //    }

        //    log.WriteLog(auth.Id, dataset, (int)Worker.Workers.GetPosition_new,
        //        (int)spatialInterpolation,
        //        (int)temporalInterpolation,
        //       points.Length, StartTime, database.Bitfield);

        //    return result;
        //}
        
        /*
        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"[UNDER DEVELOPMENT] FluidParticleTracking")]
        public Point3[] GetPosition(string authToken, string dataset, float StartTime,
            float dt, int nt, TurbulenceOptions.SpatialInterpolation spatialInterpolation, Point3[] points)
        {
            throw new Exception("FluidParticleTacking is an experimental function and currently disabled.  Please e-mail turbulence@jhu.edu if you need to use it.");
            //
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3[] vel1, vel2, vel3;
            int npnt = points.Length;
            Point3[] xs = new Point3[npnt];
            for (int i = 0; i < npnt; i++)
            {
                xs[i] = new Point3();
            }

            vel1 = GetVelocity(authToken, dataset, StartTime, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, points);
            for (int i = 0; i < npnt; i++)
            {
                xs[i].x = points[i].x + vel1[i].x * dt;
                xs[i].y = points[i].y + vel1[i].y * dt;
                xs[i].z = points[i].z + vel1[i].z * dt;
            }
            vel2 = GetVelocity(authToken, dataset, StartTime + dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, xs);
            for (int i = 0; i < npnt; i++)
            {
                points[i].x = (points[i].x + xs[i].x) / 2 + vel2[i].x * dt / 2;
                points[i].y = (points[i].y + xs[i].y) / 2 + vel2[i].y * dt / 2;
                points[i].z = (points[i].z + xs[i].z) / 2 + vel2[i].z * dt / 2;
            }
            vel2 = GetVelocity(authToken, dataset, StartTime + dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, points);
            for (int i = 0; i < npnt; i++)
            {
                xs[i].x = points[i].x + vel2[i].x * dt;
                xs[i].y = points[i].y + vel2[i].y * dt;
                xs[i].z = points[i].z + vel2[i].z * dt;
            }
            vel3 = GetVelocity(authToken, dataset, StartTime + 2 * dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, xs);
            for (int i = 0; i < npnt; i++)
            {
                points[i].x = (points[i].x + xs[i].x) / 2 + vel3[i].x * dt / 2;
                points[i].y = (points[i].y + xs[i].y) / 2 + vel3[i].y * dt / 2;
                points[i].z = (points[i].z + xs[i].z) / 2 + vel3[i].z * dt / 2;
            }


            float time = StartTime;
            Point3[] result = new Point3[points.Length];
            ParticleTracking[] Output = new ParticleTracking[points.Length];
            database.selectServers(time);

            dataset = DataInfo.findDataSet(dataset);
            DataInfo.verifyTimeInRange(dataset, time);

            //database.AddBulkParticles(points);

            nt = nt - 2;
            database.ExecutePosition(dataset, points, time,
                spatialInterpolation, vel1, vel2, dt, nt, Output);

            log.WriteLog(auth.Id, dataset, (int)Worker.Workers.GetPositionParticle,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
                points.Length, time, database.Bitfield);

            for (int i = 0; i < npnt; i++)
            {
                result[i].x = Output[i].x;
                result[i].y = Output[i].y;
                result[i].z = Output[i].z;
            }
            return result; */

            /*for (int j = 2; j < nt; j++)
            {
                vel3 = GetVelocity(authToken, dataset, StartTime + j * dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, points);
                for (int i = 0; i < npnt; i++)
                {
                    points[i].x = points[i].x + dt * (23.0f / 12 * vel3[i].x - 4.0f / 3 * vel2[i].x + 5.0f / 12 * vel1[i].x);
                    points[i].y = points[i].y + dt * (23.0f / 12 * vel3[i].y - 4.0f / 3 * vel2[i].y + 5.0f / 12 * vel1[i].y);
                    points[i].z = points[i].z + dt * (23.0f / 12 * vel3[i].z - 4.0f / 3 * vel2[i].z + 5.0f / 12 * vel1[i].z);
                    vel1[i].x = vel2[i].x;
                    vel1[i].y = vel2[i].y;
                    vel1[i].z = vel2[i].z;
                    vel2[i].x = vel3[i].x;
                    vel2[i].y = vel3[i].y;
                    vel2[i].z = vel3[i].z;
                }
            }
            return points;*/

            /*case TurbulenceOptions.ParticleTrackingScheme.RK4:
                for (int j = 0; j < nt; j++)
                {
                    vel1 = GetVelocity(authToken, dataset, StartTime + j * dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, points);
                    for (int i = 0; i < npnt; i++)
                    {
                        xs[i].x = points[i].x + dt / 2 * vel1[i].x;
                        xs[i].y = points[i].y + dt / 2 * vel1[i].y;
                        xs[i].z = points[i].z + dt / 2 * vel1[i].z;
                    }
                    vel2 = GetVelocity(authToken, dataset, StartTime + j * dt + dt / 2, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, xs);
                    for (int i = 0; i < npnt; i++)
                    {
                        xs[i].x = points[i].x + dt / 2 * vel2[i].x;
                        xs[i].y = points[i].y + dt / 2 * vel2[i].y;
                        xs[i].z = points[i].z + dt / 2 * vel2[i].z;
                    }
                    vel3 = GetVelocity(authToken, dataset, StartTime + j * dt + dt / 2, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, xs);
                    for (int i = 0; i < npnt; i++)
                    {
                        xs[i].x = points[i].x + dt * vel3[i].x;
                        xs[i].y = points[i].y + dt * vel3[i].y;
                        xs[i].z = points[i].z + dt * vel3[i].z;
                    }
                    vel4 = GetVelocity(authToken, dataset, StartTime + j * dt + dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, xs);
                    for (int i = 0; i < npnt; i++)
                    {
                        points[i].x = points[i].x + dt / 6 * (vel1[i].x + 2 * vel2[i].x + 2 * vel3[i].x + vel4[i].x);
                        points[i].y = points[i].y + dt / 6 * (vel1[i].y + 2 * vel2[i].y + 2 * vel3[i].y + vel4[i].y);
                        points[i].z = points[i].z + dt / 6 * (vel1[i].z + 2 * vel2[i].z + 2 * vel3[i].z + vel4[i].z);
                    }
                }
                return points;
            default:
                throw new Exception(String.Format("Unknown scheme: {0}", schemeoption));

        }
        */


        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"[UNDER DEVELOPMENT] GetPositionParticleControl")]
        public Point3[] GetPositionParticleControl(string authToken, string dataset, float StartTime,
            float EndTime, TurbulenceOptions.SpatialInterpolation spatialInterpolation, Point3[] points)
        {
            throw new Exception("GetPositionParticleControl is an experimental function and currently disabled.  Please e-mail turbulence@jhu.edu if you need to use it.");
            /*
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3[] vel1, vel2, vel3;
            int npnt = points.Length;
            Point3[] xs = new Point3[npnt];
            float DNSdt = 0.0002f;
            int nt = (int)(Math.Ceiling((EndTime - StartTime) / DNSdt));
            float dt = (EndTime - StartTime) / nt;
            for (int i = 0; i < npnt; i++)
            {
                xs[i] = new Point3();
            }

            vel1 = GetVelocity(authToken, dataset, StartTime, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, points);
            for (int i = 0; i < npnt; i++)
            {
                xs[i].x = points[i].x + vel1[i].x * dt;
                xs[i].y = points[i].y + vel1[i].y * dt;
                xs[i].z = points[i].z + vel1[i].z * dt;
            }
            vel2 = GetVelocity(authToken, dataset, StartTime + dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, xs);
            for (int i = 0; i < npnt; i++)
            {
                points[i].x = (points[i].x + xs[i].x) / 2 + vel2[i].x * dt / 2;
                points[i].y = (points[i].y + xs[i].y) / 2 + vel2[i].y * dt / 2;
                points[i].z = (points[i].z + xs[i].z) / 2 + vel2[i].z * dt / 2;
            }
            vel2 = GetVelocity(authToken, dataset, StartTime + dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, points);
            for (int i = 0; i < npnt; i++)
            {
                xs[i].x = points[i].x + vel2[i].x * dt;
                xs[i].y = points[i].y + vel2[i].y * dt;
                xs[i].z = points[i].z + vel2[i].z * dt;
            }
            vel3 = GetVelocity(authToken, dataset, StartTime + 2 * dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, xs);
            for (int i = 0; i < npnt; i++)
            {
                points[i].x = (points[i].x + xs[i].x) / 2 + vel3[i].x * dt / 2;
                points[i].y = (points[i].y + xs[i].y) / 2 + vel3[i].y * dt / 2;
                points[i].z = (points[i].z + xs[i].z) / 2 + vel3[i].z * dt / 2;
            }


            
            for (int j = 2; j < nt; j++)
            {
                vel3 = GetVelocity(authToken, dataset, StartTime + j * dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.None, points);
                
                for (int i = 0; i < npnt; i++)
                {
                    points[i].x = points[i].x + dt * (23.0f / 12 * vel3[i].x - 4.0f / 3 * vel2[i].x + 5.0f / 12 * vel1[i].x);
                    points[i].y = points[i].y + dt * (23.0f / 12 * vel3[i].y - 4.0f / 3 * vel2[i].y + 5.0f / 12 * vel1[i].y);
                    points[i].z = points[i].z + dt * (23.0f / 12 * vel3[i].z - 4.0f / 3 * vel2[i].z + 5.0f / 12 * vel1[i].z);
                    
                    vel1[i].x = vel2[i].x;
                    vel1[i].y = vel2[i].y;
                    vel1[i].z = vel2[i].z;
                    vel2[i].x = vel3[i].x;
                    vel2[i].y = vel3[i].y;
                    vel2[i].z = vel3[i].z;
                }

            }
            //throw new Exception(String.Format("{0}, {1}, {2}, {3}", points[0].x, points[0].y, points[0].z, nt));
            return points; */
        }
#endif

        #region RawandOthers
        // get velocity for batching
        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetVelocityBatch",
             Description = @"Spatially interpolate the velocity at a number of points for a given time (allow batching).")]
        public Vector3[] GetVelocityBatch(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, string addr = null)
        {
            if (batchQueue == null)
            {
                batchQueue = new BatchWorkerQueue(2);
                batchQueue.start();
            }

            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            object rowid = null;
            DataInfo.verifyTimeInRange(dataset_enum, time);

            bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    throw new Exception("Batching only supported for MHD");

                case DataInfo.DataSets.mhd1024:
                    database.selectServers(dataset_enum, 1);

                    rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetMHDVelocity,
                        (int)spatialInterpolation,
                        (int)temporalInterpolation,
                        points.Length, time, null, null, addr);
                    log.UpdateRecordCount(auth.Id, points.Length);

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
                    {
                        throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterpolation));
                    }

                    // invoke batch processing for select dataset and no temporal interpolation
                    if (dataset.Equals("mhd1024coarse") &&
                        (temporalInterpolation == TurbulenceOptions.TemporalInterpolation.None)
                        )
                    {
                        // batch worker thread
                        GetVelocityBatchWorker barchWorker = new GetVelocityBatchWorker(batchQueue,
                            dataset, time, spatialInterpolation, temporalInterpolation, (int)Worker.Workers.GetMHDVelocity,
                            points, round, kernelSize, result);
                        barchWorker.work();
                    }

                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetRawVelocity",
        Description = @"Get a cube of the raw velocity data with the given width cornered at the specified coordinates for the given time.")]
        public byte[] GetRawVelocity(string authToken, string dataset, float time,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the 3 components of the velocity field
            int components = 3;
            byte[] result = null;
            DataInfo.TableNames tableName;

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

            result = database.GetRawData(dataset_enum, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetRawMagneticField",
        Description = @"Get a cube of the raw magnetic field data with the given width cornered at the specified coordinates for the given time.")]
        public byte[] GetRawMagneticField(string authToken, string dataset, float time,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the 3 components of the magnetic field
            int components = 3;
            byte[] result = null;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.strat4096:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    throw new Exception(String.Format("GetRawMagneticField is available only for MHD datasets!"));

                case DataInfo.DataSets.mhd1024:
                    DataInfo.TableNames tableName = DataInfo.TableNames.magnetic08;
                    rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawMagnetic,
                        (int)TurbulenceOptions.SpatialInterpolation.None,
                        (int)TurbulenceOptions.TemporalInterpolation.None,
                       Xwidth * Ywidth * Zwidth, time, null, null, addr);
                    log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

                    result = database.GetRawData(dataset_enum, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetRawVectorPotential",
        Description = @"Get a cube of the raw vector potential data with the given width cornered at the specified coordinates for the given time.")]
        public byte[] GetRawVectorPotential(string authToken, string dataset, float time,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the 3 components of the vector potential field
            int components = 3;
            byte[] result = null;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.strat4096:
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    throw new Exception(String.Format("GetRawVectorPotential is available only for MHD datasets!"));

                case DataInfo.DataSets.mhd1024:
                    DataInfo.TableNames tableName = DataInfo.TableNames.potential08;
                    rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawPotential,
                        (int)TurbulenceOptions.SpatialInterpolation.None,
                        (int)TurbulenceOptions.TemporalInterpolation.None,
                       Xwidth * Ywidth * Zwidth, time, null, null, addr);
                    log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

                    result = database.GetRawData(dataset_enum, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetRawPressure",
        Description = @"Get a cube of the raw pressure data with the given width cornered at the specified coordinates for the given time.")]
        public byte[] GetRawPressure(string authToken, string dataset, float time,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the scalar pressure field
            int components = 1;
            byte[] result = null;
            DataInfo.TableNames tableName;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    tableName = DataInfo.TableNames.isotropic1024fine_pr;
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic4096:
                case DataInfo.DataSets.mixing:
                    tableName = DataInfo.TableNames.pr;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.pressure08;
                    break;
                case DataInfo.DataSets.channel:
                case DataInfo.DataSets.channel5200:
                case DataInfo.DataSets.bl_zaki:
                    tableName = DataInfo.TableNames.pr;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawPressure,
                (int)TurbulenceOptions.SpatialInterpolation.None,
                (int)TurbulenceOptions.TemporalInterpolation.None,
               Xwidth * Ywidth * Zwidth, time, null, null, addr);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

            result = database.GetRawData(dataset_enum, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetRawDensity",
        Description = @"Get a cube of the raw density data with the given width cornered at the specified coordinates for the given time.")]
        public byte[] GetRawDensity(string authToken, string dataset, float time,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the scalar pressure field
            int components = 1;
            byte[] result = null;
            DataInfo.TableNames tableName;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.mixing:
                    tableName = DataInfo.TableNames.density;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawDensity,
                (int)TurbulenceOptions.SpatialInterpolation.None,
                (int)TurbulenceOptions.TemporalInterpolation.None,
               Xwidth * Ywidth * Zwidth, time, null, null, addr);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

            result = database.GetRawData(dataset_enum, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetRawTemperature",
        Description = @"Get a cube of the raw density data with the given width cornered at the specified coordinates for the given time.")]
        public byte[] GetRawTemperature(string authToken, string dataset, float time,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth, string addr = null)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            if (authToken == "edu.jhu.pha.turbulence-monitor" || authToken == "edu.jhu.pha.turbulence-dev")
            {
                log.devmode = true;//This makes sure we don't log the monitoring service.
            }
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_enum, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_enum, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the scalar pressure field
            int components = 1;
            byte[] result = null;
            DataInfo.TableNames tableName;

            switch (dataset_enum)
            {
                case DataInfo.DataSets.strat4096:
                    tableName = DataInfo.TableNames.th;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawDensity,
                (int)TurbulenceOptions.SpatialInterpolation.None,
                (int)TurbulenceOptions.TemporalInterpolation.None,
               Xwidth * Ywidth * Zwidth, time, null, null, addr);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

            result = database.GetRawData(dataset_enum, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }
        #endregion
    }
}

