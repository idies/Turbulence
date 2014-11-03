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
    /// This class will retrieve the velocity field.
    /// </summary>
    public class GetVelocityWorkerOld : Worker
    {
        Computations compute;
        bool usePressure;

        public GetVelocityWorkerOld(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            bool pressure)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            this.usePressure = pressure;
            compute = new Computations(setInfo);

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag4)
            { this.kernelSize = 4; }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag6)
            { this.kernelSize = 6; }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag8)
            { this.kernelSize = 8; }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
            { this.kernelSize = 0; }
            else
            {
                throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            if (usePressure)
            {
                return new SqlMetaData[] {
                    new SqlMetaData("Req", SqlDbType.Int),
                    new SqlMetaData("X", SqlDbType.Real),
                    new SqlMetaData("Y", SqlDbType.Real),
                    new SqlMetaData("Z", SqlDbType.Real),
                    new SqlMetaData("P", SqlDbType.Real) };
            }
            else
            {
                return new SqlMetaData[] {
                    new SqlMetaData("Req", SqlDbType.Int),
                    new SqlMetaData("X", SqlDbType.Real),
                    new SqlMetaData("Y", SqlDbType.Real),
                    new SqlMetaData("Z", SqlDbType.Real) };
            }
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            if (usePressure)
            {
                return (float[])compute.CalcVelocityWithPressureOld(blob,
                    new float[] { (float)input.x, (float)input.y, (float)input.z }, spatialInterp).Clone();
            }
            else
            {
                return (float[])compute.CalcVelocityOld(blob,
                    new float[] { (float)input.x, (float)input.y, (float)input.z }, spatialInterp).Clone();
            }
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            throw new NotImplementedException();
        }

        public override int GetResultSize()
        {
            return 3;
        }

    }

}
