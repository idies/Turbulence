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
    class VelocityLaplacian : Worker
    {
        private Computations compute;

        public VelocityLaplacian(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            this.setInfo= setInfo;
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
                new SqlMetaData("grad2ux", SqlDbType.Real),
                new SqlMetaData("grad2uy", SqlDbType.Real),
                new SqlMetaData("grad2uz", SqlDbType.Real)};
        }

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            return (double[])compute.CalcVelocityLaplacian(blob,
                new float[] { (float)input.x, (float)input.y, (float)input.z }, spatialInterp).Clone();
        }

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            throw new NotImplementedException();
        }

        public override int GetResultSize()
        {
            return 3;
        }

    }
}
