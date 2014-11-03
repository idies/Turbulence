using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using Turbulence.SQLInterface;
using System.Collections;
namespace Turbulence.SQLInterface.workers
{
    class LaplacianVelocityWorker : Worker
    {
        private TurbulenceOptions.SpatialInterpolation spatialInterp;
        private Computations compute;

        public LaplacianVelocityWorker(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            this.compute = new Computations(setInfo);
        }

        public override SqlMetaData[]  GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("grad2ux", SqlDbType.Real),
                new SqlMetaData("grad2uy", SqlDbType.Real),
                new SqlMetaData("grad2uz", SqlDbType.Real)};
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            return (float[])compute.CalcLaplacianOfVelocity(blob,
                new float[] { input.x, input.y, input.z }, spatialInterp).Clone();
        }

    }
}
