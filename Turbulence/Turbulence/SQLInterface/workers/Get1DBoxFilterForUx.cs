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
    public class Get1DBoxFilterForUx : Worker
    {

        TurbulenceOptions.SpatialInterpolation spatialInterp;
        Computations compute;

        public Get1DBoxFilterForUx(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            compute = new Computations(setInfo);
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("x", SqlDbType.Real),
                new SqlMetaData("y", SqlDbType.Real),
                new SqlMetaData("z", SqlDbType.Real),
                new SqlMetaData("filterlength", SqlDbType.Real),
                new SqlMetaData("filteredvalue", SqlDbType.Real)};
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            //return new float[] { 5.0432f};
            
            return (float[]) compute.Calc1DBoxFilterForUx(blob,
                new float[] { input.x, input.y, input.z }, spatialInterp).Clone();
        }
    }
}
