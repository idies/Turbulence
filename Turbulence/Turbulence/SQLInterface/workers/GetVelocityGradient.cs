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

    /// <summary>
    /// Basic example of a computational worker for processing data.
    /// </summary>
    public class GetVelocityGradient : Worker
    {
        Computations compute;

        public GetVelocityGradient(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            compute = new Computations(setInfo);

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

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("duxdx", SqlDbType.Real),
                new SqlMetaData("duydx", SqlDbType.Real),
                new SqlMetaData("duzdx", SqlDbType.Real),
                new SqlMetaData("duxdy", SqlDbType.Real),
                new SqlMetaData("duydy", SqlDbType.Real),
                new SqlMetaData("duzdy", SqlDbType.Real),
                new SqlMetaData("duxdz", SqlDbType.Real),
                new SqlMetaData("duydz", SqlDbType.Real),
                new SqlMetaData("duzdz", SqlDbType.Real) };
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            return (float[])compute.CalcVelocityGradient(blob,
                    new float[] { (float)input.x, (float)input.y, (float)input.z }, spatialInterp).Clone();
            
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            throw new NotImplementedException();
        }

        public override int GetResultSize()
        {
            return 9;
        }
    }
}
