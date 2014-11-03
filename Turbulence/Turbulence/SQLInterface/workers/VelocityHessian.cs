using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using Turbulence.SQLInterface;
using System.Collections.Generic;
namespace Turbulence.SQLInterface.workers
{
    class VelocityHessian : Worker
    {
        private Computations compute;

        public VelocityHessian (TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            this.compute = new Computations(setInfo);

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd4)
            { this.kernelSize = 4; }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd6)
            { this.kernelSize = 6; }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd8)
            { this.kernelSize = 8; }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            { this.kernelSize = 8; }
            else
            {
                throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
        }

        public override SqlMetaData[]  GetRecordMetaData()
        {
           return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("d2uxdxdx", SqlDbType.Real),
                new SqlMetaData("d2uxdxdy", SqlDbType.Real),
                new SqlMetaData("d2uxdxdz", SqlDbType.Real),
                new SqlMetaData("d2uxdydy", SqlDbType.Real),
                new SqlMetaData("d2uxdydz", SqlDbType.Real),
                new SqlMetaData("d2uxdzdz", SqlDbType.Real),
                new SqlMetaData("d2uydxdx", SqlDbType.Real),
                new SqlMetaData("d2uydxdy", SqlDbType.Real),
                new SqlMetaData("d2uydxdz", SqlDbType.Real),
                new SqlMetaData("d2uydydy", SqlDbType.Real),
                new SqlMetaData("d2uydydz", SqlDbType.Real),
                new SqlMetaData("d2uydzdz", SqlDbType.Real),
                new SqlMetaData("d2uzdxdx", SqlDbType.Real),
                new SqlMetaData("d2uzdxdy", SqlDbType.Real),
                new SqlMetaData("d2uzdxdz", SqlDbType.Real),
                new SqlMetaData("d2uzdydy", SqlDbType.Real),
                new SqlMetaData("d2uzdydz", SqlDbType.Real),
                new SqlMetaData("d2uzdzdz", SqlDbType.Real)};
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            //return new float[] { 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f };
            return (float[])compute.CalcVelocityHessian(blob,
                new float[] { (float)input.x, (float)input.y, (float)input.z }, spatialInterp).Clone();
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            throw new NotImplementedException();
        }

        public override int GetResultSize()
        {
            return 18;
        }
    }
}
