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
    public class SampleWorker : Worker
    {
        int count;

        public SampleWorker(TurbDataTable setInfo)
        {
            this.kernelSize = 0;
            this.setInfo = setInfo;
            count = 0;
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("Value", SqlDbType.Real) };
        }

        public override void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points)
        {
            throw new NotImplementedException();
        }

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            count++;
            return new double[] { (double)count };
        }

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            count++;
            return new double[] { (double)count };
        }

        public override int GetResultSize()
        {
            return count;
        }
    }
}
