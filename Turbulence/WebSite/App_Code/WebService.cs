using System;
using System.Web;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Configuration;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using Turbulence.SQLInterface;
using Turbulence.TurbBatch;

namespace TurbulenceService {
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
        public const string infodb = "turbinfo";

        // batch scheduler queue
        public static BatchWorkerQueue batchQueue = null;

        Database database;
        AuthInfo authInfo;
        Log log;

        public TurbulenceService()
        {
            database = new Database(infodb, DEVEL_MODE);
            authInfo = new AuthInfo(infodb, DEVEL_MODE);
            log = new Log(infodb, DEVEL_MODE);
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

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetVelocity",
        Description = @"Spatially interpolate the velocity at a number of points for a given time.")]
        public Vector3[] GetVelocity(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3[] result = new Vector3[points.Length];
            //dataset = DataInfo.findDataSet(dataset);
            /* Old hardcoded way */
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            string tablename = DataInfo.getTableName(dataset, "vel");
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            object rowid = null;
            /*Not sure why channel is special at this point...*/
            if (dataset == "channel")
            {
                int worker = (int)Worker.Workers.GetChannelVelocity;
                GetMHDData(auth, dataset, dataset_id, tablename, worker,
                            time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
            }
            else
            {
                int worker = (int)Worker.Workers.GetMHDVelocity;
                GetMHDData(auth, dataset, dataset_id, tablename, worker,
                            time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                
            }
              
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        private void GetMHDData(AuthInfo.AuthToken auth, string dataset, int dataset_id, string tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, Vector3[] result, ref object rowid)
        {
            bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
            int kernelSize = -1;
            int kernelSizeY = -1;

            //bool IsChannelGrid = dataset_enum == DataInfo.DataSets.channel ? true : false;
            bool IsChannelGrid = false;
            if (dataset == "channel")
            {
                IsChannelGrid = true;
            }

            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            //database.AddBulkParticles(points, round, spatialInterpolation, worker);
            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);
            /* Was tableName, changed to dataset */
            database.ExecuteGetMHDData(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
        }


        // get velocity for batching
        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetVelocityBatch",
             Description = @"Spatially interpolate the velocity at a number of points for a given time (allow batching).")]
        public Vector3[] GetVelocityBatch(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            if (batchQueue == null)
            {
                batchQueue = new BatchWorkerQueue(2);
                batchQueue.start();
            }

            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            object rowid = null;
            DataInfo.verifyTimeInRange(dataset_id, time);

            bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
            switch (dataset_id)
            {
                case 5:
                case 4:
                case 6:
                    throw new Exception("Batching only supported for MHD");

                case 3:
                    database.selectServers(dataset_id);

                    rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetMHDVelocity,
                        (int)spatialInterpolation,
                        (int)temporalInterpolation,
                        points.Length, time, null, null);
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
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the 3 components of the velocity field
            int components = 3;
            byte[] result = null;
            string tableName;

            /*switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    tableName = DataInfo.TableNames.isotropic1024fine_vel;
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                    tableName = DataInfo.TableNames.vel;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.velocity08;
                    break;
                case DataInfo.DataSets.channel:
                    tableName = DataInfo.TableNames.vel;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
             * */
            tableName = DataInfo.getTableName(dataset, "vel");
            
            rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawVelocity,
                (int)TurbulenceOptions.SpatialInterpolation.None,
                (int)TurbulenceOptions.TemporalInterpolation.None,
                Xwidth * Ywidth * Zwidth, time, null, null);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);
            
            result = database.GetRawData(dataset_id, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);
            
            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetPressure",
        Description = @"Spatially interpolate the pressure field at a number of points for a given time.")]
        public Pressure[] GetPressure(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Pressure[] result = new Pressure[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            object rowid = null;
            DataInfo.verifyTimeInRange(dataset_id, time);

            bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
            int kernelSize = -1;
            int kernelSizeY = -1;

            //DataInfo.TableNames tableName;
            int worker;
            /*
            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    tableName = DataInfo.TableNames.isotropic1024fine_pr;
                    worker = (int)Worker.Workers.GetMHDPressure;
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                    tableName = DataInfo.TableNames.pr;
                    worker = (int)Worker.Workers.GetMHDPressure;
                    break;
                case DataInfo.DataSets.channel:
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
            */
            string tableName = DataInfo.getTableName(dataset, "pr");
            
            bool IsChannelGrid = dataset_id ==6 ? true : false;
            if (IsChannelGrid)
            {
                worker = (int)Worker.Workers.GetChannelPressure;
            }
            else
            {
                worker = (int)Worker.Workers.GetMHDPressure;
            }


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

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetRawPressure",
        Description = @"Get a cube of the raw pressure data with the given width cornered at the specified coordinates for the given time.")]
        public byte[] GetRawPressure(string authToken, string dataset, float time,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the scalar pressure field
            int components = 1;
            byte[] result = null;
            //DataInfo.TableNames tableName;

            string tableName = DataInfo.getTableName(dataset, "pr");
            /*
            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    tableName = DataInfo.TableNames.isotropic1024fine_pr;
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                    tableName = DataInfo.TableNames.pr;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.pressure08;
                    break;
                case DataInfo.DataSets.channel:
                    tableName = DataInfo.TableNames.pr;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
             */
            rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawPressure,
                (int)TurbulenceOptions.SpatialInterpolation.None,
                (int)TurbulenceOptions.TemporalInterpolation.None,
               Xwidth * Ywidth * Zwidth, time, null, null);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

            result = database.GetRawData(dataset_id, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetMagneticField",
        Description = @"Spatially interpolate the magnetic field at a number of points for a given time.")]
        public Vector3[] GetMagneticField(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            object rowid = null;
            /*
            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.channel:
                    throw new Exception(String.Format("GetMagneticField is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    DataInfo.TableNames tableName = DataInfo.TableNames.magnetic08;
                    GetMHDData(auth, dataset, dataset_enum, tableName, (int)Worker.Workers.GetMHDMagnetic, 
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */

            string tableName = DataInfo.getTableName(dataset, "mag");
            /*This is a hack.  Fix in db...check the fields or something */
            if (dataset_id == 6) throw new Exception(String.Format("GetMagneticField is not available on channel datasets!"));
            GetMHDData(auth, dataset, dataset_id, tableName, (int)Worker.Workers.GetMHDMagnetic,
                  time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
              
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetRawMagneticField",
        Description = @"Get a cube of the raw magnetic field data with the given width cornered at the specified coordinates for the given time.")]
        public byte[] GetRawMagneticField(string authToken, string dataset, float time,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the 3 components of the magnetic field
            int components = 3;
            byte[] result = null;
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.channel:
                    throw new Exception(String.Format("GetRawMagneticField is available only for MHD datasets!"));

                case DataInfo.DataSets.mhd1024:
                    DataInfo.TableNames tableName = DataInfo.TableNames.magnetic08;
                    rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawMagnetic,
                        (int)TurbulenceOptions.SpatialInterpolation.None,
                        (int)TurbulenceOptions.TemporalInterpolation.None,
                       Xwidth * Ywidth * Zwidth, time, null, null);
                    log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

                    result = database.GetRawData(dataset_enum, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
             */
            
            string tableName = DataInfo.getTableName(dataset, "mag");
            rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawMagnetic,
                        (int)TurbulenceOptions.SpatialInterpolation.None,
                        (int)TurbulenceOptions.TemporalInterpolation.None,
                       Xwidth * Ywidth * Zwidth, time, null, null);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

            result = database.GetRawData(dataset_id, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

            if (dataset_id == 6) throw new Exception(String.Format("GetMagneticField is not available on channel datasets!"));

            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetVectorPotential",
        Description = @"Spatially interpolate the magnetic field at a number of points for a given time.")]
        public Vector3[] GetVectorPotential(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            object rowid = null;
            /*
            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.channel:
                    throw new Exception(String.Format("GetVectorPotential is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    DataInfo.TableNames tableName = DataInfo.TableNames.potential08;
                    GetMHDData(auth, dataset, dataset_enum, tableName, (int)Worker.Workers.GetMHDPotential,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
             */
            /*Do some exception handling here in the future--this is only for MHD data*/

            string tableName = DataInfo.getTableName(dataset, "vec");
            GetMHDData(auth, dataset, dataset_id, tableName, (int)Worker.Workers.GetMHDPotential,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetRawVectorPotential",
        Description = @"Get a cube of the raw vector potential data with the given width cornered at the specified coordinates for the given time.")]
        public byte[] GetRawVectorPotential(string authToken, string dataset, float time,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the 3 components of the vector potential field
            int components = 3;
            byte[] result = null;
            /*
            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.channel:
                    throw new Exception(String.Format("GetRawVectorPotential is available only for MHD datasets!"));

                case DataInfo.DataSets.mhd1024:
                    DataInfo.TableNames tableName = DataInfo.TableNames.potential08;
                    rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawPotential,
                        (int)TurbulenceOptions.SpatialInterpolation.None,
                        (int)TurbulenceOptions.TemporalInterpolation.None,
                       Xwidth * Ywidth * Zwidth, time, null, null);
                    log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

                    result = database.GetRawData(dataset_enum, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
             * */
            String connectionString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
            using (SqlConnection sqlcon = new SqlConnection(connectionString))
            {
                sqlcon.Open();
                using (SqlCommand cmd = new SqlCommand(
                    String.Format("select tablename from turbinfo.dbo.datafields where dataset_id= '{0}' and name='vec'", dataset_id), sqlcon))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        if (reader.HasRows)
                {
                    string tableName = reader.GetString(0);
                    rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawPotential,
                        (int)TurbulenceOptions.SpatialInterpolation.None,
                        (int)TurbulenceOptions.TemporalInterpolation.None,
                       Xwidth * Ywidth * Zwidth, time, null, null);
                    log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);
                    result = database.GetRawData(dataset_id, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

                }
                else throw new Exception("GetRawVectorPotential is not available for this dataset!");
                    }
                }
            }

            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetDensity",
        Description = @"Spatially interpolate the density field at a number of points for a given time.")]
        public Pressure[] GetDensity(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Pressure[] result = new Pressure[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            object rowid = null;
            DataInfo.verifyTimeInRange(dataset_id, time);

            bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
            int kernelSize = -1;
            int kernelSizeY = -1;

            string tableName = "density";
            int worker = (int)Worker.Workers.GetDensity;
            /*We might need to add a check to make sure the dataset has density*/
             
            /*
            switch (dataset_enum)
            {
                case DataInfo.DataSets.mixing:
                    tableName = DataInfo.TableNames.density;
                    worker = (int)Worker.Workers.GetDensity;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */
            
            bool IsChannelGrid = dataset_id == 6 ? true : false;
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

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"Retrieve the density gradient at a fixed location")]
        public Vector3[] GetDensityGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            object rowid = null;

            bool round = true;
            int kernelSize = -1;
            int kernelSizeY = -1;

            string tableName = "density";
            int worker;
            /*
            switch (dataset_enum)
            {
                case DataInfo.DataSets.mixing:
                    tableName = DataInfo.TableNames.density;
                    worker = (int)Worker.Workers.GetDensityGradient;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
             * */
            worker = (int)Worker.Workers.GetDensityGradient;

            bool IsChannelGrid = dataset_id == 6 ? true : false;
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

        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Retrieve the density hessian at a fixed location")]
        public PressureHessian[] GetDensityHessian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            PressureHessian[] result = new PressureHessian[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);

            object rowid = null;

            bool round = true;
            int kernelSize = -1;
            int kernelSizeY = -1;

            string tableName = "density";
            int worker;
            worker = (int)Worker.Workers.GetDensityHessian;
            /*
            switch (dataset_enum)
            {
                case DataInfo.DataSets.mixing:
                    tableName = DataInfo.TableNames.density;
                    worker = (int)Worker.Workers.GetDensityHessian;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
             * */

            if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q4 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q6 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q8 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q10 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q12 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q14)
            {
                throw new Exception("This interpolation option does not support second order derivatives!");
            }

            bool IsChannelGrid = dataset_id == 6 ? true : false;
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

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetRawDensity",
        Description = @"Get a cube of the raw density data with the given width cornered at the specified coordinates for the given time.")]
        public byte[] GetRawDensity(string authToken, string dataset, float time,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int worker = (int)Worker.Workers.GetDensityHessian;
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            //DataInfo.verifyRawDataParameters(X, Y, Z, Xwidth, Ywidth, Zwidth);
            object rowid = null;
            // we return a cube of data with the specified width
            // for the scalar pressure field
            int components = 1;
            byte[] result = null;
            string tableName = "density";
            /*
            switch (dataset_enum)
            {
                case DataInfo.DataSets.mixing:
                    tableName = DataInfo.TableNames.density;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
             * */

            rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawDensity,
                (int)TurbulenceOptions.SpatialInterpolation.None,
                (int)TurbulenceOptions.TemporalInterpolation.None,
               Xwidth * Ywidth * Zwidth, time, null, null);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

            result = database.GetRawData(dataset_id, tableName, time, components, X, Y, Z, Xwidth, Ywidth, Zwidth);

            log.UpdateLogRecord(rowid, database.Bitfield);

            return result;
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
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);

            object rowid = null;
            rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetForce,
            (int)spatialInterpolation,
            (int)temporalInterpolation,
            points.Length, time, null, null);

            const int time_offset = 4800;  // integral time offset to match forcing files
            Vector3[] result = new Vector3[points.Length];
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);

            log.UpdateRecordCount(auth.Id, points.Length);
            
            switch (dataset_id)
            { /*Ugh--fix this later.  Not sure the data differences at this time...*/
                case 5: //DataInfo.DataSets.isotropic1024fine:
                case 4: //DataInfo.DataSets.isotropic1024coarse:
                    
            //sqlcon = new SqlConnection(connectionString);
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

                case 3: //DataInfo.DataSets.mhd1024:
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
        Description = @"Spatially interpolate the velocity for an array of points")]
        public Vector3P[] GetVelocityAndPressure(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3P[] result = new Vector3P[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            /* Not sure what the case was for other than for invalid sets...*/
            /*
            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mhd1024:
                case DataInfo.DataSets.mixing:
                case DataInfo.DataSets.channel:
                    Vector3[] velocities = GetVelocity(authToken, dataset, time, spatialInterpolation, temporalInterpolation, points);
                    Pressure[] pressures = GetPressure(authToken, dataset, time, spatialInterpolation, temporalInterpolation, points);
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
            */
            Vector3[] velocities = GetVelocity(authToken, dataset, time, spatialInterpolation, temporalInterpolation, points);
            Pressure[] pressures = GetPressure(authToken, dataset, time, spatialInterpolation, temporalInterpolation, points);
            for (int i = 0; i < points.Length; i++)
            {
                result[i].x = velocities[i].x;
                result[i].y = velocities[i].y;
                result[i].z = velocities[i].z;
                result[i].p = pressures[i].p;
            }
            return result;
        }


        
        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"Retrieve the velocity gradient at a fixed location")]
        public VelocityGradient [] GetVelocityGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            VelocityGradient[] result = new VelocityGradient[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            object rowid = null;

            int worker = (int)Worker.Workers.GetMHDVelocityGradient;
            /*
            switch (dataset_enum)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    GetMHDGradient(auth, dataset, dataset_enum, DataInfo.TableNames.isotropic1024fine_vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                    GetMHDGradient(auth, dataset, dataset_enum, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetMHDGradient(auth, dataset, dataset_enum, DataInfo.TableNames.velocity08, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.channel:
                    GetMHDGradient(auth, dataset, dataset_enum, DataInfo.TableNames.vel, (int)Worker.Workers.GetChannelVelocityGradient,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */
            string tablename = DataInfo.getTableName(dataset, "vel");
            if (dataset_id == 6)
            {
                GetMHDGradient(auth, dataset, dataset_id, tablename, (int)Worker.Workers.GetChannelVelocityGradient,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
            }
            else
            {
                GetMHDGradient(auth, dataset, dataset_id, tablename, worker,
                            time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
            }
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetMagneticFieldGradient",
        Description = @"Retrieve the magnetic field gradient at a number of points for a given time.")]
        public VelocityGradient[] GetMagneticFieldGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            VelocityGradient[] result = new VelocityGradient[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            object rowid = null;
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.channel:
                    throw new Exception(String.Format("GetMagneticField is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    GetMHDGradient(auth, dataset, dataset_id, DataInfo.TableNames.magnetic08, (int)Worker.Workers.GetMHDMagneticGradient,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */
            string tablename = DataInfo.getTableName(dataset, "mag");
            GetMHDGradient(auth, dataset, dataset_id, tablename, (int)Worker.Workers.GetMHDMagneticGradient,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetVectorPotentialGradient",
        Description = @"Retrieve the vector potential gradient at a number of points for a given time.")]
        public VelocityGradient[] GetVectorPotentialGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            VelocityGradient[] result = new VelocityGradient[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            object rowid = null;
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.channel:
                    throw new Exception(String.Format("GetVectorPotential is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    GetMHDGradient(auth, dataset, dataset_id, DataInfo.TableNames.potential08, (int)Worker.Workers.GetMHDPotentialGradient,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */
            string tableName = DataInfo.getTableName(dataset, "vec");
            GetMHDGradient(auth, dataset, dataset_id, tableName, (int)Worker.Workers.GetMHDPotentialGradient,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        private void GetMHDGradient(AuthInfo.AuthToken auth, string dataset, int dataset_id, string tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, VelocityGradient[] result, ref object rowid)
        {
            bool round = true;
            int kernelSize = -1;
            int kernelSizeY = -1;

            bool IsChannelGrid = dataset_id == 6 ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetMHDGradient(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"Retrieve the pressure gradient at a fixed location")]
        public Vector3[] GetPressureGradient(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {            
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            object rowid = null;

            bool round = true;
            int kernelSize = -1;
            int kernelSizeY = -1;

            //DataInfo.TableNames tableName;
            string tableName = DataInfo.getTableName(dataset, "pr");
            
            int worker;
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    tableName = DataInfo.TableNames.isotropic1024fine_pr;
                    worker = (int)Worker.Workers.GetMHDPressureGradient;
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                    tableName = DataInfo.TableNames.pr;
                    worker = (int)Worker.Workers.GetMHDPressureGradient;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.pressure08;
                    worker = (int)Worker.Workers.GetMHDPressureGradient;
                    break;
                case DataInfo.DataSets.channel:
                    tableName = DataInfo.TableNames.pr;
                    worker = (int)Worker.Workers.GetChannelPressureGradient;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */

            bool IsChannelGrid = dataset_id == 6 ? true : false;
            if (IsChannelGrid)
            {
                worker = (int)Worker.Workers.GetChannelPressureGradient;
            }
            else
            {
                worker = (int)Worker.Workers.GetMHDPressureGradient;
            }
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

        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Retrieve the pressure hessian at a fixed location")]
        public PressureHessian[] GetPressureHessian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            PressureHessian[] result = new PressureHessian[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);

            object rowid = null;

            bool round = true;
            int kernelSize = -1;
            int kernelSizeY = -1;

            //DataInfo.TableNames tableName;
            string tableName = DataInfo.getTableName(dataset, "pr");
            int worker;
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    tableName = DataInfo.TableNames.isotropic1024fine_pr;
                    worker = (int)Worker.Workers.GetMHDPressureHessian;
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                    tableName = DataInfo.TableNames.pr;
                    worker = (int)Worker.Workers.GetMHDPressureHessian;
                    break;
                case DataInfo.DataSets.mhd1024:
                    tableName = DataInfo.TableNames.pressure08;
                    worker = (int)Worker.Workers.GetMHDPressureHessian;
                    break;
                case DataInfo.DataSets.channel:
                    tableName = DataInfo.TableNames.pr;
                    worker = (int)Worker.Workers.GetChannelPressureHessian;
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */
            if (spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q4 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q6 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q8 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q10 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q12 ||
                spatialInterpolation == TurbulenceOptions.SpatialInterpolation.M1Q14)
            {
                throw new Exception("This interpolation option does not support second order derivatives!");
            }

            bool IsChannelGrid = dataset_id == 6 ? true : false;
            if (IsChannelGrid)
            {
                worker = (int)Worker.Workers.GetChannelPressureHessian;
            }
            else
            {
                worker = (int)Worker.Workers.GetMHDPressureHessian;
            }
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


        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Retrieve the velocity hessian at a fixed location")]
        public VelocityHessian[] GetVelocityHessian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            VelocityHessian[] result = new VelocityHessian[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            DataInfo.verifyTimeInRange(dataset_id, time);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            object rowid = null;

            int worker = (int)Worker.Workers.GetMHDVelocityHessian;
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    GetMHDHessian(auth, dataset, dataset_id, DataInfo.TableNames.isotropic1024fine_vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                    GetMHDHessian(auth, dataset, dataset_id, DataInfo.TableNames.vel, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetMHDHessian(auth, dataset, dataset_id, DataInfo.TableNames.velocity08, worker,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.channel:
                    GetMHDHessian(auth, dataset, dataset_id, DataInfo.TableNames.vel, (int)Worker.Workers.GetChannelVelocityHessian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
             * */
            string tableName = DataInfo.getTableName(dataset, "vel");
            if (dataset_id == 6)
            {
                GetMHDHessian(auth, dataset, dataset_id, tableName, (int)Worker.Workers.GetChannelVelocityHessian,
                       time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
            }
            else
            {
                GetMHDHessian(auth, dataset, dataset_id, tableName, worker,
                            time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetMagneticHessian",
        Description = @"Retrieve the magnetic field hessian at a number of points for a given time.")]
        public VelocityHessian[] GetMagneticHessian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            VelocityHessian[] result = new VelocityHessian[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            DataInfo.verifyTimeInRange(dataset_id, time);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            object rowid = null;
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.channel:
                    throw new Exception(String.Format("GetMagnetic is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    GetMHDHessian(auth, dataset, dataset_id, DataInfo.TableNames.magnetic08, (int)Worker.Workers.GetMHDMagneticHessian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */
            string tableName = DataInfo.getTableName(dataset, "mag");
            GetMHDHessian(auth, dataset, dataset_id,tableName, (int)Worker.Workers.GetMHDMagneticHessian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetVectorPotentialHessian",
        Description = @"Retrieve the vector potential hessian at a number of points for a given time.")]
        public VelocityHessian[] GetVectorPotentialHessian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            VelocityHessian[] result = new VelocityHessian[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            DataInfo.verifyTimeInRange(dataset_id, time);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            object rowid = null;
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.channel:
                    throw new Exception(String.Format("GetVectorPotential is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    GetMHDHessian(auth, dataset, dataset_id, DataInfo.TableNames.potential08, (int)Worker.Workers.GetMHDPotentialHessian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */
            string tableName = DataInfo.getTableName(dataset, "vec");
            GetMHDHessian(auth, dataset, dataset_id, tableName, (int)Worker.Workers.GetMHDPotentialHessian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        private void GetMHDHessian(AuthInfo.AuthToken auth, string dataset, int dataset_id, string tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, VelocityHessian[] result, ref object rowid)
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

            bool IsChannelGrid = dataset_id ==6 ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetMHDHessian(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
        }


        [WebMethod(CacheDuration = 0, BufferResponse = true,
        Description = @"Retrieve the velocity laplacian at a fixed location")]
        public Vector3[] GetVelocityLaplacian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            DataInfo.verifyTimeInRange(dataset_id, time);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            object rowid = null;
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                    GetMHDLaplacian(auth, dataset, dataset_id, DataInfo.TableNames.isotropic1024fine_vel, (int)Worker.Workers.GetMHDVelocityLaplacian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mixing:
                    GetMHDLaplacian(auth, dataset, dataset_id, DataInfo.TableNames.vel, (int)Worker.Workers.GetMHDVelocityLaplacian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.mhd1024:
                    GetMHDLaplacian(auth, dataset, dataset_id, DataInfo.TableNames.velocity08, (int)Worker.Workers.GetMHDVelocityLaplacian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                case DataInfo.DataSets.channel:
                    GetMHDLaplacian(auth, dataset, dataset_id, DataInfo.TableNames.vel, (int)Worker.Workers.GetChannelVelocityLaplacian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */
            string tableName = DataInfo.getTableName(dataset, "vel");
            if (dataset_id == 6)
            {
                GetMHDLaplacian(auth, dataset, dataset_id, tableName, (int)Worker.Workers.GetChannelVelocityLaplacian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
            }
            else
            {
                GetMHDLaplacian(auth, dataset, dataset_id, tableName, (int)Worker.Workers.GetMHDVelocityLaplacian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
            }

            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetMagneticFieldLaplacian",
        Description = @"Retrieve the magnetic field Laplacian at a number of points for a given time.")]
        public Vector3[] GetMagneticFieldLaplacian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_id = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            DataInfo.verifyTimeInRange(dataset_id, time);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            object rowid = null;
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.channel:
                    throw new Exception(String.Format("GetMagneticField is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    GetMHDLaplacian(auth, dataset, dataset_id, DataInfo.TableNames.magnetic08, (int)Worker.Workers.GetMHDMagneticLaplacian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */
            string tableName = DataInfo.getTableName(dataset, "mag");
            GetMHDLaplacian(auth, dataset, dataset_id, tableName, (int)Worker.Workers.GetMHDMagneticLaplacian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetVectorPotentialLaplacian",
        Description = @"Retrieve the vector potential Laplacian at a number of points for a given time.")]
        public Vector3[] GetVectorPotentialLaplacian(string authToken, string dataset, float time,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            Vector3[] result = new Vector3[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_id = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            DataInfo.verifyTimeInRange(dataset_id, time);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            object rowid = null;
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.channel:
                    throw new Exception(String.Format("GetVectorPotential is available only for MHD datasets!"));
                case DataInfo.DataSets.mhd1024:
                    GetMHDLaplacian(auth, dataset, dataset_id, DataInfo.TableNames.potential08, (int)Worker.Workers.GetMHDPotentialLaplacian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */
            string tableName = DataInfo.getTableName(dataset, "vec");
            GetMHDLaplacian(auth, dataset, dataset_id, tableName, (int)Worker.Workers.GetMHDPotentialLaplacian,
                        time, spatialInterpolation, temporalInterpolation, points, result, ref rowid);
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }

        private void GetMHDLaplacian(AuthInfo.AuthToken auth, string dataset, int dataset_id, string tableName, int worker,
            float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation, TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            Point3[] points, Vector3[] result, ref object rowid)
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

            bool IsChannelGrid = dataset_id== 6 ? true : false;
            TurbulenceOptions.GetKernelSize(spatialInterpolation, ref kernelSize, ref kernelSizeY, IsChannelGrid, worker);

            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)temporalInterpolation,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            database.AddBulkParticles(points, kernelSize, kernelSizeY, kernelSize, round, time);

            database.ExecuteGetMHDLaplacian(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetPosition",
        Description = @"FluidParticleTracking")]
        public Point3[] GetPosition(string authToken, string dataset, float StartTime, float EndTime,
            float dt, TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            DataInfo.verifyTimeInRange(dataset_id, StartTime);
            DataInfo.verifyTimeInRange(dataset_id, EndTime);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);

            if (Math.Abs(EndTime - StartTime) - Math.Abs(dt) < -0.000001F)
                throw new Exception(String.Format("The time step dt cannot be greater than the StartTime : EndTime range!"));

            object rowid = null;

            TurbulenceOptions.TemporalInterpolation temporalInterpolation = TurbulenceOptions.TemporalInterpolation.PCHIP;

            bool round;
            float dt1, time;
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

            Point3[] predictor = new Point3[numParticles]; // This is either the predictor or the "modified" corrector 
            //for (int i = 0; i < numParticles; i++)
            //{
            //    predictor[i] = new Point3(0, 0, 0);
            //}

            time = StartTime;
            string tableName = DataInfo.getTableName(dataset, "vel");
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.mhd1024:
                case DataInfo.DataSets.mixing:
                    //DataInfo.TableNames tableName = DataInfo.TableNames.velocity08;
                    //if (dataset_enum == DataInfo.DataSets.isotropic1024coarse || dataset_enum == DataInfo.DataSets.isotropic1024fine)
                    //    tableName = DataInfo.TableNames.vel;
                    //int worker = (int)Worker.Workers.GetMHDVelocity;

                    for (int i = 0; i < integralSteps; i++)
                    {
                        //database.AddBulkParticles(points, round, spatialInterpolation, worker);
                        database.AddBulkParticles(points, kernelSize, kernelSize, kernelSize, round, time);
                        database.ExecuteGetPosition(tableName, time,
                            spatialInterpolation, temporalInterpolation, points, predictor, true, dt);

                        time = time + dt;
                        //database.AddBulkParticles(predictor, round, spatialInterpolation, worker);
                        database.AddBulkParticles(predictor, kernelSize, kernelSize, kernelSize, round, time);
                        database.ExecuteGetPosition(tableName, time,
                            spatialInterpolation, temporalInterpolation, points, predictor, false, dt);
                    }

                    dt1 = (float)(EndTime - (StartTime + integralSteps * dt));

                    if ((StartTime > EndTime && dt1 <= -0.00001) || (StartTime < EndTime && dt1 >= 0.00001))
                    {
                        //database.AddBulkParticles(points, round, spatialInterpolation, worker);
                        database.AddBulkParticles(points, kernelSize, kernelSize, kernelSize, round, time);
                        database.ExecuteGetPosition(tableName, time,
                            spatialInterpolation, temporalInterpolation, points, predictor, true, dt1);

                        //database.AddBulkParticles(predictor, round, spatialInterpolation, worker);
                        database.AddBulkParticles(predictor, kernelSize, kernelSize, kernelSize, round, time);
                        database.ExecuteGetPosition(tableName, EndTime,
                            spatialInterpolation, temporalInterpolation, points, predictor, false, dt1);
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */
            for (int i = 0; i < integralSteps; i++)
            {
                //database.AddBulkParticles(points, round, spatialInterpolation, worker);
                database.AddBulkParticles(points, kernelSize, kernelSize, kernelSize, round, time);
                database.ExecuteGetPosition(tableName, time,
                    spatialInterpolation, temporalInterpolation, points, predictor, true, dt);

                time = time + dt;
                //database.AddBulkParticles(predictor, round, spatialInterpolation, worker);
                database.AddBulkParticles(predictor, kernelSize, kernelSize, kernelSize, round, time);
                database.ExecuteGetPosition(tableName, time,
                    spatialInterpolation, temporalInterpolation, points, predictor, false, dt);
            }

            dt1 = (float)(EndTime - (StartTime + integralSteps * dt));

            if ((StartTime > EndTime && dt1 <= -0.00001) || (StartTime < EndTime && dt1 >= 0.00001))
            {
                //database.AddBulkParticles(points, round, spatialInterpolation, worker);
                database.AddBulkParticles(points, kernelSize, kernelSize, kernelSize, round, time);
                database.ExecuteGetPosition(tableName, time,
                    spatialInterpolation, temporalInterpolation, points, predictor, true, dt1);

                //database.AddBulkParticles(predictor, round, spatialInterpolation, worker);
                database.AddBulkParticles(predictor, kernelSize, kernelSize, kernelSize, round, time);
                database.ExecuteGetPosition(tableName, EndTime,
                    spatialInterpolation, temporalInterpolation, points, predictor, false, dt1);
            }

            database.Close();


            //Point3[] positions = new Point3[ParticleNumber];

            //for (int i = 0; i < ParticleNumber; i++)
            //{
            //    positions[i] = new Point3(result[i].x, result[i].y, result[i].z);
            //}

            log.UpdateLogRecord(rowid, database.Bitfield);

            return points;
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"GetBoxFilter of the specified field; uses workload density to decide how to evaluate.")]
        public Vector3[] GetBoxFilter(string authToken, string dataset, string field, float time,
            float filterwidth, Point3[] points)
        {
            int num_virtual_servers = 4;
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            if (dataset_id == 6) //Channel hack--fix this later
            { 
                throw new Exception(String.Format("GetBoxFilter is not available for the channel flow datasets!"));
            }
            
                
            
            DataInfo.verifyTimeInRange(dataset_id, time);
            double dx = (2.0 * Math.PI) / (double)database.GridResolutionX;
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

            database.Initialize(dataset_id, num_virtual_servers);
            //database.selectServers(dataset_id, num_virtual_servers);

            Vector3[] result = new Vector3[points.Length];
            object rowid = null;

            string tableName = DataInfo.getTableName(dataset, field);
                        
            int worker = (int)Worker.Workers.GetMHDBoxFilter;

            //database.AddBulkParticles(points, filter_width, round);
            worker = database.AddBulkParticlesFiltering(points, int_filterwidth, round, worker);

            rowid = log.CreateLog(auth.Id, dataset, worker, 0, 0,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);
            if (tableName == "0")
            {
                throw new Exception(String.Format("Test.  for dataset {0} with id {1} and table {2} and field {3}", dataset, dataset_id, tableName, field));
            }
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
            float filterwidth, Point3[] points)
        {
            return GetBoxFilterSGSsymtensor(authToken, dataset, field, time, filterwidth, points);
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true, MessageName = "GetBoxFilterSGSsymtensor",
            Description = @"Retrieve the SGS symmetric tensor for a single vector field. Also, used
                            when two identical fields are specified (e.g. uu or bb).")]
        public SGSTensor[] GetBoxFilterSGSsymtensor(string authToken, string dataset, string field, float time,
            float filterwidth, Point3[] points)
        {
            SGSTensor[] result = new SGSTensor[points.Length];
            int worker = (int)Worker.Workers.GetMHDBoxFilterSGS; ;
            string tableName1;
            string tableName2;
            object rowid;

            InitializeSGSMethod(authToken, time, points, field, ref worker, ref dataset, ref filterwidth, out tableName1, out tableName2, out rowid);

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
            float filterwidth, Point3[] points)
        {
            VelocityGradient[] result = new VelocityGradient[points.Length];
            int worker = (int)Worker.Workers.GetMHDBoxFilterSGS; ;
            string tableName1;
            string tableName2;
            object rowid;

            InitializeSGSMethod(authToken, time, points, field, ref worker, ref dataset, ref filterwidth, out tableName1, out tableName2, out rowid);

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
            float filterwidth, Point3[] points)
        {
            Vector3[] result = new Vector3[points.Length];
            int worker = (int)Worker.Workers.GetMHDBoxFilterSGS; ;
            string tableName1;
            string tableName2;
            object rowid;

            InitializeSGSMethod(authToken, time, points, field, ref worker, ref dataset, ref filterwidth, out tableName1, out tableName2, out rowid);

            if (DataInfo.getNumberComponents(tableName1) == DataInfo.getNumberComponents(tableName2))
            {
                throw new Exception("The GetBoxFilterSGSvector method should be called with a vector and a scalar (e.g. \"up\" or \"bp\")");
            }
            else
            {
                // switch the table names if the first one is for a scalar field
                if (DataInfo.getNumberComponents(tableName1) == 1)
                {
                    string temp = tableName1;
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
            float filterwidth, Point3[] points)
        {
            float[] result = new float[points.Length];
            int worker = (int)Worker.Workers.GetMHDBoxFilterSGS; ;
            string tableName1;
            string tableName2;
            object rowid;

            InitializeSGSMethod(authToken, time, points, field, ref worker, ref dataset, ref filterwidth, out tableName1, out tableName2, out rowid);

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
            ref string dataset, ref float filterwidth, out string tableName1, out string tableName2,
            out object rowid)
        {
            int num_virtual_servers = 4;
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            if (dataset_id == 6)
            {
                throw new Exception(String.Format("Box filter methods are not available for the channel flow datasets!"));
            }
            DataInfo.verifyTimeInRange(dataset_id, time);

            double dx = (2.0 * Math.PI) / (double)database.GridResolutionX;
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

            database.Initialize(dataset_id, num_virtual_servers);
            
            worker = database.AddBulkParticlesFiltering(points, int_filterwidth, round, worker);

            rowid = log.CreateLog(auth.Id, dataset, worker, 0, 0,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            // The user can specify either 2 fields (e.g. "uu" or "ub")
            // or a single field (e.g. "velocity", "magnetic", "potential").
            // We determine the appropriate table name for each of these cases below.
            if (field.Length == 2)
            {
                tableName1 = DataInfo.getTableName(dataset, field.Substring(0, 1));
                tableName2 = DataInfo.getTableName(dataset, field.Substring(1, 1));
            }
            else
            {
                tableName1 = DataInfo.getTableName(dataset, field);
                tableName2 = tableName1;
            }
        }

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"GetBoxFilter of the specified field; uses workload density to decide how to evaluate.")]
        public VelocityGradient[] GetBoxFilterGradient(string authToken, string dataset, string field, float time,
            float filterwidth, float spacing, Point3[] points)
        {
            int num_virtual_servers = 4;
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            if (dataset_id == 6)
            {
                throw new Exception(String.Format("GetBoxFilter is not available for the channel flow datasets!"));
            }
            DataInfo.verifyTimeInRange(dataset_id, time);
            //database.Initialize(dataset_id);
            double dx = (2.0 * Math.PI) / (double)database.GridResolutionX;
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

            database.Initialize(dataset_id, num_virtual_servers);
            //database.selectServers(dataset_id, num_virtual_servers);

            VelocityGradient[] result = new VelocityGradient[points.Length];
            object rowid = null;

            string tableName = DataInfo.getTableName(dataset, field);

            int worker = (int)Worker.Workers.GetMHDBoxFilterGradient;

            rowid = log.CreateLog(auth.Id, dataset, worker, 0, 0,
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            // TODO: Summed Volumes technique is not yet implemented for the computation of the filtered gradient
            //worker = database.AddBulkParticlesFiltering(points, filter_width, round, worker);
            database.AddBulkParticlesFiltering(points, kernelSize, round, worker);

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

        [WebMethod(CacheDuration = 0, BufferResponse = true,
            Description = @"GetThreshold of the specified field.")]
        public ThresholdInfo[] GetThreshold(string authToken, string dataset, string field, float time, float threshold,
            TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, Xwidth * Ywidth * Zwidth);
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 4;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            object rowid = null;
            
            string tableName = DataInfo.getTableName(dataset, field);

            int worker;
            /*
            switch (dataset_id)
            {
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.mhd1024:
                case DataInfo.DataSets.mixing:
                    if (field.Contains("vorticity"))
                    {
                        worker = (int)Worker.Workers.GetCurlThreshold;
                    }
                    else if (field.Contains("q")){
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
            */

            if (dataset_id == 6)
            {
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
            }
            else
            {
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
            }
            rowid = log.CreateLog(auth.Id, dataset, worker,
                (int)spatialInterpolation,
                (int)TurbulenceOptions.TemporalInterpolation.None,
                Xwidth * Ywidth * Zwidth, time, null, null);
            log.UpdateRecordCount(auth.Id, Xwidth * Ywidth * Zwidth);

            List<ThresholdInfo> points_above_threshold = new List<ThresholdInfo>();
            database.ExecuteGetThreshold(dataset_id, tableName, worker, time, spatialInterpolation, threshold,
                X, Y, Z, Xwidth, Ywidth, Zwidth, points_above_threshold);

            log.UpdateLogRecord(rowid, database.Bitfield);

            points_above_threshold.Sort((t1, t2) => -1 * t1.value.CompareTo(t2.value));

            return points_above_threshold.ToArray();
        }


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
            Point3[] points)
        {
            AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, points.Length);
            VelocityGradient[] result = new VelocityGradient[points.Length];
            dataset = DataInfo.findDataSet(dataset);
            //DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);
            int dataset_id = DataInfo.findDataSetInt(dataset);
            int num_virtual_servers = 1;
            database.Initialize(dataset_id, num_virtual_servers);
            DataInfo.verifyTimeInRange(dataset_id, time);
            object rowid = null;

            string tableName = DataInfo.getTableName(dataset, field);
            int worker = (int)Worker.Workers.GetLaplacianOfGradient;
            /*
            switch (dataset_id)
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
                       points.Length, time, null, null);
                    log.UpdateRecordCount(auth.Id, points.Length);

                    //database.AddBulkParticles(points, round, spatialInterpolation, worker);
                    database.AddBulkParticles(points, kernelSize, kernelSize, kernelSize, round, time);

                    database.ExecuteGetMHDGradient(tableName, worker, time,
                        spatialInterpolation, temporalInterpolation, result);
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
            */
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
               points.Length, time, null, null);
            log.UpdateRecordCount(auth.Id, points.Length);

            //database.AddBulkParticles(points, round, spatialInterpolation, worker);
            database.AddBulkParticles(points, kernelSize, kernelSize, kernelSize, round, time);

            database.ExecuteGetMHDGradient(tableName, worker, time,
                spatialInterpolation, temporalInterpolation, result);
            
            log.UpdateLogRecord(rowid, database.Bitfield);
            return result;
        }




    }
}

